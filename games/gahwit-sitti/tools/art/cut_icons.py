# Cuts the Flow icon sheets (2x2 grids) into one transparent PNG per item: Resources/Art/Items/<chain>_<level>.png
# and <generator id>.png. The mapping follows flow_brief.md section 2.
# Run from anywhere: python3 tools/art/cut_icons.py. Needs Pillow and Xcode's swiftc (macOS 14 or later).
import os
from PIL import Image
from lift import lift

HERE = os.path.dirname(os.path.abspath(__file__))
GAME = os.path.dirname(os.path.dirname(HERE))
SRC = os.path.join(GAME, "art/flow/icons")
OUT = os.path.join(GAME, "unity/Assets/Content/Resources/Art/Items")
SHEETS = {
    1: ["coffee_1", "coffee_2", "coffee_3", "coffee_4"], 2: ["coffee_5", "coffee_6", "coffee_7", "coffee_8"],
    3: ["coffee_9", "coffee_10", "gen_coffee", "gen_sweets"], 4: ["sweets_1", "sweets_2", "sweets_3", "sweets_4"],
    5: ["sweets_5", "sweets_6", "sweets_7", "sweets_8"], 6: ["sweets_9", "sweets_10", "gen_tea", "gen_saffron"],
    7: ["tea_1", "tea_2", "tea_3", "tea_4"], 8: ["tea_5", "tea_6", "tea_7", "tea_8"],
    9: ["saudi_1", "saudi_2", "saudi_3", "saudi_4"], 10: ["saudi_5", "saudi_6", "saudi_7", "saudi_8"],
    11: ["dairy_1", "dairy_2", "dairy_3", "dairy_4"], 12: ["dairy_5", "dairy_6", "dairy_7", "dairy_8"],
    13: ["gen_dairy"],
}
SIZE = 256

def transparent(im, bg):
    # Vision's mask keeps pale items (milk, cream, sugar) whole; edge pixels lose the backdrop's cream, so no halo shows.
    mask = lift(im).load()
    im = im.convert("RGBA")
    data = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, _ = data[x, y]
            a = mask[x, y]
            if 0 < a < 255:
                f = a / 255
                r, g, b = (max(0, min(255, round((c - (1 - f) * k) / f))) for c, k in zip((r, g, b), bg))
            data[x, y] = (r, g, b, a)
    return im

def fit(im):
    im = im.crop(im.getbbox() or (0, 0, im.width, im.height))
    side = max(im.width, im.height)
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.paste(im, ((side - im.width) // 2, (side - im.height) // 2))
    return square.resize((SIZE, SIZE), Image.LANCZOS)

os.makedirs(OUT, exist_ok=True)
made = []
for n, names in SHEETS.items():
    path = f"{SRC}/sheet_{n:02d}.png"
    if not os.path.exists(path): continue
    im = Image.open(path).convert("RGB")
    w, h = im.size
    bg = im.getpixel((4, 4))
    cells = [(0, 0, w, h)] if len(names) == 1 else [(0, 0, w // 2, h // 2), (w // 2, 0, w, h // 2), (0, h // 2, w // 2, h), (w // 2, h // 2, w, h)]
    for name, box in zip(names, cells):
        inset = (box[2] - box[0]) // 30
        cell = im.crop((box[0] + inset, box[1] + inset, box[2] - inset, box[3] - inset))
        fit(transparent(cell, bg)).save(f"{OUT}/{name}.png")
        made.append(name)
print(len(made), "icons:", made)
