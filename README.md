# Draven-AIR

Local service host for the AIR game client.

## What this fork adds

- configurable client-root redirection through `DRAVEN_GAME_CLIENT_ROOT`
- safer local launch defaults for Windows

## Quick start

1. Start the local stack:
   - `run_sql_and_draven.bat`
2. Point the tools at your game client root:
   - `set DRAVEN_GAME_CLIENT_ROOT=C:\Path\To\GameClient420`
   - or pass the path directly to the launcher script
3. Launch the direct AIR flow:
   - `RunDirectAirWithMaestro.bat "C:\Path\To\GameClient420"`
4. Stop the local helper processes when done:
   - `StopDravenAir.bat`

## Documentation

- `docs/SETUP.md` — setup, launch flow, and troubleshooting
- `docs/CHANGES.md` — summary of the fork changes in this workspace

## Notes

- Direct AIR mode needs Node.js.
- Helper scripts may download local tools into `%USERPROFILE%\tools` on first run.
- The direct AIR launcher is tailored to the 4.20 client layout.
