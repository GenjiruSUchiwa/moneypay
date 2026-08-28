---
title: SwiftData for Cache, Keychain for Secrets, UserDefaults for Preferences
impact: HIGH
impactDescription: Keeps tokens and card data out of plaintext storage and off disk entirely
tags: data, persistence, swiftdata, keychain, userdefaults, security, pci
---

## SwiftData for Cache, Keychain for Secrets, UserDefaults for Preferences

**Impact: HIGH**

Three stores, three jobs, and each has one owning package. Putting a value in the wrong one is either a bug
or a breach.

| Store | Holds | Never holds |
|---|---|---|
| **Keychain** | Access/refresh tokens, device secret, biometric-gated flags | Anything large or non-secret |
| **SwiftData** | Offline cache: transactions, card metadata, merchants | Tokens, PAN, CVV, the authoritative balance |
| **UserDefaults** | `hiddenBalance`, preferred top-up method, last selected tab, onboarding seen | Any secret, any money |

| Store | Owning package |
|---|---|
| Keychain (`TokenStore`) and `KeyValueStoring` | `Platform` |
| SwiftData `@Model` cache and its container | `WalletStore` |
| `@AppStorage` preference keys | the feature package that owns the screen |

No feature package touches `Security` or `SwiftData` directly: it depends on `Platform`/`WalletStore` and gets
an injected `any KeyValueStoring` or an injected store (`patterns-dependency-injection.md`).

**The balance is never authoritative on device.** `Wallet` on the server is the source of truth; a cached
balance is a display hint that must be refreshed before any authorization decision.

**Never persisted, anywhere, under any condition: full PAN, CVV, or the card expiry paired with the PAN.**
Reveal is a one-shot network call; the values live in a `@State` for the lifetime of the sheet and are gone
when it dismisses. No `@Model`, no Keychain item, no log line, no analytics event, no screenshot-able cache.

**Incorrect:**

```swift
UserDefaults.standard.set(accessToken, forKey: "token")        // ❌ plaintext secret, backed up to iCloud
UserDefaults.standard.set(balanceXAF, forKey: "balance")       // ❌ money in prefs, trivially tampered

@Model final class CachedCard {
    var pan: String                                            // ❌ PCI violation, unencrypted SQLite
    var cvv: String                                            // ❌
}
```

**Correct — Keychain for tokens:**

```swift
// ios/Packages/Platform/Sources/Platform/TokenStore.swift
actor TokenStore {
    private let service = "cm.moneypay.auth"

    func save(_ token: String, account: String) throws {
        let data = Data(token.utf8)
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
            // Device-only, unlocked-only: never restored onto another device by a backup.
            kSecAttrAccessible as String: kSecAttrAccessibleWhenUnlockedThisDeviceOnly,
            kSecValueData as String: data
        ]
        SecItemDelete(query as CFDictionary)
        let status = SecItemAdd(query as CFDictionary, nil)
        guard status == errSecSuccess else { throw KeychainError.status(status) }
    }

    func token(account: String) throws -> String? {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
            kSecReturnData as String: true,
            kSecMatchLimit as String: kSecMatchLimitOne
        ]
        var item: CFTypeRef?
        let status = SecItemCopyMatching(query as CFDictionary, &item)
        if status == errSecItemNotFound { return nil }
        guard status == errSecSuccess, let data = item as? Data else { throw KeychainError.status(status) }
        return String(decoding: data, as: UTF8.self)
    }
}
```

**Correct — SwiftData for the offline cache only:**

```swift
// ios/Packages/WalletStore/Sources/WalletStore/Persistence/CachedTransaction.swift
import SwiftData

@Model
final class CachedTransaction {
    #Index<CachedTransaction>([\.date], [\.cardID])
    @Attribute(.unique) var id: UUID
    var merchant: String
    var statusRaw: String
    var date: Date
    /// Minor units + currency code. Never a Double, never a `Money` blob.
    var presentedMinorUnits: Int
    var presentedCurrency: String
    var settledMinorUnits: Int
    var cardID: UUID?

    init(_ tx: Transaction) {
        id = tx.id
        merchant = tx.merchant
        statusRaw = tx.status.rawValue
        date = tx.date
        presentedMinorUnits = tx.presented.minorUnits
        presentedCurrency = tx.presented.currency.rawValue
        settledMinorUnits = tx.settled.minorUnits
        cardID = tx.cardID
    }
}

// ios/App/MoniPayApp.swift
.modelContainer(for: CachedTransaction.self, isAutosaveEnabled: true)
```

The cache is disposable: if a migration is hard, delete the store and refetch. Never write a migration that
risks corrupting money data — the server holds the truth.

**Correct — `@AppStorage` for preferences, and nothing else:**

```swift
struct SettingsView: View {
    @AppStorage("hiddenBalance") private var hiddenBalance = false
    @AppStorage("preferredTopUpMethod") private var preferredMethod = TopUpMethod.mtnMoMo.rawValue

    var body: some View {
        Toggle("Hide balance", isOn: $hiddenBalance)
    }
}
```

**Checklist**
- `Data Protection` entitlement declared in `ios/Config/*.entitlements`; Keychain items use
  `…ThisDeviceOnly` for tokens.
- Sign-out deletes every Keychain item *and* wipes the SwiftData container.
- No secret in `UserDefaults`, `@AppStorage`, plists, environment variables shipped in the bundle, or logs.
- No PAN/CVV in any `@Model`, any cache, any crash report, any `print`.
- Cached money keeps its currency code alongside its minor units.

Reference: [Keychain item accessibility](https://developer.apple.com/documentation/security/ksecattraccessible) ·
[SwiftData](https://developer.apple.com/documentation/swiftdata)
