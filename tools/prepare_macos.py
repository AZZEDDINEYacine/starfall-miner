#!/usr/bin/env python3
"""Thin the official universal template for Godot 4.7's arm64 export preset."""
from pathlib import Path
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "builds/toolchain/templates"


def prepare():
    output = DEST / "macos-arm64.zip"
    if output.exists():
        return
    with zipfile.ZipFile(DEST / "macos.zip") as source, zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as target:
        for info in source.infolist():
            data = source.read(info)
            if info.filename.endswith(".universal"):
                binary = DEST / "thin-input"
                thinned = DEST / "thin-output"
                binary.write_bytes(data)
                subprocess.run(["/usr/bin/lipo", str(binary), "-thin", "arm64", "-output", str(thinned)], check=True)
                data = thinned.read_bytes()
                info.filename = info.filename.removesuffix(".universal") + ".arm64"
                binary.unlink(); thinned.unlink()
            target.writestr(info, data)
    print(f"Prepared Apple Silicon template: {output}")


if __name__ == "__main__":
    prepare()
