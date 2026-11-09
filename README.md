# VRProcedureTrainer
VR safety training simulator built with Unity 6 and XR Interaction Toolkit. Trainees perform a lockout/tagout (LOTO) procedure on an industrial press: data-driven steps (ScriptableObjects), a pure C# state machine with unit tests, error tracking and a JSON report. Runs without a headset via the XR Interaction Simulator.

## Why I built this
I built this project to learn how to create scenes for VR: placing a machine in a 3D space, turning its parts into controls you can press, turn and grab, and guiding the user with highlights and panels placed in the world instead of a flat UI.

I chose a LOTO procedure on a press because it is a classic use case for training simulators, and because it gives the scene a real reason to exist. The order of the actions matters: the same START button is a mistake at the beginning and the correct action near the end.

Alongside VR, I wanted to practise two things:
- **Keeping the logic independent from the engine.** The rules of the procedure are plain C#, so they run as unit tests in milliseconds, without opening a scene.
- **Making content editable without code.** A new step of the procedure can be added from the Unity editor. I tested this by adding the air release step without changing any script.

## What it does
- A 6-step procedure on a press that really reacts: flywheel, ram, lamps and voltage display follow the controls.
- The control to operate pulses with light, and a panel in the scene shows the current step.
- Wrong actions are signalled with a message and a sound, and dangerous ones end the session.
- The emergency stop is always allowed and never counts as a mistake.
- At the end, a report shows the outcome, the time and the errors, and is saved as a JSON file.
