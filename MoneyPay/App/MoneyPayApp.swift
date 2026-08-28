import SwiftUI

@main
struct MoneyPayApp: App {
    @State private var store = Store()

    var body: some Scene {
        WindowGroup {
            RootView()
                .environment(store)
                
                .tint(Brand.ink)
        }
    }
}
