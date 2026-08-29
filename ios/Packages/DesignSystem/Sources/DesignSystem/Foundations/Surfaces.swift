import SwiftUI

// Page-level surfaces. The chrome of this app is flat on purpose: a page is a
// background colour and a single horizontal margin, never a stack of cards.

public extension View {
    /// Paints the page background edge to edge. Use once, on a screen's root.
    func page() -> some View { background(Brand.bg.ignoresSafeArea()) }

    /// The app's single horizontal margin. Never hard-code a leading padding.
    func gutter() -> some View { padding(.horizontal, Metric.gutter) }

    /// Inset focus ring used by `Field` and `OTPBoxes`.
    func inputChrome(emphasized: Bool) -> some View {
        background(emphasized ? Brand.surface : Brand.well,
                   in: .rect(cornerRadius: Metric.control, style: .continuous))
        .overlay {
            RoundedRectangle(cornerRadius: Metric.control, style: .continuous)
                .strokeBorder(emphasized ? Brand.action : .clear, lineWidth: 1.5)
        }
    }
}
