import Foundation

/// Tout en unités mineures entières. Jamais de Double pour un solde.
/// USD -> cents. Le FCFA n'a pas de décimales : 1 unité = 1 FCFA.
struct FXRate: Sendable {
    /// ponytail: taux figé en dur. En prod, le tirer d'un feed et le rafraîchir
    /// (BEAC pour l'EUR/XAF fixe, un provider FX pour l'USD/EUR).
    var usdToXAF: Double = 610.0

    /// Marge de change appliquée par la fintech. C'est ici qu'est la vraie marge
    /// du business (2-4 %), pas dans les frais de carte. Bouton de calibration.
    var marginPct: Double = 0.03

    /// Convertit un montant carte (USD cents) en FCFA à débiter du wallet.
    /// Arrondi toujours au FCFA supérieur : la fintech ne perd pas sur l'arrondi.
    func xaf(fromUSDCents cents: Int) -> Int {
        precondition(cents >= 0, "montant négatif")
        return Int((Double(cents) / 100.0 * usdToXAF * (1.0 + marginPct)).rounded(.up))
    }
}

extension Int {
    var xafLabel: String { "\(self) FCFA" }
    var usdLabel: String { String(format: "$%.2f", Double(self) / 100.0) }
}
