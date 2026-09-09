import SwiftUI

public enum Brand {
    public static let bg = Color.adaptive(light: 0xF4F6F6, dark: 0x0C0F10)
    public static let surface = Color.adaptive(light: 0xFFFFFF, dark: 0x161B1C)
    public static let well = Color.adaptive(light: 0xEAEDED, dark: 0x121718)

    public static let hairline = Color.adaptive(light: 0x0E1413, dark: 0xF1F4F3).opacity(0.09)
    public static let rule = Color.adaptive(light: 0x0E1413, dark: 0xF1F4F3).opacity(0.16)

    public static let ink = Color.adaptive(light: 0x0E1413, dark: 0xF1F4F3)
    public static let inkMuted = Color.adaptive(light: 0x5E6D69, dark: 0x8C9B97)
    public static let inkFaint = Color.adaptive(light: 0x93A19D, dark: 0x5A6764)

    public static let action = Color.adaptive(light: 0x067647, dark: 0x2FC988)
    public static let onAction = Color.adaptive(light: 0xFFFFFF, dark: 0x04301F)

    public static let mark = Color.adaptive(light: 0x0B4F6C, dark: 0x6FB6D6)
    public static let markSoft = Color.adaptive(light: 0xE2EDF2, dark: 0x11262F)
    public static let greenDeep = Color.adaptive(light: 0x053826, dark: 0x0A4331)
    public static let deepInk = Color.adaptive(light: 0xFFFFFF, dark: 0xFFFFFF)
    public static let deepInkMuted = deepInk.opacity(0.45)
    public static let deepInkFill = deepInk.opacity(0.12)

    public static let credit = Color.adaptive(light: 0x10704A, dark: 0x4FBF8B)
    public static let debit = Color.adaptive(light: 0xA8331F, dark: 0xE58A78)
    public static let pending = Color.adaptive(light: 0x8A5A06, dark: 0xE0A950)
    public static let creditSoft = Color.adaptive(light: 0xE1F0E8, dark: 0x11241B)
    public static let debitSoft = Color.adaptive(light: 0xF7E5E1, dark: 0x2A1512)
    public static let pendingSoft = Color.adaptive(light: 0xF7EDDA, dark: 0x261C0C)
}
