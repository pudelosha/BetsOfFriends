from pathlib import Path
from PIL import Image
import os

TARGET_DIR = Path(
    r"C:\Projekty\Portfolio-Repository\DotNet-Ionic-Angular-AuthSystem\Frontend\FrontendApp\src\assets\images"
)

SUPPORTED_EXTENSIONS = {".jpg", ".jpeg", ".png", ".webp"}

JPEG_QUALITY = 70
WEBP_QUALITY = 70
PNG_COMPRESS_LEVEL = 9


def optimize_image(image_path: Path) -> None:
    temp_path = image_path.with_suffix(image_path.suffix + ".tmp")

    try:
        with Image.open(image_path) as img:
            original_width, original_height = img.size

            new_width = max(1, original_width // 2)
            new_height = max(1, original_height // 2)

            resized = img.resize((new_width, new_height), Image.Resampling.LANCZOS)

            ext = image_path.suffix.lower()

            if ext in {".jpg", ".jpeg"}:
                if resized.mode in {"RGBA", "P"}:
                    resized = resized.convert("RGB")

                resized.save(
                    temp_path,
                    format="JPEG",
                    quality=JPEG_QUALITY,
                    optimize=True,
                    progressive=True,
                )

            elif ext == ".png":
                resized.save(
                    temp_path,
                    format="PNG",
                    optimize=True,
                    compress_level=PNG_COMPRESS_LEVEL,
                )

            elif ext == ".webp":
                resized.save(
                    temp_path,
                    format="WEBP",
                    quality=WEBP_QUALITY,
                    method=6,
                )

        os.replace(temp_path, image_path)

        print(
            f"Optimized: {image_path} "
            f"({original_width}x{original_height} -> {new_width}x{new_height})"
        )

    except Exception as error:
        if temp_path.exists():
            temp_path.unlink()

        print(f"Failed: {image_path}")
        print(f"Reason: {error}")


def main() -> None:
    if not TARGET_DIR.exists():
        print(f"Folder does not exist: {TARGET_DIR}")
        return

    images = [
        path
        for path in TARGET_DIR.rglob("*")
        if path.is_file() and path.suffix.lower() in SUPPORTED_EXTENSIONS
    ]

    if not images:
        print("No supported images found.")
        return

    print(f"Found {len(images)} images.")
    print(f"Optimizing images in: {TARGET_DIR}")
    print()

    for image_path in images:
        optimize_image(image_path)

    print()
    print("Done.")


if __name__ == "__main__":
    main()