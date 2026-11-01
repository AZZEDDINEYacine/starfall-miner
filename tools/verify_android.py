#!/usr/bin/env python3
"""Verify the signed C# APK, including native 16 KB page compatibility."""
import hashlib
import configparser
import json
import os
from pathlib import Path
import struct
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def verify():
    config = json.loads((ROOT / "builds/toolchain/android-env.json").read_text())
    tools = Path(config["sdk"]) / "build-tools/35.0.1"
    env = dict(os.environ, JAVA_HOME=config["java"])
    apk = ROOT / "builds/Starfall-Miner-Android.apk"
    qa = ROOT / "builds/qa"
    qa.mkdir(parents=True, exist_ok=True)

    def capture(filename, *args):
        result = subprocess.run([str(a) for a in args], env=env, text=True, capture_output=True, check=True)
        (qa / filename).write_text(result.stdout + result.stderr)
        return result.stdout + result.stderr

    sig = capture("android-signature.txt", tools / "apksigner", "verify", "--verbose", "--print-certs", apk)
    capture("android-alignment.txt", tools / "zipalign", "-c", "-P", "16", "4", apk)
    metadata = capture("android-badging.txt", tools / "aapt2", "dump", "badging", apk)
    presets = configparser.RawConfigParser()
    presets.read(ROOT / "export_presets.cfg")
    options = presets["preset.1.options"]
    version_code = options["version/code"]
    version_name = options["version/name"].strip('"')
    for expected in ["name='com.starfall.miner.demo'", f"versionCode='{version_code}'", f"versionName='{version_name}'", "minSdkVersion:'24'", "targetSdkVersion:'36'", "native-code: 'arm64-v8a'"]:
        if expected not in metadata:
            raise RuntimeError(f"Unexpected Android metadata: missing {expected}")
    alignments = {}
    with zipfile.ZipFile(apk) as archive:
        if archive.testzip() is not None:
            raise RuntimeError("APK ZIP CRC check failed")
        names = archive.namelist()
        assemblies = [name for name in names if name.endswith("StarfallMiner.dll")]
        if len(assemblies) != 1 or b"QaBridge" in archive.read(assemblies[0]):
            raise RuntimeError("Missing game assembly or development code included")
        if any(name.endswith((".gd", ".gdc")) or name.startswith("assets/tests/") for name in names):
            raise RuntimeError("Retired scripts or test scenes included")
        for name in names:
            if not name.endswith(".so"):
                continue
            data = archive.read(name)
            if data[:5] != b"\x7fELF\x02":
                raise RuntimeError(f"Expected ARM64 ELF: {name}")
            offset = struct.unpack_from("<Q", data, 32)[0]
            entry_size, count = struct.unpack_from("<HH", data, 54)
            loads = [struct.unpack_from("<Q", data, offset + i * entry_size + 48)[0] for i in range(count)
                     if struct.unpack_from("<I", data, offset + i * entry_size)[0] == 1]
            alignments[name] = min(loads) if loads else 0
        if not alignments or any(value < 16384 for value in alignments.values()):
            raise RuntimeError("A native library does not support 16 KB load segment alignment")
    (qa / "android-elf-alignment.json").write_text(json.dumps(alignments, indent=2))
    for line in sig.splitlines():
        if "Signer #1 certificate SHA-256 digest:" in line:
            print(line)
    print("Verified APK: identity, signatures, CRC, ARM64 C# payload, excluded QA code, 16 KB ZIP/ELF alignment")
    print("APK SHA256:", hashlib.sha256(apk.read_bytes()).hexdigest())


if __name__ == "__main__":
    verify()
