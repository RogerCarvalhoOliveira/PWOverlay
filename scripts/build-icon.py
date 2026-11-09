"""Package the approved artwork as a Windows ICO. Not needed for normal builds."""
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
source = root / "assets" / "branding" / "icon-source.png"
target = root / "assets" / "branding" / "PWOverlay.ico"
sizes = [(n, n) for n in (16, 20, 24, 32, 40, 48, 64, 128, 256)]
with Image.open(source) as original:
    if original.width != original.height:
        raise ValueError("The icon source must be square")
    original.convert("RGBA").save(target, format="ICO", sizes=sizes, bitmap_format="bmp")
with Image.open(target) as icon:
    if icon.ico.sizes() != set(sizes):
        raise ValueError("Incomplete ICO sizes")
print(f"ICO generated and verified: {target}")
