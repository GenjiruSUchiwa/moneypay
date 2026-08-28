import SwiftUI

// MARK: - Informations personnelles

struct ProfileView: View {
    @Environment(Store.self) private var store

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                HStack(spacing: 14) {
                    Text(store.user.initials)
                        .font(.system(size: 22, weight: .medium))
                        .foregroundStyle(Brand.onInk)
                        .frame(width: 64, height: 64)
                        .background(Brand.inkFill, in: .rect(cornerRadius: 17, style: .continuous))
                    VStack(alignment: .leading, spacing: 5) {
                        Text(store.user.fullName).font(.title3).foregroundStyle(Brand.ink)
                        StatusPill(text: "Compte vérifié · niveau 2", symbol: "checkmark",
                                   tint: Brand.credit, soft: Brand.creditSoft)
                    }
                    Spacer(minLength: 0)
                }
                .gutter().padding(.vertical, 18)

                Rule()
                Eyebrow(text: "Identité").gutter().padding(.top, 22).padding(.bottom, 2)
                VStack(spacing: 0) {
                    kv("Nom complet", store.user.fullName)
                    Rule()
                    kv("Date de naissance", "12 mars 1994")
                    Rule()
                    kv("Nationalité", "Camerounaise")
                    Rule()
                    kv("Pièce d'identité", "CNI ••• 4821")
                }
                .gutter()

                Rule()
                Eyebrow(text: "Contact").gutter().padding(.top, 22).padding(.bottom, 2)
                VStack(spacing: 0) {
                    Row(icon: "phone", title: "Téléphone", subtitle: store.user.phone, chevron: true)
                    Rule(inset: 51)
                    Row(icon: "envelope", title: "E-mail", subtitle: store.user.email, chevron: true)
                    Rule(inset: 51)
                    Row(icon: "house", title: "Adresse", subtitle: "Bonapriso, Douala", chevron: true)
                }
                .gutter()

                Rule()
                Text("Pour modifier votre nom ou votre date de naissance, une nouvelle vérification d'identité est nécessaire.")
                    .font(.micro).foregroundStyle(Brand.inkFaint)
                    .fixedSize(horizontal: false, vertical: true)
                    .gutter().padding(.top, 18)
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle("Informations")
        .navigationBarTitleDisplayMode(.inline)
    }

    private func kv(_ l: String, _ v: String) -> some View {
        HStack {
            Text(l).font(.body).foregroundStyle(Brand.inkMuted)
            Spacer()
            Text(v).font(.body).foregroundStyle(Brand.ink)
        }
        .padding(.vertical, Metric.rowVertical)
    }
}

// MARK: - Plafonds

struct LimitsView: View {
    private let limits: [(String, String, Double, String)] = [
        ("Rechargement mensuel", "1 240 000 sur 3 000 000 FCFA", 0.41, "Renouvelé le 1er septembre"),
        ("Paiements carte par mois", "486 sur 2 000 USD", 0.24, "Toutes cartes confondues"),
        ("Transfert entre membres", "150 000 sur 500 000 FCFA", 0.30, "Par période de 30 jours"),
        ("Cartes actives", "3 sur 5", 0.60, "Niveau 2 · vérifié")
    ]

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                Eyebrow(text: "Niveau 2").gutter().padding(.top, 18)
                Text("Vos plafonds actuels")
                    .font(.system(size: 24, weight: .semibold)).tight(-0.5)
                    .foregroundStyle(Brand.ink).gutter().padding(.top, 8)
                Text("Ils découlent de votre niveau de vérification et de la réglementation CEMAC.")
                    .font(.body).foregroundStyle(Brand.inkMuted)
                    .fixedSize(horizontal: false, vertical: true)
                    .gutter().padding(.top, 8)

                Rule().padding(.top, 24)

                VStack(spacing: 0) {
                    ForEach(limits.indices, id: \.self) { i in
                        VStack(alignment: .leading, spacing: 9) {
                            HStack {
                                Text(limits[i].0).font(.body).foregroundStyle(Brand.ink)
                                Spacer()
                                Text("\(Int(limits[i].2 * 100)) %")
                                    .font(.subMed).foregroundStyle(Brand.inkMuted).monospacedDigit()
                            }
                            Meter(value: limits[i].2, tint: Brand.ink, height: 4)
                            HStack {
                                Text(limits[i].1).font(.sub).foregroundStyle(Brand.inkMuted)
                                    .monospacedDigit()
                                Spacer()
                                Text(limits[i].3).font(.micro).foregroundStyle(Brand.inkFaint)
                            }
                        }
                        .padding(.vertical, 18)
                        if i < limits.count - 1 { Rule() }
                    }
                }
                .gutter()

                Rule()

                VStack(alignment: .leading, spacing: 9) {
                    Text("Passer au niveau 3").font(.bodyMed).foregroundStyle(Brand.ink)
                    Text("Ajoutez un justificatif de domicile et un justificatif de revenus pour porter votre plafond à 10 000 000 FCFA.")
                        .font(.sub).foregroundStyle(Brand.inkMuted)
                        .fixedSize(horizontal: false, vertical: true)
                    MPButton(title: "Augmenter mes plafonds", tone: .quiet) {}.padding(.top, 6)
                }
                .gutter().padding(.top, 22)
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle("Plafonds")
        .navigationBarTitleDisplayMode(.inline)
    }
}

// MARK: - Sécurité

struct SecurityView: View {
    @State private var twoFA = true
    @State private var confirmEach = false

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                VStack(spacing: 0) {
                    Row(icon: "lock.rotation", title: "Modifier le code secret", chevron: true)
                    Rule(inset: 51)
                    Row(icon: "key.horizontal", title: "Code oublié",
                        subtitle: "Réinitialiser par SMS", chevron: true)
                }
                .gutter().padding(.top, 8)

                Rule()
                Eyebrow(text: "Confirmation").gutter().padding(.top, 22).padding(.bottom, 2)
                VStack(spacing: 0) {
                    toggle("Double authentification", "shield.lefthalf.filled", $twoFA)
                    Rule(inset: 51)
                    toggle("Confirmer chaque paiement", "hand.raised", $confirmEach)
                }
                .gutter()
                Text("Avec la confirmation systématique, chaque autorisation attend votre validation dans l'application. Sans réponse, elle est refusée au bout de 20 secondes.")
                    .font(.micro).foregroundStyle(Brand.inkFaint)
                    .fixedSize(horizontal: false, vertical: true)
                    .gutter().padding(.top, 10)

                Rule().padding(.top, 20)
                Eyebrow(text: "Activité récente").gutter().padding(.top, 22).padding(.bottom, 2)
                VStack(spacing: 0) {
                    log("Connexion réussie", "iPhone 16 Pro · Douala", "Il y a 2 h", Brand.credit)
                    Rule(inset: 20)
                    log("Code secret modifié", "iPhone 16 Pro · Douala", "12 août", Brand.inkFaint)
                    Rule(inset: 20)
                    log("Tentative bloquée", "Appareil inconnu · Lagos", "3 août", Brand.debit)
                }
                .gutter()
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle("Sécurité")
        .navigationBarTitleDisplayMode(.inline)
    }

    private func toggle(_ t: String, _ i: String, _ v: Binding<Bool>) -> some View {
        HStack(spacing: 13) {
            IconTile(symbol: i)
            Text(t).font(.body).foregroundStyle(Brand.ink)
            Spacer()
            Toggle("", isOn: v).labelsHidden().tint(Brand.inkFill)
        }
        .padding(.vertical, 11)
    }

    private func log(_ t: String, _ s: String, _ d: String, _ c: Color) -> some View {
        HStack(spacing: 12) {
            Circle().fill(c).frame(width: 7, height: 7)
            VStack(alignment: .leading, spacing: 2) {
                Text(t).font(.body).foregroundStyle(Brand.ink)
                Text(s).font(.sub).foregroundStyle(Brand.inkMuted)
            }
            Spacer()
            Text(d).font(.micro).foregroundStyle(Brand.inkFaint)
        }
        .padding(.vertical, 13)
    }
}

// MARK: - Appareils

struct DevicesView: View {
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                VStack(spacing: 0) {
                    Row(icon: "iphone", title: "iPhone 16 Pro", subtitle: "Cet appareil · Douala") {
                        StatusPill(text: "Actif", symbol: "circle.fill",
                                   tint: Brand.credit, soft: Brand.creditSoft)
                    }
                    Rule(inset: 51)
                    Row(icon: "ipad", title: "iPad Air", subtitle: "Dernière activité : 18 août") {
                        Text("Révoquer").font(.subMed).foregroundStyle(Brand.debit)
                    }
                }
                .gutter().padding(.top, 8)
                Rule()
                Text("Révoquer un appareil le déconnecte immédiatement et invalide ses sessions.")
                    .font(.micro).foregroundStyle(Brand.inkFaint)
                    .fixedSize(horizontal: false, vertical: true)
                    .gutter().padding(.top, 14)
            }
        }
        .page()
        .navigationTitle("Appareils")
        .navigationBarTitleDisplayMode(.inline)
    }
}

// MARK: - Documents

struct DocumentsView: View {
    private let months = ["Août 2026", "Juillet 2026", "Juin 2026", "Mai 2026", "Avril 2026"]

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                Eyebrow(text: "Relevés mensuels").gutter().padding(.top, 18).padding(.bottom, 2)
                VStack(spacing: 0) {
                    ForEach(months.indices, id: \.self) { i in
                        Row(icon: "doc.text", title: months[i], subtitle: "PDF · relevé complet") {
                            Image(systemName: "arrow.down.circle")
                                .font(.system(size: 17)).foregroundStyle(Brand.mark)
                        }
                        if i < months.count - 1 { Rule(inset: 51) }
                    }
                }
                .gutter()

                Rule()
                Eyebrow(text: "Justificatifs").gutter().padding(.top, 22).padding(.bottom, 2)
                VStack(spacing: 0) {
                    Row(icon: "checkmark.seal", title: "Attestation de compte",
                        subtitle: "Générée à la demande", chevron: true)
                    Rule(inset: 51)
                    Row(icon: "person.text.rectangle", title: "Pièce d'identité",
                        subtitle: "CNI · vérifiée le 3 mai 2026", chevron: true)
                }
                .gutter()
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle("Documents")
        .navigationBarTitleDisplayMode(.inline)
    }
}

// MARK: - Parrainage

struct ReferralView: View {
    @State private var toastMsg: Toast?
    private let code = "ARISTIDE237"

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                Text("2 500 FCFA par ami")
                    .font(.system(size: 28, weight: .semibold)).tight(-0.7)
                    .foregroundStyle(Brand.ink).gutter().padding(.top, 20)
                Text("Votre ami reçoit lui aussi 2 500 FCFA dès la création de sa première carte virtuelle.")
                    .font(.body).foregroundStyle(Brand.inkMuted)
                    .fixedSize(horizontal: false, vertical: true)
                    .gutter().padding(.top, 8)

                Button {
                    UIPasteboard.general.string = code
                    Haptic.success(); toastMsg = Toast(text: "Code copié", icon: "doc.on.doc")
                } label: {
                    HStack {
                        VStack(alignment: .leading, spacing: 5) {
                            Eyebrow(text: "Votre code")
                            Text(code)
                                .font(.system(size: 22, weight: .medium, design: .monospaced))
                                .foregroundStyle(Brand.ink)
                        }
                        Spacer()
                        Image(systemName: "doc.on.doc").font(.system(size: 16))
                            .foregroundStyle(Brand.inkMuted)
                    }
                    .padding(16)
                    .background(Brand.well, in: .rect(cornerRadius: Metric.card, style: .continuous))
                    .contentShape(.rect)
                }
                .buttonStyle(Press())
                .gutter().padding(.top, 26)

                Rule().padding(.top, 26)
                HStack(spacing: 0) {
                    stat("Amis parrainés", "4")
                    Rectangle().fill(Brand.hairline).frame(width: 1, height: 38)
                    stat("Gains cumulés", "10 000 FCFA").padding(.leading, 18)
                }
                .gutter().padding(.vertical, 20)
                Rule()

                MPButton(title: "Partager mon code", icon: "square.and.arrow.up") {}
                    .gutter().padding(.top, 24)
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle("Parrainage")
        .navigationBarTitleDisplayMode(.inline)
        .toast($toastMsg)
    }

    private func stat(_ l: String, _ v: String) -> some View {
        VStack(alignment: .leading, spacing: 5) {
            Text(l).font(.micro).foregroundStyle(Brand.inkMuted)
            Text(v).font(.system(size: 22, weight: .semibold)).monospacedDigit()
                .foregroundStyle(Brand.ink)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
    }
}

// MARK: - Aide

struct HelpView: View {
    @State private var query = ""
    @State private var openIndex: Int?

    private let faq: [(String, String)] = [
        ("Pourquoi mon paiement a-t-il été refusé ?",
         "Le plus souvent, votre solde FCFA ne couvrait pas le montant converti au moment de l'autorisation. Rechargez puis réessayez. Chaque refus est facturé 220 FCFA."),
        ("Combien de temps pour recharger en Mobile Money ?",
         "MTN MoMo et Orange Money créditent votre wallet en quelques secondes. Un virement bancaire prend 1 à 2 jours ouvrés."),
        ("Ma carte marche-t-elle sur tous les sites ?",
         "Partout où Visa et Mastercard sont acceptés en ligne. Les marchands qui exigent une empreinte de caution — location de voiture, hôtels — refusent les cartes virtuelles."),
        ("Quel taux de change est appliqué ?",
         "Le taux interbancaire du jour, majoré de 3 % de marge MoneyPay. Le détail est affiché avant chaque conversion et sur chaque reçu."),
        ("Que se passe-t-il si je gèle une carte ?",
         "Toutes les autorisations sont refusées immédiatement. Les abonnements en cours échoueront tant que la carte reste gelée. Vous pouvez la dégeler à tout moment.")
    ]

    private var filtered: [(offset: Int, element: (String, String))] {
        Array(faq.enumerated()).filter {
            query.isEmpty || $0.element.0.localizedCaseInsensitiveContains(query)
        }
    }

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                Field(placeholder: "Rechercher une question", text: $query, icon: "magnifyingglass")
                    .gutter().padding(.top, 12)

                VStack(spacing: 0) {
                    ForEach(filtered, id: \.offset) { item in
                        VStack(alignment: .leading, spacing: 8) {
                            Button {
                                Haptic.tap()
                                withAnimation(.easeOut(duration: 0.2)) {
                                    openIndex = openIndex == item.offset ? nil : item.offset
                                }
                            } label: {
                                HStack(alignment: .top, spacing: 10) {
                                    Text(item.element.0).font(.body).foregroundStyle(Brand.ink)
                                        .multilineTextAlignment(.leading)
                                        .fixedSize(horizontal: false, vertical: true)
                                    Spacer(minLength: 8)
                                    Image(systemName: "chevron.down")
                                        .font(.system(size: 11, weight: .bold))
                                        .foregroundStyle(Brand.inkFaint)
                                        .rotationEffect(.degrees(openIndex == item.offset ? 180 : 0))
                                }
                                .contentShape(.rect)
                            }
                            .buttonStyle(.plain)

                            if openIndex == item.offset {
                                Text(item.element.1).font(.sub).foregroundStyle(Brand.inkMuted)
                                    .fixedSize(horizontal: false, vertical: true)
                            }
                        }
                        .padding(.vertical, 16)
                        if item.offset != filtered.last?.offset { Rule() }
                    }
                }
                .gutter().padding(.top, 12)

                Rule()
                VStack(alignment: .leading, spacing: 9) {
                    HStack(spacing: 7) {
                        Circle().fill(Brand.credit).frame(width: 7, height: 7)
                        Text("Conseillers en ligne").font(.bodyMed).foregroundStyle(Brand.ink)
                    }
                    Text("Du lundi au samedi, 8 h – 20 h (WAT). Réponse moyenne en 4 minutes.")
                        .font(.sub).foregroundStyle(Brand.inkMuted)
                    MPButton(title: "Démarrer une discussion", tone: .quiet) {}.padding(.top, 6)
                }
                .gutter().padding(.top, 22)
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle("Centre d'aide")
        .navigationBarTitleDisplayMode(.inline)
    }
}

// MARK: - Notifications

struct NotificationsView: View {
    @Environment(Store.self) private var store
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 0) {
                    ForEach(Array(store.notifications.enumerated()), id: \.element.id) { i, n in
                        HStack(alignment: .top, spacing: 12) {
                            IconTile(symbol: n.symbol, tint: n.tint)
                            VStack(alignment: .leading, spacing: 3) {
                                HStack(spacing: 6) {
                                    Text(n.title).font(.bodyMed).foregroundStyle(Brand.ink)
                                    if n.unread { Circle().fill(Brand.mark).frame(width: 6, height: 6) }
                                }
                                Text(n.body).font(.sub).foregroundStyle(Brand.inkMuted)
                                    .fixedSize(horizontal: false, vertical: true)
                                Text("\(Fmt.relativeDay(n.date)) · \(Fmt.time(n.date))")
                                    .font(.micro).foregroundStyle(Brand.inkFaint)
                            }
                            Spacer(minLength: 0)
                        }
                        .padding(.vertical, 16)
                        if i < store.notifications.count - 1 { Rule(inset: 51) }
                    }
                }
                .gutter()
                .padding(.bottom, 28)
            }
            .scrollIndicators(.hidden)
            .page()
            .navigationTitle("Notifications")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .topBarLeading) {
                    Button("Fermer") { dismiss() }.foregroundStyle(Brand.inkMuted)
                }
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Tout lire") { store.markAllNotificationsRead() }
                        .font(.subMed).foregroundStyle(Brand.ink)
                }
            }
        }
    }
}
