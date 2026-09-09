import Foundation
import SwiftUI

public enum Viz {
    public static let categorical: [Color] = [
        .adaptive(light: 0x2a78d6, dark: 0x3987e5),
        .adaptive(light: 0xeb6834, dark: 0xd95926),
        .adaptive(light: 0x1baf7a, dark: 0x199e70),
        .adaptive(light: 0xeda100, dark: 0xc98500),
        .adaptive(light: 0xe87ba4, dark: 0xd55181),
        .adaptive(light: 0x008300, dark: 0x008300),
        .adaptive(light: 0x4a3aa7, dark: 0x9085e9)
    ]

    public static var series: Color { categorical[0] }

    public static let rank: [Color] = [
        .adaptive(light: 0x86b6ef, dark: 0x86b6ef),
        .adaptive(light: 0x2a78d6, dark: 0x3987e5),
        .adaptive(light: 0x184f95, dark: 0x1c5cab)
    ]

    public static var neutral: Color { Brand.inkFaint }

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
