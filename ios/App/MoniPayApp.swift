import DesignSystem
import SwiftUI
import WalletStore

@main
struct MoniPayApp: App {
    @State private var store = Store()
    private let configuration = AppConfiguration.live()

    var body: some Scene {
        WindowGroup {
            RootView(configuration: configuration)
                .environment(store)
                .tint(Brand.ink)
        }
    }
}
