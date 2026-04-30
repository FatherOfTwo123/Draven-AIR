# Draven-AIR

Local service host for the AIR game client.

## What this fork adds

- configurable client-root redirection through `DRAVEN_GAME_CLIENT_ROOT`
- safer local launch defaults for Windows

## Quick start

1. Point the tools at your game client root:
   - `set DRAVEN_GAME_CLIENT_ROOT=C:\Path\To\GameClient`
   - or pass the path directly to the launcher script
2. Start the local stack:
   - `START_LOCAL_STACK.bat "C:\Path\To\GameClient"`
3. Launch the direct AIR flow:
   - `START_DIRECT_AIR_CLIENT.bat "C:\Path\To\GameClient"`
4. Stop the local helper processes when done:
   - `STOP_LOCAL_STACK.bat`

## Entry scripts

- `START_LOCAL_STACK.bat` — restore, build, database/bootstrap, then start `Draven.exe`
- `START_DIRECT_AIR_CLIENT.bat` — ensure the local stack is running, then launch the 4.20 AIR client through the local Maestro bridge
- `STOP_LOCAL_STACK.bat` — stop `Draven.exe`, the AIR client, and the local Maestro helper

## Documentation

- `docs/SETUP.md` — setup, launch flow, and troubleshooting
- `docs/CHANGES.md` — summary of the fork changes in this workspace

## Folder layout

- `Draven/` — main local service host
- `rtmp-sharp/` — RTMP/RTMPS transport layer used by the service host
- `Database/` — SQL bootstrap files imported by `START_LOCAL_STACK.bat`
- `docs/` — setup and maintenance notes
- repo root scripts — the intended entrypoints for everyday use

## Notes

- Direct AIR mode needs Node.js.
- Helper scripts may download local tools into `%USERPROFILE%\tools` on first run.
- The direct AIR launcher is tailored to the 4.20 client layout.
