import os
import glob
import random
from PIL import Image

SAVE_DIR = "Assets/Sprites/Generated"
NOISE_VARIANCE = 12  # How much RGB shift to apply to metal/armor for texture

# Create Unity directory if it doesn't exist
os.makedirs(SAVE_DIR, exist_ok=True)

# Palettes
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
    """Applies slight RGB variation to add texture to metal, ignores transparent/lights."""
    r, g, b, a = color_tuple
    # Skip transparency, pure black, and glowing lights (assuming lights are highly saturated)
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


def process_file(filepath):
    # Only process base visual files (skip the armor ones to avoid double-processing)
    if filepath.endswith("_armor.txt"):
        return

    base_name = os.path.splitext(os.path.basename(filepath))[0]
    faction = "enemy" if "enemy" in base_name else "player"
    palette = VISUAL_PALETTES[faction]

    armor_filepath = f"{base_name}_armor.txt"
    has_armor = os.path.exists(armor_filepath)

    # Read Visual Text
    with open(filepath, "r") as f:
        visual_lines = [line.strip() for line in f.readlines() if line.strip()]

    width, height = len(visual_lines[0]), len(visual_lines)

    # Generate Visual PNG
    img_vis = Image.new("RGBA", (width, height))
    vis_pixels = []
    for row in visual_lines:
        for char in row:
            vis_pixels.append(
                apply_noise(palette.get(char, (255, 0, 255, 255)))
            )  # Magenta fallback
    img_vis.putdata(vis_pixels)
    img_vis.save(os.path.join(SAVE_DIR, f"{base_name}.png"))

    # Generate Armor PNG
    if has_armor:
        with open(armor_filepath, "r") as f:
            armor_lines = [line.strip() for line in f.readlines() if line.strip()]
        img_arm = Image.new("RGBA", (width, height))
        arm_pixels = [
            ARMOR_PALETTE.get(char, (0, 0, 0, 0)) for row in armor_lines for char in row
        ]
        img_arm.putdata(arm_pixels)
        img_arm.save(os.path.join(SAVE_DIR, f"{base_name}_armor.png"))

    print(f"Processed: {base_name} ({width}x{height})")


for txt_file in glob.glob("sprite_generator/inputs/*.txt"):
    process_file(txt_file)

print(f"All generated sprites saved to {SAVE_DIR}!")
