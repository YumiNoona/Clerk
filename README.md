# Clerk

Clerk is a first-person store simulator built with Unity 6.

The current prototype includes the complete first playable store loop:

`order -> deliver -> stock -> customer shopping -> queue -> scan -> pay -> exit`

It also includes runtime product pricing and demand, furniture placement and
selling services, daily finances, loans, employees, progression, objectives,
save slots, settings/input rebinding, and responsive desktop/mobile UI shells.

## Project direction

The project architecture and coding rules are documented in
[Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md). The implemented systems and scene
wiring are listed in [Docs/SYSTEMS.md](Docs/SYSTEMS.md).

For complete scene construction, component assignments, prefab requirements,
data authoring, and a description of every script, see [SETUP.md](SETUP.md).

## Unity version

Unity `6000.5.5f1`

## Running

Open `Assets/Scenes/SampleScene.unity` and press Play. The main menu is generated
at runtime. The desktop and phone can be opened from the pause menu; both use
the same gameplay services and present different responsive shells.
