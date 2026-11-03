#!/usr/bin/env python3
"""Build and run against the installed .NET Godot, with project-local SDK caches."""
import argparse
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[1]
GODOT = os.environ.get("GODOT4") or shutil.which("godot") or "/Applications/Godot_mono.app/Contents/MacOS/Godot"
ENV = dict(os.environ, DOTNET_CLI_HOME=str(ROOT / ".godot/dotnet"), NUGET_PACKAGES=str(ROOT / ".godot/nuget"),
           DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1")


def run(*args):
    subprocess.run(args, cwd=ROOT, env=ENV, check=True)


def build():
    args = ["dotnet", "build", "StarfallMiner.csproj", "-p:NuGetAudit=false"]
    if os.name == "nt":
        # The default NuGet.Config includes a Godot package feed in a macOS app bundle.
        args.append("-p:RestoreConfigFile=NuGet.Windows.Config")
    run(*args)


def godot(*args):
    run(GODOT, "--path", str(ROOT), "--log-file", str(ROOT / ".godot/runtime.log"), *args)


def configure_android():
    config = ROOT / "builds/toolchain/android-env.json"
    if not config.exists():
        raise SystemExit("Run python3 tools/setup_android.py first (JDK 17 and Android SDK are required).")
    paths = json.loads(config.read_text())
    ENV.update(JAVA_HOME=paths["java"], ANDROID_HOME=paths["sdk"], ANDROID_SDK_ROOT=paths["sdk"])
    # Godot's native APK exporter reads these two editor settings, not ANDROID_HOME.
    settings = Path.home() / "Library/Application Support/Godot/editor_settings-4.7.tres"
    text = settings.read_text()
    backup = ROOT / ".godot/editor-settings-before-android.tres"
    if not backup.exists():
        backup.write_text(text)
    for key, value in {"export/android/java_sdk_path": paths["java"], "export/android/android_sdk_path": paths["sdk"]}.items():
        text, count = re.subn(r"^" + re.escape(key) + r" = .*?$", key + " = " + json.dumps(value), text, flags=re.MULTILINE)
        if count != 1:
            raise SystemExit(f"Could not find Godot editor setting: {key}")
    settings.write_text(text)
    if not ENV.get("GODOT_ANDROID_KEYSTORE_DEBUG_PATH"):
        key = ROOT / ".godot/keys/android-debug.keystore"
        key.parent.mkdir(parents=True, exist_ok=True)
        if not key.exists():
            run(str(Path(paths["java"]) / "bin/keytool"), "-genkeypair", "-keystore", str(key), "-storepass", "android", "-keypass", "android",
                "-alias", "androiddebugkey", "-dname", "CN=Starfall Miner Local Debug,O=Local Development,C=US", "-keyalg", "RSA", "-keysize", "2048", "-validity", "10000")
        ENV.update(GODOT_ANDROID_KEYSTORE_DEBUG_PATH=str(key), GODOT_ANDROID_KEYSTORE_DEBUG_USER="androiddebugkey", GODOT_ANDROID_KEYSTORE_DEBUG_PASSWORD="android")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["build", "import", "test", "test-rendered", "balance", "run", "qa", "export-macos", "export-android"])
    parser.add_argument("--label", default="current")
    parser.add_argument("--minutes", type=int, default=60)
    args = parser.parse_args()
    (ROOT / ".godot").mkdir(exist_ok=True)
    if args.action == "build":
        build()
    elif args.action == "import":
        build()
        godot("--headless", "--import")
    elif args.action in ("test", "test-rendered"):
        build()
        godot(*(["--headless"] if args.action == "test" else []), "--scene", "res://tests/smoke.tscn", "--", "--test")
    elif args.action == "balance":
        if not re.fullmatch(r"[A-Za-z0-9_-]{1,32}", args.label):
            raise SystemExit("Balance label must contain only letters, numbers, underscores or hyphens.")
        build()
        godot("--headless", "--scene", "res://tests/balance.tscn", "--", "--test", f"--balance-label={args.label}", f"--balance-minutes={args.minutes}")
    elif args.action == "qa":
        build()
        godot("--scene", "res://tests/qa.tscn", "--", "--qa")
    elif args.action == "run":
        build()
        godot()
    else:
        (ROOT / "builds").mkdir(exist_ok=True)
        (ROOT / "builds/.gdignore").touch()
        platform = "macOS" if args.action == "export-macos" else "Android"
        if platform == "macOS":
            run("python3", "tools/prepare_macos.py")
        else:
            configure_android()
        output = "builds/Starfall-Miner-macOS.zip" if platform == "macOS" else "builds/Starfall-Miner-Android.apk"
        godot("--headless", "--export-debug", platform, output)
        with zipfile.ZipFile(ROOT / output) as archive:
            if archive.testzip() is not None:
                raise SystemExit("Export archive CRC verification failed")
        if platform == "macOS":
            run("/usr/bin/ditto", "-x", "-k", output, "builds/macos")
            run("/usr/bin/codesign", "--verify", "--deep", "--strict", "--verbose=2", "builds/macos/Starfall Miner.app")
            print("Verified runnable app: builds/macos/Starfall Miner.app")
        else:
            run("python3", "tools/verify_android.py")
