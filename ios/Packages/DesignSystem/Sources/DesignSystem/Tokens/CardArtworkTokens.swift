import SwiftUI

enum CardArtworkTokens {
    static let ndopFill = Color(rgb: 0x23307C)
    static let ndopInk = Color.white
    static let diamondInk = ndopInk.opacity(0.17)
    static let tickInk = ndopInk.opacity(0.1)
    static let diamondStroke: CGFloat = 1.6
    static let pineInk = Color(rgb: 0x3CDD9B).opacity(0.13 * 0.55)
    static let pineStroke: CGFloat = 1
    static let viewBox = CGSize(width: 200, height: 126)
    static let tileSize: CGFloat = 28

    static func scale(for size: CGSize) -> CGFloat {
        max(size.width / viewBox.width, size.height / viewBox.height)
    }

    static func ndop(in size: CGSize, ticks: Bool = false) -> Path {
        guard size.width > 0, size.height > 0 else { return Path() }
        let path = ticks ? repeatedTicks : repeatedDiamonds
        let scale = scale(for: size)
        return path.applying(CGAffineTransform(
            a: scale, b: 0, c: 0, d: scale,
            tx: (size.width - viewBox.width * scale) / 2,
            ty: (size.height - viewBox.height * scale) / 2
        ))
    }

    private static func tiled(_ tile: Path) -> Path {
        var path = Path()
        for row in 0..<Int(ceil(viewBox.height / tileSize)) {
            for column in 0..<Int(ceil(viewBox.width / tileSize)) {
                let offset = CGAffineTransform(translationX: CGFloat(column) * tileSize,
                                              y: CGFloat(row) * tileSize)
                path.addPath(tile, transform: offset)
            }
        }
        return path
    }

    static func pine(in size: CGSize) -> Path {
        guard size.width > 0, size.height > 0 else { return Path() }
        var path = Path()
        let circles: [(CGFloat, CGFloat, CGFloat)] = [
            (100, 100, 28), (100, 100, 46), (100, 100, 64), (100, 100, 82),
            (82, 100, 55), (118, 100, 55), (100, 82, 55), (100, 118, 55),
            (87, 87, 55), (113, 113, 55), (87, 113, 55), (113, 87, 55)
        ]
        for (x, y, radius) in circles {
            path.addEllipse(in: CGRect(x: x - radius, y: y - radius,
                                       width: radius * 2, height: radius * 2))
        }
        return path.applying(CGAffineTransform(translationX: size.width - 154, y: size.height - 148))
    }

    private static let tileDiamonds = Path { path in
        path.addLines([CGPoint(x: 0, y: 14), CGPoint(x: 14, y: 0),
                       CGPoint(x: 28, y: 14), CGPoint(x: 14, y: 28)])
        path.closeSubpath()
        path.addLines([CGPoint(x: 7, y: 14), CGPoint(x: 14, y: 7),
                       CGPoint(x: 21, y: 14), CGPoint(x: 14, y: 21)])
        path.closeSubpath()
    }

    private static let tileTicks = Path { path in
        for y: CGFloat in [0, 28] {
            for x: CGFloat in [0, 24] {
                path.move(to: CGPoint(x: x, y: y))
                path.addLine(to: CGPoint(x: x + 4, y: y))
            }
        }
    }

    private static let repeatedDiamonds = tiled(tileDiamonds)
    private static let repeatedTicks = tiled(tileTicks)
}
