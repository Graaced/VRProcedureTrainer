# Engineering diary

One entry per working day: what I did, one decision and its trade-off, what broke and how I found out, what I learned.

## Day 1: interactive machine

**What I did**
- Built the machine controls: STOP, START and emergency buttons, isolator switch, padlock socket and hinged guard (PR #1 to #4).
- Added `MachineSimulation`, which owns the physical rules: power, motion, lockout, lamps, flywheel and voltage display (PR #5).

**Decision and trade-off**
- The push button is driven by XRI select events instead of copying `XRPushButton` from the XRI Examples, which only supports poke and ignores the ray.
- Gain: the whole machine can be operated with the simulator, without a headset.
- Cost: on a headset the buttons are operated by select, not by physically pushing them.

**What broke and how I found out**
- The Console shows "Failed to get haptic capabilities" on the simulated controllers. It comes from XRI and the simulated device, so it is a known exception and not a bug in the project.
- `main` and `master` had unrelated histories. I merged them once with `--allow-unrelated-histories`, then deleted `master`.

**What I learned**
- TODO (Simone): what you would explain to someone else after this day.

## Day 2: data, state machine and guidance

**What I did**
- Wrote the 14 `ProcedureRunner` tests and the runner in plain C#, with no Unity dependency (PR #6).
- Moved the procedure into ScriptableObjects: `ProcedureStep`, `TrainingProcedure` and the `LOTO_Pressa` asset (PR #7).
- Connected the scene to the logic through `ProcedureController` (PR #8).
- Added the pulsing highlight and the instruction panel (PR #9), then sound and haptic feedback (PR #10).
- Added step 3b, the air release valve, from the editor only (PR #11).

**Decision and trade-off**
- `ProcedureRunner` never reads ScriptableObjects: `TrainingProcedure.ToDefinition()` converts the asset to plain data first.
- Gain: every rule runs in EditMode tests in milliseconds, and the logic has no Unity reference.
- Cost: an extra conversion layer, and two parallel shapes of the same data to keep aligned.

**What broke and how I found out**
- Validation errors with an empty name, `[] Id is empty.`, appeared when entering Play. `Logs/Editor.log` showed that the real assets were clean and the errors came from an unnamed temporary instance. `OnValidate` now ignores unnamed instances. I did not identify what creates that instance.
- The highlight did not light up, for two reasons found by reading the scene file. The highlight materials had been added as extra slots after the original ones, and the STOP and START targets pointed to the button base instead of the moving key.
- Disabling a component in `Awake` still calls `OnDisable`, which threw a `NullReferenceException` when a reference was missing. `OnEnable` and `OnDisable` now check that the runner exists.
- No sound in Play: the Game view had Mute Audio enabled. The project was fine.

**Data-driven test (2.8)**
- Result: the new step needed no change to any `.cs` file. It only took two new assets, one edited asset and scene setup.
- Limit found: judging a new step is fully data-driven, but the machine's reaction is not. The pressure gauge does not drop to 0, because that would need code in `MachineSimulation`.

**What I learned**
- TODO (Simone): what you would explain to someone else after this day.
