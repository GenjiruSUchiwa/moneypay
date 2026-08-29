import SwiftUI

/// Segments: an underline, not a floating pill.
public struct Segments: View {
    public init(items: [Text], selection: Binding<Int>) {
        self.items = items
        self._selection = selection
    }

    public var items: [Text]
    @Binding public var selection: Int
    @Namespace private var ns

    public var body: some View {
        HStack(spacing: 22) {
            ForEach(items.indices, id: \.self) { i in
                Button {
                    withAnimation(.spring(response: 0.3, dampingFraction: 0.85)) { selection = i }
                } label: {
                    VStack(spacing: 8) {
                        items[i]
                            .font(.system(size: 15, weight: selection == i ? .medium : .regular))
                            .foregroundStyle(selection == i ? Brand.ink : Brand.inkMuted)
                        ZStack {
                            Capsule().fill(.clear).frame(height: 2)
                            if selection == i {
                                Capsule().fill(Brand.inkFill).frame(height: 2)
                                    .matchedGeometryEffect(id: "seg", in: ns)
                            }
                        }
                    }
                    .fixedSize()
                }
                .buttonStyle(.plain)
            }
            Spacer(minLength: 0)
        }
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
