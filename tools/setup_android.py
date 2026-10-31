#!/usr/bin/env python3
"""Install a project-local JDK 17 and Android SDK for native APK export.

Downloads official archives, verifies SHA-256 and installs only the prebuilt-export
SDK packages. sdkmanager presents the Android SDK licenses during installation.
"""
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tarfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "builds/toolchain"
CHUNK = 2 * 1024 * 1024


def download(url, size, checksum, filename):
    target = DEST / filename
    if target.exists() and hashlib.sha256(target.read_bytes()).hexdigest() == checksum:
        return target
    def part(i):
        lo, hi = i * CHUNK, min(size - 1, (i + 1) * CHUNK - 1)
        path = DEST / f"{filename}.{i:03d}.chunk"
        if not path.exists() or path.stat().st_size != hi - lo + 1:
            subprocess.run(["curl", "-fsSL", "--retry", "3", "--max-time", "180", "--range", f"{lo}-{hi}",
                            "--max-filesize", str(hi - lo + 1), "-o", str(path), url], check=True)
        if path.stat().st_size != hi - lo + 1:
            raise RuntimeError(f"Incorrect download range {filename} {i}")
        return path
    count = (size + CHUNK - 1) // CHUNK
    print(f"Downloading {filename} ({size // 1024 // 1024} MiB)", flush=True)
    with ThreadPoolExecutor(max_workers=4) as pool:
        parts = list(pool.map(part, range(count)))
    with target.open("wb") as output:
        for path in parts:
            output.write(path.read_bytes())
    if hashlib.sha256(target.read_bytes()).hexdigest() != checksum:
        raise RuntimeError(f"SHA-256 mismatch: {filename}")
    for path in parts:
        path.unlink()
    print(f"Verified {filename}", flush=True)
    return target


if __name__ == "__main__":
    DEST.mkdir(parents=True, exist_ok=True)
    (ROOT / "builds/.gdignore").touch()
    metadata = DEST / "jdk17.json"
    if not metadata.exists():
        subprocess.run(["curl", "-fsSL", "--max-time", "30", "-o", str(metadata),
                        "https://api.adoptium.net/v3/assets/latest/17/hotspot?architecture=aarch64&image_type=jdk&os=mac"], check=True)
    package = json.loads(metadata.read_text())[0]["binary"]["package"]
    if not list(DEST.glob("jdk-*/Contents/Home/bin/java")):
        archive = download(package["link"], package["size"], package["checksum"], "jdk17.tar.gz")
        with tarfile.open(archive) as bundle:
            bundle.extractall(DEST, filter="data")
    java = next(DEST.glob("jdk-*/Contents/Home"))
    sdk = DEST / "android-sdk"
    sdkmanager = sdk / "cmdline-tools/latest/bin/sdkmanager"
    if not sdkmanager.exists():
        archive = download("https://dl.google.com/android/repository/commandlinetools-mac_arm64-15859902_latest.zip", 156083281,
                           "835b62a26162b229b441d1f6d4680383815a270809eb33522c0d480fa5002c4e", "android-command-tools.zip")
        temporary = DEST / "android-command-tools"
        with zipfile.ZipFile(archive) as bundle:
            bundle.extractall(temporary)
        sdkmanager.parent.parent.parent.mkdir(parents=True, exist_ok=True)
        shutil.move(str(temporary / "cmdline-tools"), str(sdkmanager.parent.parent))
        for executable in sdkmanager.parent.iterdir():
            executable.chmod(0o755)
    env = dict(os.environ, JAVA_HOME=str(java), ANDROID_HOME=str(sdk), ANDROID_SDK_ROOT=str(sdk))
    print("Installing Android platform-tools, build-tools 35.0.1 and platform 35 (Android SDK licenses apply).", flush=True)
    subprocess.run([str(sdkmanager), f"--sdk_root={sdk}", "platform-tools", "build-tools;35.0.1", "platforms;android-35"],
                   env=env, input="y\n" * 20, text=True, check=True)
    (DEST / "android-env.json").write_text(json.dumps({"java": str(java), "sdk": str(sdk)}, indent=2))
    print(f"Android toolchain ready: {sdk}", flush=True)
