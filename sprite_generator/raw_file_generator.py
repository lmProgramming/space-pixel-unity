import os

INPUT_FILE = "sprite_generator/raw_sprites.txt"
SAVE_DIR = "sprite_generator/inputs"

os.makedirs(SAVE_DIR, exist_ok=True)

print(f"Reading from {INPUT_FILE}...")
try:
    with open(INPUT_FILE, "r") as f:
        lines = [line.strip() for line in f.readlines()]
except FileNotFoundError:
    print(
        f"ERROR: Could not find '{INPUT_FILE}'. Create it and paste the LLM output inside."
    )
    exit()

current_filename = None
parsed_files: dict[str, list[str]] = {}

for line in lines:
    if not line:
        continue

    if line.endswith(".txt"):
        current_filename = line
        parsed_files[current_filename] = []

    elif current_filename and all(c in "01234567" for c in line):
        parsed_files[current_filename].append(line)

for filename, matrix in parsed_files.items():
    if not matrix:
        continue

    filepath = os.path.join(SAVE_DIR, filename)
    with open(filepath, "w") as f:
        f.write("\n".join(matrix))

    print(f"Saved text file: {filepath}")

print("Parsing complete! Ready for PNG generation.")
