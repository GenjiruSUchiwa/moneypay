import SwiftUI

/// Hairline. Full width between two sections, inset between two rows of the
/// same list.
public struct Rule: View {
    public init(inset: CGFloat = 0, strong: Bool = false) {
        self.inset = inset
        self.strong = strong
    }

    public var inset: CGFloat = 0
    public var strong = false
    public var body: some View {
        Rectangle()
            .fill(strong ? Brand.rule : Brand.hairline)
            .frame(height: 1)
            .padding(.leading, inset)
    }
}

#Preview("Rule") {
    VStack(spacing: 22) {
        Rule()
        Rule(inset: 51)
        Rule(strong: true)
    }
    .page()
}
