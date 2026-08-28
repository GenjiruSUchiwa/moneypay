import SwiftUI

// The palette. Monochrome first: the neutral is not a pure grey, it is nudged
// green-blue, chosen rather than inherited. Colour appears in three places only
// - card artwork, money semantics, and the identity tint. No gradients in the
// chrome of the interface. Raw hex lives here and in Foundations/Theme.swift,
// nowhere else.

public enum Brand {
    /// Page background. Cold paper in light, deep ink in dark.
    public static let bg = Color.adaptive(light: 0xF4F6F6, dark: 0x0C0F10)
    /// Raised surface, reserved for elements genuinely lifted off the page.
    public static let surface = Color.adaptive(light: 0xFFFFFF, dark: 0x161B1C)
    /// Well: secondary content, input fields, keypad.
    public static let well = Color.adaptive(light: 0xEAEDED, dark: 0x121718)

    /// Hairlines. Always an opacity, never an opaque grey.
    public static let hairline = Color.adaptive(light: 0x0E1413, dark: 0xF1F4F3).opacity(0.09)
    public static let rule = Color.adaptive(light: 0x0E1413, dark: 0xF1F4F3).opacity(0.16)

    /// Ink.
    public static let ink = Color.adaptive(light: 0x0E1413, dark: 0xF1F4F3)
    public static let inkMuted = Color.adaptive(light: 0x5E6D69, dark: 0x8C9B97)
    public static let inkFaint = Color.adaptive(light: 0x93A19D, dark: 0x5A6764)
    /// Reversed ink: text on an ink fill.
    public static let onInk = Color.adaptive(light: 0xFFFFFF, dark: 0x0C0F10)
    /// Ink fill: the primary action. Black reads as serious where a
    /// saturated colour reads as a demo.
    public static let inkFill = Color.adaptive(light: 0x101615, dark: 0xF1F4F3)

    /// Primary action fill, matching the prototype's light and dark themes.
    public static let action = Color.adaptive(light: 0x067647, dark: 0x2FC988)
    /// Text color that remains legible on the primary action fill.
    public static let onAction = Color.adaptive(light: 0xFFFFFF, dark: 0x04301F)

    /// Identity tint: the monogram, the active tab, links. Never a large
    /// fill, never confused with the credit green.
    public static let mark = Color.adaptive(light: 0x0B4F6C, dark: 0x6FB6D6)
    public static let markSoft = Color.adaptive(light: 0xE2EDF2, dark: 0x11262F)
    /// Immersive deep-green panel for hero and full-screen identity moments.
    public static let greenDeep = Color.adaptive(light: 0x053826, dark: 0x0A4331)
    /// Constant white text and glyphs rendered on `greenDeep`.
    public static let deepInk = Color.adaptive(light: 0xFFFFFF, dark: 0xFFFFFF)
    /// Muted legal copy rendered on `greenDeep`.
    public static let deepInkMuted = deepInk.opacity(0.45)
    /// A translucent disc or logo tile rendered on `greenDeep`.
    public static let deepInkFill = deepInk.opacity(0.12)

    /// Money semantics. Reserved: never used as a series colour.
    public static let credit = Color.adaptive(light: 0x10704A, dark: 0x4FBF8B)
    public static let debit = Color.adaptive(light: 0xA8331F, dark: 0xE58A78)
    public static let pending = Color.adaptive(light: 0x8A5A06, dark: 0xE0A950)
    public static let creditSoft = Color.adaptive(light: 0xE1F0E8, dark: 0x11241B)
    public static let debitSoft = Color.adaptive(light: 0xF7E5E1, dark: 0x2A1512)
    public static let pendingSoft = Color.adaptive(light: 0xF7EDDA, dark: 0x261C0C)
}
