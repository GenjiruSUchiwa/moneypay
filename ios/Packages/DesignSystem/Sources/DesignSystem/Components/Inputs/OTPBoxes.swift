import SwiftUI

/// OTP code slots: a rule under each digit, not a box.
public struct OTPBoxes: View {
    public init(code: String, length: Int = 6) {
        self.code = code
        self.length = length
    }

    public var code: String
    public var length: Int = 6

    public var body: some View {
        HStack(spacing: 12) {
            ForEach(0..<length, id: \.self) { i in
                let ch = i < code.count ? String(Array(code)[i]) : ""
                let active = i == code.count
                VStack(spacing: 9) {
                    Text(verbatim: ch.isEmpty ? " " : ch)
                        .font(.system(size: 27, weight: .medium))
                        .monospacedDigit()
                        .foregroundStyle(Brand.ink)
                        .contentTransition(.numericText())
                    Rectangle()
                        .fill(active ? Brand.ink : Brand.rule)
                        .frame(height: active ? 2 : 1)
                }
            }
        }
        .animation(.spring(response: 0.22, dampingFraction: 0.85), value: code)
    }
}

#Preview("OTPBoxes") {
    VStack(spacing: 26) {
        OTPBoxes(code: "")
        OTPBoxes(code: "4821")
        OTPBoxes(code: "482193")
    }
    .padding()
    .page()
}
