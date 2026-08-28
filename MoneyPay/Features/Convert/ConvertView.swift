import SwiftUI

struct ConvertView: View {
    @Environment(Store.self) private var store
    @Environment(\.dismiss) private var dismiss

    @State private var digits = ""
    @State private var done = false

    private var xaf: Int { Int(digits) ?? 0 }
    private var usdCents: Int {
        Int(Double(xaf) / (store.fx.usdToXAF * (1 + store.fx.marginPct)) * 100)
    }
    private var marketCents: Int { Int(Double(xaf) / store.fx.usdToXAF * 100) }
    private var marginXAF: Int { Int(Double(xaf) * store.fx.marginPct / (1 + store.fx.marginPct)) }
    private var valid: Bool { xaf >= 1_000 && xaf <= store.balanceXAF }

    var body: some View {
        NavigationStack { if done { success } else { form } }
    }

    private var form: some View {
        VStack(spacing: 0) {
            NavBar(title: "Convertir", onClose: { dismiss() })

            VStack(spacing: 0) {
                leg(flag: "🇨🇲", code: "FCFA", note: "Disponible : \(Fmt.xaf(store.balanceXAF))",
                    value: digits.isEmpty ? "0" : Fmt.group(xaf), active: true)
                ZStack {
                    Rule()
                    Image(systemName: "arrow.down")
                        .font(.system(size: 11, weight: .bold))
                        .foregroundStyle(Brand.onInk)
                        .frame(width: 26, height: 26)
                        .background(Brand.inkFill, in: .circle)
                }
                leg(flag: "🇺🇸", code: "USD", note: "Utilisable sur toutes vos cartes",
                    value: Fmt.usd(usdCents, symbol: false), active: false)
            }
            .gutter()

            Rule()

            // La marge est affichée, pas noyée dans le taux : c'est l'argument
            // face aux applis qui la cachent.
            VStack(spacing: 0) {
                kv("Taux interbancaire", "1 USD = \(Int(store.fx.usdToXAF)) FCFA")
                Rule()
                kv("Marge MoneyPay · \(Int(store.fx.marginPct * 100)) %",
                   marginXAF > 0 ? "− \(Fmt.xaf(marginXAF))" : "—", tint: Brand.pending)
                Rule()
                kv("Vous recevez", Fmt.usd(usdCents), strong: true)
            }
            .gutter()
            Rule()

            if marketCents > 0 {
                Text("Au taux interbancaire pur, vous auriez \(Fmt.usd(marketCents)).")
                    .font(.micro).foregroundStyle(Brand.inkFaint)
                    .gutter().padding(.top, 12)
            }

            Spacer()

            Keypad(onDigit: { d in if digits.count < 8 { digits.append("\(d)") } },
                   onDelete: { if !digits.isEmpty { digits.removeLast() } })
                .gutter()

            MPButton(title: xaf > store.balanceXAF ? "Solde insuffisant" : "Convertir", enabled: valid) {
                Haptic.success()
                withAnimation(.easeOut(duration: 0.25)) { done = true }
            }
            .gutter().padding(.bottom, 10)
        }
        .page()
    }

    private func leg(flag: String, code: String, note: String, value: String, active: Bool) -> some View {
        HStack(spacing: 12) {
            Text(flag).font(.system(size: 26))
            VStack(alignment: .leading, spacing: 2) {
                Text(code).font(.bodyMed).foregroundStyle(Brand.ink)
                Text(note).font(.micro).foregroundStyle(Brand.inkMuted).lineLimit(1)
            }
            Spacer(minLength: 8)
            Text(value)
                .font(.system(size: 26, weight: .semibold))
                .monospacedDigit()
                .foregroundStyle(active ? Brand.ink : Brand.inkMuted)
                .contentTransition(.numericText())
        }
        .padding(.vertical, 18)
    }

    private func kv(_ l: String, _ v: String, tint: Color = Brand.ink, strong: Bool = false) -> some View {
        HStack {
            Text(l).font(.body).foregroundStyle(Brand.inkMuted)
            Spacer()
            Text(v).font(strong ? .bodyMed : .body).foregroundStyle(tint).monospacedDigit()
        }
        .padding(.vertical, 13)
    }

    private var success: some View {
        VStack(alignment: .leading, spacing: 0) {
            Spacer()
            SuccessMark()
            Text("Conversion effectuée")
                .font(.system(size: 26, weight: .semibold)).tight(-0.6)
                .foregroundStyle(Brand.ink).padding(.top, 24)
            Text("\(Fmt.xaf(xaf)) convertis en \(Fmt.usd(usdCents)).")
                .font(.body).foregroundStyle(Brand.inkMuted).padding(.top, 8)
            Spacer()
            MPButton(title: "Terminé") { dismiss() }.padding(.bottom, 14)
        }
        .gutter()
        .page()
    }
}

// MARK: - Envoi

struct SendMoneyView: View {
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

    var body: some View {
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
                                        Text(c.name).font(.body).foregroundStyle(Brand.ink)
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
                .font(.body).foregroundStyle(Brand.inkMuted).padding(.top, 8)
            Spacer()
            MPButton(title: "Terminé") { dismiss() }.padding(.bottom, 14)
        }
        .gutter()
    }
}
