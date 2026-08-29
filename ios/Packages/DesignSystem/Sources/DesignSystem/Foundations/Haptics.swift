import UIKit

/// `@MainActor` explicitly, not by inference: UIFeedbackGenerator must be
/// created and fired on the main actor, and saying so here keeps the rule true
/// even if a future target builds this package with a different default.
@MainActor
public enum Haptic {
    public static func tap() { UIImpactFeedbackGenerator(style: .light).impactOccurred() }
    public static func success() { UINotificationFeedbackGenerator().notificationOccurred(.success) }
    public static func warning() { UINotificationFeedbackGenerator().notificationOccurred(.warning) }
}
