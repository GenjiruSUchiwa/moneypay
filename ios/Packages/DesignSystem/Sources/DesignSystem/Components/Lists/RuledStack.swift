import Foundation
import SwiftUI

public struct RuledStack<Data: RandomAccessCollection, Content: View>: View where Data.Element: Identifiable {
    public init(data: Data, inset: CGFloat = 51, @ViewBuilder row: @escaping (Data.Element) -> Content) {
        self.data = data
        self.inset = inset
        self.row = row
    }

    public var data: Data
    public var inset: CGFloat = 51
    @ViewBuilder public var row: (Data.Element) -> Content

    public var body: some View {
        VStack(spacing: 0) {
            ForEach(Array(data.enumerated()), id: \.element.id) { i, item in
                row(item)
                if i < data.count - 1 { Rule(inset: inset) }
            }
        }
    }
}

#Preview("RuledStack") {
    struct Item: Identifiable { let id = UUID(); let name: String }
    return RuledStack(data: [Item(name: "Netflix"), Item(name: "Spotify"), Item(name: "Figma")]) { item in
        Row(icon: "arrow.triangle.2.circlepath", title: Text(verbatim: item.name), chevron: true)
    }
    .gutter()
    .page()
}
