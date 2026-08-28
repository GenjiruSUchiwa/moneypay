import Foundation
import SwiftUI

/// Data-visualisation tokens, kept apart from brand and status colours.
///
/// A documented categorical palette in a fixed order. The order IS the
/// colour-blind safety mechanism, so it never changes. Validated against the
/// app surface (#11161D): lightness band, chroma floor, CVD separation
/// (deltaE 8.4 for the worst neighbours), normal-vision floor (deltaE 19.3),
/// contrast at least 3:1. Status colours (Brand.credit / debit / pending) are
/// reserved and are never used as a series hue.
public enum Viz {
    /// Slots 1 to 7. The 8th (red) is dropped: it would read as an error.
    public static let categorical: [Color] = [
        .adaptive(light: 0x2a78d6, dark: 0x3987e5),   // 1 bleu
        .adaptive(light: 0xeb6834, dark: 0xd95926),   // 2 orange
        .adaptive(light: 0x1baf7a, dark: 0x199e70),   // 3 aqua
        .adaptive(light: 0xeda100, dark: 0xc98500),   // 4 jaune
        .adaptive(light: 0xe87ba4, dark: 0xd55181),   // 5 magenta
        .adaptive(light: 0x008300, dark: 0x008300),   // 6 vert
        .adaptive(light: 0x4a3aa7, dark: 0x9085e9)    // 7 violet
    ]

    /// Single series: magnitude without identity (plain bars, a trend line).
    public static var series: Color { categorical[0] }

    /// Single-hue ordinal ramp, light to dark, for a ranking (rank 1...3).
    /// Validated: monotonic lightness, deltaL at least 0.06, light end at 2.74:1.
    public static let rank: [Color] = [
        .adaptive(light: 0x86b6ef, dark: 0x86b6ef),
        .adaptive(light: 0x2a78d6, dark: 0x3987e5),
        .adaptive(light: 0x184f95, dark: 0x1c5cab)
    ]

    /// The neutral "Other" bucket: never an identity, always labelled.
    public static var neutral: Color { Brand.inkFaint }

    /// Recessive grid and axes.
    public static var grid: Color { Brand.hairline }
}

#Preview("Viz palette") {
    VStack(alignment: .leading, spacing: 10) {
        Eyebrow(text: Text(verbatim: "categorical"))
        HStack(spacing: 6) {
            ForEach(Array(Viz.categorical.enumerated()), id: \.offset) { _, colour in
                RoundedRectangle(cornerRadius: 4).fill(colour).frame(height: 34)
            }
        }
        Eyebrow(text: Text(verbatim: "rank"))
        HStack(spacing: 6) {
            ForEach(Array(Viz.rank.enumerated()), id: \.offset) { _, colour in
                RoundedRectangle(cornerRadius: 4).fill(colour).frame(height: 34)
            }
        }
    }
    .gutter()
    .page()
}
