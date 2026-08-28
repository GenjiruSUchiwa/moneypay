import SwiftUI

/// Index de toutes les vues de la maquette : parcourir chaque écran sans
/// rejouer les parcours.
struct GalleryView: View {
    @Environment(Store.self) private var store

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                ForEach(ScreenCatalog.sections, id: \.self) { section in
                    let entries = ScreenCatalog.all.filter { $0.section == section }
                    Eyebrow(text: section).gutter().padding(.top, 24).padding(.bottom, 2)
                    VStack(spacing: 0) {
                        ForEach(Array(entries.enumerated()), id: \.element.id) { i, entry in
                            NavigationLink { entry.make(store) } label: {
                                Row(icon: entry.icon, iconTint: entry.tint,
                                    title: entry.title, chevron: true)
                            }
                            .buttonStyle(.plain)
                            if i < entries.count - 1 { Rule(inset: 51) }
                        }
                    }
                    .gutter()
                    Rule().padding(.top, 4)
                }
                Text("\(ScreenCatalog.all.count) écrans")
                    .font(.micro).foregroundStyle(Brand.inkFaint)
                    .gutter().padding(.top, 18)
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle("Galerie")
        .navigationBarTitleDisplayMode(.inline)
    }
}

// MARK: - Vitrines

struct EmptyStatesShowcase: View {
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                Rule()
                EmptyNote(title: "Aucune carte",
                          message: "Créez une carte dédiée à chaque usage : abonnements, achats, publicité.",
                          actionTitle: "Créer une carte")
                Rule()
                EmptyNote(title: "Aucune transaction",
                          message: "Les paiements effectués avec cette carte apparaîtront ici.")
                Rule()
                EmptyNote(title: "Aucun résultat",
                          message: "Essayez un autre filtre ou un autre nom de marchand.")
                Rule()
                EmptyNote(title: "Pas de connexion",
                          message: "Vérifiez votre réseau. Vos données seront synchronisées au retour.",
                          actionTitle: "Réessayer")
                Rule()
            }
            .gutter()
        }
        .page()
        .navigationTitle("États vides")
        .navigationBarTitleDisplayMode(.inline)
    }
}

struct CardThemeShowcase: View {
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                ForEach(Array(CardTheme.allCases.enumerated()), id: \.element.id) { i, t in
                    VStack(alignment: .leading, spacing: 10) {
                        Eyebrow(text: t.label)
                        VirtualCardView(card: VirtualCard(
                            id: UUID(), label: "Abonnements", theme: t,
                            network: i % 2 == 0 ? .mastercard : .visa,
                            pan: "5399471028834412", cvv: "417", expiry: "09/29",
                            createdAt: .now, monthlyLimitUSDCents: 15_000, spentUSDCents: 4_780
                        ), revealed: true)
                        .frame(maxWidth: 300)
                    }
                    .padding(.vertical, 20)
                    if i < CardTheme.allCases.count - 1 { Rule() }
                }
            }
            .gutter()
        }
        .page()
        .navigationTitle("Habillages")
        .navigationBarTitleDisplayMode(.inline)
    }
}

struct ComponentsShowcase: View {
    @State private var seg = 0
    @State private var toastMsg: Toast?
    @State private var text = ""

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                block("Boutons") {
                    MPButton(title: "Action principale") { toastMsg = Toast(text: "Touché") }
                    MPButton(title: "Action neutre", tone: .quiet) {}
                    MPButton(title: "Action secondaire", tone: .outline) {}
                    MPButton(title: "Action destructive", tone: .danger) {}
                    MPButton(title: "En cours", loading: true) {}
                    MPButton(title: "Désactivé", enabled: false) {}
                }
                block("Actions rapides") {
                    HStack(spacing: 4) {
                        QuickAction(icon: "plus", label: "Recharger", emphasis: true)
                        QuickAction(icon: "arrow.left.arrow.right", label: "Convertir")
                        QuickAction(icon: "snowflake", label: "Geler")
                        QuickAction(icon: "trash", label: "Supprimer")
                    }
                }
                block("Segments et pastilles") {
                    Segments(items: ["Tout", "Cartes", "Recharges"], selection: $seg)
                    HStack(spacing: 7) {
                        Chip(text: "Sélectionné", selected: true)
                        Chip(text: "Normal")
                        Chip(text: "Illimité")
                    }
                    HStack(spacing: 7) {
                        StatusPill(text: "Réussi", symbol: "checkmark",
                                   tint: Brand.credit, soft: Brand.creditSoft)
                        StatusPill(text: "En attente", symbol: "clock",
                                   tint: Brand.pending, soft: Brand.pendingSoft)
                        StatusPill(text: "Refusé", symbol: "xmark",
                                   tint: Brand.debit, soft: Brand.debitSoft)
                    }
                }
                block("Montants") {
                    MoneyText.xaf(428_500, size: 34)
                    MoneyText.usd(109_945, size: 28)
                    HStack(spacing: 18) {
                        MoneyText.xaf(98_500, size: 17, weight: .medium,
                                      color: Brand.credit, signed: true)
                        MoneyText.xaf(-6_907, size: 17, weight: .medium, signed: true)
                    }
                }
                block("Jauges") {
                    Meter(value: 0.32)
                    Meter(value: 0.88, tint: Brand.debit)
                }
                block("Saisie") {
                    OTPBoxes(code: "418")
                    PasscodeDots(filled: 2)
                    Field(placeholder: "Nom de la carte", text: $text, icon: "creditcard")
                }
                block("Lignes") {
                    VStack(spacing: 0) {
                        Row(icon: "creditcard", title: "Avec icône", subtitle: "Et un sous-titre",
                            chevron: true)
                        Rule(inset: 51)
                        Row(glyph: "🇨🇲", title: "Avec drapeau", chevron: true) {
                            RowValue(text: "+237")
                        }
                        Rule(inset: 51)
                        Row(icon: "trash", title: "Destructive", destructive: true)
                    }
                }
                block("Palette de visualisation") {
                    HStack(spacing: 5) {
                        ForEach(Viz.categorical.indices, id: \.self) { i in
                            VStack(spacing: 5) {
                                RoundedRectangle(cornerRadius: 6, style: .continuous)
                                    .fill(Viz.categorical[i]).frame(height: 38)
                                Text("\(i + 1)").font(.eyebrow).foregroundStyle(Brand.inkFaint)
                            }
                        }
                    }
                    Text("Ordre fixe, jamais recyclé. Validé au script : ΔE daltonisme 8,4 · vision normale 19,3 · contraste ≥ 3:1.")
                        .font(.micro).foregroundStyle(Brand.inkFaint)
                        .fixedSize(horizontal: false, vertical: true)
                }
            }
            .gutter()
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle("Composants")
        .navigationBarTitleDisplayMode(.inline)
        .toast($toastMsg)
    }

    @ViewBuilder
    private func block<C: View>(_ title: String, @ViewBuilder content: () -> C) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            Eyebrow(text: title)
            content()
        }
        .padding(.vertical, 22)
        Rule()
    }
}
