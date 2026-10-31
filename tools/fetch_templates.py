#!/usr/bin/env python3
"""Extract matching official .NET templates using HTTP ranges and ZIP CRC checks.

Avoids downloading unrelated platforms from the 1.2 GB official archive.
Cached range files can be reused if a download is interrupted.
"""
from concurrent.futures import ThreadPoolExecutor
import argparse
from pathlib import Path
import struct
import subprocess
import zlib

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / "builds/toolchain"
URL = "https://downloads.godotengine.org/?version=4.7.2&flavor=stable&slug=mono_export_templates.tpz&platform=templates"
ARCHIVE_SIZE = 1202598411
CHUNK = 2 * 1024 * 1024


def fetch(begin, end, path):
    if path.exists() and path.stat().st_size == end - begin + 1:
        return path.read_bytes()
    temporary = path.with_suffix(".part")
    subprocess.run(["curl", "-fsSL", "--retry", "3", "--max-time", "180", "--range", f"{begin}-{end}",
                    "--max-filesize", str(end - begin + 1), "-o", str(temporary), URL], check=True)
    if temporary.stat().st_size != end - begin + 1:
        raise RuntimeError("Server returned an incorrect byte range")
    temporary.replace(path)
    return path.read_bytes()


def install(name, header):
    output = CACHE / "templates" / name
    if output.exists():
        existing = output.read_bytes()
        if len(existing) == header[9] and zlib.crc32(existing) == header[7]:
            print(f"Already verified: {output}", flush=True)
            return
    size, offset = header[8], header[16]
    prefix = fetch(offset, offset + 1023, CACHE / f"{name}.header")
    local = struct.unpack_from("<4s5H3L2H", prefix)
    begin = offset + 30 + local[9] + local[10]
    count = (size + CHUNK - 1) // CHUNK
    def part(i):
        lo, hi = begin + i * CHUNK, min(begin + size - 1, begin + (i + 1) * CHUNK - 1)
        return fetch(lo, hi, CACHE / f"{name}.{i:03d}.range")
    print(f"Downloading {name}: {size // 1024 // 1024} MiB in {count} ranges", flush=True)
    with ThreadPoolExecutor(max_workers=4) as pool:
        compressed = b"".join(pool.map(part, range(count)))
    data = zlib.decompress(compressed, -15) if header[4] == 8 else compressed
    if len(data) != header[9] or zlib.crc32(data) != header[7]:
        raise RuntimeError(f"Official archive CRC mismatch for {name}")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(data)
    for i in range(count):
        (CACHE / f"{name}.{i:03d}.range").unlink()
    print(f"Verified {output}", flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("names", nargs="*", default=["macos.zip", "android_debug.apk", "android_release.apk", "version.txt"])
    args = parser.parse_args()
    CACHE.mkdir(parents=True, exist_ok=True)
    tail = fetch(ARCHIVE_SIZE - 65536, ARCHIVE_SIZE - 1, CACHE / "templates-tail.bin")
    eocd = struct.unpack_from("<4s4H2LH", tail, tail.rfind(b"PK\x05\x06"))
    cursor = eocd[6] - (ARCHIVE_SIZE - len(tail))
    entries = {}
    while tail[cursor:cursor + 4] == b"PK\x01\x02":
        header = struct.unpack_from("<4s6H3L5H2L", tail, cursor)
        name = tail[cursor + 46:cursor + 46 + header[10]].decode().removeprefix("templates/")
        entries[name] = header
        cursor += 46 + header[10] + header[11] + header[12]
    for name in args.names:
        install(name, entries[name])
