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
    check(state["blocks"] > 2000, "Rendered new planet contains thousands of ore tiles")
    check(len(state["owned_weapons"]) == 1 and not state["drones"], "A fresh game starts with one owned lance and no free drones")
    screenshot = request("screenshot", name="auto-portrait.png")
    check(Path(screenshot["screenshot"]).stat().st_size > 1000, "Live rendered portrait screenshot captured")
    # Existing multi-weapon input checks use a migrated unlocked inventory.
    request("legacy_controls_fixture")
    state = tap("systems")
    check(state["panel"] == "systems", "Rendered menu opens through a touchscreen finger")
    request("screenshot", name="ship-systems.png")
    state = tap("tab_fleet")
    state = tap("primary_2")
    check(state["weapon"] == 1, "Touch selects Nova as the primary ship weapon")
    state = tap("primary_3")
    check(state["weapon"] == 2, "Touch selects Ion as the primary ship weapon")
    state = tap("primary_1")
    check(state["weapon"] == 0, "Touch selects Photon as the primary ship weapon")
    rect = state["buttons"]["primary_2"]
    x, y = rect["x"] + rect["w"] / 2, rect["y"] + rect["h"] / 2
    touch(x, y, True)
    state = touch(x, y, False, canceled=True)
    check(state["weapon"] == 0, "Rendered canceled touch does not activate")
    touch(x, y, True)
    state = touch(0, 0, False)
    check(state["weapon"] == 0, "Rendered release outside does not activate")
    state = tap("tab_systems")
    before = state["credits"]
    rect = state["buttons"]["upgrade_2"]
    x, y = rect["x"] + rect["w"] / 2, rect["y"] + rect["h"] / 2
    touch(x, y, True, 9)
    request("mouse", x=x, y=y, pressed=True)
    request("mouse", x=x, y=y, pressed=False)
    state = touch(x, y, False, 9)
    request("mouse", x=x, y=y, pressed=True)
    state = request("mouse", x=x, y=y, pressed=False)
    check(state["levels"][2] == 1 and state["credits"] == before - 50, "Rendered touch purchase charges once despite synthesized mouse")
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
    check(not state["auto"] and state["height"] > state["width"] and
          (state["window_width"], state["window_height"]) == (before["window_width"], before["window_height"]),
          "Take Control retains the native portrait window")
    check(not state["layout_auto"] and "boost" in state["buttons"],
          "Portrait navigation HUD commits without an orientation wait")
    request("screenshot", name="manual-transition-start.png")
    activation_duration = state["activation_duration"]
    state = request("activation_frame", seconds=activation_duration * 0.05)
    check(state["hologram_channels"][0]["brightness"] > 1 and state["hologram_channels"][-1]["brightness"] == 0,
          "Holographic identity overshoots before the command module ignites")
    request("screenshot", name="hologram-ignition.png")
    state = request("activation_frame", seconds=activation_duration * 0.21)
    request("screenshot", name="hologram-rebound.png")
    state = request("activation_frame", seconds=activation_duration * 0.45)
    request("screenshot", name="hologram-stabilizing.png")
    state = request("activation_frame", seconds=activation_duration)
    check(all(c["brightness"] == 1 and not c["processing"] and c["alpha"] == 1 for c in state["hologram_channels"]),
          "All holographic modules stabilize within the authored duration and stop animation processing")
    check(2.9 <= state["activation_duration"] <= 3.1,
          "The authored HUD ignition budget is a perceptible three seconds")
    request("screenshot", name="hologram-settled.png")
    request("advance_view", seconds=0.11)
    request("screenshot", name="manual-transition-mid.png")
    state = request("advance_view", seconds=1.0)
    request("activation_frame", seconds=activation_duration)
    state = request()
    check(state["planet_id"] == before["planet_id"] and state["blocks"] == before["blocks"] and
          state["ship_x"] == before["ship_x"] and state["ship_y"] == before["ship_y"] and
          state["projectiles"] == before["projectiles"] and state["pending_debris"] == before["pending_debris"],
          "Portrait mode switch preserves the live simulation")
    check("systems" not in state["buttons"] and "weapon_2" in state["buttons"],
          "Portrait manual HUD exposes weapons without movement or firing sticks")

    # Compare every design on exactly the same frozen terrain through real menu touches.
    state = tap("pause")
    state = tap("designs")
    check(state["paused"] and state["panel"] == "designs" and len(state["visual_styles"]) >= 8,
          "Pause menu opens a live world-design comparison with at least eight presets")
    preview_before = state
    for index, art in enumerate(state["visual_styles"]):
        while f"design_{index}" not in state["buttons"]:
            state = tap("design_next")
        state = tap(f"design_{index}")
        check(state["visual_style"] == art["id"] and state["paused"] and state["panel"] == "designs",
              f"Touch previews {art['title']} while the simulation stays paused")
        check(all(state[field] == preview_before[field] for field in
                  ("planet_id", "blocks", "planet_hp", "ship_x", "ship_y", "credits", "flux", "pending_debris")),
              f"{art['title']} preserves the identical world and pending rewards for comparison")
        request("activation_frame", seconds=activation_duration)
        if index == 0:
            request("screenshot", name="world-design-picker.png")
        tap("resume")
        request("activation_frame", seconds=activation_duration)
        request("screenshot", name=f"world-design-{art['id']}.png")
        tap("pause")
        state = tap("designs")
    while "design_0" not in state["buttons"]:
        state = tap("design_prev")
    tap("design_0")
    tap("resume")

    # A stationary held thumb keeps steering through the world as the camera follows.
    dx, dy = state["ship_x"] - 270, state["ship_y"] - 408
    length = (dx * dx + dy * dy) ** 0.5
    x = state["ship_screen_x"] + dx / length * 140
    y = state["ship_screen_y"] + dy / length * 140
    state = touch(x, y, True, 2)
    check(state["navigating"] and state["pointer_id"] == 2,
          "Holding the thumb starts steering without waiting for release")
    original_point = state["destination_x"], state["destination_y"]
    flight_start = state
    state = request("advance_simulation", seconds=0.25)
    check((state["ship_x"] - flight_start["ship_x"]) ** 2 + (state["ship_y"] - flight_start["ship_y"]) ** 2 > 1000,
          "The ship moves toward the held thumb")
    check(not state["clearing_path"] and state["pointer_id"] == 2,
          "Held empty-space flight does not shoot unrelated ore")
    state = tap("weapon_2", 3)
    check(state["weapon"] == 2 and state["navigating"] and state["pointer_id"] == 2,
          "A menu finger changes weapon while the steering thumb remains held")
    request("screenshot", name="manual-steering.png")
    state = request("advance_simulation", seconds=1.0)
    traveled = ((state["ship_x"] - flight_start["ship_x"]) ** 2 + (state["ship_y"] - flight_start["ship_y"]) ** 2) ** 0.5
    original_distance = ((original_point[0] - flight_start["ship_x"]) ** 2 + (original_point[1] - flight_start["ship_y"]) ** 2) ** 0.5
    check(state["navigating"] and state["pointer_id"] == 2 and traveled > original_distance + 80,
          "Holding a stationary thumb keeps moving beyond its initial world point")
    check(abs(state["ship_screen_x"] - state["width"] / 2) < 60,
          "Camera follows continuously held steering")
    # Drag the same finger inward; terrain must be cleared from a safe stand-off.
    tap("weapon_0")
    state = request()
    dx, dy = 270 - state["ship_x"], 408 - state["ship_y"]
    length = (dx * dx + dy * dy) ** 0.5
    x = state["ship_screen_x"] + dx / length * 260
    y = state["ship_screen_y"] + dy / length * 260
    mined_before = state["mined"]
    state = request("drag", x=x, y=y, finger=2)
    check(state["pointer_id"] == 2 and state["navigating"],
          "Dragging the existing thumb changes steering without a new press")
    state = request("advance_simulation", seconds=4.8)
    check(state["mined"] > mined_before, "Held inward steering automatically mines blocking rocks")
    request("screenshot", name="manual-clearing-path.png")
    x, y = state["ship_screen_x"], state["ship_screen_y"]
    request("drag", x=x, y=y, finger=2)
    state = request("advance_simulation", seconds=0.05)
    check(not state["navigating"] and state["pointer_id"] == 2, "Moving the held thumb onto the ship enters its steering dead zone")
    state = touch(x, y, False, 2)
    check(not state["navigating"] and state["pointer_id"] == -1, "Lifting the thumb stops steering and clears ownership")
    stopped = state["ship_x"], state["ship_y"]
    state = request("advance_simulation", seconds=0.4)
    check((state["ship_x"], state["ship_y"]) == stopped, "The released ship stays stopped instead of following a waypoint")
    touch(x - 70, y, True, 2)
    state = touch(x - 70, y, False, 2, canceled=True)
    check(not state["navigating"] and state["pointer_id"] == -1, "Canceled steering touch stops movement")
    touch(x - 70, y, True, 2)
    state = request("lifecycle", event="focus_out")
    check(not state["navigating"] and state["pointer_id"] == -1, "Focus loss cancels held steering")
    touch(x - 70, y, True, 2)
    state = request("lifecycle", event="pause")
    check(not state["navigating"] and state["pointer_id"] == -1, "OS pause cancels held steering")
    state = tap("pause")
    check(state["paused"], "Portrait pause button works by touch")
    request("screenshot", name="pause-portrait.png")
    muted = state["muted"]
    state = tap("sound")
    check(state["muted"] != muted, "Rendered sound toggle works by touch")
    state = tap("resume")
    check(not state["paused"] and not state["navigating"], "Resume requires a fresh steering press")
    before = state
    state = tap("mode")
    check(state["auto"] and (state["window_width"], state["window_height"]) ==
          (before["window_width"], before["window_height"]), "Returning to autopilot retains the same portrait window")
    time.sleep(0.8)
    # These are desktop windows at phone dimensions, not physical Android tests.
    state = request("resize", width=390, height=844)
    time.sleep(0.4)
    request("screenshot", name="auto-phone-size.png")
    check(state["height"] > state["width"] and "systems" in state["buttons"], "Phone-sized automatic portrait layout remains usable")
    before = state
    tap("mode")
    request("advance_view", seconds=1.0)
    request("activation_frame", seconds=activation_duration)
    state = request()
    request("screenshot", name="manual-phone-size.png")
    check(state["height"] > state["width"] and "weapon_2" in state["buttons"] and
          (state["window_width"], state["window_height"]) == (before["window_width"], before["window_height"]),
          "Phone-sized manual mode retains portrait and exposes all weapons")

    # The full desktop path must also enter through Input (window coordinates), not PushInput.
    def desktop_click(x, y):
        request("desktop_mouse", x=x, y=y, pressed=True)
        return request("desktop_mouse", x=x, y=y, pressed=False)

    def desktop_button(name):
        rect = request()["buttons"][name]
        return desktop_click(rect["x"] + rect["w"] / 2, rect["y"] + rect["h"] / 2)

    state = desktop_button("mode")
    check(state["auto"], "Engine mouse input activates the portrait autopilot button")
    time.sleep(0.8)
    state = desktop_button("mode")
    check(not state["auto"], "Engine mouse input activates Take Control")
    request("advance_view", seconds=1.0)
    time.sleep(0.8)
    state = request()
    dx, dy = state["ship_x"] - 270, state["ship_y"] - 408
    length = (dx * dx + dy * dy) ** 0.5
    mouse_x = state["ship_screen_x"] + dx / length * 100
    mouse_y = state["ship_screen_y"] + dy / length * 100
    state = request("desktop_mouse", x=mouse_x, y=mouse_y, pressed=True)
    check(state["navigating"] and state["pointer_id"] == -2,
          "Holding the mouse starts steering through the scaled window input path")
    start = state["ship_x"], state["ship_y"]
    request("freeze", value=False)
    time.sleep(0.7)
    state = request("freeze", value=True)
    check((state["ship_x"] - start[0]) ** 2 + (state["ship_y"] - start[1]) ** 2 > 25,
          "Holding the mouse actually moves the ship in the running process loop")
    mouse_x, mouse_y = state["ship_screen_x"], state["ship_screen_y"] - 110
    request("desktop_motion", x=mouse_x, y=mouse_y)
    start_y = state["ship_y"]
    state = request("advance_simulation", seconds=0.2)
    check(state["ship_y"] < start_y - 20 and state["pointer_id"] == -2,
          "Dragging the held desktop mouse changes the ship direction")
    state = request("desktop_mouse", x=mouse_x, y=mouse_y, pressed=False)
    check(not state["navigating"] and state["pointer_id"] == -1, "Releasing the desktop mouse stops steering")
    stopped = state["ship_x"], state["ship_y"]
    state = request("advance_simulation", seconds=0.3)
    check((state["ship_x"], state["ship_y"]) == stopped, "Mouse release leaves no persistent waypoint")

    # A final outer-edge block used to leave the ship circling empty space forever.
    state = request("sparse_autopilot")
    time.sleep(0.45)
    check(state["auto"] and state["blocks"] == 1, "Sparse autopilot fixture starts with one distant outer-edge block")
    sector_before = state["sector"]
    start_x = state["ship_x"]
    state = request("advance_simulation", seconds=5.0)
    check(state["ship_x"] > start_x + 500, "Autopilot closes the gap to isolated ore instead of orbiting the empty center")
    state = request("advance_simulation", seconds=0.35)
    request("screenshot", name="autopilot-last-block-approach.png")
    state = request("advance_simulation", seconds=2.4)
    check(state["blocks"] == 0, "Autopilot destroys the final outer-edge block")
    request("screenshot", name="autopilot-last-block-cleared.png")
    state = request("advance_simulation", seconds=4.0)
    check(state["sector"] == sector_before + 1 and state["blocks"] > 2000,
          "Clearing the last remnant progresses into the next generated sector")

    # End-to-end fabrication, individual upgrades, mount ownership, and market actions.
    state = request("economy_fixture")
    tap("pause")
    state = tap("workshop")
    request("activation_frame", seconds=activation_duration)
    request("screenshot", name="crafting-fabricator.png")
    check(state["panel"] == "fabricator" and state["engineering_rank"] == 3,
          "Pause opens the Fabricator with engineering rank and material inventory")
    before = state["credits"]
    rect = state["buttons"]["craft_1"]
    x, y = rect["x"] + rect["w"] / 2, rect["y"] + rect["h"] / 2
    touch(x, y, True, 10)
    request("mouse", x=x, y=y, pressed=True)
    request("mouse", x=x, y=y, pressed=False)
    state = touch(x, y, False, 10)
    check(len(state["owned_weapons"]) == 2 and state["credits"] == before - 600 and state["materials"][0] == 16,
          "Rendered touch crafting creates one Nova and consumes one complete recipe")
    check(not state["activation_running"], "Crafting refresh preserves a settled readable menu")
    tap("craft_drones")
    state = tap("craft_3")
    check(len(state["drones"]) == 1 and state["drones"][0]["weapon_id"] == 0, "Crafted Scout occupies a bay and starts unarmed")
    request("screenshot", name="crafting-drone-blueprints.png")
    drone_id = state["drones"][0]["id"]
    nova_id = state["owned_weapons"][1]["id"]
    tap("tab_fleet")
    tap("fleet_drones")
    state = tap(f"fit_{drone_id}")
    check(state["drones"][0]["weapon_id"] == nova_id and state["ship_weapon_id"] != nova_id,
          "Fleet mounts the spare crafted weapon without duplicating the ship mount")
    state = tap(f"drone_upgrade_{drone_id}")
    check(state["drones"][0]["level"] == 1, "Credit upgrade improves the owned drone chassis")
    request("screenshot", name="crafting-fleet.png")
    tap("fleet_weapons")
    state = tap(f"weapon_upgrade_{nova_id}")
    check(state["owned_weapons"][1]["level"] == 1, "Credit upgrade improves the individual mounted weapon")
    request("screenshot", name="crafting-weapon-upgrades.png")
    state = tap("tab_market")
    before, iridium = state["credits"], state["materials"][1]
    state = tap("buy_1")
    check(state["credits"] == before - 1800 and state["materials"][1] == iridium + 1,
          "Intergalactic market exchanges credits for a rare crafting resource")
    request("screenshot", name="crafting-market.png")
    tap("done"); tap("resume")
    state = request("drone_mining_trial")
    request("activation_frame", seconds=activation_duration)
    hp = state["planet_hp"]
    state = request("advance_simulation", seconds=3)
    check(state["planet_hp"] < hp and state["pointer_id"] == -1,
          "An armed crafted drone mines in the rendered game while the ship is idle")
    request("screenshot", name="crafting-drone-mining.png")
    # The mothership remains visible throughout servicing and supports concurrent mounts.
    tap("pause"); tap("workshop"); tap("craft_weapons"); tap("recipes_next")
    state = tap("craft_6")
    wave_id = next(w["id"] for w in state["owned_weapons"] if w["type"] == 3)
    check(state["visible_ship_previews"] == 1, "Fabricator keeps the live fitted ship visible")
    request("screenshot", name="drydock-new-weapons.png")
    tap("tab_systems")
    before = request()["credits"]
    state = tap("hardpoint")
    check(state["ship_hardpoints"] == 2 and state["credits"] == before - 2400,
          "Ship upgrading installs a second hardpoint at the displayed credit cost")
    tap("tab_fleet"); tap("fleet_weapons")
    state = tap(f"equip_{wave_id}")
    check(state["ship_mounts"] == 2 and wave_id in state["ship_aux_weapon_ids"] and state["visible_ship_previews"] == 1,
          "Fleet fits an auxiliary Nova wave and updates the visible ship schematic")
    request("screenshot", name="drydock-ship-mounts.png")
    tap("done"); tap("resume")
    state = request("fleet_cruise_trial")
    start = state["ship_x"], state["ship_y"]
    hp = state["planet_hp"]
    for _ in range(8):
        state = request("advance_simulation", seconds=5)
    drone = state["drones"][0]
    check((drone["x"] - start[0]) ** 2 + (drone["y"] - start[1]) ** 2 > 150 ** 2 and state["planet_hp"] < hp and
          (state["ship_x"], state["ship_y"]) == start,
          "Deployed drone cruises and mines independently far from the idle mothership")
    tap("pause"); tap("workshop"); tap("tab_fleet"); tap("fleet_drones")
    before = request()
    state = tap(f"drone_upgrade_{drone_id}")
    check(state["credits"] == before["credits"] and state["drones"][0]["level"] == before["drones"][0]["level"],
          "A deployed drone cannot be upgraded remotely")
    state = tap(f"drone_recall_{drone_id}")
    check(state["drones"][0]["recalling"], "Fleet touch sends a recall to the deployed drone")
    hp = state["planet_hp"]
    state = request("advance_simulation", seconds=5)
    state = request("advance_simulation", seconds=5)
    check(state["drones"][0]["docked"] and state["paused"] and state["planet_hp"] == hp,
          "Recall docks the drone during paused servicing without secretly mining terrain")
    level = state["drones"][0]["level"]
    state = tap(f"drone_upgrade_{drone_id}")
    check(state["drones"][0]["level"] == level + 1, "Docked drone upgrades become available after the return")
    request("screenshot", name="drydock-returned-drone.png")
    state = tap(f"drone_recall_{drone_id}")
    check(not state["drones"][0]["docked"] and not state["drones"][0]["recalling"], "A serviced drone can be redeployed through touch")
    tap("done"); tap("resume")
    state = request("nova_wave_trial")
    state = request("advance_simulation", seconds=0.7)
    check(state["waves"] == 1, "Nova wave remains visibly expanding after seven tenths of a second")
    request("activation_frame", seconds=activation_duration)
    request("screenshot", name="nova-expanding-wave.png")
    state = request("advance_simulation", seconds=1.1)
    check(state["waves"] == 0, "The expanding Nova front expires within its authored lifetime")
    state = request("deep_deposits_fixture")
    tap("pause"); state = tap("designs")
    for index in range(10):
        while f"design_{index}" not in state["buttons"]:
            state = tap("design_prev" if index < state["visual_style_index"] else "design_next")
        state = tap(f"design_{index}")
        tap("resume"); request("activation_frame", seconds=activation_duration)
        request("screenshot", name=f"technical-deposits-{state['visual_style']}.png")
        tap("pause"); state = tap("designs")
    result = {"checks": checks, "passed": len(checks), "input": "Injected Viewport touch/drag and Input.ParseInputEvent mouse events (not hardware input)",
              "orientation": "Portrait in both modes; macOS phone-sized windows, no physical Android test", "state": state}
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
        print(f"RESULT: {result['passed']} rendered input checks passed", flush=True)
    finally:
        try:
            request("quit")
            process.wait(timeout=5)
        except (OSError, RuntimeError, subprocess.TimeoutExpired):
            process.terminate()
        log.close()
