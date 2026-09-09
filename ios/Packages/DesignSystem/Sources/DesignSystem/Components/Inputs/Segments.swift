import SwiftUI

public struct Segments: View {
    public init(items: [Text], selection: Binding<Int>) {
        self.items = items
        self._selection = selection
    }

    public var items: [Text]
    @Binding public var selection: Int

    public var body: some View {
        Picker(selection: $selection) {
            ForEach(items.indices, id: \.self) { index in
                items[index].tag(index)
            }
        } label: {
            EmptyView()
        }
        .pickerStyle(.segmented)
        .sensoryFeedback(.selection, trigger: selection)
    }
}

#Preview("Segments") {
    @Previewable @State var selection = 1
    Segments(items: [Text(verbatim: "All"), Text(verbatim: "In"), Text(verbatim: "Out")],
             selection: $selection)
        .gutter()
        .page()
}
