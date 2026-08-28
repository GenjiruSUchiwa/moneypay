import DesignSystem
import SwiftUI
import WalletStore

@main
struct MoniPayApp: App {
    @State private var store = Store()

    var body: some Scene {
        WindowGroup {
            RootView()
                .environment(store)
                .tint(Brand.ink)
        }
    }
}
