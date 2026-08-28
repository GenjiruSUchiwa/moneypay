import SwiftUI

/// Jetons de visualisation, séparés des couleurs de marque et de statut.
///
/// Palette catégorielle documentée, dans son ordre fixe — l'ordre EST le
/// mécanisme de sûreté daltonisme, il ne change jamais. Validée sur la surface
/// de l'app (#11161D) : bande de clarté, plancher de chroma, séparation CVD
/// (ΔE 8,4 pires voisins), plancher vision normale (ΔE 19,3), contraste ≥ 3:1.
/// Les couleurs de statut (Brand.positive / negative / warning) sont réservées
/// et ne servent jamais de teinte de série.
enum Viz {
    /// Slots 1 à 7. Le 8e (rouge) est écarté : il se confondrait avec l'erreur.
    static let categorical: [Color] = [
        .adaptive(light: 0x2a78d6, dark: 0x3987e5),   // 1 bleu
        .adaptive(light: 0xeb6834, dark: 0xd95926),   // 2 orange
        .adaptive(light: 0x1baf7a, dark: 0x199e70),   // 3 aqua
        .adaptive(light: 0xeda100, dark: 0xc98500),   // 4 jaune
        .adaptive(light: 0xe87ba4, dark: 0xd55181),   // 5 magenta
        .adaptive(light: 0x008300, dark: 0x008300),   // 6 vert
        .adaptive(light: 0x4a3aa7, dark: 0x9085e9)    // 7 violet
    ]

    /// Série unique : magnitude sans identité (barres nominales, tendance).
    static var series: Color { categorical[0] }

    /// Rampe ordinale une teinte, clair → foncé, pour un classement (rang 1..3).
    /// Validée : clarté monotone, ΔL ≥ 0,06, extrémité claire à 2,74:1.
    static let rank: [Color] = [
        .adaptive(light: 0x86b6ef, dark: 0x86b6ef),
        .adaptive(light: 0x2a78d6, dark: 0x3987e5),
        .adaptive(light: 0x184f95, dark: 0x1c5cab)
    ]

    /// Bucket neutre « Autre » — jamais une identité, toujours étiqueté.
    static var neutral: Color { Brand.inkFaint }

    /// Grille et axes récessifs.
    static var grid: Color { Brand.hairline }
}
