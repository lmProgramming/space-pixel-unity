import os

INPUT_FILE = "sprite_generator/raw_sprites.txt"
SAVE_DIR = "sprite_generator/inputs"

def generate_raw_file():
    os.makedirs(SAVE_DIR, exist_ok=True)

    print(f"Reading from {INPUT_FILE}...")
    try:
        with open(INPUT_FILE, "r") as f:
            lines = [line.strip() for line in f.readlines()]
    except FileNotFoundError:
        print(f"ERROR: Could not find '{INPUT_FILE}'.")
        exit()

    current_filename = None
    parsed_files: dict[str, list[str]] = {}

    for line in lines:
        line_stripped = line.strip()
        if not line_stripped:
            continue

        if line_stripped.endswith(".txt"):
            current_filename = line_stripped
            parsed_files[current_filename] = []
            continue

        if current_filename:
            parts = line_stripped.split(" ")
            for part in parts:
                if not part.strip():
                    continue
                if not all(c in "01234567" for c in part):
                    continue
                parsed_files[current_filename].append(part)

    for filename, matrix in parsed_files.items():
        if not matrix:
            continue

        filepath = os.path.join(SAVE_DIR, filename)
        with open(filepath, "w") as f:
            f.write("\n".join(matrix))

        print(f"Saved text file: {filepath}")

    print("Parsing complete! Ready for PNG generation.")
