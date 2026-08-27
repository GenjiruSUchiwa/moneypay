"use strict";
/* ================================================================
   Icônes — famille unique : trait 1.8, bouts ronds, grille 24
   ================================================================ */
const ICON_PATHS = {
  home: '<path d="M4.5 10.2 12 4l7.5 6.2V19a1.4 1.4 0 0 1-1.4 1.4h-3.6v-5.6h-5v5.6H5.9A1.4 1.4 0 0 1 4.5 19Z"/>',
  card: '<rect x="3.2" y="5.8" width="17.6" height="12.4" rx="2.6"/><path d="M3.2 9.8h17.6M6.6 14.6h4"/>',
  list: '<path d="M8.6 6.4h11M8.6 12h11M8.6 17.6h11"/><path d="M4.4 6.4h.01M4.4 12h.01M4.4 17.6h.01" stroke-width="2.6"/>',
  chart: '<path d="M5.4 19.5v-6.2M12 19.5V4.8M18.6 19.5v-9.6"/>',
  user: '<circle cx="12" cy="8.2" r="3.6"/><path d="M4.9 19.6c.9-3.4 3.7-5.1 7.1-5.1s6.2 1.7 7.1 5.1"/>',
  bell: '<path d="M12 4.4a5.3 5.3 0 0 1 5.3 5.3c0 3.1.8 4.6 1.6 5.6H5.1c.8-1 1.6-2.5 1.6-5.6A5.3 5.3 0 0 1 12 4.4Z"/><path d="M10 18.6a2.1 2.1 0 0 0 4 0"/>',
  chevL: '<path d="m14.5 5.5-6 6.5 6 6.5"/>',
  chevR: '<path d="m9.5 5.5 6 6.5-6 6.5"/>',
  chevD: '<path d="m5.5 9.5 6.5 6 6.5-6"/>',
  plus: '<path d="M12 5v14M5 12h14"/>',
  swap: '<path d="M4.5 8.2h13l-3.4-3.4M19.5 15.8h-13l3.4 3.4"/>',
  arrUpR: '<path d="M7 17 17 7M9.5 7H17v7.5"/>',
  arrDnL: '<path d="M17 7 7 17M14.5 17H7V9.5"/>',
  arrDn: '<path d="M12 5v14m-6-6.5 6 6.5 6-6.5"/>',
  eye: '<path d="M3.5 12S6.6 6.4 12 6.4 20.5 12 20.5 12 17.4 17.6 12 17.6 3.5 12 3.5 12Z"/><circle cx="12" cy="12" r="2.6"/>',
  eyeOff: '<path d="M5 5l14 14M9.6 6.9A8.7 8.7 0 0 1 12 6.4c5.4 0 8.5 5.6 8.5 5.6a15 15 0 0 1-2.9 3.4M6.3 8.3A14.4 14.4 0 0 0 3.5 12s3.1 5.6 8.5 5.6c1 0 1.9-.2 2.8-.5"/>',
  snow: '<path d="M12 3.8v16.4M4.9 7.9l14.2 8.2M4.9 16.1l14.2-8.2M12 3.8 9.9 5.9M12 3.8l2.1 2.1M12 20.2l-2.1-2.1M12 20.2l2.1-2.1M4.9 7.9l.6 2.9M4.9 7.9l2.9-.6M19.1 16.1l-.6-2.9M19.1 16.1l-2.9.6M4.9 16.1l2.9.6M4.9 16.1l.6-2.9M19.1 7.9l-2.9-.6M19.1 7.9l-.6 2.9"/>',
  sun: '<circle cx="12" cy="12" r="4.2"/><path d="M12 3.4v2M12 18.6v2M3.4 12h2M18.6 12h2M5.9 5.9l1.4 1.4M16.7 16.7l1.4 1.4M18.1 5.9l-1.4 1.4M7.3 16.7l-1.4 1.4"/>',
  sliders: '<path d="M4 7.4h9M17.2 7.4H20M4 16.6h2.8M11 16.6h9"/><circle cx="15.1" cy="7.4" r="2.1"/><circle cx="8.9" cy="16.6" r="2.1"/>',
  wallet: '<path d="M4 7.6A1.8 1.8 0 0 1 5.8 5.8h11.4v2.4"/><rect x="4" y="8.2" width="16" height="10" rx="2"/><path d="M15.2 13.3h2.2"/>',
  copy: '<rect x="8.6" y="8.6" width="11" height="11" rx="2"/><path d="M5.4 15.4h-.2a1.8 1.8 0 0 1-1.8-1.8V6.2a1.8 1.8 0 0 1 1.8-1.8h7.4a1.8 1.8 0 0 1 1.8 1.8v.2"/>',
  share: '<path d="M12 3.8v10.4M8.2 7 12 3.5 15.8 7"/><path d="M6 11.4H5.2A1.6 1.6 0 0 0 3.6 13v5.4A1.6 1.6 0 0 0 5.2 20h13.6a1.6 1.6 0 0 0 1.6-1.6V13a1.6 1.6 0 0 0-1.6-1.6H18"/>',
  check: '<path d="m4.8 12.6 4.6 4.8L19.2 6.8"/>',
  moon: '<path d="M20.2 14.7A8.3 8.3 0 1 1 9.3 3.8a6.9 6.9 0 0 0 10.9 10.9Z"/>',
  x: '<path d="M6 6l12 12M18 6 6 18"/>',
  search: '<circle cx="10.8" cy="10.8" r="6.2"/><path d="m15.4 15.4 4.6 4.6"/>',
  lock: '<rect x="5.4" y="10.4" width="13.2" height="9" rx="2.2"/><path d="M8.4 10.4V8a3.6 3.6 0 0 1 7.2 0v2.4"/>',
  shield: '<path d="M12 3.8 5 6.4v5.2c0 4.4 2.9 7.3 7 8.6 4.1-1.3 7-4.2 7-8.6V6.4Z"/><path d="m9 11.8 2.2 2.3 3.8-4"/>',
  phone: '<rect x="7" y="3.6" width="10" height="16.8" rx="2.4"/><path d="M10.6 17.6h2.8"/>',
  mail: '<rect x="3.4" y="5.6" width="17.2" height="12.8" rx="2.2"/><path d="m4.4 7.4 7.6 5.6 7.6-5.6"/>',
  globe: '<circle cx="12" cy="12" r="8.3"/><path d="M3.7 12h16.6M12 3.7c2.3 2.2 3.5 5.1 3.5 8.3s-1.2 6.1-3.5 8.3c-2.3-2.2-3.5-5.1-3.5-8.3S9.7 5.9 12 3.7Z"/>',
  doc: '<path d="M6.2 4.6h7.2l4.4 4.4v9.8a1.6 1.6 0 0 1-1.6 1.6H6.2a1.6 1.6 0 0 1-1.6-1.6V6.2a1.6 1.6 0 0 1 1.6-1.6Z"/><path d="M13.2 4.8V9h4.2M8.6 13.2h6.8M8.6 16.4h4.4"/>',
  download: '<path d="M12 4.2v9.6m-3.8-3.4 3.8 3.6 3.8-3.6"/><path d="M4.6 16.4v1.8A1.8 1.8 0 0 0 6.4 20h11.2a1.8 1.8 0 0 0 1.8-1.8v-1.8"/>',
  gift: '<rect x="4" y="9.4" width="16" height="4" rx="1.2"/><path d="M5.4 13.4v5A1.6 1.6 0 0 0 7 20h10a1.6 1.6 0 0 0 1.6-1.6v-5M12 9.4V20M12 9.2c-1.6 0-4.2-.4-4.2-2.6a2 2 0 0 1 2-2c2.2 0 2.2 3 2.2 4.6Zm0 0c1.6 0 4.2-.4 4.2-2.6a2 2 0 0 0-2-2c-2.2 0-2.2 3-2.2 4.6Z"/>',
  help: '<circle cx="12" cy="12" r="8.3"/><path d="M9.6 9.6A2.4 2.4 0 0 1 12 7.6a2.3 2.3 0 0 1 2.4 2.2c0 1.6-2.4 1.9-2.4 3.6"/><path d="M12 16.6h.01" stroke-width="2.4"/>',
  gauge: '<path d="M4.2 15.6a8 8 0 1 1 15.6 0"/><path d="m12 13.8 3.6-3.8"/><circle cx="12" cy="14.2" r="1" fill="currentColor" stroke="none"/>',
  recur: '<path d="M5 10.2A7.3 7.3 0 0 1 18.4 8.4M19 13.8A7.3 7.3 0 0 1 5.6 15.6"/><path d="M18.6 4.6v3.8h-3.8M5.4 19.4v-3.8h3.8"/>',
  trash: '<path d="M4.8 6.8h14.4M9.2 6.6V5.2A1.2 1.2 0 0 1 10.4 4h3.2a1.2 1.2 0 0 1 1.2 1.2v1.4M6.4 6.9l.8 11.6A1.6 1.6 0 0 0 8.8 20h6.4a1.6 1.6 0 0 0 1.6-1.5l.8-11.6M10.1 10.4v6M13.9 10.4v6"/>',
  pencil: '<path d="m14.4 5.4 4.2 4.2L8.4 19.8l-4.6.4.4-4.6ZM12.8 7l4.2 4.2"/>',
  logout: '<path d="M14.6 8V6a1.8 1.8 0 0 0-1.8-1.8H6.2A1.8 1.8 0 0 0 4.4 6v12a1.8 1.8 0 0 0 1.8 1.8h6.6a1.8 1.8 0 0 0 1.8-1.8v-2M9.8 12h9.8m-3.2-3.4L19.8 12l-3.4 3.4"/>',
  warn: '<path d="M12 4.6 21 19.4H3Z"/><path d="M12 10.2v3.6M12 16.6h.01" stroke-width="2"/>',
  clock: '<circle cx="12" cy="12" r="8.3"/><path d="M12 7.2V12l3.2 2"/>',
  bolt: '<path d="M13.2 3.8 5.6 13.4h5.2l-.9 6.8 7.5-9.6h-5.2Z"/>',
  faceid: '<path d="M4 8V6.2A2.2 2.2 0 0 1 6.2 4H8M16 4h1.8A2.2 2.2 0 0 1 20 6.2V8M20 16v1.8a2.2 2.2 0 0 1-2.2 2.2H16M8 20H6.2A2.2 2.2 0 0 1 4 17.8V16M8.6 9.4v1.4M15.4 9.4v1.4M12 9.6v3.2h-1M8.9 15.4a4.6 4.6 0 0 0 6.2 0"/>',
  backspace: '<path d="M8.8 5.4h9.4A1.8 1.8 0 0 1 20 7.2v9.6a1.8 1.8 0 0 1-1.8 1.8H8.8L3.4 12Z"/><path d="m11.4 9.6 4.8 4.8m0-4.8-4.8 4.8"/>',
  dots: '<path d="M6 12h.01M12 12h.01M18 12h.01" stroke-width="2.8"/>',
  chat: '<path d="M12 4.4c4.7 0 8.4 3 8.4 6.8s-3.7 6.8-8.4 6.8a10 10 0 0 1-2.6-.3L5.2 19.6l.9-3.3a6.3 6.3 0 0 1-2.5-4.9c0-3.8 3.7-7 8.4-7Z"/>',
  idcard: '<rect x="3.2" y="5.4" width="17.6" height="13.2" rx="2.2"/><circle cx="8.6" cy="10.6" r="1.8"/><path d="M6.2 15.4c.5-1.3 1.4-1.9 2.4-1.9s1.9.6 2.4 1.9M14.2 9.6h3.6M14.2 12.8h3.6"/>',
  one: '<circle cx="12" cy="12" r="8.3"/><path d="M10.4 9.4 12.4 8v8"/>',
  undo: '<path d="M8.4 5.8 4.6 9.4l3.8 3.6"/><path d="M4.8 9.4h9a5.4 5.4 0 0 1 0 10.8H9.4"/>',
  bulb: '<path d="M9 18.2h6M10 20.6h4M12 3.8a5.6 5.6 0 0 1 3.2 10.2c-.7.6-1 1.2-1 1.8h-4.4c0-.6-.3-1.2-1-1.8A5.6 5.6 0 0 1 12 3.8Z"/>',
  store: '<path d="M4.6 9.8 6 4.6h12l1.4 5.2M4.6 9.8a2.2 2.2 0 1 0 4.4 0 2.4 2.4 0 1 0 4.8 0 2.4 2.4 0 1 0 4.8 0M5.6 12.6v6.8h12.8v-6.8M9.8 19.2v-4.6h4.4v4.6"/>',
  bank: '<path d="m12 3.8 8.4 4.6H3.6ZM5 8.6v7.6M9.7 8.6v7.6M14.3 8.6v7.6M19 8.6v7.6M3.8 16.4h16.4M3 19.6h18"/>',
  signal: '<path d="M5.2 15.4a9.6 9.6 0 0 1 13.6 0M8 12.4a5.7 5.7 0 0 1 8 0M10.8 15.2a2 2 0 0 1 2.4 0"/><path d="M12 18.2h.01" stroke-width="2.4"/>',
  /* catégories */
  play: '<path d="M8.4 5.8 18 12l-9.6 6.2Z"/>',
  code: '<path d="m8.4 7.6-4.6 4.4 4.6 4.4M15.6 7.6l4.6 4.4-4.6 4.4"/>',
  bag: '<path d="M5.8 8.4h12.4l-1 11a1.6 1.6 0 0 1-1.6 1.4H8.4a1.6 1.6 0 0 1-1.6-1.4Z"/><path d="M8.8 10.6V7.4a3.2 3.2 0 0 1 6.4 0v3.2"/>',
  carIco: '<path d="M5.2 12.8 6.6 8a1.6 1.6 0 0 1 1.5-1.2h7.8A1.6 1.6 0 0 1 17.4 8l1.4 4.8M5.2 12.8h13.6a1.4 1.4 0 0 1 1.4 1.4v3.2h-2.4M5.2 12.8a1.4 1.4 0 0 0-1.4 1.4v3.2h2.4"/><path d="M6.2 17.4h11.6M7.4 15h.01M16.6 15h.01" stroke-width="2"/>',
  fork: '<path d="M7.2 3.8v5.4a2.2 2.2 0 0 0 4.4 0V3.8M9.4 3.8v16.4M15.4 13.6V4.2c1.8.8 2.8 3 2.8 5.6 0 1.8-.6 3-1.4 3.8h-1.4Zm0 0v6.6"/>',
  mega: '<path d="M4.4 10.2v3.6a1.4 1.4 0 0 0 1.4 1.4h2L13 19V5l-5.2 3.8h-2a1.4 1.4 0 0 0-1.4 1.4ZM16.2 9.4a3.6 3.6 0 0 1 0 5.2M18.6 7.4a6.6 6.6 0 0 1 0 9.2"/>',
  plane: '<path d="M10.4 13.6 4.6 11l1.6-1.6 6.8 1L17.4 6a1.6 1.6 0 0 1 2.3 2.3L15.3 12.7l1 6.8-1.6 1.6-2.6-5.8-3.2 3.2.3 2.3-1.3 1.2-1.6-3.4-3.4-1.6 1.2-1.3 2.3.3Z"/>',
  grid: '<path d="M5.6 5.6h.01M12 5.6h.01M18.4 5.6h.01M5.6 12h.01M12 12h.01M18.4 12h.01M5.6 18.4h.01M12 18.4h.01M18.4 18.4h.01" stroke-width="2.8"/>'
};
function ico(name, size = 20, cls = "") {
  const d = ICON_PATHS[name] || ICON_PATHS.dots;
  return `<svg class="${cls}" width="${size}" height="${size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${d}</svg>`;
}
function icoFill(name, size = 20, cls = "") {
  const d = ICON_PATHS[name] || ICON_PATHS.dots;
  return `<svg class="${cls}" width="${size}" height="${size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${d}</svg>`;
}

/* ================================================================
   Logos de marques — SVG dédiés (tuile 40×40)
   ================================================================ */
const LOGOS = {
  netflix: `<svg viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" fill="#141414"/><path d="M14.5 9h4.1v22c-1.4.15-2.7.35-4.1.6Z" fill="#8E0709"/><path d="M21.4 31.1c1.4.1 2.7.25 4.1.45V9h-4.1Z" fill="#8E0709"/><path d="M14.5 9h4.1l6.9 22.55c-1.35-.2-2.75-.35-4.1-.45L14.5 9Z" fill="#E50914"/></svg>`,
  spotify: `<svg viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" fill="#FFFFFF"/><circle cx="20" cy="20" r="13.5" fill="#1ED760"/><path d="M13.6 15.9c4.6-1.4 9.6-.9 13.2 1.3" stroke="#121212" stroke-width="2.2" stroke-linecap="round" fill="none"/><path d="M14.2 19.9c3.9-1.1 7.9-.7 10.9 1.1" stroke="#121212" stroke-width="1.9" stroke-linecap="round" fill="none"/><path d="M14.8 23.6c3.2-.9 6.3-.5 8.8.9" stroke="#121212" stroke-width="1.6" stroke-linecap="round" fill="none"/></svg>`,
  openai: `<svg viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" fill="#FFFFFF"/><g stroke="#0F0F0F" stroke-width="2.5" stroke-linecap="round" fill="none"><path d="M20 8.6a7.6 7.6 0 0 1 6.58 11.4"/><path d="M29.87 14.3a7.6 7.6 0 0 1-6.58 11.4"/><path d="M29.87 25.7A7.6 7.6 0 0 1 16.71 25.7"/><path d="M20 31.4a7.6 7.6 0 0 1-6.58-11.4"/><path d="M10.13 25.7a7.6 7.6 0 0 1 6.58-11.4"/><path d="M10.13 14.3a7.6 7.6 0 0 1 13.16 0"/></g></svg>`,
  figma: `<svg viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" fill="#FFFFFF"/><path d="M20 8h-3.9a3.9 3.9 0 0 0 0 7.8H20Z" fill="#F24E1E"/><path d="M20 8h3.9a3.9 3.9 0 0 1 0 7.8H20Z" fill="#FF7262"/><path d="M20 15.8h-3.9a3.9 3.9 0 0 0 0 7.8H20Z" fill="#A259FF"/><circle cx="23.9" cy="19.7" r="3.9" fill="#1ABCFE"/><path d="M20 23.6h-3.9a3.9 3.9 0 1 0 3.9 3.9Z" fill="#0ACF83"/></svg>`,
  meta: `<svg viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" fill="#FFFFFF"/><path d="M8.6 25.4c0-9.4 6.2-9.8 11.4-2.4 5.6 8 11.4 6.6 11.4-2.4" stroke="#0081FB" stroke-width="3.4" stroke-linecap="round" fill="none"/></svg>`,
  amazon: `<svg viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" fill="#FFFFFF"/><text x="20" y="23.5" text-anchor="middle" font-family="Google Sans Flex, sans-serif" font-size="19" font-weight="700" fill="#131A22">a</text><path d="M11.5 27.2c4.8 3.4 12.4 3.5 16.7.4" stroke="#FF9900" stroke-width="2" stroke-linecap="round" fill="none"/><path d="m28.4 26.4.5 1.9-2 .3" stroke="#FF9900" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" fill="none"/></svg>`,
  uber: `<svg viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" fill="#000000"/><text x="20" y="24.5" text-anchor="middle" font-family="Google Sans Flex, sans-serif" font-size="12" font-weight="500" letter-spacing=".2" fill="#FFFFFF">Uber</text></svg>`,
  booking: `<svg viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" fill="#003580"/><text x="20" y="26.5" text-anchor="middle" font-family="Google Sans Flex, sans-serif" font-size="19" font-weight="700" fill="#FFFFFF">B.</text></svg>`,
  digitalocean: `<svg viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" fill="#FFFFFF"/><path d="M20 30v-4.6a9.4 9.4 0 1 0-9.4-9.4H15A5 5 0 1 1 20 21v4.4Z" fill="#0080FF"/><rect x="11.6" y="25.4" width="4.6" height="4.6" fill="#0080FF"/><rect x="8" y="29" width="3.6" height="3.6" fill="#0080FF"/></svg>`,
  aliexpress: `<svg viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" fill="#FFFFFF"/><path d="M10 27 15.8 13h2.4L24 27h-2.6l-1.5-3.7h-6l-1.4 3.7Zm5-6h4l-2-5.2Z" fill="#E62E04"/><path d="M25.5 13h2.3v14h-2.3Z" fill="#FF7A45"/></svg>`,
  glovo: `<svg viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" fill="#FFC244"/><circle cx="20" cy="21.5" r="7.6" fill="none" stroke="#FFFFFF" stroke-width="3.4"/><path d="M20 10.2v4" stroke="#FFFFFF" stroke-width="3.4" stroke-linecap="round"/><circle cx="20" cy="9.4" r="1.9" fill="#FFFFFF"/></svg>`,
  mtn: `<svg viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" fill="#FFCB05"/><ellipse cx="20" cy="20" rx="13.5" ry="8.6" fill="none" stroke="#003E7E" stroke-width="1.9"/><text x="20" y="23.4" text-anchor="middle" font-family="Google Sans Flex, sans-serif" font-size="9.5" font-weight="700" font-style="italic" fill="#003E7E">MTN</text></svg>`,
  orange: `<svg viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" fill="#FF7900"/><text x="20" y="32" text-anchor="middle" font-family="Google Sans Flex, sans-serif" font-size="8.2" font-weight="600" fill="#FFFFFF">orange</text></svg>`,
  moneypay: `<svg viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" fill="#067647"/><path d="M12 27V13.4L20 22l8-8.6V27" fill="none" stroke="#FFFFFF" stroke-width="3.1" stroke-linejoin="miter" stroke-linecap="square"/></svg>`
};
function logoTile(key, size = 40, cls = "logo-tile") {
  const svg = LOGOS[key];
  if (svg) return `<span class="${cls}" style="width:${size}px;height:${size}px">${svg}</span>`;
  return "";
}

/* logo MoniPay (paramétrable) */
function logoMark(size = 34, bg = "var(--green)", glyph = "#FFFFFF", r = 0.28) {
  return `<svg width="${size}" height="${size}" viewBox="0 0 40 40" aria-hidden="true"><rect width="40" height="40" rx="${40 * r}" fill="${bg}"/><path d="M12 27V13.4L20 22l8-8.6V27" fill="none" stroke="${glyph}" stroke-width="3.1" stroke-linejoin="miter" stroke-linecap="square"/></svg>`;
}

/* marques réseau */
function netMark(network, inkColor, h = 22) {
  if (network === "mastercard") {
    const w = h * 1.62;
    return `<svg width="${w}" height="${h}" viewBox="0 0 39 24" aria-hidden="true"><circle cx="12" cy="12" r="11.2" fill="#EB001B"/><circle cx="27" cy="12" r="11.2" fill="#F79E1B"/><path d="M19.5 3.1a11.2 11.2 0 0 1 0 17.8 11.2 11.2 0 0 1 0-17.8Z" fill="#FF5F00"/></svg>`;
  }
  return `<svg width="${h * 2.1}" height="${h * 0.82}" viewBox="0 0 50 18" aria-hidden="true"><text x="25" y="14.5" text-anchor="middle" font-family="Google Sans Flex, sans-serif" font-size="15.5" font-weight="800" font-style="italic" letter-spacing="-.4" fill="${inkColor}">VISA</text></svg>`;
}

/* drapeaux (pastilles rondes) */
const FLAGS = {
  CM: `<svg viewBox="0 0 40 40"><rect width="14" height="40" fill="#007A5E"/><rect x="13" width="14" height="40" fill="#CE1126"/><rect x="26" width="14" height="40" fill="#FCD116"/><path d="m20 14 1.6 4.4 4.6.1-3.7 2.8 1.4 4.4L20 23l-3.9 2.7 1.4-4.4-3.7-2.8 4.6-.1Z" fill="#FCD116"/></svg>`,
  US: `<svg viewBox="0 0 40 40"><rect width="40" height="40" fill="#FFFFFF"/><g fill="#B22234"><rect width="40" height="5.7"/><rect y="11.4" width="40" height="5.7"/><rect y="22.8" width="40" height="5.7"/><rect y="34.2" width="40" height="5.8"/></g><rect width="21" height="20" fill="#3C3B6E"/><g fill="#FFFFFF"><circle cx="5" cy="5" r="1.5"/><circle cx="11" cy="5" r="1.5"/><circle cx="17" cy="5" r="1.5"/><circle cx="8" cy="10" r="1.5"/><circle cx="14" cy="10" r="1.5"/><circle cx="5" cy="15" r="1.5"/><circle cx="11" cy="15" r="1.5"/><circle cx="17" cy="15" r="1.5"/></g></svg>`,
  CI: `<svg viewBox="0 0 40 40"><rect width="14" height="40" fill="#FF8200"/><rect x="13" width="14" height="40" fill="#FFFFFF"/><rect x="26" width="14" height="40" fill="#009A44"/></svg>`,
  SN: `<svg viewBox="0 0 40 40"><rect width="14" height="40" fill="#00853F"/><rect x="13" width="14" height="40" fill="#FDEF42"/><rect x="26" width="14" height="40" fill="#E31B23"/><path d="m20 14 1.6 4.4 4.6.1-3.7 2.8 1.4 4.4L20 23l-3.9 2.7 1.4-4.4-3.7-2.8 4.6-.1Z" fill="#00853F"/></svg>`,
  GA: `<svg viewBox="0 0 40 40"><rect width="40" height="14" fill="#009E60"/><rect y="13" width="40" height="14" fill="#FCD116"/><rect y="26" width="40" height="14" fill="#3A75C4"/></svg>`,
  CD: `<svg viewBox="0 0 40 40"><rect width="40" height="40" fill="#007FFF"/><path d="M-4 34 34 -4h14L10 48H-4Z" fill="#F7D618"/><path d="M-2 36 36 -2h8L6 46H-2Z" fill="#CE1021"/><path d="m9 5 1.4 3.8 4 .1-3.2 2.4 1.2 3.8L9 12.8 5.6 15.1l1.2-3.8L3.6 8.9l4-.1Z" fill="#F7D618"/></svg>`,
  BJ: `<svg viewBox="0 0 40 40"><rect width="16" height="40" fill="#008751"/><rect x="16" width="24" height="20" fill="#FCD116"/><rect x="16" y="20" width="24" height="20" fill="#E8112D"/></svg>`
};
function flagDisc(code, size = 28) {
  return `<span class="fl-flag" style="width:${size}px;height:${size}px">${FLAGS[code] || ""}</span>`;
}

/* barre d'état */
function statusBar(dark = false) {
  const c = dark ? "on-dark" : "";
  return `<div class="statusbar ${c}">
    <span class="sb-time">9:41</span>
    <span class="sb-icons">
      <svg width="17" height="11" viewBox="0 0 17 11" fill="currentColor" aria-hidden="true"><rect x="0" y="7" width="3" height="4" rx="1"/><rect x="4.6" y="5" width="3" height="6" rx="1"/><rect x="9.2" y="2.6" width="3" height="8.4" rx="1"/><rect x="13.8" y="0" width="3" height="11" rx="1"/></svg>
      <svg width="16" height="11" viewBox="0 0 16 11" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" aria-hidden="true"><path d="M1.5 3.9a10.6 10.6 0 0 1 13 0M4 6.6a7 7 0 0 1 8 0M6.5 9.2a3.4 3.4 0 0 1 3 0"/></svg>
      <svg width="25" height="12" viewBox="0 0 25 12" aria-hidden="true"><rect x=".5" y=".5" width="21" height="11" rx="3.2" fill="none" stroke="currentColor" stroke-opacity=".4"/><rect x="2" y="2" width="15" height="8" rx="1.8" fill="currentColor"/><path d="M23 4v4a2.2 2.2 0 0 0 0-4Z" fill="currentColor" fill-opacity=".4"/></svg>
    </span>
  </div>`;
}

/* ================================================================
   Modèle de données
   ================================================================ */
const now = Date.now();
const H = 3600 * 1000;
const uid = (() => { let n = 0; return () => "id" + (++n); })();

const CARD_THEMES = {
  sapin:  { fill: "#053826", ink: "#FFFFFF", accent: true,  label: "Sapin",  light: false },
  encre:  { fill: "#15181B", ink: "#F2F3F1", accent: false, label: "Encre",  light: false },
  ivoire: { fill: "#E7E9EC", ink: "#171C22", accent: false, label: "Argent", light: true  },
  terre:  { fill: "#9E4A2E", ink: "#F7EDE4", accent: false, label: "Terre",  light: false },
  cobalt: { fill: "#1E3C72", ink: "#EAEFF8", accent: false, label: "Cobalt", light: false },
  ardoise:{ fill: "#44525A", ink: "#EEF2F1", accent: false, label: "Ardoise",light: false }
};

const CATEGORIES = {
  streaming: { label: "Divertissement", icon: "play",  tint: "var(--cat-streaming)" },
  shopping:  { label: "Achats",         icon: "bag",   tint: "var(--cat-shopping)" },
  software:  { label: "Logiciels",      icon: "code",  tint: "var(--cat-software)" },
  food:      { label: "Restauration",   icon: "fork",  tint: "var(--cat-food)" },
  transport: { label: "Transport",      icon: "carIco",tint: "var(--cat-transport)" },
  travel:    { label: "Voyage",         icon: "plane", tint: "var(--cat-travel)" },
  ads:       { label: "Publicité",      icon: "mega",  tint: "var(--cat-ads)" },
  other:     { label: "Autre",          icon: "grid",  tint: "var(--cat-other)" }
};

const KIND_LABELS = { payment: "Paiement carte", topUp: "Rechargement", conversion: "Conversion", refund: "Remboursement", fee: "Frais", transfer: "Transfert" };
const STATUS_META = {
  approved: { label: "Réussi",     cls: "credit", icon: "check" },
  pending:  { label: "En attente", cls: "pend",   icon: "clock" },
  declined: { label: "Refusé",     cls: "debit",  icon: "x" },
  refunded: { label: "Remboursé",  cls: "neutral",icon: "undo" }
};

const S = {
  user: { first: "Aristide", last: "Mbassi", phone: "+237 6 99 12 34 56", email: "aristide@moneypay.cm", kyc: true },
  balance: 428500,
  fx: { rate: 610, margin: 0.03 },
  hidden: false,
  cards: [
    { id: "c1", label: "Abonnements", theme: "sapin", network: "mastercard", pan: "5399471028834412", cvv: "417", exp: "09/29", frozen: false, limit: 15000, spent: 4780, singleUse: false, online: true, subs: true, declines: 0, created: now - 120 * 24 * H },
    { id: "c2", label: "Shopping en ligne", theme: "ivoire", network: "visa", pan: "4539118820047761", cvv: "882", exp: "03/28", frozen: false, limit: 50000, spent: 31240, singleUse: false, online: true, subs: true, declines: 1, created: now - 45 * 24 * H },
    { id: "c3", label: "Serveurs & outils", theme: "encre", network: "mastercard", pan: "5399002914775530", cvv: "205", exp: "11/27", frozen: true, limit: null, spent: 12900, singleUse: false, online: true, subs: false, declines: 2, created: now - 210 * 24 * H }
  ],
  txs: [
    { id: "t1",  merchant: "Netflix",          logo: "netflix",      kind: "payment", cat: "streaming", status: "approved", date: now - 3 * H,     usd: -1099, xaf: -6907,  card: "c1" },
    { id: "t2",  merchant: "MTN Mobile Money", logo: "mtn",          kind: "topUp",   cat: "other",     status: "approved", date: now - 6 * H,     usd: 0,     xaf: 98500,  card: null },
    { id: "t3",  merchant: "OpenAI",           logo: "openai",       kind: "payment", cat: "software",  status: "approved", date: now - 9 * H,     usd: -2000, xaf: -12566, card: "c2" },
    { id: "t4",  merchant: "Amazon",           logo: "amazon",       kind: "payment", cat: "shopping",  status: "declined", date: now - 26 * H,    usd: -8499, xaf: 0,      card: "c2", reason: "Solde insuffisant au moment de l’autorisation" },
    { id: "t5",  merchant: "Frais de refus",   logo: null, icon: "warn", kind: "fee", cat: "other",     status: "approved", date: now - 26.1 * H,  usd: -35,   xaf: -220,   card: "c2" },
    { id: "t6",  merchant: "Spotify",          logo: "spotify",      kind: "payment", cat: "streaming", status: "approved", date: now - 30 * H,    usd: -1199, xaf: -7535,  card: "c1" },
    { id: "t7",  merchant: "Uber",             logo: "uber",         kind: "payment", cat: "transport", status: "approved", date: now - 52 * H,    usd: -1540, xaf: -9678,  card: "c2" },
    { id: "t8",  merchant: "Figma",            logo: "figma",        kind: "payment", cat: "software",  status: "pending",  date: now - 55 * H,    usd: -1500, xaf: -9427,  card: "c3" },
    { id: "t9",  merchant: "Meta Ads",         logo: "meta",         kind: "payment", cat: "ads",       status: "approved", date: now - 74 * H,    usd: -5000, xaf: -31415, card: "c2" },
    { id: "t10", merchant: "Orange Money",     logo: "orange",       kind: "topUp",   cat: "other",     status: "approved", date: now - 80 * H,    usd: 0,     xaf: 150000, card: null },
    { id: "t11", merchant: "Booking.com",      logo: "booking",      kind: "refund",  cat: "travel",    status: "refunded", date: now - 98 * H,    usd: 4320,  xaf: 27143,  card: "c2" },
    { id: "t12", merchant: "Glovo",            logo: "glovo",        kind: "payment", cat: "food",      status: "approved", date: now - 120 * H,   usd: -890,  xaf: -5593,  card: "c1" },
    { id: "t13", merchant: "DigitalOcean",     logo: "digitalocean", kind: "payment", cat: "software",  status: "approved", date: now - 146 * H,   usd: -2400, xaf: -15080, card: "c3" },
    { id: "t14", merchant: "AliExpress",       logo: "aliexpress",   kind: "payment", cat: "shopping",  status: "approved", date: now - 170 * H,   usd: -3265, xaf: -20515, card: "c2" }
  ],
  methods: [
    { id: "mtn",   name: "MTN Mobile Money",  detail: "•• 34 56 · instantané",           logo: "mtn",    fee: 0.015, instant: true },
    { id: "om",    name: "Orange Money",      detail: "•• 78 90 · instantané",           logo: "orange", fee: 0.015, instant: true },
    { id: "bank",  name: "Virement bancaire", detail: "Afriland First Bank · 1 à 2 jours", icon: "bank",  fee: 0,     instant: false },
    { id: "agent", name: "Agent MoniPay",    detail: "Dépôt en espèces · instantané",   icon: "store",  fee: 0.02,  instant: true }
  ],
  notifs: [
    { id: "n1", title: "Paiement autorisé",  body: "Netflix · 10,99 $ débités de « Abonnements »",                    date: now - 3 * H,  icon: "check", cls: "credit", unread: true },
    { id: "n2", title: "Rechargement reçu",  body: "98 500 F reçus depuis MTN Mobile Money",                          date: now - 6 * H,  icon: "arrDn", cls: "credit", unread: true },
    { id: "n3", title: "Paiement refusé",    body: "Amazon · solde insuffisant. 2 refus restants avant blocage.",     date: now - 26 * H, icon: "x",     cls: "debit",  unread: true },
    { id: "n4", title: "Carte gelée",        body: "« Serveurs & outils » a été gelée depuis l’application.",         date: now - 40 * H, icon: "snow",  cls: "neutral",unread: false },
    { id: "n5", title: "Taux du jour",       body: "1 USD = 610 F · marge MoniPay 3 %",                              date: now - 70 * H, icon: "swap",  cls: "neutral",unread: false }
  ],
  contacts: [
    { name: "Nadège Ateba",  phone: "+237 6 77 21 09 44" },
    { name: "Serge Kamdem",  phone: "+237 6 91 55 30 12" },
    { name: "Laure Ngo Bell",phone: "+237 6 55 88 74 21" },
    { name: "Boutique Akwa", phone: "+237 6 70 12 45 63" },
    { name: "Yannick Fotso", phone: "+237 6 96 33 18 05" }
  ],
  subs: [
    { name: "Netflix",      logo: "netflix",      usd: 1099, day: "le 3 de chaque mois",  active: true },
    { name: "Spotify",      logo: "spotify",      usd: 1199, day: "le 12 de chaque mois", active: true },
    { name: "OpenAI",       logo: "openai",       usd: 2000, day: "le 1er de chaque mois",active: true },
    { name: "Figma",        logo: "figma",        usd: 1500, day: "le 18 de chaque mois", active: true },
    { name: "DigitalOcean", logo: "digitalocean", usd: 2400, day: "le 24 de chaque mois", active: false }
  ],
  months: [
    { label: "mars",  xaf: 62400 }, { label: "avr.",  xaf: 88100 }, { label: "mai",   xaf: 74900 },
    { label: "juin",  xaf: 121300 },{ label: "juil.", xaf: 96700 }, { label: "août",  xaf: 108500, current: true }
  ],
  faq: [
    { q: "Pourquoi mon paiement a-t-il été refusé ?", a: "Le plus souvent, votre solde FCFA ne couvrait pas le montant converti au moment de l’autorisation. Rechargez puis réessayez. Chaque refus est facturé 220 F par le processeur." },
    { q: "Combien de temps pour recharger en Mobile Money ?", a: "MTN MoMo et Orange Money créditent votre wallet en quelques secondes. Un virement bancaire prend 1 à 2 jours ouvrés." },
    { q: "Ma carte marche-t-elle sur tous les sites ?", a: "Partout où Visa et Mastercard sont acceptés en ligne. Les marchands qui exigent une empreinte de caution — location de voiture, hôtels — refusent les cartes virtuelles." },
    { q: "Quel taux de change est appliqué ?", a: "Le taux interbancaire du jour, majoré de 3 % de marge MoniPay. Le détail est affiché avant chaque conversion et sur chaque reçu." },
    { q: "Que se passe-t-il si je gèle une carte ?", a: "Toutes les autorisations sont refusées immédiatement. Les abonnements en cours échoueront tant que la carte reste gelée. Vous pouvez la dégeler à tout moment." }
  ]
};

/* ================================================================
   Formatage
   ================================================================ */
const NBSP = " ";
function grp(n) {
  const s = String(Math.trunc(Math.abs(n)));
  return s.replace(/\B(?=(\d{3})+(?!\d))/g, NBSP);
}
function fmtXAF(n, { sign = false, unit = true } = {}) {
  const sg = sign && n > 0 ? "+" : n < 0 ? "−" : "";
  return sg + grp(n) + (unit ? NBSP + "F" : "");
}
function usdParts(cents) {
  return [grp(Math.abs(cents) / 100 | 0), String(Math.abs(cents) % 100).padStart(2, "0")];
}
function fmtUSD(cents, { sign = false } = {}) {
  const [w, f] = usdParts(cents);
  const sg = sign && cents > 0 ? "+" : cents < 0 ? "−" : "";
  return sg + w + "," + f + NBSP + "$";
}
/* montant composé : centimes en exposant, unité discrète */
function moneyXAF(n, size = 34, { sign = false, color = "" } = {}) {
  const sg = sign && n > 0 ? "+" : n < 0 ? "−" : "";
  return `<span class="money display" style="font-size:${size}px;${color ? "color:" + color : ""}"><span>${sg}${grp(n)}</span><span class="m-unit">FCFA</span></span>`;
}
function moneyUSD(cents, size = 34, { sign = false, color = "" } = {}) {
  const [w, f] = usdParts(cents);
  const sg = sign && cents > 0 ? "+" : cents < 0 ? "−" : "";
  return `<span class="money display" style="font-size:${size}px;${color ? "color:" + color : ""}"><span class="m-cur">${sg}$</span><span>${w}</span><span class="m-frac">${f}</span></span>`;
}
function usdToXAF(cents) { return Math.ceil(cents / 100 * S.fx.rate * (1 + S.fx.margin)); }
function xafToUSD(xaf) { return Math.floor(xaf / (S.fx.rate * (1 + S.fx.margin)) * 100); }

const MONTHS_FR = ["janvier","février","mars","avril","mai","juin","juillet","août","septembre","octobre","novembre","décembre"];
const DAYS_FR = ["dimanche","lundi","mardi","mercredi","jeudi","vendredi","samedi"];
function relDay(ts) {
  const d = new Date(ts), t = new Date();
  const day0 = x => new Date(x.getFullYear(), x.getMonth(), x.getDate()).getTime();
  const diff = (day0(t) - day0(d)) / 864e5;
  if (diff === 0) return "Aujourd’hui";
  if (diff === 1) return "Hier";
  const s = `${DAYS_FR[d.getDay()]} ${d.getDate()} ${MONTHS_FR[d.getMonth()]}`;
  return s[0].toUpperCase() + s.slice(1);
}
function fmtTime(ts) { const d = new Date(ts); return `${d.getHours()}${NBSP}h${NBSP}${String(d.getMinutes()).padStart(2, "0")}`; }
function fmtFull(ts) { const d = new Date(ts); return `${d.getDate()} ${MONTHS_FR[d.getMonth()]} ${d.getFullYear()} à ${fmtTime(ts)}`; }
function relShort(ts) {
  const h = Math.round((now - ts) / H);
  if (h < 1) return "à l’instant";
  if (h < 24) return `il y a ${h}${NBSP}h`;
  const d = Math.round(h / 24);
  return d === 1 ? "hier" : `il y a ${d}${NBSP}j`;
}
function esc(s) { return String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;"); }

/* dérivés */
const cardById = id => S.cards.find(c => c.id === id);
const txById = id => S.txs.find(t => t.id === id);
const unreadCount = () => S.notifs.filter(n => n.unread).length;
function txsOfCard(id) { return S.txs.filter(t => t.card === id); }
function groupByDay(txs) {
  const map = new Map();
  for (const t of txs) {
    const d = new Date(t.date);
    const k = new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
    if (!map.has(k)) map.set(k, []);
    map.get(k).push(t);
  }
  return [...map.entries()].sort((a, b) => b[0] - a[0])
    .map(([day, items]) => ({ day: +day, items: items.sort((a, b) => b.date - a.date) }));
}
function spendByCat() {
  const res = {};
  for (const t of S.txs) {
    if (t.xaf < 0 && t.status !== "declined") res[t.cat] = (res[t.cat] || 0) + (-t.xaf);
  }
  return Object.entries(res).map(([cat, xaf]) => ({ cat, xaf })).sort((a, b) => b.xaf - a.xaf);
}
function monthTotals() {
  let inn = 0, out = 0;
  for (const t of S.txs) {
    if (t.status === "declined") continue;
    if (t.xaf > 0) inn += t.xaf; else out += -t.xaf;
  }
  return { inn, out };
}

/* ================================================================
   Composants partagés
   ================================================================ */
function guilloche(color, size = 200) {
  return `<svg viewBox="0 0 200 200" width="${size}" height="${size}" fill="none" stroke="${color}" aria-hidden="true">
    <circle cx="100" cy="100" r="28"/><circle cx="100" cy="100" r="46"/><circle cx="100" cy="100" r="64"/><circle cx="100" cy="100" r="82"/>
    <circle cx="82" cy="100" r="55"/><circle cx="118" cy="100" r="55"/><circle cx="100" cy="82" r="55"/><circle cx="100" cy="118" r="55"/>
    <circle cx="87" cy="87" r="55"/><circle cx="113" cy="113" r="55"/><circle cx="87" cy="113" r="55"/><circle cx="113" cy="87" r="55"/>
  </svg>`;
}

function chunkPan(pan) { return pan.replace(/(.{4})/g, "$1 ").trim(); }

/* carte virtuelle — l’objet central du produit */
function vcardHTML(card, { compact = false, revealed = false, holder = null } = {}) {
  const th = CARD_THEMES[card.theme];
  const fs = compact ? .72 : 1;
  const accentInk = th.accent ? "#3CDD9B" : th.ink;
  const netInk = th.light ? "#1434CB" : th.ink;
  const front = `
    <div class="vc-inner">
      <div class="vc-top">
        <span class="vc-brand" style="font-size:${13 * fs + 1}px;color:${th.ink}">
          ${logoMark(compact ? 15 : 19, th.accent ? "rgba(60,221,155,.16)" : (th.light ? "rgba(18,22,28,.08)" : "rgba(255,255,255,.1)"), accentInk, 0.3)}
          MoniPay
        </span>
        <span class="vc-virtual" style="font-size:${8.4 * fs + .8}px;color:${th.ink}">Virtuelle</span>
      </div>
      <div class="vc-label" style="font-size:${14 * fs + 1}px;color:${th.ink}">${esc(card.label)}</div>
      <div class="vc-pan" style="font-size:${12 * fs + .8}px;color:${th.ink}">${revealed && !compact ? chunkPan(card.pan) : "••••" + NBSP + card.pan.slice(-4)}</div>
      <div class="vc-bottom">
        <span class="vc-exp" style="color:${th.ink};${revealed && !compact ? "" : "visibility:hidden"}">EXP ${card.exp}</span>
        ${netMark(card.network, netInk, compact ? 16 : 21)}
      </div>
    </div>`;
  const frozen = card.frozen ? `<div class="vc-frozen">${ico("snow", 20)}Gelée</div>` : "";
  const gl = th.accent ? `<div class="vc-guilloche">${guilloche("rgba(60,221,155,.13)")}</div>` : "";
  return `<div class="vcard ${th.light ? "light-art" : ""}" style="background:${th.fill};border-radius:${compact ? 12 : 16}px">${gl}${front}${frozen}</div>`;
}
/* dos de carte pour la révélation */
function vcardBackHTML(card, holder) {
  const th = CARD_THEMES[card.theme];
  return `<div class="vcard ${th.light ? "light-art" : ""}" style="background:${th.fill}">
    <div class="vc-inner">
      <div class="vc-magstripe"></div>
      <div class="vc-secret-row" style="color:${th.ink}">
        <span class="vc-secret"><span class="s-l">Titulaire</span><span class="s-v" style="display:block">${esc(holder)}</span></span>
      </div>
      <div class="vc-secret-row" style="color:${th.ink};margin-top:12px">
        <span class="vc-secret"><span class="s-l">Numéro</span><span class="s-v" style="display:block">${chunkPan(card.pan)}</span></span>
      </div>
      <div class="vc-secret-row" style="color:${th.ink};margin-top:12px;margin-bottom:2px">
        <span class="vc-secret"><span class="s-l">Expire</span><span class="s-v" style="display:block">${card.exp}</span></span>
        <span class="vc-secret"><span class="s-l">CVV</span><span class="s-v" style="display:block">${card.cvv}</span></span>
        <span style="margin-left:auto;align-self:flex-end">${netMark(card.network, th.light ? "#1434CB" : th.ink, 18)}</span>
      </div>
    </div>
  </div>`;
}

function navBar({ back = null, close = null, title = "", right = "" } = {}) {
  let left = "";
  if (back) left = `<button type="button" class="nb-btn" data-act="${back}" aria-label="Retour">${ico("chevL", 22)}</button>`;
  else if (close) left = `<button type="button" class="nb-btn" data-act="${close}" aria-label="Fermer">${ico("x", 21)}</button>`;
  return `<div class="navbar">${left}<span class="nb-title">${esc(title)}</span><span class="nb-spacer"></span>${right}</div>`;
}

function sectionHead(title, { count = null, link = null, act = null, chev = false } = {}) {
  return `<div class="section-head gutter" ${act ? `data-act="${act}" role="button"` : ""}>
    <span class="t-section">${title}</span>
    ${chev ? `<span class="sh-chev">${ico("chevR", 15)}</span>` : ""}
    <span class="sh-fill"></span>
    ${count != null ? `<span class="sh-count">${count}</span>` : ""}
    ${link ? `<span class="sh-link">${link}</span>` : ""}
  </div>`;
}

function eyebrow(text, cls = "") { return `<div class="eyebrow ${cls}">${text}</div>`; }

function txTint(t) {
  if (t.kind === "topUp") return "var(--credit)";
  if (t.kind === "fee") return "var(--pend)";
  return CATEGORIES[t.cat].tint;
}
function txLeadTile(t, size = 40) {
  if (t.logo) return logoTile(t.logo, size);
  const tint = txTint(t);
  return `<span class="icon-tile" style="width:${size}px;height:${size}px;background:color-mix(in srgb, ${tint} 13%, var(--paper));color:${tint}">${ico(t.icon || CATEGORIES[t.cat].icon, 18)}</span>`;
}
function statusPill(status) {
  const m = STATUS_META[status];
  return `<span class="pill ${m.cls}">${ico(m.icon, 11)}${m.label}</span>`;
}

function txRow(t, act = "openTx") {
  const amount = t.status === "declined"
    ? `<span style="font-size:15px;font-weight:500;color:var(--ink-3)">—</span>`
    : `<span class="tnum" style="font-size:15px;font-weight:500;${t.xaf > 0 ? "color:var(--credit)" : ""}">${fmtXAF(t.xaf, { sign: true, unit: false })}</span>`;
  const usd = t.usd !== 0 ? `<div class="tnum" style="font-size:12px;color:var(--ink-3);margin-top:2px">${fmtUSD(t.usd)}</div>` : "";
  return `<button type="button" class="row gutter" data-act="${act}" data-arg="${t.id}">
    ${txLeadTile(t)}
    <span class="r-body">
      <span class="r-title"><span class="rt-text">${esc(t.merchant)}</span>${t.status !== "approved" ? statusPill(t.status) : ""}</span>
      <span class="r-sub" style="display:block">${KIND_LABELS[t.kind]} · ${fmtTime(t.date)}</span>
    </span>
    <span style="text-align:right">${amount}${usd}</span>
  </button>`;
}

function listRow({ icon = null, lead = "", title, sub = null, value = null, pillHTML = "", chevron = false, act = null, arg = "", destructive = false, right = "" }) {
  const leadHTML = lead || (icon ? `<span class="icon-tile">${ico(icon, 18)}</span>` : "");
  const inner = `${leadHTML}
    <span class="r-body">
      <span class="r-title"><span class="rt-text">${title}</span>${pillHTML}</span>
      ${sub ? `<span class="r-sub" style="display:block">${sub}</span>` : ""}
    </span>
    ${value ? `<span class="r-value tnum">${value}</span>` : ""}${right}
    ${chevron ? `<span class="r-chevron">${ico("chevR", 15)}</span>` : ""}`;
  if (act) return `<button type="button" class="row gutter ${destructive ? "destructive" : ""}" data-act="${act}" data-arg="${arg}">${inner}</button>`;
  return `<div class="row static gutter ${destructive ? "destructive" : ""}">${inner}</div>`;
}

function toggleRow({ icon, title, key, act = "toggleFlag" }) {
  const on = getFlag(key);
  return `<div class="row static gutter">
    <span class="icon-tile">${ico(icon, 18)}</span>
    <span class="r-body"><span class="r-title"><span class="rt-text">${title}</span></span></span>
    <button type="button" class="toggle ${on ? "on" : ""}" data-act="${act}" data-arg="${key}" role="switch" aria-checked="${on}" aria-label="${title}"></button>
  </div>`;
}
const FLAGS_STATE = { faceid: true, push: true, twofa: true, confirmEach: false };
function getFlag(k) { return !!FLAGS_STATE[k]; }

function kvRow(label, value, { strong = false, tint = "", mono = false, copy = null } = {}) {
  const v = `<span class="kv-v tnum ${strong ? "strong" : ""} ${mono ? "mono" : ""}" style="${tint ? "color:" + tint : ""}">${value}</span>`;
  if (copy) return `<button type="button" class="kv copyable gutter" style="width:100%" data-act="copyText" data-arg="${esc(copy)}"><span class="kv-l">${label}</span><span class="kv-fill"></span>${v}<span style="color:var(--ink-3)">${ico("copy", 14)}</span></button>`;
  return `<div class="kv gutter"><span class="kv-l">${label}</span><span class="kv-fill"></span>${v}</div>`;
}
function rule(inset = false) { return `<div class="rule ${inset ? "inset" : ""}"></div>`; }

function meter(value, { warn = false } = {}) {
  const w = Math.max(2, Math.min(100, value * 100));
  return `<div class="meter"><div class="m-fill ${warn ? "warn" : ""}" style="width:${w}%"></div></div>`;
}

function keypad(act, { side = null, sideAct = "" } = {}) {
  const key = d => `<button type="button" class="kp" data-act="${act}" data-arg="${d}">${d}</button>`;
  let corner = `<span></span>`;
  if (side === "faceid") corner = `<button type="button" class="kp fn" data-act="${sideAct}" aria-label="Face ID">${ico("faceid", 24)}</button>`;
  return `<div class="keypad">
    ${key(1)}${key(2)}${key(3)}${key(4)}${key(5)}${key(6)}${key(7)}${key(8)}${key(9)}
    ${corner}${key(0)}
    <button type="button" class="kp fn" data-act="${act}" data-arg="del" aria-label="Effacer">${ico("backspace", 23)}</button>
  </div>`;
}

function btn(label, act, { style = "primary", arg = "", disabled = false, icon = null, loading = false, id = "" } = {}) {
  return `<button type="button" ${id ? `id="${id}"` : ""} class="btn ${style} ${disabled ? "disabled" : ""}" data-act="${act}" data-arg="${arg}">
    ${loading ? `<span class="spinner"></span>` : icon ? ico(icon, 18) : ""}${label}
  </button>`;
}

function quickAction(icon, label, act, arg = "") {
  return `<button type="button" class="qa" data-act="${act}" data-arg="${arg}">
    <span class="qa-disc">${ico(icon, 22)}</span>
    <span class="qa-label">${label}</span>
  </button>`;
}

function segments(items, sel, act) {
  return `<div class="segments" role="tablist">${items.map((it, i) =>
    `<button type="button" role="tab" aria-selected="${i === sel}" class="seg ${i === sel ? "on" : ""}" data-act="${act}" data-arg="${i}">${it}</button>`).join("")}</div>`;
}

function miniCardChip(card) {
  const th = CARD_THEMES[card.theme];
  return `<span class="mini-card" style="background:${th.fill}"></span>`;
}

function emptyNote(title, msg, actionLabel = null, act = null) {
  return `<div class="empty-note gutter">
    <div class="en-t">${title}</div>
    <div class="en-m">${msg}</div>
    ${actionLabel ? `<div style="margin-top:14px">${btn(actionLabel, act, { style: "quiet small" })}</div>` : ""}
  </div>`;
}

/* ================================================================
   Infrastructure — routeur, feuilles, dialogues, toasts
   ================================================================ */
const phone = document.getElementById("phone");
let PHASE = "splash";
let TAB = "home";
const STACKS = { home: [], cards: [], activity: [], insights: [], profile: [] };
let SHEET = null;          // { kind, el }
let DIALOG = null;
let timers = [];
function after(ms, fn) { const t = setTimeout(fn, ms); timers.push(t); return t; }
function every(ms, fn) { const t = setInterval(fn, ms); timers.push(t); return t; }
function clearTimers() { timers.forEach(t => { clearTimeout(t); clearInterval(t); }); timers = []; }

/* état éphémère des flux */
let F = {};

function setPhase(p, extra = {}) {
  clearTimers(); F = { ...extra };
  PHASE = p; SHEET = null; DIALOG = null;
  renderPhone(p === "main" ? "tab" : undefined); renderRail();
}
function setTab(t) {
  if (PHASE !== "main") { setPhase("main"); }
  clearTimers();
  TAB = t;
  renderPhone("tab"); renderRail();
}
function pushScreen(screen, params = {}) {
  STACKS[TAB].push({ screen, params });
  renderPhone("push"); renderRail();
}
function popScreen() {
  const stack = STACKS[TAB];
  if (!stack.length) return;
  const appEl = phone.querySelector(".app");
  const top = appEl && appEl.querySelector(".stack-view[data-top='1']");
  if (top && !matchMedia("(prefers-reduced-motion: reduce)").matches) {
    top.classList.add("popping");
    const under = appEl.querySelector(".stack-view[data-under='1']");
    if (under) { under.style.display = ""; under.classList.add("under-pop"); }
    setTimeout(() => { stack.pop(); renderPhone(); renderRail(); }, 240);
  } else {
    stack.pop(); renderPhone(); renderRail();
  }
}
function resetStacks() { for (const k in STACKS) STACKS[k] = []; }

function openSheet(kind, params = {}) {
  clearTimers();
  F = { ...params };
  SHEET = { kind };
  renderPhone(); renderRail();
  requestAnimationFrame(() => requestAnimationFrame(() => {
    const el = phone.querySelector(".sheet"); const dim = phone.querySelector(".sheet-dim");
    if (el) el.classList.add("show");
    if (dim) dim.classList.add("show");
  }));
}
function closeSheet() {
  clearTimers();
  const el = phone.querySelector(".sheet"); const dim = phone.querySelector(".sheet-dim");
  if (el) { el.classList.remove("show"); if (dim) dim.classList.remove("show"); after(280, () => { SHEET = null; renderPhone(); renderRail(); }); }
  else { SHEET = null; renderPhone(); renderRail(); }
}
function rerenderSheet() {
  const el = phone.querySelector(".sheet");
  if (el && SHEET) { el.innerHTML = sheetContent(SHEET.kind); }
}

function showDialog(d) {
  DIALOG = d;
  const host = phone.querySelector(".dialog-host");
  if (host) { host.innerHTML = dialogHTML(); requestAnimationFrame(() => requestAnimationFrame(() => {
    const as = host.querySelector(".action-sheet"); const dim = host.querySelector(".sheet-dim");
    if (as) as.classList.add("show"); if (dim) dim.classList.add("show");
  })); }
}
function closeDialog() {
  const host = phone.querySelector(".dialog-host");
  const as = host && host.querySelector(".action-sheet"); const dim = host && host.querySelector(".sheet-dim");
  if (as) { as.classList.remove("show"); if (dim) dim.classList.remove("show"); setTimeout(() => { DIALOG = null; if (host) host.innerHTML = ""; }, 280); }
  else DIALOG = null;
}
function dialogHTML() {
  if (!DIALOG) return "";
  const d = DIALOG;
  return `<div class="sheet-dim" data-act="dialogCancel" style="z-index:95"></div>
  <div class="action-sheet">
    <div class="as-box">
      <div class="as-head"><div class="as-title">${d.title}</div>${d.message ? `<div class="as-msg">${d.message}</div>` : ""}</div>
      ${d.actions.map((a, i) => `<button type="button" class="as-btn ${a.destructive ? "destructive" : ""}" data-act="dialogAction" data-arg="${i}">${a.label}</button>`).join("")}
    </div>
    <div class="as-cancel"><button type="button" class="as-btn" data-act="dialogCancel" style="color:var(--ink);font-weight:600">Annuler</button></div>
  </div>`;
}

let toastTimer = null;
function showToast(text, icon = "check") {
  let el = phone.querySelector(".toast");
  if (!el) { el = document.createElement("div"); el.className = "toast"; phone.appendChild(el); }
  el.innerHTML = `${ico(icon, 15)}${text}`;
  el.classList.add("show");
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => el.classList.remove("show"), 1700);
}
function copyText(text) {
  if (navigator.clipboard) navigator.clipboard.writeText(text).catch(() => {});
  showToast("Copié", "copy");
}

/* ================================================================
   Écrans — parcours d'entrée
   ================================================================ */
const COUNTRIES = [
  { id: "CM", name: "Cameroun",       dial: "+237", len: 9 },
  { id: "CI", name: "Côte d’Ivoire",  dial: "+225", len: 10 },
  { id: "SN", name: "Sénégal",        dial: "+221", len: 9 },
  { id: "GA", name: "Gabon",          dial: "+241", len: 8 },
  { id: "CD", name: "RD Congo",       dial: "+243", len: 9 },
  { id: "BJ", name: "Bénin",          dial: "+229", len: 8 }
];
function fmtPhoneDigits(d) {
  let out = "";
  for (let i = 0; i < d.length; i++) { if (i > 0 && i % 2 === 1 && i < 9) out += " "; out += d[i]; }
  return out;
}

function splashScreen() {
  after(1500, () => setPhase("welcome"));
  return `<div class="layer dark-bg on-green">
    ${statusBar(true)}
    <div style="flex:1;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:16px">
      ${logoMark(60, "rgba(255,255,255,.12)", "#FFFFFF", 0.26)}
      <div class="display" style="font-size:23px;color:var(--deep-ink)">MoniPay</div>
    </div>
    <div style="text-align:center;padding-bottom:46px;font-size:12px;color:rgba(255,255,255,.45)">Établissement de paiement agréé · zone CEMAC</div>
    <div class="homebar on-dark"></div>
  </div>`;
}

const WELCOME_SLIDES = [
  { title: "Une carte par usage,<br>créée en 30 secondes", body: "Visa ou Mastercard, son propre plafond, gelée d’un geste. Autant de cartes que de besoins.", card: { label: "Abonnements", theme: "sapin", network: "mastercard" } },
  { title: "Rechargée en<br>Mobile Money", body: "MTN MoMo, Orange Money ou dépôt agent. Votre solde FCFA finance chaque paiement en dollars.", card: { label: "Shopping", theme: "ivoire", network: "visa" } },
  { title: "Acceptée partout<br>en ligne", body: "Netflix, OpenAI, AliExpress. Le taux et la marge sont affichés avant chaque conversion.", card: { label: "Serveurs", theme: "encre", network: "mastercard" } }
];
function welcomeScreen() {
  const i = F.slide || 0;
  return `<div class="layer">
    ${statusBar()}
    <div class="navbar" style="padding:0 var(--gutter)">
      <span style="display:flex;align-items:center;gap:8px;font-weight:600;font-size:16px;letter-spacing:-.01em">${logoMark(24, "var(--green)", "#FFFFFF")}MoniPay</span>
      <span class="nb-spacer"></span>
      <button type="button" class="nb-text" data-act="goLock">Se connecter</button>
    </div>
    <div class="slides" id="slides" data-act-scroll="slides">
      ${WELCOME_SLIDES.map(s => `<div class="slide">
        <div style="max-width:290px">${vcardHTML({ ...s.card, pan: "5399471028834412", frozen: false }, {})}</div>
        <div class="t-page" style="margin-top:30px">${s.title}</div>
        <div style="font-size:15px;line-height:1.5;color:var(--ink-2);margin-top:10px;max-width:320px;text-wrap:pretty">${s.body}</div>
      </div>`).join("")}
    </div>
    <div class="gutter" style="padding-bottom:20px">
      <div class="welcome-dots" style="margin-bottom:20px">${WELCOME_SLIDES.map((_, j) => `<span class="wd ${j === i ? "on" : ""}"></span>`).join("")}</div>
      ${btn("Créer mon compte", "goSignup")}
      <div style="text-align:center;font-size:12px;color:var(--ink-3);margin-top:12px">Cartes émises par notre banque partenaire agréée.</div>
    </div>
    <div class="homebar"></div>
  </div>`;
}

function signupScreen() {
  const step = F.step || 0;
  const total = 5;
  const header = `<div class="navbar" style="gap:14px;padding:0 16px">
    <button type="button" class="nb-btn" data-act="signupBack" style="${step === 0 ? "opacity:0;pointer-events:none" : ""}" aria-label="Retour">${ico("chevL", 21)}</button>
    <div class="seg-progress">${Array.from({ length: total }, (_, i) => `<span class="sp ${i <= step ? "done" : ""}"></span>`).join("")}</div>
    <span style="width:38px"></span>
  </div>`;
  let body = "";
  if (step === 0) body = signupPhone();
  else if (step === 1) body = signupOTP();
  else if (step === 2) body = signupPasscode();
  else if (step === 3) body = signupBiometric();
  else body = signupProfile();
  return `<div class="layer">${statusBar()}${header}${body}<div class="homebar"></div></div>`;
}

function signupPhone() {
  const c = COUNTRIES.find(x => x.id === (F.country || "CM"));
  const d = F.phoneDigits || "";
  const valid = d.length === c.len;
  return `<div style="flex:1;display:flex;flex-direction:column;min-height:0" class="gutter">
    <div class="t-page" style="margin-top:8px">Quel est votre<br>numéro ?</div>
    <div style="font-size:15px;line-height:1.5;color:var(--ink-2);margin-top:10px;text-wrap:pretty">Un code à six chiffres part sur cette ligne. C’est aussi elle qui recevra vos rechargements Mobile Money.</div>
    <div style="display:flex;align-items:center;gap:0;margin-top:28px;padding-bottom:13px;border-bottom:1.5px solid ${valid ? "var(--ink)" : "var(--rule)"}">
      <button type="button" style="display:flex;align-items:center;gap:7px;padding-right:13px" data-act="openCountries">
        ${flagDisc(c.id, 24)}
        <span class="tnum" style="font-size:17px;font-weight:500">${c.dial}</span>
        ${ico("chevD", 12, "")}
      </button>
      <span style="width:1px;height:22px;background:var(--rule)"></span>
      <span class="tnum" style="font-size:20px;font-weight:500;padding-left:13px;color:${d ? "var(--ink)" : "var(--ink-3)"};letter-spacing:.02em">${d ? fmtPhoneDigits(d) : "6 XX XX XX XX"}</span>
    </div>
    <div style="flex:1"></div>
    ${keypad("phoneKey")}
    <div style="padding:8px 0 6px">${btn("Recevoir le code", "signupNext", { disabled: !valid })}</div>
    <div class="note-micro" style="padding-bottom:8px">En continuant, vous acceptez les conditions générales et la politique de confidentialité de MoniPay.</div>
  </div>`;
}

function signupOTP() {
  const code = F.otp || "";
  const secs = F.otpSecs != null ? F.otpSecs : 42;
  if (!F.otpTimerOn) {
    F.otpTimerOn = true; F.otpSecs = secs;
    every(1000, () => {
      if (F.otpSecs > 0) { F.otpSecs--; const el = phone.querySelector("#otp-resend"); if (el) el.textContent = F.otpSecs > 0 ? `Renvoyer le code dans ${F.otpSecs} s` : ""; if (F.otpSecs === 0) renderPhone(); }
    });
  }
  return `<div style="flex:1;display:flex;flex-direction:column;min-height:0" class="gutter">
    <div class="t-page" style="margin-top:8px">Entrez le code</div>
    <div style="font-size:15px;color:var(--ink-2);margin-top:10px" class="tnum">Envoyé au +237 ${fmtPhoneDigits(F.phoneDigits || "699123456")}</div>
    <div class="otp-boxes" style="margin-top:30px">${Array.from({ length: 6 }, (_, i) =>
      `<span class="ob ${i === code.length ? "cursor" : ""}">${code[i] || ""}</span>`).join("")}</div>
    <div style="margin-top:18px;min-height:20px">
      ${secs > 0
        ? `<span id="otp-resend" class="tnum" style="font-size:13.5px;color:var(--ink-2)">Renvoyer le code dans ${secs} s</span>`
        : `<button type="button" style="font-size:13.5px;font-weight:500;color:var(--accent)" data-act="otpResend">Renvoyer le code</button>`}
    </div>
    <div style="flex:1"></div>
    ${keypad("otpKey")}
    <div style="padding:8px 0 10px">${btn(F.verifying ? "Vérification" : "Vérifier", "otpVerify", { disabled: code.length < 6, loading: !!F.verifying })}</div>
  </div>`;
}

function signupPasscode() {
  const confirming = (F.pass1 || "").length === 4;
  const cur = confirming ? (F.pass2 || "") : (F.pass1 || "");
  return `<div style="flex:1;display:flex;flex-direction:column;min-height:0;align-items:center" class="gutter">
    <div class="t-page" style="margin-top:22px;text-align:center;font-size:24px">${confirming ? "Confirmez votre code" : "Créez un code secret"}</div>
    <div style="font-size:13.5px;line-height:1.5;color:${F.passError ? "var(--debit)" : "var(--ink-2)"};margin-top:8px;text-align:center;max-width:290px;text-wrap:pretty">
      ${F.passError ? "Les codes ne correspondent pas. Recommencez." : "Il déverrouille l’application et valide vos paiements sensibles."}
    </div>
    <div class="pass-dots ${F.passError ? "error" : ""}" style="margin-top:36px">${Array.from({ length: 4 }, (_, i) => `<span class="pd ${i < cur.length ? "fill" : ""}"></span>`).join("")}</div>
    <div style="flex:1"></div>
    <div style="width:100%">${keypad("passKey")}</div>
    <div style="height:22px"></div>
  </div>`;
}

function signupBiometric() {
  return `<div style="flex:1;display:flex;flex-direction:column;min-height:0" class="gutter">
    <div style="flex:1"></div>
    <span style="color:var(--ink)">${ico("faceid", 46)}</span>
    <div class="t-page" style="margin-top:24px;font-size:26px">Activer Face ID ?</div>
    <div style="font-size:15px;line-height:1.5;color:var(--ink-2);margin-top:9px;text-wrap:pretty">Ouvrez l’application et confirmez vos paiements sans saisir votre code.</div>
    <div class="note-row" style="margin-top:20px">${ico("shield", 15)}<span>Vos données biométriques ne quittent pas votre téléphone.</span></div>
    <div style="flex:1"></div>
    <div style="display:flex;flex-direction:column;gap:9px;padding-bottom:14px">
      ${btn("Activer Face ID", "signupNext")}
      ${btn("Plus tard", "signupNext", { style: "ghost" })}
    </div>
  </div>`;
}

function signupProfile() {
  return `<div style="flex:1;display:flex;flex-direction:column;min-height:0" class="gutter">
    <div class="t-page" style="margin-top:8px">Vos informations</div>
    <div style="font-size:15px;line-height:1.5;color:var(--ink-2);margin-top:10px;text-wrap:pretty">Le nom doit correspondre exactement à votre pièce d’identité : c’est celui qui sera porté par vos cartes.</div>
    <div style="display:flex;flex-direction:column;gap:10px;margin-top:26px">
      <label class="field"><span class="f-ico">${ico("user", 17)}</span><input type="text" name="first" placeholder="Prénom" value="Aristide" aria-label="Prénom"></label>
      <label class="field"><span class="f-ico">${ico("idcard", 17)}</span><input type="text" name="last" placeholder="Nom" value="Mbassi" aria-label="Nom"></label>
      <label class="field"><span class="f-ico">${ico("mail", 17)}</span><input type="email" name="email" placeholder="Adresse e-mail" value="aristide@moneypay.cm" aria-label="Adresse e-mail"></label>
    </div>
    <div style="flex:1"></div>
    <div style="padding-bottom:14px">${btn("Continuer", "goKYC")}</div>
  </div>`;
}

/* ---------------- KYC ------------------------------------------- */
function kycScreen() {
  const step = F.step || 0;
  let body;
  if (step === 0) body = kycIntro();
  else if (step === 1) body = kycDocs();
  else if (step === 2) body = kycCapture("doc");
  else if (step === 3) body = kycCapture("selfie");
  else body = kycReview();
  return `<div class="layer">${statusBar()}${body}<div class="homebar"></div></div>`;
}

function kycIntro() {
  const steps = [
    ["Choisissez votre pièce", "CNI, passeport ou permis en cours de validité."],
    ["Photographiez-la", "Recto et verso, à plat, dans un endroit bien éclairé."],
    ["Prenez un selfie", "Pour confirmer que la pièce vous appartient."]
  ];
  return `<div class="scroll">
    <div class="gutter" style="padding-top:22px">
      ${eyebrow("≈ 3 minutes")}
      <div class="t-page" style="margin-top:12px">Vérifions votre identité</div>
      <div style="font-size:15px;line-height:1.5;color:var(--ink-2);margin-top:10px;text-wrap:pretty">La réglementation CEMAC nous impose de vous identifier avant d’émettre une carte à votre nom.</div>
      <div class="step-list" style="margin-top:18px">
        ${steps.map((s, i) => `${i > 0 ? `<div class="rule" style="margin-left:34px"></div>` : ""}<div class="step-item"><span class="si-n">${i + 1}</span><span><span class="si-t" style="display:block">${s[0]}</span><span class="si-s" style="display:block">${s[1]}</span></span></div>`).join("")}
      </div>
      <div class="rule"></div>
      <div class="note-row" style="margin-top:18px">${ico("lock", 15)}<span>Vos documents sont chiffrés et transmis uniquement à notre partenaire d’identification.</span></div>
    </div>
  </div>
  <div class="gutter" style="padding:12px 20px 14px;display:flex;flex-direction:column;gap:9px">
    ${btn("Commencer la vérification", "kycNext")}
    ${btn("Plus tard", "enterApp", { style: "ghost" })}
  </div>`;
}

function kycDocs() {
  const docs = [
    { t: "CNI camerounaise", s: "Recto et verso", i: "idcard", fast: true },
    { t: "Passeport", s: "Page avec photo", i: "doc", fast: true },
    { t: "Permis de conduire", s: "Recto et verso", i: "carIco", fast: false },
    { t: "Récépissé CNI", s: "Vérification manuelle sous 48 h", i: "doc", fast: false }
  ];
  return `${navBar({ back: "kycBack" })}
  <div class="scroll">
    <div class="gutter">
      <div class="t-page">Quelle pièce<br>utilisez-vous ?</div>
      <div style="font-size:15px;color:var(--ink-2);margin-top:10px">Elle doit être en cours de validité et parfaitement lisible.</div>
    </div>
    <div style="margin-top:16px">
      ${docs.map((d, i) => `${i > 0 ? rule(true) : ""}${listRow({ icon: d.i, title: esc(d.t), sub: d.s, pillHTML: d.fast ? `<span class="pill credit">${ico("bolt", 11)}Instantané</span>` : "", chevron: true, act: "kycNext" })}`).join("")}
    </div>
  </div>`;
}

function kycCapture(mode) {
  const ok = !!F.captured;
  const isDoc = mode === "doc";
  const tips = isDoc
    ? ["Sur fond uni, sans reflet", "Les quatre coins visibles", "Texte net et lisible"]
    : ["Retirez lunettes et couvre-chef", "Regardez droit vers l’objectif", "Lumière naturelle de préférence"];
  const frame = isDoc
    ? `<div class="viewfinder" style="aspect-ratio:1.586">
        <div style="position:absolute;inset:6px;border-radius:14px;background:var(--well);display:flex;align-items:center;justify-content:center;color:var(--ink-3)">${ico("idcard", 54)}</div>
        <svg class="vf-corners ${ok ? "ok" : ""}" viewBox="0 0 100 63" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" preserveAspectRatio="none" style="width:100%;height:100%">
          <path d="M1 16V8a7 7 0 0 1 7-7h10M82 1h10a7 7 0 0 1 7 7v8M99 47v8a7 7 0 0 1-7 7H82M18 62H8a7 7 0 0 1-7-7v-8" vector-effect="non-scaling-stroke"/>
        </svg>
        ${ok ? `<div style="position:absolute;inset:0;display:flex;align-items:center;justify-content:center"><span class="success-mark" style="width:64px;height:64px">${ico("check", 26)}</span></div>` : ""}
      </div>`
    : `<div style="position:relative;width:264px;height:264px;margin:0 auto">
        <div style="position:absolute;inset:8px;border-radius:99px;background:var(--well);display:flex;align-items:center;justify-content:center;color:var(--ink-3)">${ico("user", 64)}</div>
        <svg viewBox="0 0 100 100" style="position:absolute;inset:0;width:100%;height:100%;transform:rotate(-90deg)" fill="none">
          <circle cx="50" cy="50" r="48.5" stroke="${ok ? "var(--credit)" : "var(--ink)"}" stroke-width="2" stroke-linecap="round" stroke-dasharray="${ok ? "305 0" : "220 85"}"/>
        </svg>
        ${ok ? `<div style="position:absolute;inset:0;display:flex;align-items:center;justify-content:center"><span class="success-mark" style="width:64px;height:64px">${ico("check", 26)}</span></div>` : ""}
      </div>`;
  return `${navBar({ back: "kycBack", title: isDoc ? "Pièce d’identité" : "Selfie" })}
  <div style="flex:1;display:flex;flex-direction:column;min-height:0">
    <div style="text-align:center;font-size:16px;font-weight:600" class="gutter">${isDoc ? "Cadrez le recto de votre pièce" : "Placez votre visage dans le cercle"}</div>
    <div style="flex:1"></div>
    <div class="gutter">${frame}</div>
    <div class="check-list gutter" style="margin-top:26px">
      ${tips.map(t => `<span class="cl-item">${ico("check", 13)}${t}</span>`).join("")}
    </div>
    <div style="flex:1"></div>
    <div class="gutter" style="padding-bottom:22px">
      ${ok ? btn("Continuer", "kycNext") : `<button type="button" class="shutter" data-act="kycShoot" aria-label="Photographier"><span class="sh-core"></span></button>`}
    </div>
  </div>`;
}

function kycReview() {
  const done = !!F.kycDone;
  if (!done && !F.kycTimerOn) {
    F.kycTimerOn = true;
    after(1900, () => { F.kycDone = true; renderPhone(); });
  }
  return `<div style="flex:1;display:flex;flex-direction:column;min-height:0" class="gutter">
    <div style="flex:1"></div>
    ${done ? `<span class="success-mark">${ico("check", 25)}</span>` : `<span class="spin-arc"></span>`}
    <div class="t-page" style="margin-top:24px;font-size:26px">${done ? "Identité vérifiée" : "Vérification en cours"}</div>
    <div style="font-size:15px;line-height:1.5;color:var(--ink-2);margin-top:9px;text-wrap:pretty">
      ${done ? "Votre compte est actif. Vous pouvez créer votre première carte virtuelle." : "Notre partenaire compare votre selfie à votre pièce. Cela prend moins d’une minute."}
    </div>
    ${done ? `<div style="margin-top:22px">
      ${rule()}${kvRow("Plafond mensuel", "3" + NBSP + "000" + NBSP + "000" + NBSP + "F").replace('class="kv gutter"','class="kv"')}
      ${rule()}<div class="kv"><span class="kv-l">Cartes simultanées</span><span class="kv-fill"></span><span class="kv-v">Jusqu’à 5</span></div>${rule()}
    </div>` : ""}
    <div style="flex:1"></div>
    ${done ? `<div style="padding-bottom:16px">${btn("Accéder à mon compte", "enterApp")}</div>` : ""}
  </div>`;
}

/* ---------------- Verrouillage ---------------------------------- */
function lockScreen() {
  const code = F.lockCode || "";
  return `<div class="layer">
    ${statusBar()}
    <div style="flex:1;display:flex;flex-direction:column;align-items:center;min-height:0" class="gutter">
      <div style="flex:.9"></div>
      ${logoMark(46, "var(--green)", "#FFFFFF")}
      <div style="font-size:17px;font-weight:600;margin-top:18px">Bon retour, ${S.user.first}</div>
      <div style="font-size:13.5px;margin-top:6px;color:${F.lockError ? "var(--debit)" : "var(--ink-2)"}">${F.lockError ? "Code incorrect · 2 essais restants" : "Entrez votre code secret"}</div>
      <div class="pass-dots ${F.lockError ? "error" : ""}" style="margin-top:30px">${Array.from({ length: 4 }, (_, i) => `<span class="pd ${i < code.length ? "fill" : ""}"></span>`).join("")}</div>
      <div style="flex:1"></div>
      <div style="width:100%">${keypad("lockKey", { side: "faceid", sideAct: "enterApp" })}</div>
      <button type="button" style="font-size:13.5px;font-weight:500;color:var(--accent);padding:12px 0 18px" data-act="toastForgot">Code oublié ?</button>
    </div>
    <div class="homebar"></div>
  </div>`;
}

/* ================================================================
   Écrans — application principale
   ================================================================ */
const TABS = [
  { id: "home",     label: "Accueil",  icon: "home" },
  { id: "cards",    label: "Cartes",   icon: "card" },
  { id: "activity", label: "Activité", icon: "list" },
  { id: "insights", label: "Analyse",  icon: "chart" },
  { id: "profile",  label: "Profil",   icon: "user" }
];
function tabbarHTML() {
  return `<div class="tabbar" role="tablist">${TABS.map(t =>
    `<button type="button" role="tab" aria-selected="${TAB === t.id}" class="tb ${TAB === t.id ? "on" : ""}" data-act="switchTab" data-arg="${t.id}">
      ${ico(t.icon, 23)}<span class="tb-label">${t.label}</span>
    </button>`).join("")}</div>`;
}

function homeScreen() {
  const usd = xafToUSD(S.balance);
  const { inn, out } = monthTotals();
  const initials = S.user.first[0] + S.user.last[0];
  return `${statusBar()}
  <div class="navbar" style="padding:0 var(--gutter)">
    <button type="button" style="display:flex;align-items:center;gap:9px" data-act="switchTab" data-arg="profile">
      <span class="avatar" style="width:30px;height:30px;font-size:12px">${initials}</span>
      <span style="font-size:14.5px;font-weight:500">${S.user.first} ${S.user.last}</span>
      ${ico("chevD", 11)}
    </button>
    <span class="nb-spacer"></span>
    <button type="button" class="nb-btn" style="position:relative" data-act="openNotifs" aria-label="Notifications">
      ${ico("bell", 21)}
      ${unreadCount() ? `<span class="bell-badge"></span>` : ""}
    </button>
  </div>
  <div class="scroll">
    <div class="hero-panel on-green">
      <div class="hp-guilloche">${guilloche("rgba(60,221,155,.12)", 260)}</div>
      <div class="hp-eyebrow-row">
        ${eyebrow("Solde disponible")}
        <button type="button" class="hp-eye" data-act="toggleHidden" aria-label="Masquer le solde">${ico(S.hidden ? "eyeOff" : "eye", 15)}</button>
      </div>
      <div class="hp-balance">${S.hidden
        ? `<span class="display" style="font-size:40px;letter-spacing:.1em">••••••</span>`
        : moneyXAF(S.balance, 40)}</div>
      <div class="hp-usd" style="${S.hidden ? "visibility:hidden" : ""}">${ico("swap", 13)}≈ ${fmtUSD(usd)} dépensables en carte</div>
      <div class="quick-actions">
        ${quickAction("plus", "Recharger", "openTopUp")}
        ${quickAction("swap", "Convertir", "openConvert")}
        ${quickAction("card", "Nouvelle carte", "openCreateCard")}
        ${quickAction("arrUpR", "Envoyer", "openSend")}
      </div>
    </div>

    <div class="month-strip gutter">
      <div class="ms-cell">
        <div class="ms-label">Entrées · août</div>
        <div class="ms-value"><span style="color:var(--credit)">${ico("arrDnL", 14)}</span>${grp(inn)}<span style="font-size:12px;color:var(--ink-2);font-weight:400">F</span></div>
      </div>
      <div class="ms-cell">
        <div class="ms-label">Sorties · août</div>
        <div class="ms-value"><span style="color:var(--debit)">${ico("arrUpR", 14)}</span>${grp(out)}<span style="font-size:12px;color:var(--ink-2);font-weight:400">F</span></div>
      </div>
    </div>
    <div class="rule" style="margin:0 var(--gutter)"></div>

    ${sectionHead("Cartes", { count: S.cards.length, act: "switchTabCards", chev: true })}
    <div style="display:flex;gap:11px;overflow-x:auto;padding:2px var(--gutter) 6px">
      ${S.cards.map(c => `<button type="button" style="width:198px;flex-shrink:0" data-act="openCard" data-arg="${c.id}">${vcardHTML(c, { compact: true })}</button>`).join("")}
      <button type="button" style="width:104px;flex-shrink:0;border-radius:12px;border:1.5px dashed var(--rule);display:flex;flex-direction:column;align-items:center;justify-content:center;gap:7px;color:var(--ink-2)" data-act="openCreateCard">
        ${ico("plus", 18)}<span style="font-size:12px;font-weight:500">Nouvelle</span>
      </button>
    </div>

    ${sectionHead("Activité", { act: "switchTabActivity", chev: true })}
    <div style="margin-top:-8px">
      ${S.txs.slice(0, 5).map(t => txRow(t)).join("")}
    </div>
    <div style="height:16px"></div>
  </div>
  ${tabbarHTML()}
  <div class="homebar"></div>`;
}

/* ---------------- Cartes ---------------------------------------- */
function cardsScreen() {
  const scope = F.cardScope || 0;
  const shown = scope === 1 ? S.cards.filter(c => !c.frozen) : scope === 2 ? S.cards.filter(c => c.frozen) : S.cards;
  return `${statusBar()}
  <div class="gutter" style="padding-top:6px;padding-bottom:13px">
    <div style="display:flex;align-items:center">
      <span class="t-page">Cartes</span>
      <span style="flex:1"></span>
      <button type="button" class="nb-btn" data-act="openCreateCard" aria-label="Nouvelle carte">${ico("plus", 21)}</button>
    </div>
    <div style="margin-top:13px">${segments(["Toutes", "Actives", "Gelées"], scope, "cardScope")}</div>
  </div>
  <div class="rule"></div>
  <div class="scroll">
    ${shown.length === 0 ? emptyNote("Aucune carte ici", "Créez une carte dédiée à chaque usage. Geler l’une d’elles suspend ses prélèvements sans toucher aux autres.", "Créer une carte", "openCreateCard") : ""}
    ${shown.map((c, i) => `
      ${i > 0 ? `<div class="rule" style="margin:0 var(--gutter)"></div>` : ""}
      <button type="button" class="gutter" style="display:block;width:100%;padding-top:22px;padding-bottom:22px" data-act="openCard" data-arg="${c.id}">
        <div style="max-width:300px">${vcardHTML(c)}</div>
        <div style="max-width:300px;margin-top:14px">
          <div style="display:flex;align-items:center;gap:7px">
            <span style="font-size:15px;font-weight:500">${esc(c.label)}</span>
            ${c.frozen ? `<span class="pill neutral">${ico("snow", 11)}Gelée</span>` : ""}
            ${c.singleUse ? `<span class="pill neutral">${ico("one", 11)}Usage unique</span>` : ""}
          </div>
          ${c.limit ? `
            <div style="display:flex;justify-content:space-between;margin-top:10px;font-size:13px" class="tnum">
              <span style="color:var(--ink-2)">${fmtUSD(c.spent)} dépensés</span>
              <span style="color:var(--ink-3)">plafond ${fmtUSD(c.limit)}</span>
            </div>
            <div style="margin-top:7px">${meter(c.spent / c.limit, { warn: c.spent / c.limit > .85 })}</div>`
          : `<div style="margin-top:9px;font-size:13px;color:var(--ink-2)" class="tnum">${fmtUSD(c.spent)} dépensés · sans plafond</div>`}
        </div>
      </button>`).join("")}
    <div class="rule" style="margin:0 var(--gutter)"></div>
    ${listRow({ icon: "plus", title: "Créer une nouvelle carte", chevron: true, act: "openCreateCard" })}
    <div class="rule" style="margin:0 var(--gutter)"></div>
    <div class="note-micro gutter" style="padding-top:18px;padding-bottom:20px">Cartes émises par notre banque partenaire agréée, sous licence Visa et Mastercard International.</div>
  </div>
  ${tabbarHTML()}
  <div class="homebar"></div>`;
}

/* ---------------- Activité -------------------------------------- */
function activityScreen() {
  const filter = F.txFilter || 0;
  const q = (F.txQuery || "").toLowerCase();
  let list = S.txs.slice().sort((a, b) => b.date - a.date);
  if (filter === 1) list = list.filter(t => t.kind === "payment");
  if (filter === 2) list = list.filter(t => t.kind === "topUp");
  if (filter === 3) list = list.filter(t => t.status === "declined");
  if (q) list = list.filter(t => t.merchant.toLowerCase().includes(q));
  const groups = groupByDay(list);
  const dayTotal = items => {
    const net = items.filter(t => t.status !== "declined").reduce((s, t) => s + t.xaf, 0);
    return (net > 0 ? "+" : net < 0 ? "−" : "") + grp(net) + NBSP + "F";
  };
  return `${statusBar()}
  <div class="gutter" style="padding-top:6px;padding-bottom:13px">
    <div style="display:flex;align-items:center">
      <span class="t-page">Activité</span>
      <span style="flex:1"></span>
      <button type="button" class="nb-btn" data-act="toastExport" aria-label="Exporter">${ico("share", 20)}</button>
    </div>
    <label class="field" style="margin-top:13px;height:44px">
      <span class="f-ico">${ico("search", 16)}</span>
      <input type="search" name="txsearch" id="tx-search" placeholder="Rechercher un marchand" value="${esc(F.txQuery || "")}" aria-label="Rechercher un marchand">
    </label>
    <div style="margin-top:10px">${segments(["Tout", "Cartes", "Recharges", "Refusés"], filter, "txFilter")}</div>
  </div>
  <div class="rule"></div>
  <div class="scroll">
    ${groups.map(g => `
      <div class="gutter" style="display:flex;align-items:baseline;padding-top:22px;padding-bottom:4px">
        ${eyebrow(relDay(g.day))}
        <span style="flex:1"></span>
        <span class="tnum" style="font-size:12px;color:var(--ink-3)">${dayTotal(g.items)}</span>
      </div>
      ${g.items.map(t => txRow(t)).join("")}`).join("")}
    ${groups.length === 0 ? emptyNote("Aucun résultat", "Essayez un autre filtre ou un autre nom de marchand.") : ""}
    <div style="height:20px"></div>
  </div>
  ${tabbarHTML()}
  <div class="homebar"></div>`;
}

/* ---------------- Analyse --------------------------------------- */
function insightsScreen() {
  const period = F.insPeriod || 0;
  const showTable = !!F.insTable;
  const sel = F.insSel != null ? F.insSel : 5;
  const months = S.months;
  const cur = months[5].xaf, prev = months[4].xaf;
  const delta = Math.round((cur - prev) / prev * 100);
  const cats = spendByCat();
  const maxCat = cats.length ? cats[0].xaf : 1;

  /* barres : ≤24px d'épaisseur, bout arrondi 4px côté donnée, base carrée */
  const CW = 353, CH = 168, padL = 34, padB = 22, padT = 16;
  const maxV = 140000, innerW = CW - padL - 6, innerH = CH - padB - padT;
  const bw = 22, slot = innerW / 6;
  const bars = months.map((m, i) => {
    const h = Math.max(6, m.xaf / maxV * innerH);
    const x = padL + slot * i + (slot - bw) / 2;
    const y = CH - padB - h;
    const hot = i === sel;
    return `<path class="bar ${hot ? "hot" : ""}" data-act="insSelect" data-arg="${i}" d="M${x} ${CH - padB} v${-(h - 4)} a4 4 0 0 1 4 -4 h${bw - 8} a4 4 0 0 1 4 4 v${h - 4} Z"/>
      ${hot ? `<text class="cap-label" x="${x + bw / 2}" y="${y - 6}" text-anchor="middle">${Math.round(m.xaf / 1000)}k</text>` : ""}
      <text class="xlabel" x="${x + bw / 2}" y="${CH - 7}" text-anchor="middle">${m.label}</text>`;
  }).join("");
  const gridLines = [0, 70000, 140000].map(v => {
    const y = CH - padB - v / maxV * innerH;
    return `<line class="grid-line" x1="${padL}" x2="${CW - 4}" y1="${y}" y2="${y}"/><text class="tick" x="${padL - 6}" y="${y + 3}" text-anchor="end">${v / 1000}k</text>`;
  }).join("");
  const selM = months[sel];

  return `${statusBar()}
  <div class="gutter" style="padding-top:6px;padding-bottom:13px">
    <span class="t-page">Analyse</span>
    <div style="margin-top:13px">${segments(["6 mois", "Cette année", "Tout"], period, "insPeriod")}</div>
  </div>
  <div class="rule"></div>
  <div class="scroll">
    <div class="gutter" style="padding-top:20px">
      ${eyebrow("Dépensé en août")}
      <div style="margin-top:8px">${moneyXAF(cur, 36)}</div>
      <div style="display:flex;align-items:center;gap:5px;margin-top:8px;font-size:13.5px;color:${delta >= 0 ? "var(--pend)" : "var(--credit)"}">
        ${ico(delta >= 0 ? "arrUpR" : "arrDnL", 13)}<span class="tnum">${delta >= 0 ? "+" : ""}${delta}${NBSP}% par rapport à juillet</span>
      </div>
    </div>

    <div class="section-head gutter" style="padding-top:26px">
      <span class="t-section">Par mois</span><span class="sh-fill"></span>
      <button type="button" class="sh-link" data-act="insToggleTable">${showTable ? "Graphique" : "Tableau"}</button>
    </div>
    ${showTable
      ? `<div>${months.slice().reverse().map((m, i) => `${i > 0 ? `<div class="rule" style="margin:0 var(--gutter)"></div>` : ""}
          <div class="kv gutter"><span class="kv-l" style="color:var(--ink)">${m.label[0].toUpperCase() + m.label.slice(1)}</span><span class="kv-fill"></span><span class="kv-v ${m.current ? "strong" : ""}">${grp(m.xaf)}</span></div>`).join("")}</div>`
      : `<div class="chart-caption">${selM ? `${selM.label[0].toUpperCase() + selM.label.slice(1)} · ${fmtXAF(selM.xaf)}` : "Touchez une barre pour le détail"}</div>
        <div class="bars-chart"><svg class="bars-svg" viewBox="0 0 ${CW} ${CH}" role="img" aria-label="Dépenses mensuelles sur six mois">${gridLines}${bars}</svg></div>`}

    ${sectionHead("Par catégorie", { count: "FCFA" })}
    <div class="gutter">
      ${cats.map(r => {
        const c = CATEGORIES[r.cat];
        return `<div class="cat-row">
          <div class="cr-line">
            <span class="cr-tile" style="background:color-mix(in srgb, ${c.tint} 14%, var(--paper));color:${c.tint}">${ico(c.icon, 14)}</span>
            <span class="cr-name">${c.label}</span>
            <span class="cr-val">${grp(r.xaf)}</span>
          </div>
          ${meter(r.xaf / maxCat)}
        </div>`;
      }).join("")}
    </div>

    ${sectionHead("Coût réel du mois", { count: "FCFA" })}
    <div>
      <div class="kv gutter"><span class="kv-l" style="color:var(--ink)">Marge de change · 3${NBSP}%<span style="display:block;font-size:12px;color:var(--ink-2);margin-top:2px">Prélevée sur chaque conversion F → USD</span></span><span class="kv-fill"></span><span class="kv-v">3${NBSP}254</span></div>
      <div class="rule" style="margin:0 var(--gutter)"></div>
      <div class="kv gutter"><span class="kv-l" style="color:var(--ink)">Frais de rechargement<span style="display:block;font-size:12px;color:var(--ink-2);margin-top:2px">1,5${NBSP}% sur MTN MoMo et Orange Money</span></span><span class="kv-fill"></span><span class="kv-v">1${NBSP}477</span></div>
      <div class="rule" style="margin:0 var(--gutter)"></div>
      <div class="kv gutter"><span class="kv-l" style="color:var(--ink)">Frais de refus<span style="display:block;font-size:12px;color:var(--ink-2);margin-top:2px">1 autorisation refusée ce mois</span></span><span class="kv-fill"></span><span class="kv-v">220</span></div>
      <div class="rule" style="margin:0 var(--gutter)"></div>
      ${kvRow("Total des frais", "4" + NBSP + "951" + NBSP + "F", { strong: true, tint: "var(--pend)" })}
    </div>

    ${sectionHead("Par carte", {})}
    <div>
      ${S.cards.map((c, i) => `${i > 0 ? rule(true) : ""}
        ${listRow({ lead: miniCardChip(c), title: esc(c.label), sub: "••" + NBSP + c.pan.slice(-4), value: fmtUSD(c.spent), act: "openCard", arg: c.id })}`).join("")}
    </div>
    <div style="height:20px"></div>
  </div>
  ${tabbarHTML()}
  <div class="homebar"></div>`;
}

/* ---------------- Profil ---------------------------------------- */
function profileScreen() {
  const initials = S.user.first[0] + S.user.last[0];
  const group = (title, rows) => `${eyebrow(title, "gutter")}${rows}<div class="rule" style="margin:8px var(--gutter) 0"></div>`;
  return `${statusBar()}
  <div class="gutter" style="padding-top:6px;padding-bottom:10px"><span class="t-page">Profil</span></div>
  <div class="scroll">
    <button type="button" class="row gutter" data-act="pushSub" data-arg="profileInfo" style="padding-top:16px;padding-bottom:16px">
      <span class="avatar" style="width:50px;height:50px;font-size:17px;border-radius:14px">${initials}</span>
      <span class="r-body">
        <span style="font-size:16px;font-weight:600;display:block">${S.user.first} ${S.user.last}</span>
        <span class="tnum" style="font-size:13px;color:var(--ink-2);display:block;margin-top:2px">${S.user.phone}</span>
        <span style="display:inline-flex;margin-top:6px"><span class="pill credit">${ico("check", 11)}Identité vérifiée</span></span>
      </span>
      <span class="r-chevron">${ico("chevR", 15)}</span>
    </button>
    <div class="rule" style="margin:0 var(--gutter)"></div>
    ${listRow({ icon: "gift", title: "Parrainez, gagnez 2" + NBSP + "500" + NBSP + "F", sub: "Pour chaque ami qui crée sa première carte", chevron: true, act: "pushSub", arg: "referral" })}
    <div class="rule" style="margin:0 var(--gutter) 0"></div>

    <div style="height:18px"></div>
    ${group("Compte", `
      ${listRow({ icon: "idcard", title: "Informations personnelles", chevron: true, act: "pushSub", arg: "profileInfo" })}
      ${rule(true)}
      ${listRow({ icon: "gauge", title: "Plafonds et limites", chevron: true, act: "pushSub", arg: "limits" })}
      ${rule(true)}
      ${listRow({ icon: "recur", title: "Abonnements récurrents", value: "4", chevron: true, act: "pushSub", arg: "subs" })}
      ${rule(true)}
      ${listRow({ icon: "doc", title: "Documents et relevés", chevron: true, act: "pushSub", arg: "documents" })}
    `)}
    <div style="height:18px"></div>
    ${group("Sécurité", `
      ${listRow({ icon: "lock", title: "Code secret", chevron: true, act: "pushSub", arg: "security" })}
      ${rule(true)}
      ${toggleRow({ icon: "faceid", title: "Face ID", key: "faceid" })}
      ${rule(true)}
      ${listRow({ icon: "phone", title: "Appareils connectés", value: "2", chevron: true, act: "pushSub", arg: "devices" })}
    `)}
    <div style="height:18px"></div>
    ${group("Préférences", `
      ${listRow({ icon: "moon", title: "Apparence", right: `<span style="width:176px;flex-shrink:0">${segments(["Auto", "Clair", "Sombre"], ["auto", "light", "dark"].indexOf(themePref()), "setTheme")}</span>` })}
      ${rule(true)}
      ${toggleRow({ icon: "bell", title: "Notifications push", key: "push" })}
      ${rule(true)}
      ${listRow({ icon: "globe", title: "Langue", value: "Français", chevron: true })}
      ${rule(true)}
      ${listRow({ icon: "swap", title: "Devise d’affichage", value: "FCFA", chevron: true })}
    `)}
    <div style="height:18px"></div>
    ${group("Aide", `
      ${listRow({ icon: "help", title: "Centre d’aide", chevron: true, act: "pushSub", arg: "help" })}
      ${rule(true)}
      ${listRow({ icon: "chat", title: "Discuter avec un conseiller", right: `<span class="pill credit no-ico">En ligne</span>`, chevron: true, act: "pushSub", arg: "help" })}
      ${rule(true)}
      ${listRow({ icon: "doc", title: "Conditions générales", chevron: true })}
    `)}
    <div style="height:18px"></div>
    ${group("Maquette", `
      ${listRow({ icon: "bolt", title: "Simuler une autorisation", sub: "Le paiement vu en temps réel", chevron: true, act: "openAuth" })}
    `)}
    ${listRow({ icon: "logout", title: "Se déconnecter", destructive: true, act: "goLockFromApp" })}
    <div class="rule" style="margin:0 var(--gutter)"></div>
    <div class="note-micro gutter" style="padding-top:18px;padding-bottom:22px">MoniPay · maquette de refonte.<br>Aucune donnée réelle n’est traitée.</div>
  </div>
  ${tabbarHTML()}
  <div class="homebar"></div>`;
}

/* ================================================================
   Écrans poussés — détails et réglages
   ================================================================ */
function cardDetailScreen(params) {
  const c = cardById(params.id);
  if (!c) return `${navBar({ back: "pop" })}<div class="gutter">Carte introuvable.</div>`;
  const revealed = !!F.revealed;
  const holder = (S.user.first + " " + S.user.last).toUpperCase();
  return `${statusBar()}
  ${navBar({ back: "pop", title: c.label, right: `<button type="button" class="nb-btn" data-act="cardMenu" data-arg="${c.id}" aria-label="Plus d’options">${ico("dots", 20)}</button>` })}
  <div class="scroll">
    <div class="gutter" style="padding-top:4px">
      <div class="flip-scene ${revealed ? "flipped" : ""}" style="max-width:300px;margin:0 auto">
        <div class="flip-inner" style="aspect-ratio:1.586">
          <div class="flip-face" style="position:absolute;inset:0">${vcardHTML(c)}</div>
          <div class="flip-back">${vcardBackHTML(c, holder)}</div>
        </div>
      </div>
    </div>
    <div class="quick-actions gutter" style="margin-top:22px">
      ${quickAction(revealed ? "eyeOff" : "eye", revealed ? "Masquer" : "Détails", "cardReveal")}
      ${quickAction(c.frozen ? "sun" : "snow", c.frozen ? "Dégeler" : "Geler", "cardFreezeAsk", c.id)}
      ${quickAction("sliders", "Contrôles", "openControls", c.id)}
      ${quickAction("wallet", "Wallet", "toastWallet")}
    </div>
    <div class="rule" style="margin:24px var(--gutter) 0"></div>

    ${revealed ? `
      ${eyebrow("Détails de la carte", "gutter")}
      <div style="margin-top:-6px">
        ${kvRow("Titulaire", esc(holder), { copy: holder })}
        <div class="rule" style="margin:0 var(--gutter)"></div>
        ${kvRow("Numéro", chunkPan(c.pan), { mono: true, copy: c.pan })}
        <div class="rule" style="margin:0 var(--gutter)"></div>
        ${kvRow("Expiration", c.exp, { mono: true, copy: c.exp })}
        <div class="rule" style="margin:0 var(--gutter)"></div>
        ${kvRow("CVV", c.cvv, { mono: true, copy: c.cvv })}
      </div>
      <div class="note-micro gutter" style="padding-bottom:16px">MoniPay ne vous demandera jamais ces informations.</div>
      <div class="rule" style="margin:0 var(--gutter)"></div>` : ""}

    <div class="gutter" style="padding-top:22px;display:flex;align-items:baseline">
      ${eyebrow("Plafond mensuel")}
      <span style="flex:1"></span>
      <button type="button" class="sh-link" data-act="openControls" data-arg="${c.id}" style="font-size:13px;font-weight:500;color:var(--accent)">Modifier</button>
    </div>
    <div class="gutter" style="margin-top:10px;display:flex;align-items:baseline;gap:8px">
      ${moneyUSD(c.spent, 27)}
      <span style="font-size:13.5px;color:var(--ink-2)" class="tnum">${c.limit ? "sur " + fmtUSD(c.limit) : "sans plafond"}</span>
    </div>
    ${c.limit ? `
      <div class="gutter" style="margin-top:12px">${meter(c.spent / c.limit, { warn: c.spent / c.limit > .85 })}</div>
      <div class="gutter tnum" style="display:flex;margin-top:8px;font-size:12px;color:var(--ink-2)">
        <span class="tnum">Reste ${fmtUSD(Math.max(0, c.limit - c.spent))}</span><span style="flex:1"></span><span style="color:var(--ink-3)">Réinitialisé le 1ᵉʳ</span>
      </div>` : ""}
    ${c.declines > 0 ? `<div class="note-row gutter" style="margin-top:14px"><span style="color:var(--pend)">${ico("warn", 15)}</span><span>${c.declines} refus ce mois · la carte se bloque à 3.</span></div>` : ""}
    <div class="rule" style="margin:20px var(--gutter) 0"></div>

    <div style="padding-top:6px">
      ${listRow({ icon: "globe", title: "Paiements en ligne", value: c.online ? "Autorisés" : "Bloqués", act: "openControls", arg: c.id })}
      ${rule(true)}
      ${listRow({ icon: "recur", title: "Abonnements récurrents", value: c.subs ? "Autorisés" : "Bloqués", act: "openControls", arg: c.id })}
      ${rule(true)}
      ${listRow({ icon: "sliders", title: "Tous les contrôles", chevron: true, act: "openControls", arg: c.id })}
    </div>
    <div class="rule" style="margin:0 var(--gutter)"></div>

    ${sectionHead("Transactions", { count: txsOfCard(c.id).length || null })}
    ${txsOfCard(c.id).length
      ? txsOfCard(c.id).slice(0, 6).map(t => txRow(t)).join("")
      : emptyNote("Aucune transaction", "Les paiements effectués avec cette carte apparaîtront ici.")}
    <div class="rule" style="margin:8px var(--gutter) 0"></div>
    ${listRow({ icon: "trash", title: "Supprimer cette carte", destructive: true, act: "cardDeleteAsk", arg: c.id })}
    <div style="height:26px"></div>
  </div>
  <div class="homebar"></div>`;
}

function txDetailScreen(params) {
  const t = txById(params.id);
  if (!t) return `${navBar({ back: "pop" })}`;
  const card = t.card ? cardById(t.card) : null;
  const gross = Math.round(Math.abs(t.usd) / 100 * S.fx.rate);
  const margin = Math.max(0, Math.abs(t.xaf) - gross);
  return `${statusBar()}
  ${navBar({ back: "pop" })}
  <div class="scroll">
    <div class="gutter" style="padding-top:2px">
      ${txLeadTile(t, 48)}
      <div style="font-size:16px;color:var(--ink-2);margin-top:14px">${esc(t.merchant)}</div>
      <div style="margin-top:4px">
        ${t.usd !== 0
          ? moneyUSD(t.usd, 36, { color: t.status === "declined" ? "var(--ink-3)" : "" })
          : moneyXAF(t.xaf, 36, { sign: true, color: t.xaf > 0 ? "var(--credit)" : "" })}
      </div>
      <div style="display:flex;align-items:center;gap:8px;margin-top:12px">
        ${statusPill(t.status)}
        <span style="font-size:13.5px;color:var(--ink-2)">${fmtFull(t.date)}</span>
      </div>
      ${t.status === "declined" ? `
        <div class="alert-card" style="margin-top:20px">
          ${ico("warn", 16)}
          <span><span class="ac-t" style="display:block">${esc(t.reason || "Autorisation refusée par MoniPay.")}</span>
          <span class="ac-s" style="display:block">Un refus est facturé 220${NBSP}F par le processeur.</span></span>
        </div>` : ""}
    </div>
    <div class="rule" style="margin:26px var(--gutter) 0"></div>

    ${eyebrow("Décompte", "gutter")}
    <div style="margin-top:-4px">
      ${t.usd !== 0 ? `
        ${kvRow("Montant marchand", fmtUSD(t.usd))}
        <div class="rule" style="margin:0 var(--gutter)"></div>
        ${kvRow("Taux appliqué", "1" + NBSP + "USD = 610" + NBSP + "F", { mono: true })}
        <div class="rule" style="margin:0 var(--gutter)"></div>
        ${kvRow("Contre-valeur", fmtXAF(gross))}
        <div class="rule" style="margin:0 var(--gutter)"></div>
        ${kvRow("Marge de change · 3" + NBSP + "%", t.status === "declined" ? "—" : fmtXAF(margin), { tint: "var(--pend)" })}
        <div class="rule" style="margin:0 var(--gutter)"></div>` : ""}
      ${kvRow("Total " + (t.xaf >= 0 ? "crédité" : "débité"), fmtXAF(Math.abs(t.xaf)), { strong: true })}
      <div class="rule" style="margin:0 var(--gutter)"></div>
      ${kvRow("Catégorie", CATEGORIES[t.cat].label)}
      <div class="rule" style="margin:0 var(--gutter)"></div>
      ${kvRow("Référence", "MP-" + t.id.toUpperCase() + "-2608", { mono: true, copy: "MP-" + t.id.toUpperCase() + "-2608" })}
    </div>

    ${card ? `
      <div class="rule" style="margin:0 var(--gutter)"></div>
      ${eyebrow("Payé avec", "gutter")}
      <div style="margin-top:-4px">${listRow({ lead: miniCardChip(card), title: esc(card.label), sub: "••" + NBSP + card.pan.slice(-4), chevron: true, act: "openCard", arg: card.id })}</div>` : ""}
    <div class="rule" style="margin:0 var(--gutter)"></div>
    <div style="padding-top:6px">
      ${listRow({ icon: "undo", title: "Contester ce paiement", chevron: true, act: "toastSoon" })}
      ${rule(true)}
      ${listRow({ icon: "help", title: "Obtenir de l’aide", chevron: true, act: "toastSoon" })}
      ${rule(true)}
      ${listRow({ icon: "share", title: "Partager le reçu", chevron: true, act: "toastReceipt" })}
    </div>
    <div style="height:26px"></div>
  </div>
  <div class="homebar"></div>`;
}

/* ---------------- Notifications --------------------------------- */
function notifsScreen() {
  return `${statusBar()}
  ${navBar({ back: "pop", title: "Notifications", right: unreadCount() ? `<button type="button" class="nb-text" data-act="notifsReadAll">Tout lire</button>` : "" })}
  <div class="scroll">
    ${S.notifs.map((n, i) => `${i > 0 ? rule(true) : ""}
      <div class="row static gutter" style="align-items:flex-start">
        <span class="icon-tile" style="background:var(--${n.cls === "neutral" ? "well" : n.cls + "-soft"});color:var(--${n.cls === "neutral" ? "ink-2" : n.cls})">${ico(n.icon, 17)}</span>
        <span class="r-body">
          <span style="font-size:14.5px;font-weight:${n.unread ? "600" : "500"};display:block">${esc(n.title)}</span>
          <span style="font-size:13px;line-height:1.45;color:var(--ink-2);display:block;margin-top:2px;text-wrap:pretty">${esc(n.body)}</span>
        </span>
        <span style="display:flex;flex-direction:column;align-items:flex-end;gap:6px;padding-top:2px">
          <span style="font-size:11.5px;color:var(--ink-3);white-space:nowrap">${relShort(n.date)}</span>
          ${n.unread ? `<span class="notif-dot"></span>` : ""}
        </span>
      </div>`).join("")}
    <div style="height:20px"></div>
  </div>
  <div class="homebar"></div>`;
}

/* ---------------- Réglages : sous-écrans ------------------------ */
const SUBSCREENS = {
  profileInfo: { title: "Informations", render: subProfileInfo },
  limits:      { title: "Plafonds",     render: subLimits },
  security:    { title: "Sécurité",     render: subSecurity },
  devices:     { title: "Appareils",    render: subDevices },
  documents:   { title: "Documents",    render: subDocuments },
  referral:    { title: "Parrainage",   render: subReferral },
  help:        { title: "Centre d’aide",render: subHelp },
  subs:        { title: "Abonnements",  render: subSubscriptions }
};
function subScreen(params) {
  const def = SUBSCREENS[params.key];
  return `${statusBar()}${navBar({ back: "pop", title: def.title })}<div class="scroll">${def.render()}</div><div class="homebar"></div>`;
}

function subProfileInfo() {
  const initials = S.user.first[0] + S.user.last[0];
  return `
  <div class="row static gutter" style="padding-top:16px;padding-bottom:16px">
    <span class="avatar" style="width:60px;height:60px;font-size:20px;border-radius:16px">${initials}</span>
    <span class="r-body">
      <span style="font-size:17px;font-weight:600;display:block">${S.user.first} ${S.user.last}</span>
      <span style="display:inline-flex;margin-top:6px"><span class="pill credit">${ico("check", 11)}Compte vérifié · niveau 2</span></span>
    </span>
  </div>
  <div class="rule" style="margin:0 var(--gutter)"></div>
  ${eyebrow("Identité", "gutter")}
  <div style="margin-top:-4px">
    ${kvRow("Nom complet", S.user.first + " " + S.user.last)}
    <div class="rule" style="margin:0 var(--gutter)"></div>
    ${kvRow("Date de naissance", "12 mars 1994")}
    <div class="rule" style="margin:0 var(--gutter)"></div>
    ${kvRow("Nationalité", "Camerounaise")}
    <div class="rule" style="margin:0 var(--gutter)"></div>
    ${kvRow("Pièce d’identité", "CNI" + NBSP + "•••" + NBSP + "4821", { mono: true })}
  </div>
  <div class="rule" style="margin:0 var(--gutter)"></div>
  ${eyebrow("Contact", "gutter")}
  <div style="margin-top:-4px">
    ${listRow({ icon: "phone", title: "Téléphone", sub: S.user.phone, chevron: true })}
    ${rule(true)}
    ${listRow({ icon: "mail", title: "E-mail", sub: S.user.email, chevron: true })}
    ${rule(true)}
    ${listRow({ icon: "home", title: "Adresse", sub: "Bonapriso, Douala", chevron: true })}
  </div>
  <div class="rule" style="margin:0 var(--gutter)"></div>
  <div class="note-micro gutter" style="padding-top:18px;padding-bottom:24px">Pour modifier votre nom ou votre date de naissance, une nouvelle vérification d’identité est nécessaire.</div>`;
}

function subLimits() {
  const rows = [
    ["Rechargement mensuel", "1" + NBSP + "240" + NBSP + "000 sur 3" + NBSP + "000" + NBSP + "000" + NBSP + "F", .41, "Renouvelé le 1ᵉʳ septembre"],
    ["Paiements carte par mois", "486 sur 2" + NBSP + "000" + NBSP + "USD", .24, "Toutes cartes confondues"],
    ["Transfert entre membres", "150" + NBSP + "000 sur 500" + NBSP + "000" + NBSP + "F", .30, "Par période de 30 jours"],
    ["Cartes actives", "3 sur 5", .60, "Niveau 2 · vérifié"]
  ];
  return `
  <div class="gutter" style="padding-top:12px">
    ${eyebrow("Niveau 2")}
    <div class="t-page" style="font-size:24px;margin-top:8px">Vos plafonds actuels</div>
    <div style="font-size:14.5px;line-height:1.5;color:var(--ink-2);margin-top:8px;text-wrap:pretty">Ils découlent de votre niveau de vérification et de la réglementation CEMAC.</div>
  </div>
  <div class="rule" style="margin:22px var(--gutter) 0"></div>
  <div class="gutter">
    ${rows.map((r, i) => `${i > 0 ? `<div class="rule"></div>` : ""}
      <div style="padding:18px 0">
        <div style="display:flex;align-items:baseline"><span style="font-size:15px">${r[0]}</span><span style="flex:1"></span><span class="tnum" style="font-size:13.5px;font-weight:500;color:var(--ink-2)">${Math.round(r[2] * 100)}${NBSP}%</span></div>
        <div style="margin-top:9px">${meter(r[2])}</div>
        <div style="display:flex;margin-top:8px;font-size:12.5px;color:var(--ink-2)"><span class="tnum">${r[1]}</span><span style="flex:1"></span><span style="color:var(--ink-3)">${r[3]}</span></div>
      </div>`).join("")}
  </div>
  <div class="rule" style="margin:0 var(--gutter)"></div>
  <div class="gutter" style="padding-top:22px;padding-bottom:26px">
    <div style="font-size:15px;font-weight:600">Passer au niveau 3</div>
    <div style="font-size:13.5px;line-height:1.5;color:var(--ink-2);margin-top:6px;text-wrap:pretty">Ajoutez un justificatif de domicile et un justificatif de revenus pour porter votre plafond à 10${NBSP}000${NBSP}000${NBSP}F.</div>
    <div style="margin-top:14px">${btn("Augmenter mes plafonds", "toastSoon", { style: "quiet small" })}</div>
  </div>`;
}

function subSecurity() {
  return `
  <div style="padding-top:6px">
    ${listRow({ icon: "lock", title: "Modifier le code secret", chevron: true, act: "toastSoon" })}
    ${rule(true)}
    ${listRow({ icon: "help", title: "Code oublié", sub: "Réinitialiser par SMS", chevron: true, act: "toastSoon" })}
  </div>
  <div class="rule" style="margin:0 var(--gutter)"></div>
  ${eyebrow("Confirmation", "gutter")}
  <div style="margin-top:-4px">
    ${toggleRow({ icon: "shield", title: "Double authentification", key: "twofa" })}
    ${rule(true)}
    ${toggleRow({ icon: "faceid", title: "Confirmer chaque paiement", key: "confirmEach" })}
  </div>
  <div class="note-micro gutter" style="padding-top:8px;padding-bottom:18px">Avec la confirmation systématique, chaque autorisation attend votre validation dans l’application. Sans réponse, elle est refusée au bout de 20 secondes.</div>
  <div class="rule" style="margin:0 var(--gutter)"></div>
  ${eyebrow("Activité récente", "gutter")}
  <div class="gutter" style="margin-top:-2px;padding-bottom:24px">
    <div class="log-row"><span class="lr-dot" style="background:var(--credit)"></span><span><span class="lr-t" style="display:block">Connexion réussie</span><span class="lr-s" style="display:block">iPhone 16 Pro · Douala</span></span><span class="lr-d">Il y a 2${NBSP}h</span></div>
    <div class="rule"></div>
    <div class="log-row"><span class="lr-dot" style="background:var(--ink-3)"></span><span><span class="lr-t" style="display:block">Code secret modifié</span><span class="lr-s" style="display:block">iPhone 16 Pro · Douala</span></span><span class="lr-d">12 août</span></div>
    <div class="rule"></div>
    <div class="log-row"><span class="lr-dot" style="background:var(--debit)"></span><span><span class="lr-t" style="display:block">Tentative bloquée</span><span class="lr-s" style="display:block">Appareil inconnu · Lagos</span></span><span class="lr-d">3 août</span></div>
  </div>`;
}

function subDevices() {
  return `
  <div style="padding-top:6px">
    ${listRow({ icon: "phone", title: "iPhone 16 Pro", sub: "Cet appareil · Douala", right: `<span class="pill credit no-ico">Actif</span>` })}
    ${rule(true)}
    ${listRow({ icon: "phone", title: "iPad Air", sub: "Dernière activité : 18 août", right: `<button type="button" style="font-size:13.5px;font-weight:500;color:var(--debit)" data-act="toastRevoke">Révoquer</button>` })}
  </div>
  <div class="rule" style="margin:0 var(--gutter)"></div>
  <div class="note-micro gutter" style="padding-top:14px;padding-bottom:24px">Révoquer un appareil le déconnecte immédiatement et invalide ses sessions.</div>`;
}

function subDocuments() {
  const months = ["Août 2026", "Juillet 2026", "Juin 2026", "Mai 2026", "Avril 2026"];
  return `
  ${eyebrow("Relevés mensuels", "gutter")}
  <div style="margin-top:-4px">
    ${months.map((m, i) => `${i > 0 ? rule(true) : ""}${listRow({ icon: "doc", title: m, sub: "PDF · relevé complet", right: `<span style="color:var(--accent)">${ico("download", 18)}</span>`, act: "toastDownload" })}`).join("")}
  </div>
  <div class="rule" style="margin:0 var(--gutter)"></div>
  ${eyebrow("Justificatifs", "gutter")}
  <div style="margin-top:-4px;padding-bottom:20px">
    ${listRow({ icon: "shield", title: "Attestation de compte", sub: "Générée à la demande", chevron: true, act: "toastSoon" })}
    ${rule(true)}
    ${listRow({ icon: "idcard", title: "Pièce d’identité", sub: "CNI · vérifiée le 3 mai 2026", chevron: true, act: "toastSoon" })}
  </div>`;
}

function subReferral() {
  return `
  <div class="gutter" style="padding-top:14px">
    <div class="t-page">2${NBSP}500${NBSP}F par ami</div>
    <div style="font-size:15px;line-height:1.5;color:var(--ink-2);margin-top:8px;text-wrap:pretty">Votre ami reçoit lui aussi 2${NBSP}500${NBSP}F dès la création de sa première carte virtuelle.</div>
    <button type="button" class="referral-code" style="margin-top:24px" data-act="copyText" data-arg="ARISTIDE237">
      <span style="flex:1;text-align:left">${eyebrow("Votre code")}<span class="rc-code" style="display:block">ARISTIDE237</span></span>
      <span style="color:var(--ink-2)">${ico("copy", 18)}</span>
    </button>
  </div>
  <div class="rule" style="margin:24px var(--gutter) 0"></div>
  <div class="month-strip gutter">
    <div class="ms-cell"><div class="ms-label">Amis parrainés</div><div class="ms-value">4</div></div>
    <div class="ms-cell"><div class="ms-label">Gains cumulés</div><div class="ms-value">10${NBSP}000<span style="font-size:12px;color:var(--ink-2);font-weight:400">F</span></div></div>
  </div>
  <div class="rule" style="margin:0 var(--gutter)"></div>
  <div class="gutter" style="padding-top:22px">${btn("Partager mon code", "toastShare", { icon: "share" })}</div>`;
}

function subHelp() {
  const open = F.faqOpen;
  return `
  <div class="gutter" style="padding-top:10px">
    <label class="field" style="height:44px"><span class="f-ico">${ico("search", 16)}</span><input type="search" name="faqsearch" placeholder="Rechercher une question" aria-label="Rechercher une question"></label>
  </div>
  <div class="gutter" style="margin-top:8px">
    ${S.faq.map((f, i) => `${i > 0 ? `<div class="rule"></div>` : ""}
      <div class="faq-item ${open === i ? "open" : ""}">
        <button type="button" class="fq-q" data-act="faqToggle" data-arg="${i}"><span class="q-t">${f.q}</span>${ico("chevD", 15)}</button>
        <div class="fq-a">${f.a}</div>
      </div>`).join("")}
  </div>
  <div class="rule" style="margin:0 var(--gutter)"></div>
  <div class="gutter" style="padding-top:20px;padding-bottom:26px">
    <div style="display:flex;align-items:center;gap:7px"><span style="width:7px;height:7px;border-radius:99px;background:var(--credit)"></span><span style="font-size:15px;font-weight:600">Conseillers en ligne</span></div>
    <div style="font-size:13.5px;line-height:1.5;color:var(--ink-2);margin-top:6px">Du lundi au samedi, 8${NBSP}h – 20${NBSP}h (WAT). Réponse moyenne en 4 minutes.</div>
    <div style="margin-top:14px">${btn("Démarrer une discussion", "toastSoon", { style: "quiet small" })}</div>
  </div>`;
}

function subSubscriptions() {
  const total = S.subs.filter(s => s.active).reduce((a, s) => a + s.usd, 0);
  return `
  <div class="gutter" style="padding-top:12px">
    ${eyebrow("Total mensuel")}
    <div style="display:flex;align-items:baseline;gap:9px;margin-top:8px">
      ${moneyUSD(total, 32)}
      <span class="tnum" style="font-size:13.5px;color:var(--ink-2)">≈ ${fmtXAF(usdToXAF(total))}</span>
    </div>
    <div style="font-size:12.5px;color:var(--ink-3);margin-top:6px">${S.subs.filter(s => s.active).length} abonnements actifs sur ${S.subs.length}</div>
  </div>
  <div class="rule" style="margin:22px var(--gutter) 0"></div>
  <div class="note-row gutter" style="padding:16px 20px">${ico("bulb", 15)}<span>Gardez une carte dédiée aux abonnements : la geler suspend tous les prélèvements d’un coup.</span></div>
  <div class="rule" style="margin:0 var(--gutter)"></div>
  ${eyebrow("Prélèvements récurrents", "gutter")}
  <div style="margin-top:-4px;padding-bottom:20px">
    ${S.subs.map((s, i) => `${i > 0 ? rule(true) : ""}
      ${listRow({
        lead: logoTile(s.logo, 40) ,
        title: `<span style="${s.active ? "" : "color:var(--ink-2)"}">${esc(s.name)}</span>`,
        sub: s.day,
        pillHTML: s.active ? "" : `<span class="pill neutral no-ico">En pause</span>`,
        value: fmtUSD(s.usd),
        chevron: true, act: "toastSoon"
      })}`).join("")}
  </div>`;
}

/* ================================================================
   Feuilles — flux monétaires
   ================================================================ */
function amountEntry(digits, unit, sub, { warn = false } = {}) {
  const has = digits.length > 0;
  const val = has ? grp(parseInt(digits, 10)) : "0";
  return `<div class="amount-entry">
    <div class="ae-value display ${has ? "" : "empty"}">${val}<span style="font-size:17px;font-weight:500;color:var(--ink-2);margin-left:7px;letter-spacing:0">${unit}</span></div>
    <div class="ae-sub ${warn ? "warn" : ""}">${sub}</div>
  </div>`;
}

/* ---------------- Recharger ------------------------------------- */
function topupSheet() {
  const step = F.step || "amount";
  const digits = F.digits || "";
  const amount = parseInt(digits || "0", 10);
  const m = S.methods.find(x => x.id === (F.methodId || "mtn"));
  const fee = Math.round(amount * m.fee);
  const credited = amount - fee;
  const valid = amount >= 1000;

  if (step === "done") {
    return `<div class="sheet-grab"></div>
    <div style="flex:1;display:flex;flex-direction:column;min-height:0" class="gutter">
      <div style="flex:1"></div>
      <span class="success-mark">${ico("check", 24)}</span>
      <div class="t-page" style="font-size:26px;margin-top:22px">Rechargement effectué</div>
      <div style="font-size:15px;color:var(--ink-2);margin-top:8px" class="tnum">${fmtXAF(credited)} ajoutés à votre solde.</div>
      <div style="margin-top:22px">
        <div class="rule"></div>
        <div class="kv"><span class="kv-l">Nouveau solde</span><span class="kv-fill"></span><span class="kv-v strong">${fmtXAF(S.balance)}</span></div>
        <div class="rule"></div>
        <div class="kv"><span class="kv-l">Référence</span><span class="kv-fill"></span><span class="kv-v mono">MP-${String(340000 + (amount % 400000)).slice(0, 6)}</span></div>
        <div class="rule"></div>
        <div class="kv"><span class="kv-l">Date</span><span class="kv-fill"></span><span class="kv-v">${fmtFull(Date.now())}</span></div>
        <div class="rule"></div>
      </div>
      <div style="flex:1"></div>
      <div style="display:flex;flex-direction:column;gap:9px;padding-bottom:16px">
        ${btn("Partager le reçu", "toastReceipt", { style: "quiet", icon: "share" })}
        ${btn("Terminé", "closeSheet")}
      </div>
    </div>`;
  }

  if (step === "confirm") {
    return `<div class="sheet-grab"></div>
    ${navBar({ back: "topupBackAmount", title: "Confirmer" })}
    <div class="scroll">
      <div class="gutter" style="padding-top:6px">
        ${moneyXAF(amount, 36)}
        <div style="font-size:13.5px;color:var(--ink-2);margin-top:5px">depuis ${m.name}</div>
      </div>
      <div class="rule" style="margin:24px var(--gutter) 0"></div>
      ${kvRow("Montant", fmtXAF(amount))}
      <div class="rule" style="margin:0 var(--gutter)"></div>
      ${kvRow("Frais · " + (m.fee * 100).toLocaleString("fr-FR") + NBSP + "%", fee === 0 ? "Offerts" : "− " + fmtXAF(fee), { tint: fee === 0 ? "var(--credit)" : "var(--pend)" })}
      <div class="rule" style="margin:0 var(--gutter)"></div>
      ${kvRow("Crédité sur le wallet", fmtXAF(credited), { strong: true })}
      <div class="rule" style="margin:0 var(--gutter)"></div>
      ${kvRow("Nouveau solde", fmtXAF(S.balance + credited))}
      <div class="rule" style="margin:0 var(--gutter)"></div>
      ${kvRow("Délai", m.instant ? "Immédiat" : "1 à 2 jours ouvrés")}
      <div class="rule" style="margin:0 var(--gutter)"></div>
      <div class="note-row gutter" style="padding-top:18px">${ico("phone", 15)}<span>Vous allez recevoir une demande de confirmation sur votre téléphone. Validez-la avec votre code ${m.id === "mtn" ? "MoMo" : "opérateur"}.</span></div>
    </div>
    <div class="gutter" style="padding:10px 20px 16px">${btn("Confirmer le rechargement", "topupConfirm")}</div>`;
  }

  return `<div class="sheet-grab"></div>
  ${navBar({ close: "closeSheet", title: "Recharger" })}
  <div style="flex:1"></div>
  ${amountEntry(digits, "FCFA", amount > 0 ? `≈ ${fmtUSD(xafToUSD(credited))} dépensables en carte` : `Minimum 1${NBSP}000${NBSP}F`)}
  <div style="flex:1"></div>
  <div class="chip-row" style="margin-bottom:16px">
    ${[10000, 25000, 50000, 100000].map(v => `<button type="button" class="chip ${amount === v ? "on" : ""}" data-act="topupPreset" data-arg="${v}">${grp(v)}</button>`).join("")}
  </div>
  <div class="rule" style="margin:0 var(--gutter)"></div>
  <button type="button" class="row gutter" data-act="openMethods">
    ${m.logo ? logoTile(m.logo, 38) : `<span class="icon-tile">${ico(m.icon, 18)}</span>`}
    <span class="r-body">
      <span class="r-title"><span class="rt-text">${m.name}</span></span>
      <span class="r-sub" style="display:block">${m.detail}</span>
    </span>
    <span style="font-size:13.5px;font-weight:500;color:var(--accent)">Changer</span>
  </button>
  <div class="rule" style="margin:0 var(--gutter)"></div>
  ${keypad("topupKey")}
  <div class="gutter" style="padding:8px 20px 16px">${btn("Continuer", "topupToConfirm", { disabled: !valid })}</div>`;
}

function methodsSheet() {
  return `<div class="sheet-grab"></div>
  <div class="gutter" style="padding-top:14px;padding-bottom:12px"><span class="t-section" style="font-size:20px">Recharger depuis</span></div>
  <div class="rule"></div>
  <div class="scroll">
    ${S.methods.map((m, i) => `${i > 0 ? rule(true) : ""}
      <button type="button" class="row gutter" data-act="pickMethod" data-arg="${m.id}">
        ${m.logo ? logoTile(m.logo, 38) : `<span class="icon-tile">${ico(m.icon, 18)}</span>`}
        <span class="r-body">
          <span class="r-title"><span class="rt-text">${m.name}</span>${m.fee === 0 ? `<span class="pill credit">${ico("check", 11)}Sans frais</span>` : ""}</span>
          <span class="r-sub" style="display:block">${m.detail}</span>
        </span>
        <span style="color:var(--ink);${(F.methodId || "mtn") === m.id ? "" : "visibility:hidden"}">${ico("check", 17)}</span>
      </button>`).join("")}
    <div style="height:16px"></div>
  </div>`;
}

function countriesSheet() {
  return `<div class="sheet-grab"></div>
  <div class="gutter" style="padding-top:14px;padding-bottom:12px"><span class="t-section" style="font-size:20px">Pays</span></div>
  <div class="rule"></div>
  <div class="scroll">
    ${COUNTRIES.map((c, i) => `${i > 0 ? rule(true) : ""}
      <button type="button" class="row gutter" data-act="pickCountry" data-arg="${c.id}">
        ${flagDisc(c.id, 30)}
        <span class="r-body"><span class="r-title"><span class="rt-text">${esc(c.name)}</span></span></span>
        <span class="tnum" style="font-size:14.5px;color:var(--ink-2)">${c.dial}</span>
        <span style="color:var(--ink);${(F.country || "CM") === c.id ? "" : "visibility:hidden"}">${ico("check", 17)}</span>
      </button>`).join("")}
    <div style="height:16px"></div>
  </div>`;
}

/* ---------------- Convertir ------------------------------------- */
function convertSheet() {
  if (F.step === "done") {
    const xaf = parseInt(F.digits || "0", 10);
    return `<div class="sheet-grab"></div>
    <div style="flex:1;display:flex;flex-direction:column;min-height:0" class="gutter">
      <div style="flex:1"></div>
      <span class="success-mark">${ico("check", 24)}</span>
      <div class="t-page" style="font-size:26px;margin-top:22px">Conversion effectuée</div>
      <div style="font-size:15px;color:var(--ink-2);margin-top:8px" class="tnum">${fmtXAF(xaf)} convertis en ${fmtUSD(xafToUSD(xaf))}.</div>
      <div style="flex:1"></div>
      <div style="padding-bottom:16px">${btn("Terminé", "closeSheet")}</div>
    </div>`;
  }
  const digits = F.digits || "";
  const xaf = parseInt(digits || "0", 10);
  const usd = xafToUSD(xaf);
  const market = Math.floor(xaf / S.fx.rate * 100);
  const marginXAF = Math.round(xaf * S.fx.margin / (1 + S.fx.margin));
  const over = xaf > S.balance;
  const valid = xaf >= 1000 && !over;
  return `<div class="sheet-grab"></div>
  ${navBar({ close: "closeSheet", title: "Convertir" })}
  <div class="gutter">
    <div class="fx-leg">
      ${flagDisc("CM", 30)}
      <span><span class="fl-code" style="display:block">FCFA</span><span class="fl-note tnum" style="display:block">Disponible : ${fmtXAF(S.balance)}</span></span>
      <span class="fl-value ${digits ? "" : "dim"}">${digits ? grp(xaf) : "0"}</span>
    </div>
    <div class="fx-swap"><span class="fs-disc">${ico("arrDn", 14)}</span></div>
    <div class="fx-leg">
      ${flagDisc("US", 30)}
      <span><span class="fl-code" style="display:block">USD</span><span class="fl-note" style="display:block">Utilisable sur toutes vos cartes</span></span>
      <span class="fl-value dim">${fmtUSD(usd).replace(NBSP + "$", "")}</span>
    </div>
  </div>
  <div class="rule" style="margin:0 var(--gutter)"></div>
  ${kvRow("Taux interbancaire", "1" + NBSP + "USD = 610" + NBSP + "F", { mono: true })}
  <div class="rule" style="margin:0 var(--gutter)"></div>
  ${kvRow("Marge MoniPay · 3" + NBSP + "%", marginXAF > 0 ? "− " + fmtXAF(marginXAF) : "—", { tint: "var(--pend)" })}
  <div class="rule" style="margin:0 var(--gutter)"></div>
  ${kvRow("Vous recevez", fmtUSD(usd), { strong: true })}
  <div class="rule" style="margin:0 var(--gutter)"></div>
  ${market > 0 ? `<div class="note-micro gutter tnum" style="padding-top:12px">Au taux interbancaire pur, vous auriez ${fmtUSD(market)}.</div>` : ""}
  <div style="flex:1"></div>
  ${keypad("convertKey")}
  <div class="gutter" style="padding:8px 20px 16px">${btn(over ? "Solde insuffisant" : "Convertir", "convertDo", { disabled: !valid })}</div>`;
}

/* ---------------- Envoyer --------------------------------------- */
function sendSheet() {
  const step = F.step || "pick";
  if (step === "done") {
    const amount = parseInt(F.digits || "0", 10);
    return `<div class="sheet-grab"></div>
    <div style="flex:1;display:flex;flex-direction:column;min-height:0" class="gutter">
      <div style="flex:1"></div>
      <span class="success-mark">${ico("check", 24)}</span>
      <div class="t-page" style="font-size:26px;margin-top:22px">Argent envoyé</div>
      <div style="font-size:15px;color:var(--ink-2);margin-top:8px" class="tnum">${fmtXAF(amount)} envoyés à ${esc(F.recipient.name)}.</div>
      <div style="flex:1"></div>
      <div style="padding-bottom:16px">${btn("Terminé", "closeSheet")}</div>
    </div>`;
  }
  if (step === "amount") {
    const c = F.recipient;
    const digits = F.digits || "";
    const amount = parseInt(digits || "0", 10);
    const initials = c.name.split(" ").slice(0, 2).map(w => w[0]).join("");
    const valid = amount >= 500 && amount <= S.balance;
    return `<div class="sheet-grab"></div>
    ${navBar({ back: "sendBackPick" })}
    <div style="display:flex;flex-direction:column;align-items:center;gap:8px">
      <span class="avatar round" style="width:52px;height:52px;font-size:17px;font-weight:500">${initials}</span>
      <span style="font-size:15px;font-weight:600">${esc(c.name)}</span>
      <span class="tnum" style="font-size:13px;color:var(--ink-2)">${c.phone}</span>
    </div>
    <div style="flex:1"></div>
    ${amountEntry(digits, "FCFA", amount > S.balance ? "Solde insuffisant" : `Transfert MoniPay instantané, sans frais`, { warn: amount > S.balance })}
    <div style="flex:1"></div>
    <div class="gutter"><label class="field" style="height:44px"><span class="f-ico">${ico("chat", 16)}</span><input type="text" name="note" placeholder="Ajouter une note" aria-label="Ajouter une note"></label></div>
    ${keypad("sendKey")}
    <div class="gutter" style="padding:8px 20px 16px">${btn(amount >= 500 && !((amount > S.balance)) ? "Envoyer " + fmtXAF(amount) : "Envoyer", "sendDo", { disabled: !valid })}</div>`;
  }
  const q = (F.sendQuery || "").toLowerCase();
  const list = S.contacts.filter(c => !q || c.name.toLowerCase().includes(q));
  return `<div class="sheet-grab"></div>
  ${navBar({ close: "closeSheet", title: "Envoyer" })}
  <div class="gutter"><label class="field" style="height:44px"><span class="f-ico">${ico("search", 16)}</span><input type="search" name="sendsearch" id="send-search" placeholder="Nom ou numéro MoniPay" value="${esc(F.sendQuery || "")}" aria-label="Rechercher un contact"></label></div>
  <div class="scroll">
    ${eyebrow("Récents", "gutter")}
    <div style="margin-top:-2px">
      ${list.map((c, i) => {
        const initials = c.name.split(" ").slice(0, 2).map(w => w[0]).join("");
        return `${i > 0 ? rule(true) : ""}
        <button type="button" class="row gutter" data-act="sendPick" data-arg="${c.name}">
          <span class="avatar round" style="width:40px;height:40px;font-size:13.5px;font-weight:500">${initials}</span>
          <span class="r-body">
            <span class="r-title"><span class="rt-text">${esc(c.name)}</span></span>
            <span class="r-sub tnum" style="display:block">${c.phone}</span>
          </span>
          <span class="r-chevron">${ico("chevR", 15)}</span>
        </button>`;
      }).join("")}
      ${list.length === 0 ? emptyNote("Aucun contact", "Essayez un autre nom.") : ""}
    </div>
    <div style="height:16px"></div>
  </div>`;
}

/* ---------------- Nouvelle carte — parcours en 4 étapes --------- */
const LIMIT_PRESETS = [5000, 15000, 50000, null];
const NAME_SUGGESTIONS = ["Abonnements", "Shopping", "Publicité", "Serveurs", "Voyage"];
const CC_THEME_NOTES = {
  sapin:  "Le vert signature, guilloché ton sur ton.",
  encre:  "Un noir d’encre, sobre au quotidien.",
  ivoire: "Clair et minéral, finition satinée.",
  terre:  "Terre cuite chaleureuse, pleine de caractère.",
  cobalt: "Un bleu nuit franc et électrique.",
  ardoise:"Gris ardoise, tout en retenue."
};
function createCardSheet() {
  if (F.step === "created") {
    const c = cardById(F.createdId);
    return `<div class="sheet-grab"></div>
    <div style="flex:1;display:flex;flex-direction:column;min-height:0" class="gutter">
      <div style="flex:1"></div>
      <div style="max-width:300px;align-self:center;width:100%">${vcardHTML(c)}</div>
      <div class="t-page" style="font-size:26px;margin-top:32px">Votre carte est prête</div>
      <div style="font-size:15px;line-height:1.5;color:var(--ink-2);margin-top:8px;text-wrap:pretty">Utilisez-la immédiatement en ligne. Chaque paiement est financé par votre solde FCFA au moment de l’autorisation.</div>
      <div style="display:flex;align-items:center;gap:7px;margin-top:20px;font-size:13.5px;color:var(--ink-2)">
        ${ico("bolt", 14)}Émise en 0,9${NBSP}s par le processeur
        <span style="flex:1"></span><span class="eyebrow">sandbox</span>
      </div>
      <div style="flex:1"></div>
      <div style="display:flex;flex-direction:column;gap:9px;padding-bottom:16px">
        ${btn("Ajouter à Apple Wallet", "toastWallet", { style: "quiet", icon: "wallet" })}
        ${btn("Terminé", "createDone")}
      </div>
    </div>`;
  }
  const step = F.ccStep || 0;
  const dir = F.ccDir || "fwd";
  const theme = F.ccTheme || "sapin";
  const network = F.ccNetwork || "mastercard";
  const label = F.ccLabel != null ? F.ccLabel : "";
  const limitIdx = F.ccLimit != null ? F.ccLimit : 1;
  const preview = { label: label || "Ma carte", theme, network, pan: "0000000000000000", frozen: false };
  const progress = `<div class="seg-progress" style="margin:2px var(--gutter) 0;flex:none" role="progressbar" aria-valuenow="${step + 1}" aria-valuemin="1" aria-valuemax="4" aria-label="Étape ${step + 1} sur 4">${[0, 1, 2, 3].map(i => `<span class="sp ${i <= step ? "done" : ""}"></span>`).join("")}</div>`;
  let body = "", cta = btn("Continuer", "ccNext");

  if (step === 0) {
    /* 1 — habillage : la carte en héros, une pastille par habillage */
    const t = CARD_THEMES[theme];
    body = `
      <div style="max-width:300px;margin:16px auto 0;width:100%">${vcardHTML(preview)}</div>
      <div style="text-align:center;margin-top:24px" class="gutter">
        <div style="font-size:17px;font-weight:600">${t.label}</div>
        <div style="font-size:13.5px;color:var(--ink-2);margin-top:5px;text-wrap:pretty">${CC_THEME_NOTES[theme]}</div>
      </div>
      <div style="display:flex;gap:14px;justify-content:center;margin-top:26px;padding:6px 0">
        ${Object.entries(CARD_THEMES).map(([k, th]) => `<button type="button" class="dot-swatch ${theme === k ? "sel" : ""}" style="background:${th.fill}" data-act="ccTheme" data-arg="${k}" aria-label="${th.label}"></button>`).join("")}
      </div>`;
  } else if (step === 1) {
    /* 2 — réseau */
    body = `
      <div style="max-width:240px;margin:16px auto 0;width:100%">${vcardHTML(preview, { compact: true })}</div>
      <div class="gutter" style="margin-top:28px">
        <div class="t-section">Choisissez le réseau</div>
        <div style="display:flex;flex-direction:column;gap:9px;margin-top:14px">
          <button type="button" class="net-option ${network === "mastercard" ? "sel" : ""}" style="flex:none;width:100%;height:56px" data-act="ccNetwork" data-arg="mastercard">${netMark("mastercard", "var(--ink)", 17)}Mastercard<span style="margin-left:auto;color:var(--green)">${network === "mastercard" ? ico("check", 16) : ""}</span></button>
          <button type="button" class="net-option ${network === "visa" ? "sel" : ""}" style="flex:none;width:100%;height:56px" data-act="ccNetwork" data-arg="visa">${netMark("visa", "var(--net-visa)", 16)}<span style="margin-left:auto;color:var(--green)">${network === "visa" ? ico("check", 16) : ""}</span></button>
        </div>
        <div class="note-micro" style="margin-top:14px">Les deux réseaux sont acceptés pour les paiements en ligne dans le monde entier.</div>
      </div>`;
  } else if (step === 2) {
    /* 3 — nom */
    body = `
      <div class="gutter" style="margin-top:12px">
        <div class="t-page" style="font-size:25px">Nommez votre carte</div>
        <div style="font-size:14px;color:var(--ink-2);margin-top:7px;text-wrap:pretty">Visible uniquement par vous — jamais par le marchand.</div>
        <label class="field" style="margin-top:20px"><input type="text" name="cardlabel" id="cc-label" maxlength="20" placeholder="Ex. Abonnements" value="${esc(label)}" aria-label="Nom de la carte"><span class="faint tnum" id="cc-count" style="font-size:12px">${label.length}/20</span></label>
      </div>
      <div class="chip-row" style="margin-top:12px">
        ${NAME_SUGGESTIONS.map(s => `<button type="button" class="chip ${label === s ? "on" : ""}" data-act="ccSuggest" data-arg="${s}">${s}</button>`).join("")}
      </div>`;
  } else {
    /* 4 — plafond & options */
    body = `
      <div class="gutter" style="margin-top:12px">
        <div class="t-page" style="font-size:25px">Plafond mensuel</div>
        <div style="font-size:14px;color:var(--ink-2);margin-top:7px;text-wrap:pretty">Au-delà, les paiements sont refusés. Modifiable à tout moment.</div>
      </div>
      <div class="chip-row" style="margin-top:18px">
        ${LIMIT_PRESETS.map((v, i) => `<button type="button" class="chip ${limitIdx === i ? "on" : ""}" data-act="ccLimit" data-arg="${i}">${v ? fmtUSD(v) : "Illimité"}</button>`).join("")}
      </div>
      <div class="rule" style="margin:20px var(--gutter) 0"></div>
      <div class="row static gutter">
        <span class="icon-tile">${ico("one", 18)}</span>
        <span class="r-body">
          <span class="r-title"><span class="rt-text">Carte à usage unique</span></span>
          <span class="r-sub" style="display:block">Se supprime après le premier paiement</span>
        </span>
        <button type="button" class="toggle ${F.ccSingle ? "on" : ""}" data-act="ccSingle" role="switch" aria-checked="${!!F.ccSingle}" aria-label="Carte à usage unique"></button>
      </div>
      <div class="note-micro gutter" style="padding-top:8px">Le numéro, la date d’expiration et le CVV sont générés par notre processeur au moment de la création.</div>`;
    cta = btn(F.issuing ? "Émission en cours" : "Créer la carte", "ccIssue", { loading: !!F.issuing, disabled: !!F.issuing });
  }

  return `<div class="sheet-grab"></div>
  ${navBar({ back: step > 0 ? "ccBack" : null, close: "closeSheet", title: "Nouvelle carte", right: step === 2 ? `<button type="button" class="nb-text" data-act="ccSkipName">Passer</button>` : "" })}
  ${progress}
  <div class="scroll"><div class="cc-step ${dir}${step === 0 ? " center" : ""}">${body}<div style="height:16px"></div></div></div>
  <div class="gutter" style="padding:10px 20px 16px;border-top:1px solid var(--hairline)">${cta}</div>`;
}

/* ---------------- Contrôles de carte ---------------------------- */
const CTRL_PRESETS = [5000, 15000, 50000, 100000, null];
function controlsSheet() {
  const c = cardById(F.cardId);
  const limitIdx = F.ctrlLimit != null ? F.ctrlLimit : Math.max(0, CTRL_PRESETS.findIndex(v => v === c.limit));
  const d = F.ctrlDraft;
  return `<div class="sheet-grab"></div>
  <div class="navbar">
    <button type="button" class="nb-text" data-act="closeSheet">Annuler</button>
    <span class="nb-title">Contrôles</span>
    <span class="nb-spacer"></span>
    <button type="button" class="nb-text strong" data-act="ctrlSave">Enregistrer</button>
  </div>
  <div class="scroll">
    <div style="max-width:210px;margin:4px auto 0">${vcardHTML({ ...c, frozen: d.frozen }, { compact: true })}</div>
    <div class="rule" style="margin:24px var(--gutter) 0"></div>

    ${eyebrow("Plafond mensuel", "gutter")}
    <div class="gutter" style="margin-top:10px">
      ${CTRL_PRESETS[limitIdx] ? moneyUSD(CTRL_PRESETS[limitIdx], 30) : `<span class="display" style="font-size:30px">Sans plafond</span>`}
    </div>
    <div class="chip-row" style="margin-top:14px">
      ${CTRL_PRESETS.map((v, i) => `<button type="button" class="chip ${limitIdx === i ? "on" : ""}" data-act="ctrlLimit" data-arg="${i}">${v ? fmtUSD(v) : "Illimité"}</button>`).join("")}
    </div>
    <div class="note-micro gutter" style="padding-top:12px;padding-bottom:18px">Au-delà, chaque autorisation est refusée automatiquement. Un refus coûte 220${NBSP}F.</div>
    <div class="rule" style="margin:0 var(--gutter)"></div>

    ${eyebrow("Autorisations", "gutter")}
    <div style="margin-top:-2px">
      ${ctrlToggle("globe", "Paiements en ligne", "online", d.online)}
      ${rule(true)}
      ${ctrlToggle("recur", "Abonnements récurrents", "subs", d.subs)}
      ${rule(true)}
      ${ctrlToggle("one", "Usage unique", "singleUse", d.singleUse)}
    </div>
    <div class="note-micro gutter" style="padding-top:6px;padding-bottom:18px">Une carte à usage unique s’auto-supprime après le premier paiement réussi.</div>
    <div class="rule" style="margin:0 var(--gutter)"></div>

    ${eyebrow("Sécurité", "gutter")}
    <div style="margin-top:-2px;padding-bottom:18px">
      ${ctrlToggle("snow", "Geler la carte", "frozen", d.frozen)}
      ${rule(true)}
      ${listRow({ icon: "globe", title: "Pays autorisés", value: "Tous", chevron: true })}
      ${rule(true)}
      ${listRow({ icon: "bell", title: "Alerte à chaque paiement", value: "Activée", chevron: true })}
    </div>
  </div>`;
}
function ctrlToggle(icon, title, key, on) {
  return `<div class="row static gutter">
    <span class="icon-tile">${ico(icon, 18)}</span>
    <span class="r-body"><span class="r-title"><span class="rt-text">${title}</span></span></span>
    <button type="button" class="toggle ${on ? "on" : ""}" data-act="ctrlToggle" data-arg="${key}" role="switch" aria-checked="${on}" aria-label="${title}"></button>
  </div>`;
}

/* ---------------- Autorisation temps réel ----------------------- */
function authSheet() {
  const a = F.auth;
  const amountXAF = usdToXAF(a.usd);
  const afterBal = S.balance - amountXAF;
  const enough = afterBal >= 0;
  if (F.authOutcome) {
    const o = F.authOutcome;
    const card = cardById(a.cardId);
    return `<div class="sheet-grab"></div>
    <div style="flex:1;display:flex;flex-direction:column;min-height:0" class="gutter">
      <div style="flex:1"></div>
      ${o === "approved" ? `<span class="success-mark">${ico("check", 24)}</span>` : `<span class="fail-mark">${ico(o === "expired" ? "clock" : "x", 24)}</span>`}
      <div class="t-page" style="font-size:26px;margin-top:22px">${o === "approved" ? "Paiement approuvé" : o === "expired" ? "Autorisation expirée" : "Paiement refusé"}</div>
      <div style="font-size:15px;line-height:1.5;color:var(--ink-2);margin-top:8px;text-wrap:pretty" class="tnum">
        ${o === "approved" ? `${fmtXAF(amountXAF)} débités de votre wallet et versés à ${esc(a.merchant)}.`
          : o === "expired" ? "Vous n’avez pas répondu à temps. Le marchand a reçu un refus automatique."
          : `${esc(a.merchant)} a reçu un refus. Aucun montant n’a été débité.`}
      </div>
      ${o !== "approved" ? `<div class="note-row" style="margin-top:20px"><span style="color:var(--pend)">${ico("warn", 15)}</span><span>${card.declines + 1} refus sur cette carte ce mois. Elle se bloque automatiquement à 3.</span></div>` : ""}
      <div style="flex:1"></div>
      <div style="padding-bottom:16px">${btn("Fermer", "closeSheet")}</div>
    </div>`;
  }
  const remaining = F.authRemaining != null ? F.authRemaining : 20;
  const hot = remaining < 6;
  const card = cardById(a.cardId);
  return `<div class="sheet-grab"></div>
  <div style="flex:1;display:flex;flex-direction:column;min-height:0" class="gutter">
    <div style="display:flex;align-items:center;gap:8px;padding-top:16px">
      <span class="live-dot"></span>
      ${eyebrow("Autorisation en attente")}
      <span style="flex:1"></span>
      <span class="eyebrow tnum" id="auth-secs" style="color:${hot ? "var(--debit)" : "var(--ink-2)"}">${Math.ceil(remaining)}${NBSP}s</span>
    </div>
    <div class="auth-countbar"><div class="acb-fill ${hot ? "hot" : ""}" id="auth-bar" style="width:${remaining / 20 * 100}%"></div></div>

    <div style="margin-top:26px">${logoTile(a.logo, 48)}</div>
    <div style="font-size:16px;color:var(--ink-2);margin-top:13px">${esc(a.merchant)}</div>
    <div style="margin-top:3px">${moneyUSD(a.usd, 40)}</div>
    <div class="tnum" style="font-size:13.5px;color:var(--ink-2);margin-top:7px">soit ${fmtXAF(amountXAF)} au taux du jour</div>

    <div style="margin-top:22px">
      <div class="rule"></div>
      <div class="kv"><span class="kv-l">Carte</span><span class="kv-fill"></span><span class="kv-v">${esc(card.label)} · ••${NBSP}${card.pan.slice(-4)}</span></div>
      <div class="rule"></div>
      <div class="kv"><span class="kv-l">Solde actuel</span><span class="kv-fill"></span><span class="kv-v">${fmtXAF(S.balance)}</span></div>
      <div class="rule"></div>
      <div class="kv"><span class="kv-l">Solde après paiement</span><span class="kv-fill"></span><span class="kv-v" style="${enough ? "" : "color:var(--debit)"}">${enough ? fmtXAF(afterBal) : "Insuffisant"}</span></div>
      <div class="rule"></div>
    </div>
    <div style="flex:1"></div>
    <div style="display:flex;flex-direction:column;gap:9px">
      ${btn(enough ? "Approuver le paiement" : "Solde insuffisant", "authApprove", { disabled: !enough })}
      ${btn("Refuser", "authDecline", { style: "danger" })}
    </div>
    <div class="note-micro" style="padding:10px 0 14px">Un refus est facturé 220${NBSP}F par le processeur.</div>
  </div>`;
}

/* ================================================================
   Orchestration du rendu
   ================================================================ */
let SHEET2 = null;
const F_KEEP = ["cardScope","txFilter","txQuery","insPeriod","insTable","insSel","faqOpen","revealed","slide","country","phoneDigits","methodId","sendQuery"];

/* redéfinition : conserve l'état d'interface hors flux */
function openSheet(kind, params = {}) {
  clearTimers();
  const kept = {};
  for (const k of F_KEEP) if (k in F) kept[k] = F[k];
  F = { ...kept, ...params };
  SHEET = { kind, shown: false };
  renderPhone(); renderRail();
  requestAnimationFrame(() => requestAnimationFrame(() => {
    const el = phone.querySelector(".sheet.s1"); const dim = phone.querySelector(".dim1");
    if (el) el.classList.add("show"); if (dim) dim.classList.add("show");
    if (SHEET) SHEET.shown = true;
  }));
}
function closeSheet() {
  clearTimers();
  const cur = SHEET;
  const el = phone.querySelector(".sheet.s1"); const dim = phone.querySelector(".dim1");
  if (el) {
    el.classList.remove("show"); if (dim) dim.classList.remove("show");
    /* une sheet rouverte pendant la fermeture ne doit pas être balayée */
    setTimeout(() => { if (SHEET !== cur) return; SHEET = null; SHEET2 = null; renderPhone(); renderRail(); }, 270);
  } else { SHEET = null; SHEET2 = null; renderPhone(); renderRail(); }
}
function openSheet2(kind) {
  SHEET2 = { kind, shown: false };
  renderPhone();
  requestAnimationFrame(() => requestAnimationFrame(() => {
    const el = phone.querySelector(".sheet.s2"); const dim = phone.querySelector(".dim2");
    if (el) el.classList.add("show"); if (dim) dim.classList.add("show");
    if (SHEET2) SHEET2.shown = true;
  }));
}
function closeSheet2() {
  const el = phone.querySelector(".sheet.s2"); const dim = phone.querySelector(".dim2");
  if (el) {
    el.classList.remove("show"); if (dim) dim.classList.remove("show");
    const cur2 = SHEET2;
    setTimeout(() => { if (SHEET2 !== cur2) return; SHEET2 = null; renderPhone(); }, 270);
  } else { SHEET2 = null; renderPhone(); }
}
function rerenderSheet() { renderPhone(); }

function tabScreenHTML() {
  switch (TAB) {
    case "home": return homeScreen();
    case "cards": return cardsScreen();
    case "activity": return activityScreen();
    case "insights": return insightsScreen();
    case "profile": return profileScreen();
  }
}
function viewFor(entry) {
  switch (entry.screen) {
    case "cardDetail": return cardDetailScreen(entry.params);
    case "txDetail": return txDetailScreen(entry.params);
    case "notifs": return notifsScreen();
    case "sub": return subScreen(entry.params);
  }
  return "";
}
function sheetContent(kind) {
  switch (kind) {
    case "topup": return topupSheet();
    case "convert": return convertSheet();
    case "send": return sendSheet();
    case "createCard": return createCardSheet();
    case "controls": return controlsSheet();
    case "auth": return authSheet();
    case "methods": return methodsSheet();
    case "countries": return countriesSheet();
  }
  return "";
}

function renderPhone(mode) {
  const scrollTops = [...phone.querySelectorAll(".scroll")].map(el => el.scrollTop);
  let html = "";
  if (PHASE === "splash") html = splashScreen();
  else if (PHASE === "welcome") html = welcomeScreen();
  else if (PHASE === "signup") html = signupScreen();
  else if (PHASE === "kyc") html = kycScreen();
  else if (PHASE === "lock") html = lockScreen();
  else {
    const stack = STACKS[TAB];
    const hasStack = stack.length > 0;
    const below = stack.length > 1 ? viewFor(stack[stack.length - 2]) : tabScreenHTML();
    const animating = mode === "push";
    html = `<div class="app">
      <div class="stack-view${mode === "tab" ? " tab-in" : ""}" data-under="1" style="${hasStack && !animating ? "display:none" : ""}" ${hasStack ? "" : "data-top='1'"}>${below}</div>
      ${hasStack ? `<div class="stack-view" data-top="1" ${animating ? 'data-anim="push"' : ""}>${viewFor(stack[stack.length - 1])}</div>` : ""}
    </div>`;
  }
  const shown1 = SHEET && SHEET.shown ? "show" : "";
  if (SHEET) html += `<div class="sheet-dim dim1 ${shown1}" data-act="closeSheet"></div><div class="sheet s1 ${shown1}">${sheetContent(SHEET.kind)}</div>`;
  const shown2 = SHEET2 && SHEET2.shown ? "show" : "";
  if (SHEET2) html += `<div class="sheet-dim dim2 ${shown2}" data-act="closeSheet2" style="z-index:110"></div><div class="sheet s2 ${shown2}" style="z-index:111;top:120px">${sheetContent(SHEET2.kind)}</div>`;
  html += `<div class="dialog-host"></div>`;
  phone.innerHTML = html;

  /* restaure les positions de défilement */
  const scrolls = [...phone.querySelectorAll(".scroll")];
  scrolls.forEach((el, i) => { if (scrollTops[i] != null) el.scrollTop = scrollTops[i]; });

  /* animation de poussée */
  if (mode === "push" && !matchMedia("(prefers-reduced-motion: reduce)").matches) {
    const top = phone.querySelector('.stack-view[data-anim="push"]');
    const under = phone.querySelector('.stack-view[data-under="1"]');
    if (top) {
      top.classList.add("pushing");
      if (under) under.classList.add("under-push");
      setTimeout(() => {
        if (top.isConnected) top.classList.remove("pushing");
        if (under && under.isConnected) { under.classList.remove("under-push"); under.style.display = "none"; }
      }, 280);
    }
  } else if (mode === "push") {
    const under = phone.querySelector('.stack-view[data-under="1"]');
    if (under) under.style.display = "none";
  }

  /* diapositives d'accueil */
  const slides = phone.querySelector("#slides");
  if (slides) {
    if ((F.slide || 0) > 0) slides.scrollLeft = F.slide * slides.offsetWidth;
    {
      let ticking = false;
      slides.addEventListener("scroll", () => {
        if (ticking) return;
        ticking = true;
        requestAnimationFrame(() => {
          ticking = false;
          const w = slides.offsetWidth || 1;
          const i = Math.max(0, Math.min(WELCOME_SLIDES.length - 1, Math.round(slides.scrollLeft / w)));
          if (i !== F.slide) {
            F.slide = i;
            phone.querySelectorAll(".wd").forEach((d, j) => d.classList.toggle("on", j === i));
          }
        });
      }, { passive: true });
    }
  }

  /* compteur du solde (arrivée sur l'accueil) */
  if (mode === "tab" && PHASE === "main" && TAB === "home" && !STACKS.home.length && !S.hidden
      && !matchMedia("(prefers-reduced-motion: reduce)").matches) {
    const el = phone.querySelector(".hp-balance .money > span:first-child");
    if (el) {
      const target = S.balance, t0 = performance.now(), dur = 640;
      el.textContent = grp(0);
      const step = t => {
        if (!el.isConnected) return;
        const k = Math.min(1, (t - t0) / dur), e = 1 - Math.pow(1 - k, 3);
        el.textContent = grp(Math.round(target * e));
        if (k < 1) requestAnimationFrame(step);
      };
      requestAnimationFrame(step);
    }
  }
}

/* ================================================================
   Actions
   ================================================================ */
function digitsAction(key, cur, max) {
  if (key === "del") return cur.slice(0, -1);
  if (cur.length >= max) return cur;
  if (cur === "" && key === "0") return cur;
  return cur + key;
}

const ACTIONS = {
  /* navigation générale */
  setTheme: i => applyTheme(["auto", "light", "dark"][+i]),
  switchTab: t => { if (TAB === t) STACKS[t] = []; setTab(t); },
  switchTabCards: () => setTab("cards"),
  switchTabActivity: () => setTab("activity"),
  pop: () => popScreen(),
  closeSheet: () => closeSheet(),
  closeSheet2: () => closeSheet2(),
  dialogCancel: () => closeDialog(),
  dialogAction: i => { const a = DIALOG && DIALOG.actions[+i]; closeDialog(); if (a) setTimeout(a.fn, 120); },

  /* parcours d'entrée */
  goSignup: () => setPhase("signup", { step: 0 }),
  goLock: () => setPhase("lock"),
  goLockFromApp: () => { resetStacks(); setPhase("lock"); },
  goKYC: () => setPhase("kyc", { step: 0 }),
  enterApp: () => { resetStacks(); TAB = "home"; setPhase("main"); },
  signupBack: () => { F.step = Math.max(0, (F.step || 0) - 1); renderPhone(); },
  signupNext: () => { F.step = (F.step || 0) + 1; renderPhone(); },
  openCountries: () => openSheet2("countries"),
  pickCountry: id => {
    F.country = id;
    const c = COUNTRIES.find(x => x.id === id);
    F.phoneDigits = (F.phoneDigits || "").slice(0, c.len);
    closeSheet2();
  },
  phoneKey: k => {
    const c = COUNTRIES.find(x => x.id === (F.country || "CM"));
    F.phoneDigits = digitsAction(k, F.phoneDigits || "", c.len);
    renderPhone();
  },
  otpKey: k => {
    F.otp = digitsAction(k, F.otp || "", 6);
    renderPhone();
    if ((F.otp || "").length === 6) ACTIONS.otpVerify();
  },
  otpVerify: () => {
    if (F.verifying) return;
    F.verifying = true; renderPhone();
    after(700, () => { F.verifying = false; F.step = 2; renderPhone(); });
  },
  otpResend: () => { F.otpSecs = 42; renderPhone(); },
  passKey: k => {
    F.passError = false;
    const confirming = (F.pass1 || "").length === 4;
    if (confirming) {
      F.pass2 = digitsAction(k, F.pass2 || "", 4);
      if ((F.pass2 || "").length === 4) {
        if (F.pass2 === F.pass1) { F.step = 3; }
        else { F.passError = true; F.pass1 = ""; F.pass2 = ""; }
      }
    } else {
      F.pass1 = digitsAction(k, F.pass1 || "", 4);
    }
    renderPhone();
  },
  kycNext: () => { F.step = (F.step || 0) + 1; F.captured = false; renderPhone(); },
  kycBack: () => { F.step = Math.max(0, (F.step || 0) - 1); F.captured = false; renderPhone(); },
  kycShoot: () => { F.captured = true; renderPhone(); },
  lockKey: k => {
    F.lockError = false;
    F.lockCode = digitsAction(k, F.lockCode || "", 4);
    if ((F.lockCode || "").length === 4) {
      if (F.lockCode === "1234") { ACTIONS.enterApp(); return; }
      F.lockError = true; F.lockCode = "";
    }
    renderPhone();
  },
  toastForgot: () => showToast("Réinitialisation envoyée par SMS", "mail"),

  /* accueil */
  toggleHidden: () => { S.hidden = !S.hidden; renderPhone(); },
  openNotifs: () => pushScreen("notifs"),
  notifsReadAll: () => { S.notifs.forEach(n => n.unread = false); renderPhone(); },

  /* cartes */
  cardScope: i => { F.cardScope = +i; renderPhone(); },
  openCard: id => { delete F.revealed; pushScreen("cardDetail", { id }); },
  cardReveal: () => { F.revealed = !F.revealed; renderPhone(); },
  cardFreezeAsk: id => {
    const c = cardById(id);
    showDialog({
      title: c.frozen ? "Dégeler cette carte ?" : "Geler cette carte ?",
      message: c.frozen ? "Les autorisations reprendront immédiatement." : "Tant que la carte est gelée, chaque autorisation est refusée. Les abonnements rattachés échoueront.",
      actions: [{
        label: c.frozen ? "Dégeler" : "Geler",
        fn: () => { c.frozen = !c.frozen; showToast(c.frozen ? "Carte gelée" : "Carte dégelée", c.frozen ? "snow" : "check"); renderPhone(); renderRail(); }
      }]
    });
  },
  cardDeleteAsk: id => {
    showDialog({
      title: "Supprimer définitivement ?",
      message: "Les abonnements rattachés cesseront d’être prélevés.",
      actions: [{ label: "Supprimer la carte", destructive: true, fn: () => { S.cards = S.cards.filter(x => x.id !== id); popScreen(); showToast("Carte supprimée", "trash"); } }]
    });
  },
  cardMenu: id => {
    const c = cardById(id);
    showDialog({
      title: c.label,
      actions: [
        { label: "Renommer la carte", fn: () => showToast("Bientôt disponible", "pencil") },
        { label: "Ajouter à Apple Wallet", fn: () => showToast("Ajout à Apple Wallet simulé", "wallet") },
        { label: "Voir le relevé", fn: () => showToast("Relevé en préparation", "doc") },
        { label: "Supprimer la carte", destructive: true, fn: () => ACTIONS.cardDeleteAsk(id) }
      ]
    });
  },
  openControls: id => {
    const c = cardById(id);
    openSheet("controls", { cardId: id, ctrlDraft: { online: c.online, subs: c.subs, singleUse: c.singleUse, frozen: c.frozen } });
  },
  ctrlLimit: i => { F.ctrlLimit = +i; rerenderSheet(); },
  ctrlToggle: key => { F.ctrlDraft[key] = !F.ctrlDraft[key]; rerenderSheet(); },
  ctrlSave: () => {
    const c = cardById(F.cardId);
    Object.assign(c, F.ctrlDraft);
    if (F.ctrlLimit != null) c.limit = CTRL_PRESETS[F.ctrlLimit];
    showToast("Contrôles enregistrés");
    closeSheet();
  },

  /* activité & analyse */
  openTx: id => pushScreen("txDetail", { id }),
  txFilter: i => { F.txFilter = +i; renderPhone(); },
  insPeriod: i => { F.insPeriod = +i; renderPhone(); },
  insSelect: i => { F.insSel = +i; renderPhone(); },
  insToggleTable: () => { F.insTable = !F.insTable; renderPhone(); },

  /* profil */
  pushSub: key => pushScreen("sub", { key }),
  toggleFlag: key => { FLAGS_STATE[key] = !FLAGS_STATE[key]; renderPhone(); },
  faqToggle: i => { F.faqOpen = F.faqOpen === +i ? null : +i; renderPhone(); },
  copyText: t => copyText(t),

  /* recharger */
  openTopUp: () => openSheet("topup", { step: "amount", digits: "" }),
  topupKey: k => { F.digits = digitsAction(k, F.digits || "", 8); rerenderSheet(); },
  topupPreset: v => { F.digits = String(v); rerenderSheet(); },
  topupToConfirm: () => { F.step = "confirm"; rerenderSheet(); },
  topupBackAmount: () => { F.step = "amount"; rerenderSheet(); },
  openMethods: () => openSheet2("methods"),
  pickMethod: id => { F.methodId = id; closeSheet2(); },
  topupConfirm: () => {
    const amount = parseInt(F.digits || "0", 10);
    const m = S.methods.find(x => x.id === (F.methodId || "mtn"));
    const fee = Math.round(amount * m.fee);
    const credited = amount - fee;
    S.balance += credited;
    S.txs.unshift({ id: uid(), merchant: m.name, logo: m.logo || null, icon: m.icon, kind: "topUp", cat: "other", status: m.instant ? "approved" : "pending", date: Date.now(), usd: 0, xaf: credited, card: null });
    S.notifs.unshift({ id: uid(), title: "Rechargement reçu", body: `${fmtXAF(credited)} reçus depuis ${m.name}`, date: Date.now(), icon: "arrDn", cls: "credit", unread: true });
    F.step = "done"; rerenderSheet();
  },

  /* convertir */
  openConvert: () => openSheet("convert", { digits: "" }),
  convertKey: k => { F.digits = digitsAction(k, F.digits || "", 8); rerenderSheet(); },
  convertDo: () => { F.step = "done"; rerenderSheet(); },

  /* envoyer */
  openSend: () => openSheet("send", { step: "pick", digits: "" }),
  sendPick: name => { F.recipient = S.contacts.find(c => c.name === name); F.step = "amount"; F.digits = ""; rerenderSheet(); },
  sendBackPick: () => { F.step = "pick"; rerenderSheet(); },
  sendKey: k => { F.digits = digitsAction(k, F.digits || "", 8); rerenderSheet(); },
  sendDo: () => {
    const amount = parseInt(F.digits || "0", 10);
    S.balance -= amount;
    S.txs.unshift({ id: uid(), merchant: F.recipient.name, logo: null, icon: "arrUpR", kind: "transfer", cat: "other", status: "approved", date: Date.now(), usd: 0, xaf: -amount, card: null });
    F.step = "done"; rerenderSheet();
  },

  /* nouvelle carte */
  openCreateCard: () => openSheet("createCard", { ccTheme: "sapin", ccNetwork: "mastercard", ccLabel: "", ccLimit: 1, ccStep: 0 }),
  ccTheme: k => { F.ccTheme = k; rerenderSheet(); },
  ccNetwork: k => { F.ccNetwork = k; rerenderSheet(); },
  ccSuggest: s => { F.ccLabel = s; rerenderSheet(); },
  ccLimit: i => { F.ccLimit = +i; rerenderSheet(); },
  ccSingle: () => { F.ccSingle = !F.ccSingle; rerenderSheet(); },
  ccNext: () => { F.ccStep = Math.min(3, (F.ccStep || 0) + 1); F.ccDir = "fwd"; rerenderSheet(); },
  ccBack: () => { F.ccStep = Math.max(0, (F.ccStep || 0) - 1); F.ccDir = "back"; rerenderSheet(); },
  ccSkipName: () => { F.ccLabel = ""; F.ccStep = 3; F.ccDir = "fwd"; rerenderSheet(); },
  ccIssue: () => {
    if (F.issuing) return;
    F.issuing = true; rerenderSheet();
    after(900, () => {
      const id = "c" + Math.floor(Math.random() * 9000 + 1000);
      const pan = (F.ccNetwork === "visa" ? "4539" : "5399") + String(Math.floor(Math.random() * 1e12)).padStart(12, "0");
      const card = {
        id, label: F.ccLabel || "Ma carte", theme: F.ccTheme, network: F.ccNetwork,
        pan, cvv: String(Math.floor(Math.random() * 900 + 100)), exp: "08/29",
        frozen: false, limit: LIMIT_PRESETS[F.ccLimit != null ? F.ccLimit : 1], spent: 0,
        singleUse: !!F.ccSingle, online: true, subs: true, declines: 0, created: Date.now()
      };
      S.cards.unshift(card);
      F.issuing = false; F.step = "created"; F.createdId = id;
      rerenderSheet();
    });
  },
  createDone: () => { closeSheet(); setTab("cards"); },

  /* autorisation temps réel */
  openAuth: () => {
    if (PHASE !== "main") { PHASE = "main"; TAB = "home"; resetStacks(); }
    openSheet("auth", {
      auth: { merchant: "Netflix", logo: "netflix", usd: 1099, cardId: "c1", cat: "streaming" },
      authRemaining: 20
    });
    every(100, () => {
      if (F.authOutcome) return;
      F.authRemaining -= 0.1;
      const bar = phone.querySelector("#auth-bar"); const secs = phone.querySelector("#auth-secs");
      if (bar) {
        bar.style.width = Math.max(0, F.authRemaining / 20 * 100) + "%";
        bar.classList.toggle("hot", F.authRemaining < 6);
      }
      if (secs) {
        secs.textContent = Math.max(0, Math.ceil(F.authRemaining)) + NBSP + "s";
        secs.style.color = F.authRemaining < 6 ? "var(--debit)" : "var(--ink-2)";
      }
      if (F.authRemaining <= 0) {
        F.authOutcome = "expired";
        cardById(F.auth.cardId).declines++;
        rerenderSheet();
      }
    });
  },
  authApprove: () => {
    const a = F.auth;
    const xaf = usdToXAF(a.usd);
    S.balance -= xaf;
    const card = cardById(a.cardId);
    card.spent += a.usd;
    S.txs.unshift({ id: uid(), merchant: a.merchant, logo: a.logo, kind: "payment", cat: a.cat, status: "approved", date: Date.now(), usd: -a.usd, xaf: -xaf, card: a.cardId });
    S.notifs.unshift({ id: uid(), title: "Paiement autorisé", body: `${a.merchant} · ${fmtUSD(a.usd)} débités de « ${card.label} »`, date: Date.now(), icon: "check", cls: "credit", unread: true });
    F.authOutcome = "approved";
    rerenderSheet();
  },
  authDecline: () => {
    const a = F.auth;
    cardById(a.cardId).declines++;
    S.txs.unshift({ id: uid(), merchant: a.merchant, logo: a.logo, kind: "payment", cat: a.cat, status: "declined", date: Date.now(), usd: -a.usd, xaf: 0, card: a.cardId, reason: "Refusé manuellement depuis l’application" });
    F.authOutcome = "declined";
    rerenderSheet();
  },

  /* toasts utilitaires */
  toastSoon: () => showToast("Bientôt disponible", "clock"),
  toastWallet: () => showToast("Ajout à Apple Wallet simulé", "wallet"),
  toastExport: () => showToast("Export PDF en préparation", "download"),
  toastReceipt: () => showToast("Reçu partagé", "share"),
  toastShare: () => showToast("Lien de parrainage copié", "copy"),
  toastDownload: () => showToast("Relevé téléchargé", "download"),
  toastRevoke: () => showToast("Appareil révoqué", "check")
};

/* délégation d'événements */
phone.addEventListener("click", e => {
  const el = e.target.closest("[data-act]");
  if (!el || !phone.contains(el)) return;
  const fn = ACTIONS[el.dataset.act];
  if (fn) fn(el.dataset.arg, el);
});
phone.addEventListener("input", e => {
  const t = e.target;
  if (t.id === "tx-search") {
    F.txQuery = t.value;
    renderPhone();
    const el = phone.querySelector("#tx-search");
    if (el) { el.focus(); try { el.setSelectionRange(el.value.length, el.value.length); } catch (_) {} }
  } else if (t.id === "send-search") {
    F.sendQuery = t.value;
    renderPhone();
    const el = phone.querySelector("#send-search");
    if (el) { el.focus(); try { el.setSelectionRange(el.value.length, el.value.length); } catch (_) {} }
  } else if (t.id === "cc-label") {
    F.ccLabel = t.value;
    const lab = phone.querySelector(".sheet .vc-label");
    if (lab) lab.textContent = F.ccLabel || "Ma carte";
    const cnt = phone.querySelector("#cc-count");
    if (cnt) cnt.textContent = (F.ccLabel || "").length + "/20";
  }
});

/* ================================================================
   Rail — plan des écrans
   ================================================================ */
function railMain(tab) { clearTimers(); F = {}; SHEET = null; SHEET2 = null; DIALOG = null; PHASE = "main"; TAB = tab; resetStacks(); renderPhone("tab"); renderRail(); }
const RAIL = [
  { group: "Parcours d’entrée", items: [
    { key: "splash",  label: "Splash",                 go: () => setPhase("splash") },
    { key: "welcome", label: "Bienvenue",              go: () => setPhase("welcome") },
    { key: "signup",  label: "Inscription",            go: () => setPhase("signup", { step: 0 }) },
    { key: "kyc",     label: "Vérification d’identité",go: () => setPhase("kyc", { step: 0 }) },
    { key: "lock",    label: "Verrouillage",           go: () => setPhase("lock") }
  ]},
  { group: "Application", items: [
    { key: "tab-home",     label: "Accueil",  go: () => railMain("home") },
    { key: "tab-cards",    label: "Cartes",   go: () => railMain("cards") },
    { key: "tab-activity", label: "Activité", go: () => railMain("activity") },
    { key: "tab-insights", label: "Analyse",  go: () => railMain("insights") },
    { key: "tab-profile",  label: "Profil",   go: () => railMain("profile") }
  ]},
  { group: "Flux", items: [
    { key: "f-topup",   label: "Recharger",              go: () => { railMain("home"); ACTIONS.openTopUp(); } },
    { key: "f-convert", label: "Convertir",              go: () => { railMain("home"); ACTIONS.openConvert(); } },
    { key: "f-send",    label: "Envoyer",                go: () => { railMain("home"); ACTIONS.openSend(); } },
    { key: "f-card",    label: "Nouvelle carte",         go: () => { railMain("home"); ACTIONS.openCreateCard(); } },
    { key: "f-auth",    label: "Autorisation temps réel",go: () => { railMain("home"); ACTIONS.openAuth(); } }
  ]},
  { group: "Détails", items: [
    { key: "d-card",   label: "Détail de carte",       go: () => { railMain("cards"); STACKS.cards.push({ screen: "cardDetail", params: { id: "c1" } }); renderPhone(); renderRail(); } },
    { key: "d-tx",     label: "Détail de transaction", go: () => { railMain("activity"); STACKS.activity.push({ screen: "txDetail", params: { id: "t3" } }); renderPhone(); renderRail(); } },
    { key: "d-txko",   label: "Paiement refusé",       go: () => { railMain("activity"); STACKS.activity.push({ screen: "txDetail", params: { id: "t4" } }); renderPhone(); renderRail(); } },
    { key: "d-notifs", label: "Notifications",         go: () => { railMain("home"); STACKS.home.push({ screen: "notifs" }); renderPhone(); renderRail(); } },
    { key: "s-subs",   label: "Abonnements",           go: () => { railMain("profile"); STACKS.profile.push({ screen: "sub", params: { key: "subs" } }); renderPhone(); renderRail(); } }
  ]},
  { group: "Réglages", items: [
    { key: "s-profileInfo", label: "Informations",  go: () => { railMain("profile"); STACKS.profile.push({ screen: "sub", params: { key: "profileInfo" } }); renderPhone(); renderRail(); } },
    { key: "s-limits",      label: "Plafonds",      go: () => { railMain("profile"); STACKS.profile.push({ screen: "sub", params: { key: "limits" } }); renderPhone(); renderRail(); } },
    { key: "s-security",    label: "Sécurité",      go: () => { railMain("profile"); STACKS.profile.push({ screen: "sub", params: { key: "security" } }); renderPhone(); renderRail(); } },
    { key: "s-devices",     label: "Appareils",     go: () => { railMain("profile"); STACKS.profile.push({ screen: "sub", params: { key: "devices" } }); renderPhone(); renderRail(); } },
    { key: "s-documents",   label: "Documents",     go: () => { railMain("profile"); STACKS.profile.push({ screen: "sub", params: { key: "documents" } }); renderPhone(); renderRail(); } },
    { key: "s-referral",    label: "Parrainage",    go: () => { railMain("profile"); STACKS.profile.push({ screen: "sub", params: { key: "referral" } }); renderPhone(); renderRail(); } },
    { key: "s-help",        label: "Centre d’aide", go: () => { railMain("profile"); STACKS.profile.push({ screen: "sub", params: { key: "help" } }); renderPhone(); renderRail(); } }
  ]}
];
const RAIL_FLAT = RAIL.flatMap(g => g.items);

function currentRailKey() {
  if (PHASE !== "main") return PHASE;
  if (SHEET) {
    const m = { topup: "f-topup", convert: "f-convert", send: "f-send", createCard: "f-card", auth: "f-auth" };
    if (m[SHEET.kind]) return m[SHEET.kind];
  }
  const stack = STACKS[TAB];
  if (stack.length) {
    const top = stack[stack.length - 1];
    if (top.screen === "cardDetail") return "d-card";
    if (top.screen === "txDetail") return txById(top.params.id) && txById(top.params.id).status === "declined" ? "d-txko" : "d-tx";
    if (top.screen === "notifs") return "d-notifs";
    if (top.screen === "sub") return "s-" + top.params.key;
  }
  return "tab-" + TAB;
}

const railScroll = document.getElementById("rail-scroll");
function renderRail() {
  const active = currentRailKey();
  railScroll.innerHTML = RAIL.map(g => `
    <div class="rail-group">
      <div class="rail-eyebrow">${g.group}</div>
      ${g.items.map(it => `<button type="button" class="rail-item ${it.key === active ? "active" : ""}" data-rail="${it.key}"><span class="ri-dot"></span>${it.label}</button>`).join("")}
    </div>`).join("");
}
railScroll.addEventListener("click", e => {
  const el = e.target.closest("[data-rail]");
  if (!el) return;
  const it = RAIL_FLAT.find(x => x.key === el.dataset.rail);
  if (it) it.go();
  document.getElementById("rail").classList.remove("open");
  document.getElementById("studio").classList.remove("rail-open");
});

/* tiroir mobile */
const railEl = document.getElementById("rail");
const studioEl = document.getElementById("studio");
document.getElementById("plan-fab").addEventListener("click", () => {
  railEl.classList.add("open");
  studioEl.classList.add("rail-open");
});
studioEl.addEventListener("click", e => {
  if (studioEl.classList.contains("rail-open") && !railEl.contains(e.target) && e.target.id !== "plan-fab") {
    railEl.classList.remove("open");
    studioEl.classList.remove("rail-open");
  }
});

/* ================================================================
   Thème — clair / sombre / système
   ================================================================ */
const THEME_KEY = "mp-theme";
function themePref() { try { return localStorage.getItem(THEME_KEY) || "auto"; } catch (e) { return "auto"; } }
function applyTheme(pref, { animate = true } = {}) {
  try { pref === "auto" ? localStorage.removeItem(THEME_KEY) : localStorage.setItem(THEME_KEY, pref); } catch (e) {}
  const root = document.documentElement;
  if (animate && !matchMedia("(prefers-reduced-motion: reduce)").matches) {
    root.classList.add("theme-anim");
    setTimeout(() => root.classList.remove("theme-anim"), 360);
  }
  if (pref === "auto") delete root.dataset.theme; else root.dataset.theme = pref;
  renderThemeControl();
  renderPhone(); renderRail();
}
function renderThemeControl() {
  const host = document.getElementById("rail-theme");
  if (!host) return;
  const cur = themePref();
  host.innerHTML = [["auto", "Auto"], ["light", "Clair"], ["dark", "Sombre"]]
    .map(([k, l]) => `<button type="button" class="${k === cur ? "on" : ""}" data-theme-set="${k}">${l}</button>`).join("");
}
document.addEventListener("click", e => {
  const b = e.target.closest("[data-theme-set]");
  if (b) applyTheme(b.dataset.themeSet);
});
matchMedia("(prefers-color-scheme: dark)").addEventListener("change", () => {
  if (themePref() === "auto") { renderPhone(); renderRail(); }
});

/* ================================================================
   Démarrage
   ================================================================ */
document.getElementById("rail-logo").innerHTML = logoMark(34, "var(--green)", "#FFFFFF");
renderThemeControl();
renderRail();
renderPhone();
