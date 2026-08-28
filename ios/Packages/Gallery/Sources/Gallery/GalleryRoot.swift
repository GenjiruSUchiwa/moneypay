import DesignSystem
import SwiftUI
import WalletStore

/// Index of every view in the mockup: walk each screen without replaying the
/// flows that lead to it.
public struct GalleryView: View {
    public init() {
    }

    @Environment(Store.self) private var store

    public var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                ForEach(ScreenCatalog.sections, id: \.self) { section in
                    let entries = ScreenCatalog.all.filter { $0.section == section }
                    Eyebrow(text: Text(verbatim: section)).gutter().padding(.top, 24).padding(.bottom, 2)
                    VStack(spacing: 0) {
                        ForEach(Array(entries.enumerated()), id: \.element.id) { i, entry in
                            NavigationLink { entry.make(store) } label: {
                                Row(icon: entry.icon, iconTint: entry.tint,
                                    title: Text(verbatim: entry.title), chevron: true)
                            }
                            .buttonStyle(.plain)
                            if i < entries.count - 1 { Rule(inset: 51) }
                        }
                    }
                    .gutter()
                    Rule().padding(.top, 4)
                }
                Text(verbatim: "\(ScreenCatalog.all.count) screens")
                    .font(.micro).foregroundStyle(Brand.inkFaint)
                    .gutter().padding(.top, 18)
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle(Text(verbatim: "Gallery"))
        .navigationBarTitleDisplayMode(.inline)
    }
}
