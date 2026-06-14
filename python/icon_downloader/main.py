import os
import re
import requests
import re

ICON_LIST_FILE = "icons.txt"

OUT_DIR = "../../Assets/DesignSystem/Resources/Textures/Icons"
USS_FILE = "../../Assets/DesignSystem/Resources/UI/Styles/DesignSystem/Icons.uss"

ICONIFY_URL = "https://api.iconify.design"

COLLECTION = "material-symbols"
VARIANT = "rounded"
FILLED = True


def sanitize_svg_for_unity(svg: str) -> str:
    svg = svg.replace("currentColor", "white")

    svg = re.sub(r'width="[^"]+"', 'width="32"', svg)
    svg = re.sub(r'height="[^"]+"', 'height="32"', svg)

    if "viewBox" not in svg:
        svg = svg.replace("<svg ", '<svg viewBox="0 0 24 24" ')

    svg = svg.replace('em"', '"')
    svg = svg.replace('%"', '"')

    svg = re.sub(r"<style.*?>.*?</style>", "", svg, flags=re.DOTALL)
    svg = re.sub(r"<defs.*?>.*?</defs>", "", svg, flags=re.DOTALL)

    # Remove class / id attributes
    svg = re.sub(r'\s(class|id)="[^"]+"', "", svg)

    return svg


def kebab(name: str) -> str:
    return name.replace("_", "-").lower()


def pascal(name: str) -> str:
    return "".join(part.capitalize() for part in re.split(r"[_\-]", name))


with open(ICON_LIST_FILE) as f:
    icons = [line.strip() for line in f if line.strip()]


def main():
    os.makedirs(OUT_DIR, exist_ok=True)

    uss_lines = []

    for icon in icons:
        params = {
            "variant": VARIANT,
            "filled": "1" if FILLED else "0",
        }

        single_icons = icon.split(" ")

        found = False

        for single_icon in single_icons:
            url = f"{ICONIFY_URL}/{COLLECTION}/{kebab(single_icon)}.svg"
            r = requests.get(url, params=params)

            if r.status_code != 200:
                continue

            svg = r.text
            svg = sanitize_svg_for_unity(svg)

            file_name = f"MaterialSymbols{pascal(single_icon)}RoundedFilled.svg"
            out_path = os.path.join(OUT_DIR, file_name)

            with open(out_path, "w") as f:
                f.write(svg)

            class_name = kebab(single_icon)

            uss_lines.append(f""".ds-icon--{class_name} {{
            background-image: resource("Textures/Icons/{file_name.replace('.svg', '')}");
            }}
            """)

            found = True

            break

        if found:
            print(f"✅ {icon}")
        else:
            print(f"❌ {icon}")

    with open(USS_FILE, "r+") as f:
        if "/* Material Symbols (rounded, filled) */" not in f.read():
            f.write("\n/* Material Symbols (rounded, filled) */\n")
        f.write("\n".join(uss_lines))


if __name__ == "__main__":
    main()
