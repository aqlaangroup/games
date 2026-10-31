# Cuts each Flow expression sheet (5 portraits side by side) into 5 transparent PNGs for the dialogue card:
# Resources/Art/Portraits/<tag>_<expression>.png. Flow adds a partial row of faces above the real one, so each column
# keeps only its lowest figure: from where it ends (the shoulders) up to the narrowest row above it.
# Run from anywhere: python3 tools/art/cut_portraits.py [tag ...]. Needs Pillow and Xcode's swiftc (macOS 14 or later).
import glob, os, sys
from PIL import Image
from lift import lift

HERE = os.path.dirname(os.path.abspath(__file__))
GAME = os.path.dirname(os.path.dirname(HERE))
SRC = os.path.join(GAME, "art/flow/portraits")
OUT = os.path.join(GAME, "unity/Assets/Content/Resources/Art/Portraits")
EXPRESSIONS = ["neutral", "smile", "sad", "surprised", "thinking"]
HEIGHT = 512

def strong(px, bg, limit=40):
    return abs(px[0] - bg[0]) + abs(px[1] - bg[1]) + abs(px[2] - bg[2]) > limit

def keep_figure(crop):
    # Keeps the figure and what belongs to it (a hand at the chin, an earring): solid pieces whose middle is near the
    # middle. Slivers of the neighbours' shoulders at the sides, and faint pixels outside what's kept, are cleared.
    cw, ch = crop.size
    data = crop.load()
    label = [[-1] * cw for _ in range(ch)]
    parts = []
    # Shoulders run off the bottom, where a neighbour's can touch the figure's; pieces are found above that strip,
    # and the strip keeps only what's under the figure.
    base = ch - max(4, ch // 8)
    for sy in range(base):
        for sx in range(cw):
            if label[sy][sx] != -1 or data[sx, sy][3] <= 100: continue
            n = len(parts); label[sy][sx] = n
            stack, pts = [(sx, sy)], []
            while stack:
                x, y = stack.pop(); pts.append((x, y))
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < cw and 0 <= ny < base and label[ny][nx] == -1 and data[nx, ny][3] > 100:
                        label[ny][nx] = n; stack.append((nx, ny))
            parts.append(pts)
    if not parts: return
    big = max(len(p) for p in parts)
    kept = set()
    for n, pts in enumerate(parts):
        xs = [x for x, _ in pts]
        middle = (min(xs) + max(xs)) / 2
        if len(pts) == big or (len(pts) > big * 0.002 and 0.2 * cw < middle < 0.8 * cw): kept.add(n)
    keep_x = [x for n in kept for x, _ in parts[n]]
    left, right = min(keep_x) - 2, max(keep_x) + 2
    for y in range(ch):
        for x in range(cw):
            n = label[y][x]
            if (n != -1 and n not in kept) or (n == -1 and not left <= x <= right) or (y >= base and not left <= x <= right):
                r, g, b, _ = data[x, y]; data[x, y] = (r, g, b, 0)

def cut(path):
    tag = os.path.splitext(os.path.basename(path))[0]
    im = Image.open(path).convert("RGB")
    w, h = im.size
    small = im.resize((w // 4, h // 4))
    sw, sh = small.size
    bg = small.getpixel((2, sh - 3))
    pix = small.load()
    out = []
    for i, name in enumerate(EXPRESSIONS):
        x0, x1 = i * sw // 5, (i + 1) * sw // 5
        mid0, mid1 = x0 + (x1 - x0) // 5, x1 - (x1 - x0) // 5
        # Faint pixels don't count, so Flow's faded copies above the real row mostly drop out.
        raw = [sum(strong(pix[x, y], bg, 70) for x in range(mid0, mid1)) for y in range(sh)]
        counts = [sum(raw[max(0, y - 1):y + 2]) / len(raw[max(0, y - 1):y + 2]) for y in range(sh)]
        bottom = max(y for y in range(sh) if raw[y] > 2)
        # The figure's top: walking up from inside its face, the narrow stretch above the head (space, or where a row
        # of faces above touches it); cut at its lowest row that is about as narrow as the stretch gets.
        start, lo = max(1, bottom - int(0.40 * sh)), max(0, bottom - int(0.85 * sh))
        widest = max(counts[start:bottom + 1])
        stretch = []
        for y in range(start, lo, -1):
            if counts[y] < 0.6 * widest: stretch.append(y)
            elif stretch and counts[y] >= 0.6 * widest and y < stretch[-1] - 2: break
        if not stretch: stretch = list(range(start, lo, -1))
        least = min(counts[y] for y in stretch)
        top = max(y for y in stretch if counts[y] <= least + 0.05 * widest)
        inset = (x1 - x0) // 25  # trims slivers of the neighbours' shoulders
        box = ((x0 + inset) * 4, top * 4, (x1 - inset) * 4, min(h, bottom * 4 + 8))
        crop = im.crop(box).convert("RGBA")
        cw, ch = crop.size
        # The mask comes from macOS Vision's subject lifting (lift.py), so parts close to the backdrop's cream, like a
        # pale headscarf, stay whole.
        mask = lift(crop).load()
        data = crop.load()
        for y in range(ch):
            for x in range(cw):
                r, g, b, _ = data[x, y]
                a = mask[x, y]
                if 0 < a < 255:  # an edge pixel: take the backdrop's cream out of it, so no pale halo shows
                    f = a / 255
                    r, g, b = (max(0, min(255, round((c - (1 - f) * k) / f))) for c, k in zip((r, g, b), bg))
                data[x, y] = (r, g, b, a)
        keep_figure(crop)
        crop = crop.crop(crop.getbbox() or (0, 0, cw, ch))
        crop = crop.resize((max(1, crop.width * HEIGHT // crop.height), HEIGHT), Image.LANCZOS)
        crop.save(os.path.join(OUT, f"{tag}_{name}.png"))
        out.append(crop)
    return tag, out

os.makedirs(OUT, exist_ok=True)
names = sys.argv[1:] or [os.path.splitext(os.path.basename(p))[0] for p in sorted(glob.glob(SRC + "/*.png"))]
rows = [cut(os.path.join(SRC, n + ".png")) for n in names]
# A contact sheet to check every cut by eye.
sheet = Image.new("RGB", (5 * 180, len(rows) * 190), "#3A2731")
for r, (tag, crops) in enumerate(rows):
    for c, crop in enumerate(crops):
        thumb = crop.copy(); thumb.thumbnail((175, 185))
        sheet.paste(thumb, (c * 180, r * 190), thumb)
os.makedirs(os.path.join(HERE, ".build"), exist_ok=True)
sheet.save(os.path.join(HERE, ".build", "portraits_check.png"))
print("cut", [t for t, _ in rows])
