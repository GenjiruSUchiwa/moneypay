import Foundation
import SwiftUI

public struct FlagMark: View {
    public enum Country: String, CaseIterable, Sendable {
        case cm, ci, sn, ga, cd, bj

        public var regionCode: String { rawValue.uppercased() }
    }

    public init(_ country: Country, size: CGFloat) {
        self.country = country
        self.size = size
    }
    private let country: Country
    private let size: CGFloat

    public var body: some View {
        ZStack {
            ForEach(FlagArt.layers(for: country)) { layer in
                layer.shape.fill(layer.color)
            }
        }
        .frame(width: size, height: size)
        .clipShape(.circle)
        .overlay { Circle().strokeBorder(Brand.hairline, lineWidth: 1) }
        .accessibilityElement(children: .ignore)
        .accessibilityLabel(Text(verbatim: Locale.current.localizedString(
            forRegionCode: country.regionCode) ?? country.regionCode))
        .accessibilityAddTraits(.isImage)
    }
}

private enum FlagCanvas {
    nonisolated static let side: CGFloat = 40

    nonisolated static func point(_ x: CGFloat, _ y: CGFloat, in rect: CGRect) -> CGPoint {
        CGPoint(x: rect.minX + x / side * rect.width,
                y: rect.minY + y / side * rect.height)
    }
}

private struct FlagShape: Shape {
    enum Art {
        case polygon([CGPoint])
        case star(center: CGPoint, radius: CGFloat)
    }

    let art: Art

    func path(in rect: CGRect) -> Path {
        switch art {
        case .polygon(let points):
            return Self.polygonPath(points, in: rect)
        case .star(let center, let radius):
            return Self.polygonPath(Self.starPoints(center: center, radius: radius), in: rect)
        }
    }

    static func band(x: CGFloat, y: CGFloat, width: CGFloat, height: CGFloat) -> FlagShape {
        FlagShape(art: .polygon([
            CGPoint(x: x, y: y),
            CGPoint(x: x + width, y: y),
            CGPoint(x: x + width, y: y + height),
            CGPoint(x: x, y: y + height)
        ]))
    }

    private static func polygonPath(_ points: [CGPoint], in rect: CGRect) -> Path {
        var path = Path()
        guard let first = points.first else { return path }
        path.move(to: FlagCanvas.point(first.x, first.y, in: rect))
        for point in points.dropFirst() {
            path.addLine(to: FlagCanvas.point(point.x, point.y, in: rect))
        }
        path.closeSubpath()
        return path
    }

    private static func starPoints(center: CGPoint, radius: CGFloat) -> [CGPoint] {
        let innerRadius = radius * 0.4
        return (0..<10).map { index in
            let angle = -CGFloat.pi / 2 + CGFloat(index) * CGFloat.pi / 5
            let pointRadius = index.isMultiple(of: 2) ? radius : innerRadius
            return CGPoint(x: center.x + pointRadius * cos(angle),
                           y: center.y + pointRadius * sin(angle))
        }
    }
}

private struct FlagLayer: Identifiable {
    let id: Int
    let shape: FlagShape
    let color: Color
}

private enum FlagArt {
    static func layers(for country: FlagMark.Country) -> [FlagLayer] {
        switch country {
        case .cm: cameroon
        case .ci: ivoryCoast
        case .sn: senegal
        case .ga: gabon
        case .cd: democraticRepublicOfCongo
        case .bj: benin
        }
    }

    private static let cameroon: [FlagLayer] = [
        FlagLayer(id: 0, shape: .band(x: 0, y: 0, width: 14, height: 40), color: FlagPalette.cmGreen),
        FlagLayer(id: 1, shape: .band(x: 13, y: 0, width: 14, height: 40), color: FlagPalette.cmRed),
        FlagLayer(id: 2, shape: .band(x: 26, y: 0, width: 14, height: 40), color: FlagPalette.cmYellow),
        FlagLayer(id: 3, shape: FlagShape(art: .star(center: CGPoint(x: 20, y: 20.4), radius: 6.4)), color: FlagPalette.cmYellow)
    ]

    private static let ivoryCoast: [FlagLayer] = [
        FlagLayer(id: 0, shape: .band(x: 0, y: 0, width: 14, height: 40), color: FlagPalette.ciOrange),
        FlagLayer(id: 1, shape: .band(x: 13, y: 0, width: 14, height: 40), color: FlagPalette.ciWhite),
        FlagLayer(id: 2, shape: .band(x: 26, y: 0, width: 14, height: 40), color: FlagPalette.ciGreen)
    ]

    private static let senegal: [FlagLayer] = [
        FlagLayer(id: 0, shape: .band(x: 0, y: 0, width: 14, height: 40), color: FlagPalette.snGreen),
        FlagLayer(id: 1, shape: .band(x: 13, y: 0, width: 14, height: 40), color: FlagPalette.snYellow),
        FlagLayer(id: 2, shape: .band(x: 26, y: 0, width: 14, height: 40), color: FlagPalette.snRed),
        FlagLayer(id: 3, shape: FlagShape(art: .star(center: CGPoint(x: 20, y: 20.4), radius: 6.4)), color: FlagPalette.snGreen)
    ]

    private static let gabon: [FlagLayer] = [
        FlagLayer(id: 0, shape: .band(x: 0, y: 0, width: 40, height: 14), color: FlagPalette.gaGreen),
        FlagLayer(id: 1, shape: .band(x: 0, y: 13, width: 40, height: 14), color: FlagPalette.gaYellow),
        FlagLayer(id: 2, shape: .band(x: 0, y: 26, width: 40, height: 14), color: FlagPalette.gaBlue)
    ]

    private static let democraticRepublicOfCongo: [FlagLayer] = [
        FlagLayer(id: 0, shape: .band(x: 0, y: 0, width: 40, height: 40), color: FlagPalette.cdBlue),
        FlagLayer(id: 1, shape: FlagShape(art: .polygon([
            CGPoint(x: -4, y: 34), CGPoint(x: 34, y: -4), CGPoint(x: 48, y: -4),
            CGPoint(x: 10, y: 48), CGPoint(x: -4, y: 48)
        ])), color: FlagPalette.cdYellow),
        FlagLayer(id: 2, shape: FlagShape(art: .polygon([
            CGPoint(x: -2, y: 36), CGPoint(x: 36, y: -2), CGPoint(x: 44, y: -2),
            CGPoint(x: 6, y: 46), CGPoint(x: -2, y: 46)
        ])), color: FlagPalette.cdRed),
        FlagLayer(id: 3, shape: FlagShape(art: .star(center: CGPoint(x: 9, y: 10.05), radius: 5.05)), color: FlagPalette.cdYellow)
    ]

    private static let benin: [FlagLayer] = [
        FlagLayer(id: 0, shape: .band(x: 0, y: 0, width: 16, height: 40), color: FlagPalette.bjGreen),
        FlagLayer(id: 1, shape: .band(x: 16, y: 0, width: 24, height: 20), color: FlagPalette.bjYellow),
        FlagLayer(id: 2, shape: .band(x: 16, y: 20, width: 24, height: 20), color: FlagPalette.bjRed)
    ]
}

#Preview("FlagMark — every country") {
    VStack(spacing: 20) {
        HStack(spacing: 14) {
            ForEach(FlagMark.Country.allCases, id: \.self) { FlagMark($0, size: 24) }
        }
        HStack(spacing: 14) {
            ForEach(FlagMark.Country.allCases, id: \.self) { FlagMark($0, size: 30) }
        }
    }
    .padding()
    .page()
}
