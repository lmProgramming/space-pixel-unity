import os
import glob
import random
from PIL import Image

SAVE_DIR = "Assets/Sprites/Generated"
READ_DIR = "sprite_generator/inputs"
NOISE_VARIANCE = 12

VISUAL_PALETTES = {
    "player": {
        "0": (0, 0, 0, 0),  # Transparent
        "1": (20, 20, 20, 255),  # Black Outlines
        "2": (75, 94, 101, 255),  # Deep Shadow
        "3": (129, 151, 150, 255),  # Base Armor
        "4": (195, 212, 206, 255),  # Highlights
        "5": (255, 136, 0, 255),  # Glass/Lights (No noise)
        "6": (255, 200, 50, 255),  # Hot Plasma (No noise)
        "7": (85, 95, 100, 255),  # Dark Iron
    },
    "enemy": {
        "0": (0, 0, 0, 0),
        "1": (15, 15, 15, 255),
        "2": (35, 35, 40, 255),
        "3": (65, 70, 75, 255),
        "4": (105, 110, 115, 255),
        "5": (220, 20, 40, 255),
        "6": (255, 80, 50, 255),
        "7": (45, 40, 35, 255),
    },
}

ARMOR_PALETTE = {
    "0": (0, 0, 0, 0),
    "4": (255, 255, 255, 255),
    "3": (200, 200, 200, 255),
    "7": (150, 150, 150, 255),
    "1": (60, 60, 60, 255),
    "2": (50, 50, 50, 255),
    "5": (25, 25, 25, 255),
    "6": (10, 10, 10, 255),
}


def apply_noise(color_tuple):
    r, g, b, a = color_tuple
    if (
        a == 0
        or (r, g, b) == (20, 20, 20)
        or (r, g, b) == (15, 15, 15)
        or max(r, g, b) > 210
    ):
        return color_tuple
    noise = random.randint(-NOISE_VARIANCE, NOISE_VARIANCE)
    return (
        max(0, min(255, r + noise)),
        max(0, min(255, g + noise)),
        max(0, min(255, b + noise)),
        a,
    )


# Find all text files in the directory
txt_files = glob.glob(os.path.join(READ_DIR, "*.txt"))

if not txt_files:
    print(f"No .txt files found in {READ_DIR}. Run the parser script first!")
    exit()

for filepath in txt_files:
    filename = os.path.basename(filepath)
    base_name = filename.replace(".txt", "")

    with open(filepath, "r") as f:
        matrix = [line.strip() for line in f.readlines() if line.strip()]

    if not matrix:
        continue

    width = len(matrix[0])
    height = len(matrix)

    img = Image.new("RGBA", (width, height))
    pixels = []

    # Generate Armor Map
    if "_armor" in base_name:
        for row in matrix:
            for char in row:
                pixels.append(ARMOR_PALETTE.get(char, (0, 0, 0, 0)))

    # Generate Visual Map
    else:
        faction = "enemy" if "enemy" in base_name else "player"
        palette = VISUAL_PALETTES[faction]
        for row in matrix:
            for char in row:
                pixels.append(apply_noise(palette.get(char, (255, 0, 255, 255))))

    img.putdata(pixels)
    png_path = os.path.join(SAVE_DIR, f"{base_name}.png")
    img.save(png_path)

    print(f"Generated PNG: {png_path} ({width}x{height})")

print("All PNGs generated successfully!")
