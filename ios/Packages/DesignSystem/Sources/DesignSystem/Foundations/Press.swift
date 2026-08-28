import SwiftUI

public struct Press: ButtonStyle {
    public init() {
    }

    public func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .opacity(configuration.isPressed ? 0.62 : 1)
            .animation(.easeOut(duration: 0.12), value: configuration.isPressed)
    }
}
