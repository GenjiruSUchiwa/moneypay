import DesignSystem
import Foundation
import SwiftUI
import WalletStore

public struct SendMoneyView: View {
    public init() {
    }

    @Environment(Store.self) private var store
    @Environment(\.dismiss) private var dismiss

    @State private var recipient: Contact?
    @State private var digits = ""
    @State private var note = ""
    @State private var sent = false
    @State private var query = ""

    struct Contact: Identifiable, Hashable {
        let id = UUID()
        var name: String, phone: String
        var initials: String {
            name.split(separator: " ").prefix(2).map { String($0.prefix(1)) }.joined()
        }
    }

    private let contacts: [Contact] = [
        .init(name: "Nadège Ateba", phone: "+237 6 77 21 09 44"),
        .init(name: "Serge Kamdem", phone: "+237 6 91 55 30 12"),
        .init(name: "Laure Ngo Bell", phone: "+237 6 55 88 74 21"),
        .init(name: "Boutique Akwa", phone: "+237 6 70 12 45 63"),
        .init(name: "Yannick Fotso", phone: "+237 6 96 33 18 05")
    ]

    private var filtered: [Contact] {
        query.isEmpty ? contacts : contacts.filter { $0.name.localizedCaseInsensitiveContains(query) }
    }
    private var amount: Int { Int(digits) ?? 0 }

    public var body: some View {
        NavigationStack {
            Group {
                if sent { success }
                else if let recipient { amountView(recipient) }
                else { picker }
            }
            .page()
        }
    }

    private var picker: some View {
        VStack(spacing: 0) {
            NavBar(title: "Envoyer", onClose: { dismiss() })
            Field(placeholder: "Nom ou numéro MoneyPay", text: $query, icon: "magnifyingglass").gutter()

            ScrollView {
                VStack(alignment: .leading, spacing: 0) {
                    Eyebrow(text: "Récents").gutter().padding(.top, 22).padding(.bottom, 4)
                    VStack(spacing: 0) {
                        ForEach(Array(filtered.enumerated()), id: \.element.id) { i, c in
                            Button {
                                Haptic.tap()
                                withAnimation(.easeOut(duration: 0.22)) { recipient = c }
                            } label: {
                                HStack(spacing: 12) {
                                    Text(c.initials)
                                        .font(.system(size: 13, weight: .medium))
                                        .foregroundStyle(Brand.ink)
                                        .frame(width: 38, height: 38)
                                        .background(Brand.well, in: .circle)
                                    VStack(alignment: .leading, spacing: 2) {
                                        Text(c.name).font(.bodyReg).foregroundStyle(Brand.ink)
                                        Text(c.phone).font(.sub).foregroundStyle(Brand.inkMuted)
                                            .monospacedDigit()
                                    }
                                    Spacer()
                                    Image(systemName: "chevron.right")
                                        .font(.system(size: 13, weight: .semibold))
                                        .foregroundStyle(Brand.inkFaint)
                                }
                                .padding(.vertical, Metric.rowVertical)
                                .contentShape(.rect)
                            }
                            .buttonStyle(.plain)
                            if i < filtered.count - 1 { Rule(inset: 50) }
                        }
                    }
                    .gutter()
                }
            }
            .scrollIndicators(.hidden)
        }
    }

    private func amountView(_ c: Contact) -> some View {
        VStack(spacing: 0) {
            NavBar(onBack: { withAnimation(.easeOut(duration: 0.22)) { recipient = nil } })

            VStack(spacing: 8) {
                Text(c.initials)
                    .font(.system(size: 18, weight: .medium)).foregroundStyle(Brand.ink)
                    .frame(width: 52, height: 52)
                    .background(Brand.well, in: .circle)
                Text(c.name).font(.bodyMed).foregroundStyle(Brand.ink)
                Text(c.phone).font(.sub).foregroundStyle(Brand.inkMuted).monospacedDigit()
            }

            Spacer()
            AmountEntry(digits: digits.isEmpty ? "" : Fmt.group(amount), currency: "FCFA")
            Text("Transfert MoneyPay instantané, sans frais")
                .font(.sub).foregroundStyle(Brand.inkMuted).padding(.top, 10)
            Spacer()

            Field(placeholder: "Ajouter une note", text: $note, icon: "text.bubble").gutter()

            Keypad(onDigit: { d in if digits.count < 8 { digits.append("\(d)") } },
                   onDelete: { if !digits.isEmpty { digits.removeLast() } })
                .gutter().padding(.top, 6)

            MPButton(title: amount > 0 ? "Envoyer \(Fmt.xaf(amount))" : "Envoyer",
                     enabled: amount >= 500 && amount <= store.balanceXAF) {
                Haptic.success()
                withAnimation(.easeOut(duration: 0.25)) { sent = true }
            }
            .gutter().padding(.bottom, 10)
        }
    }

    private var success: some View {
        VStack(alignment: .leading, spacing: 0) {
            Spacer()
            SuccessMark()
            Text("Argent envoyé")
                .font(.system(size: 26, weight: .semibold)).tight(-0.6)
                .foregroundStyle(Brand.ink).padding(.top, 24)
            Text("\(Fmt.xaf(amount)) envoyés à \(recipient?.name ?? "").")
                .font(.bodyReg).foregroundStyle(Brand.inkMuted).padding(.top, 8)
            Spacer()
            MPButton(title: "Terminé") { dismiss() }.padding(.bottom, 14)
        }
        .gutter()
    }
}
