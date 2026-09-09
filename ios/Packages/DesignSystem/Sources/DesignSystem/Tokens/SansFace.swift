import CoreText
import Foundation
import SwiftUI

enum SansFace {
    static let registration: Void = {
        let urls = Bundle.module.urls(forResourcesWithExtension: "ttf", subdirectory: nil) ?? []
        for url in urls {
            CTFontManagerRegisterFontsForURL(url as CFURL, .process, nil)
        }
    }()

    static func postScriptName(for weight: Font.Weight) -> String {
        switch weight {
        case .ultraLight, .thin, .light: "GoogleSansFlex-Light"
        case .medium: "GoogleSansFlex-Medium"
        case .semibold: "GoogleSansFlex-SemiBold"
        case .bold: "GoogleSansFlex-Bold"
        case .heavy, .black: "GoogleSansFlex-ExtraBold"
        default: "GoogleSansFlex-Regular"
        }
    }
}
