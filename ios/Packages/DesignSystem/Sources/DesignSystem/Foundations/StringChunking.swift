import Foundation

public extension String {
    func chunked(_ size: Int = 4, sep: String = " ") -> String {
        stride(from: 0, to: count, by: size).map {
            let s = index(startIndex, offsetBy: $0)
            let e = index(s, offsetBy: Swift.min(size, count - $0))
            return String(self[s..<e])
        }.joined(separator: sep)
    }
}
