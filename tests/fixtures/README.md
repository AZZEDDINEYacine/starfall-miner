# Save and generation fixture provenance

Captured from Git checkpoint `ce1e4b6` (the incoming 0.2 GDScript game) using its original `save_progress` serializer in a disposable copy, with the save path redirected to that copy. Original GDScript regression suite: 52 checks, zero failures on Godot 4.7.2 .NET.

- `gdscript-generation.json`: untouched sector 4 output, for exact legacy-world restoration of keys, types, shades, health, and rewards. Current generation intentionally produces larger planets; size and determinism are tested separately.
- `gdscript-v02.json`: sector 4 with seven removed tiles, eleven partly damaged tiles, known wallet/upgrades/mode, ship coordinates, and one rewarded plus one cosmetic pending fragment.
- `gdscript-v01.json`: the same original-serializer save with the four 0.2 additions (`pickups`, `lifetime_energy`, `ship_x`, `ship_y`) omitted, reproducing the 0.1 schema. A physical user's original 0.1 save was not available.

The fixed timestamps are intentional. Compatibility fixtures use manual mode to avoid catch-up affecting the wallet. Offline-income tests modify their own disposable copies and supply a current relative timestamp. No fixture is a copy of the user's private save.
