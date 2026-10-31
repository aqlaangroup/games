# Subject masks from macOS Vision's subject lifting (lift.swift, built on first use into .build/), so a pale item or
# headscarf on Flow's cream backdrop is cut out whole, which keying by colour can't do.
import os, subprocess, tempfile
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
TOOL = os.path.join(HERE, ".build", "lift")

def lift(image):
    """The subject mask for a Pillow image, as an "L" image the same size."""
    source = os.path.join(HERE, "lift.swift")
    if not os.path.exists(TOOL) or os.path.getmtime(TOOL) < os.path.getmtime(source):
        os.makedirs(os.path.dirname(TOOL), exist_ok=True)
        subprocess.run(["swiftc", "-O", source, "-o", TOOL], check=True)
    with tempfile.TemporaryDirectory() as work:
        raw, matte = os.path.join(work, "in.png"), os.path.join(work, "mask.png")
        image.convert("RGB").save(raw)
        subprocess.run([TOOL, raw, matte], check=True)
        if not os.path.exists(matte): return Image.new("L", image.size, 0)
        return Image.open(matte).convert("L").resize(image.size)
