#!/usr/bin/env python3
"""Launch the real rendered C# game, connect to it and run injected-touch checks."""
import json
from pathlib import Path
import subprocess
import time
from dev import ROOT, GODOT, ENV
from inspect import request


def touch(x, y, pressed, finger=7, canceled=False):
    return request("touch", x=x, y=y, pressed=pressed, finger=finger, canceled=canceled)


def tap(name, finger=8):
    state = request()
    rect = state["buttons"][name]
    x, y = rect["x"] + rect["w"] / 2, rect["y"] + rect["h"] / 2
    touch(x, y, True, finger)
    return touch(x, y, False, finger)


def run_checks():
    checks = []
    def check(value, description):
        checks.append({"check": description, "passed": bool(value)})
        print(("PASS: " if value else "FAIL: ") + description, flush=True)
        if not value:
            raise AssertionError(description)

    state = request()
    check(state["language"] == "C#" and state["assembly"] == "StarfallMiner", "Live connection reports the C# game assembly")
    time.sleep(3.3)
    state = request("freeze", value=True)
    check(state["auto"] and state["height"] > state["width"], "Rendered automatic mode is portrait")
    screenshot = request("screenshot", name="auto-portrait.png")
    check(Path(screenshot["screenshot"]).stat().st_size > 1000, "Live rendered portrait screenshot captured")
    state = tap("systems")
    check(state["panel"] == "systems", "Rendered menu opens through a touchscreen finger")
    request("screenshot", name="ship-systems.png")
    state = tap("weapon_1")
    check(state["weapon"] == 1, "Touch selects Nova in Ship Systems")
    state = tap("weapon_2")
    check(state["weapon"] == 2, "Touch selects Ion in Ship Systems")
    state = tap("weapon_0")
    check(state["weapon"] == 0, "Touch selects Photon in Ship Systems")
    before = state["credits"]
    rect = state["buttons"]["upgrade_0"]
    x, y = rect["x"] + rect["w"] / 2, rect["y"] + rect["h"] / 2
    touch(x, y, True, 9)
    request("mouse", x=x, y=y, pressed=True)
    request("mouse", x=x, y=y, pressed=False)
    state = touch(x, y, False, 9)
    request("mouse", x=x, y=y, pressed=True)
    state = request("mouse", x=x, y=y, pressed=False)
    check(state["levels"][0] == 1 and state["credits"] == before - 45, "Rendered touch purchase charges once despite synthesized mouse")
    rect = state["buttons"]["weapon_1"]
    x, y = rect["x"] + rect["w"] / 2, rect["y"] + rect["h"] / 2
    touch(x, y, True)
    state = touch(x, y, False, canceled=True)
    check(state["weapon"] == 0, "Rendered canceled touch does not activate")
    touch(x, y, True)
    state = touch(0, 0, False)
    check(state["weapon"] == 0, "Rendered release outside does not activate")
    state = touch(270, 813, True)
    state = touch(270, 813, False)
    check(state["auto"] and state["panel"] == "systems", "Rendered modal blocks underlying mode control")
    tap("done")
    state = tap("cargo")
    check(state["panel"] == "cargo", "Cargo Log opens through touch")
    request("screenshot", name="cargo-portrait.png")
    tap("close")
    before = request()
    state = tap("mode")
    check(not state["auto"] and state["width"] > state["height"], "Take Control requests desktop landscape by touch")
    check(state["planet_id"] == before["planet_id"] and state["blocks"] == before["blocks"] and
          state["ship_x"] == before["ship_x"] and state["ship_y"] == before["ship_y"] and
          state["projectiles"] == before["projectiles"] and state["pending_debris"] == before["pending_debris"],
          "Rendered mode switch preserves live simulation objects")
    state = request("resize", width=960, height=540)
    request("freeze", value=False)
    time.sleep(0.8)
    state = request("freeze", value=True)
    check(state["width"] > state["height"] and "systems" not in state["buttons"] and "boost" in state["buttons"], "Landscape flight layout replaces management controls")
    stick, fire = state["stick"], state["fire"]
    state = touch(**stick, pressed=True, finger=2)
    check(state["touch_steer"] and state["steer_id"] == 2, "Rendered steering finger acquires the stick")
    state = tap("weapon_2", 3)
    check(state["weapon"] == 2 and state["steer_id"] == 2, "Second finger changes weapon while steering")
    state = touch(**fire, pressed=True, finger=4)
    check(state["touch_fire"] and state["touch_steer"], "Rendered two-finger steering and firing")
    request("drag", x=stick["x"] + 32, y=stick["y"] - 15, finger=2)
    request("screenshot", name="manual-landscape.png")
    state = touch(950, 50, False, 4)
    check(not state["touch_fire"] and state["touch_steer"], "Fire releases independently over HUD")
    touch(**fire, pressed=True, finger=4)
    state = touch(**stick, pressed=False, finger=2)
    check(not state["touch_steer"] and state["touch_fire"], "Steering releases independently while firing")
    state = request("lifecycle", event="focus_out")
    check(state["steer_id"] == -1 and state["fire_id"] == -1, "Focus loss clears rendered flight ownership")
    touch(**stick, pressed=True, finger=2)
    touch(**fire, pressed=True, finger=4)
    state = request("lifecycle", event="pause")
    check(not state["touch_steer"] and not state["touch_fire"], "OS pause clears rendered held input")
    state = tap("pause")
    check(state["paused"], "Rendered pause button works by touch")
    request("screenshot", name="pause-landscape.png")
    muted = state["muted"]
    state = tap("sound")
    check(state["muted"] != muted, "Rendered sound toggle works by touch")
    state = tap("resume")
    check(not state["paused"], "Rendered resume works by touch")
    state = tap("mode")
    request("resize", width=540, height=960)
    check(state["auto"] and state["height"] > state["width"], "Touch restores autopilot and desktop portrait")
    # Exercise phone-sized windows; these are desktop resizes, not Android OS rotation.
    state = request("resize", width=390, height=844)
    time.sleep(0.4)  # Let the HUD's 280 ms mode fade finish before visual capture.
    request("screenshot", name="auto-phone-size.png")
    check(state["height"] > state["width"] and "systems" in state["buttons"], "Phone-sized portrait layout remains usable")
    state = tap("mode")
    request("resize", width=844, height=390)
    request("freeze", value=False); time.sleep(0.8)
    state = request("freeze", value=True)
    time.sleep(0.4)
    request("screenshot", name="manual-phone-size.png")
    check(state["width"] > state["height"] and "weapon_2" in state["buttons"], "Phone-sized landscape exposes all weapon selectors")
    result = {"checks": checks, "passed": len(checks), "input": "Injected InputEventScreenTouch through rendered Viewport",
              "orientation": "macOS Window resize; no physical Android rotation", "state": state}
    (ROOT / "builds/qa/rendered-results.json").write_text(json.dumps(result, indent=2))
    return result


if __name__ == "__main__":
    (ROOT / "builds/qa").mkdir(parents=True, exist_ok=True)
    log = open(ROOT / "builds/qa/runtime.log", "w")
    process = subprocess.Popen([GODOT, "--path", str(ROOT), "--scene", "res://tests/qa.tscn", "--log-file",
                                str(ROOT / "builds/qa/godot.log"), "--", "--qa"], cwd=ROOT, env=ENV, stdout=log, stderr=subprocess.STDOUT)
    try:
        deadline = time.monotonic() + 30
        while True:
            try:
                request()
                break
            except (OSError, RuntimeError):
                if process.poll() is not None or time.monotonic() > deadline:
                    raise RuntimeError("Rendered inspection launch failed; see builds/qa/runtime.log")
                time.sleep(0.2)
        result = run_checks()
        print(f"RESULT: {result['passed']} rendered touchscreen checks passed", flush=True)
    finally:
        try:
            request("quit")
            process.wait(timeout=5)
        except (OSError, RuntimeError, subprocess.TimeoutExpired):
            process.terminate()
        log.close()
