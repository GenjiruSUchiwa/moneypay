import DesignSystem
import Money
import SwiftUI
import WalletStore

public struct MethodSheet: View {
    public init(selection: Binding<TopUpMethod>) {
        self._selection = selection
    }

    @Binding public var selection: TopUpMethod
    @Environment(\.dismiss) private var dismiss

    public var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("Top up from", bundle: .module).font(.heading2).tight().foregroundStyle(Brand.ink)
                .gutter().padding(.top, 20).padding(.bottom, 12)
            Rule()
            ScrollView {
                VStack(spacing: 0) {
                    ForEach(Array(SampleData.methods.enumerated()), id: \.element.id) { i, m in
                        Button { selection = m; dismiss() } label: {
                            HStack(spacing: 12) {
                                IconTile(symbol: m.symbol, tint: m.tint)
                                VStack(alignment: .leading, spacing: 3) {
                                    HStack(spacing: 6) {
                                        Text(verbatim: m.name).font(.bodyReg).foregroundStyle(Brand.ink)
                                        if m.feePct == 0 {
                                            StatusPill(text: Text("No fee", bundle: .module), symbol: "checkmark",
                                                       tint: Brand.credit, soft: Brand.creditSoft)
                                        }
                                    }
                                    Text(verbatim: m.detail).font(.sub).foregroundStyle(Brand.inkMuted)
                                }
                                Spacer(minLength: 8)
                                Image(systemName: "checkmark")
                                    .font(.system(size: 13, weight: .bold)).foregroundStyle(Brand.ink)
                                    .opacity(m.id == selection.id ? 1 : 0)
                            }
                            .padding(.vertical, Metric.rowVertical)
                            .contentShape(.rect)
                        }
                        .buttonStyle(.plain)
                        if i < SampleData.methods.count - 1 { Rule(inset: 51) }
                    }
                }
                .gutter()
            }
        }
    }
}
