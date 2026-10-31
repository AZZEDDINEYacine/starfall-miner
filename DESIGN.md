# Starfall Miner — revision 0.3

Revision 0.3 migrates the production game to typed C# on Godot 4.7.2 .NET. Simulation, direct-finger input, camera/orientation, presentation, and persistence have distinct responsibilities; the visual and gameplay design below is retained. Native macOS and Android are the delivery targets. There is no C# browser build.

The game now separates its two activities spatially. Automatic mining is a quiet portrait view: watch the square planet dissolve, check the wallet, and open ship systems or the extraction log when needed. A full-width accented Take Control button is the main action. It requests landscape orientation and hands control to the pilot. Returning to autopilot requests portrait again.

The simulation uses a fixed coordinate space. Orientation changes only reshape the viewport, reposition the camera, and replace the HUD. They do not recreate the planet or reposition its blocks, ship, or loot. Camera scale eases during the transition and the new HUD fades in. Joystick and firing ownership are cleared during a switch so an old finger cannot become an accidental flight command.

Manual flight removes upgrade and inventory panels. The pilot sees a left stick, a right fire circle, a boost control, three weapon selectors, a compact wallet, and resources recovered during the current session. Detailed statistics remain available by tapping the wallet. A second finger can operate a weapon selector while the first continues to steer.

The visual language is flat: plain colored squares, a line-drawn arrowhead ship, thin beams, and a sparse background. A block's persistent brightness increases with accumulated damage, avoiding temporary flashes that obscure its health. Ore type determines its hue. Individual blocks have no bevel, crack texture, or interior symbol.

Each destroyed block emits two tiny fragments. Both appear to drift and then move toward the ship; one carries the block's credit and energy reward. Resources are awarded once on collection. Pending fragments survive saving and loading. Effects have a bounded particle count and a short lifetime so cascades stay readable.

The original progression is retained: seeded planets, three distinct weapons, three shared upgrade paths, volatile block cascades, energy overdrive, sector rewards, and endless planet generation. Numerical difficulty growth remains capped at sector 501 in this prototype. Economy tuning and physical-device handling remain iteration work.
