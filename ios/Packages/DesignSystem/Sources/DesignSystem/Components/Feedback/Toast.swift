import SwiftUI

public struct Toast: Equatable {
    public init(text: Text, icon: String = "checkmark") {
        self.text = text
        self.icon = icon
    }

    public var text: Text
    public var icon: String = "checkmark"
}

public extension View {
    func toast(_ toast: Binding<Toast?>) -> some View {
        overlay(alignment: .bottom) {
            if let t = toast.wrappedValue {
                HStack(spacing: 8) {
                    Image(systemName: t.icon).font(.system(size: 12, weight: .bold))
                    t.text.font(.system(size: 14, weight: .medium))
                }
                .foregroundStyle(Brand.onInk)
                .padding(.horizontal, 16).padding(.vertical, 11)
                .background(Brand.inkFill, in: .capsule)
                .padding(.bottom, 34)
                .transition(.move(edge: .bottom).combined(with: .opacity))
                .task {
                    try? await Task.sleep(for: .seconds(2))
                    withAnimation(Motion.quick) { toast.wrappedValue = nil }
                }
            }
        }
        .animation(Motion.toast, value: toast.wrappedValue)
    }
}

#Preview("Toast") {
    @Previewable @State var toast: Toast? = Toast(text: Text(verbatim: "Card frozen"), icon: "snowflake")
    Color.clear.page().toast($toast)
}
