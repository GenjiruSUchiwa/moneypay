import SwiftUI

public extension View {
    func page() -> some View { background(Brand.bg.ignoresSafeArea()) }

    func gutter() -> some View { padding(.horizontal, Metric.gutter) }

    func inputChrome(emphasized: Bool) -> some View {
        background(emphasized ? Brand.surface : Brand.well,
                   in: .rect(cornerRadius: Metric.control, style: .continuous))
        .overlay {
            RoundedRectangle(cornerRadius: Metric.control, style: .continuous)
                .strokeBorder(emphasized ? Brand.action : .clear, lineWidth: 1.5)
        }
    }
}
