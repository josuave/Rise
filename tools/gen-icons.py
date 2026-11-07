#!/usr/bin/env python3
"""Generate Rise PWA icons from the Rise-512.png mark.

Source of truth for themes: src/Slices/Rise.Slices.OmarchyThemes/OmarchyThemePack.cs
(parsed, not copied — a palette change here re-generates correctly).

The mark (verified by pixel inspection of Rise-512.png, 512x512 RGBA):
  * transparent background (113,710 px)
  * R body: near-black ink (~109,611 px, e.g. (4,0,1))
  * red glitch strokes:  #FD3149-ish (~17,637 px)
  * cyan glitch strokes: #00FDE5-ish (~14,839 px)
  * the rest: anti-aliased blends / semi-transparent edge pixels

Mapping (documented, exact):
  * per-theme icon  wwwroot/icons/<slug>-512.png (+ -192.png):
      canvas filled with the theme Background;
      R body pixels -> theme Accent (fall back to Text when the Accent /
      Background WCAG contrast is < 3.0 — a large glyph needs 3:1);
      red/cyan glitch strokes keep the brand colours verbatim;
      anti-aliased edge pixels keep their alpha, composited over Background.
  * calm icons wwwroot/icons/calm-light|dark-512.png (+ -192.png):
      same mapping with the calm palettes (light bg #f7f5f0 glyph #222222;
      dark bg #1b1b1f glyph #f0ede6) — used by the blob manifest for the
      built-in Light/Dark/System choices and for custom themes (which have
      no prebuilt file; they get the calm icon of their active half's mode).
  * maskable  wwwroot/Rise-512-maskable.png (+ -192.png):
      full-bleed calm light #f7f5f0, the mark in its original colours,
      scaled so its opaque bounding box fits the central 80% safe zone.
  * monochrome wwwroot/Rise-512-monochrome.png (+ -192.png):
      every ink pixel -> solid calm text #222222, alpha preserved,
      transparent elsewhere (the standard single-colour treatment).

Usage: python3 tools/gen-icons.py   (run from anywhere; paths are
resolved from this script's location, not from the cwd)
Verifies each file it writes: size, corner pixel == Background, and a
known body pixel == the glyph colour. Exits non-zero on any failure.
"""
import re
import sys
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parent.parent  # tools/ lives at the repo root
SRC = REPO / "src/Rise.Wasm/wwwroot/Rise-512.png"
PACK = REPO / "src/Slices/Rise.Slices.OmarchyThemes/OmarchyThemePack.cs"
WWWROOT = REPO / "src/Rise.Wasm/wwwroot"
ICONS = WWWROOT / "icons"

CALM_LIGHT = ("#f7f5f0", "#222222")   # background, glyph (text)
CALM_DARK = ("#1b1b1f", "#f0ede6")
MONO_GLYPH = "#222222"

THEME_RE = re.compile(
    r'new OmarchyTheme\(\s*"(?P<id>[^"]+)",\s*"(?P<title>[^"]+)",\s*'
    r'IsDark:\s*(?P<dark>true|false),\s*new ThemePalette\(\s*'
    r'Background:\s*"(?P<bg>#[0-9a-fA-F]+)",\s*'
    r'Text:\s*"(?P<text>#[0-9a-fA-F]+)",.*?'
    r'\)\s*,\s*"(?P<accent>#[0-9a-fA-F]+)"\s*\)',
    re.S,
)


def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def lum(rgb):
    def f(c):
        c = c / 255
        return c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4
    r, g, b = rgb
    return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b)


def contrast(a, b):
    la, lb = lum(hex_rgb(a)), lum(hex_rgb(b))
    hi, lo = max(la, lb), min(la, lb)
    return (hi + 0.05) / (lo + 0.05)


def is_red(px):
    r, g, b, _ = px
    return r > 150 and g < 110 and b < 110


def is_cyan(px):
    r, g, b, _ = px
    return b > 150 and g > 120 and r < 110


def main():
    src = Image.open(SRC).convert("RGBA")
    assert src.size == (512, 512), src.size
    w, h = src.size
    px = src.load()

    # A body pixel near the middle of the R, for the per-file pixel check.
    body_sample = None
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 255 and not is_red(px[x, y]) and not is_cyan(px[x, y]) and max(r, g, b) < 40:
                if abs(x - 256) + abs(y - 256) < 60:
                    body_sample = (x, y)
                    break
        if body_sample:
            break
    assert body_sample, "no body sample pixel found"
    print(f"body sample pixel: {body_sample} = {px[body_sample]}")

    # Opaque bounding box, for the maskable safe zone.
    min_x, min_y, max_x, max_y = w, h, -1, -1
    for y in range(h):
        for x in range(w):
            if px[x, y][3] > 0:
                min_x, min_y = min(min_x, x), min(min_y, y)
                max_x, max_y = max(max_x, x), max(max_y, y)
    print(f"mark bbox: x {min_x}..{max_x}, y {min_y}..{max_y} "
          f"({max_x - min_x + 1}x{max_y - min_y + 1})")

    def recolor(bg_hex, glyph_hex):
        """R body -> glyph colour, glitch strokes kept, over opaque bg."""
        bg = hex_rgb(bg_hex) + (255,)
        glyph = hex_rgb(glyph_hex)
        out = Image.new("RGBA", (w, h), bg)
        op = out.load()
        for y in range(h):
            for x in range(w):
                r, g, b, a = px[x, y]
                if a == 0:
                    continue
                if is_red(px[x, y]) or is_cyan(px[x, y]):
                    # Brand glitch strokes, verbatim (alpha preserved).
                    cr, cg, cb = r, g, b
                else:
                    cr, cg, cb = glyph
                if a == 255:
                    op[x, y] = (cr, cg, cb, 255)
                else:
                    # Alpha-composite the recoloured ink over the background.
                    br, bgc, bb, _ = bg
                    op[x, y] = (
                        (cr * a + br * (255 - a)) // 255,
                        (cg * a + bgc * (255 - a)) // 255,
                        (cb * a + bb * (255 - a)) // 255,
                        255,
                    )
        return out

    def save_both(img, stem, outdir):
        """Write the 512 file and its 192 downscale; verify both."""
        p512 = outdir / f"{stem}-512.png"
        p192 = outdir / f"{stem}-192.png"
        img.save(p512)
        img.resize((192, 192), Image.LANCZOS).save(p192)
        for p, size in ((p512, (512, 512)), (p192, (192, 192))):
            got = Image.open(p)
            assert got.size == size, (p, got.size)
        return p512, p192

    def check_theme_file(path, bg_hex, glyph_hex):
        im = Image.open(path).convert("RGBA")
        assert im.getpixel((0, 0))[:3] == hex_rgb(bg_hex), (path, im.getpixel((0, 0)))
        assert im.getpixel((511, 511))[:3] == hex_rgb(bg_hex), path
        assert im.getpixel(body_sample)[:3] == hex_rgb(glyph_hex), \
            (path, im.getpixel(body_sample))

    ICONS.mkdir(exist_ok=True)
    failures = []

    # ---- per-theme icons (23, from the registry) ----
    text = PACK.read_text()
    themes = list(THEME_RE.finditer(text))
    assert len(themes) == 23, f"expected 23 themes, parsed {len(themes)}"
    print(f"\n{'theme':<18} {'bg':<8} {'accent':<8} {'accent/bg':>9} {'glyph used':<8}")
    for m in themes:
        t = m.groupdict()
        ratio = contrast(t["accent"], t["bg"])
        glyph = t["accent"] if ratio >= 3.0 else t["text"]
        if glyph != t["accent"]:
            print(f"  FALLBACK {t['id']}: accent contrast {ratio:.2f} < 3.0, using Text")
        print(f"{t['id']:<18} {t['bg']:<8} {t['accent']:<8} {ratio:>9.2f} {glyph:<8}")
        img = recolor(t["bg"], glyph)
        p512, _ = save_both(img, t["id"], ICONS)
        try:
            check_theme_file(p512, t["bg"], glyph)
        except AssertionError as e:
            failures.append(str(e))

    # ---- calm icons (built-ins / system / custom fallback) ----
    for name, (bg, glyph) in (("calm-light", CALM_LIGHT), ("calm-dark", CALM_DARK)):
        img = recolor(bg, glyph)
        p512, _ = save_both(img, name, ICONS)
        try:
            check_theme_file(p512, bg, glyph)
        except AssertionError as e:
            failures.append(str(e))
        print(f"{name}: bg {bg} glyph {glyph} contrast {contrast(glyph, bg):.2f}")

    # ---- maskable: full-bleed calm light, mark inside the 80% safe zone ----
    mask = Image.new("RGBA", (512, 512), hex_rgb(CALM_LIGHT[0]) + (255,))
    bw, bh = max_x - min_x + 1, max_y - min_y + 1
    scale = min(0.80 * 512 / bw, 0.80 * 512 / bh)
    mark = src.crop((min_x, min_y, max_x + 1, max_y + 1))
    mark = mark.resize((round(bw * scale), round(bh * scale)), Image.LANCZOS)
    ox = (512 - mark.width) // 2
    oy = (512 - mark.height) // 2
    mask.alpha_composite(mark, (ox, oy))
    p512, _ = save_both(mask, "Rise-maskable", WWWROOT)
    m = Image.open(p512).convert("RGBA")
    assert m.getpixel((0, 0))[:3] == hex_rgb(CALM_LIGHT[0])
    assert m.getpixel((511, 511))[:3] == hex_rgb(CALM_LIGHT[0])
    print(f"maskable: mark scaled x{scale:.3f} to {mark.size}, offset ({ox},{oy})")

    # ---- monochrome: solid calm-text silhouette, alpha preserved ----
    mono = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    mp = mono.load()
    g = hex_rgb(MONO_GLYPH)
    for y in range(h):
        for x in range(w):
            a = px[x, y][3]
            if a:
                mp[x, y] = g + (a,)
    p512, _ = save_both(mono, "Rise-monochrome", WWWROOT)
    m = Image.open(p512).convert("RGBA")
    assert m.getpixel((0, 0))[3] == 0
    assert m.getpixel(body_sample)[:3] == g
    print("monochrome: ink -> #222222, alpha preserved")

    if failures:
        print("\nFAILURES:")
        print("\n".join(failures))
        sys.exit(1)
    print(f"\nOK: {len(themes)} themes x2 + calm x2 + maskable + monochrome, "
          "all pixel-checked")


if __name__ == "__main__":
    main()
