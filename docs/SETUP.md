# Setup Guide

## Requirements

- Windows
- .NET SDK with legacy framework build support available locally
- Node.js for direct AIR launch mode
- PowerShell

## Client root

All redirect and direct-AIR helpers need the root folder of the 4.20 game client.

You can provide it in either of these ways:

- pass the folder to the helper script
- set `DRAVEN_GAME_CLIENT_ROOT` before launching the tools

Example:

```bat
set DRAVEN_GAME_CLIENT_ROOT=C:\Path\To\GameClient420
```

The folder you point to must be the top-level client folder that contains `RADS`.

## First run

Run:

```bat
run_sql_and_draven.bat
```

This helper does the following:

- ensures local tool dependencies exist
- starts a local MySQL instance when needed
- imports the bundled database files
- restores packages
- builds `Draven.exe`
- launches the service host

If you want the helper to launch with a client root already configured:

```bat
run_sql_and_draven.bat "C:\Path\To\GameClient420"
```

## Direct AIR launch

Run:

```bat
RunDirectAirWithMaestro.bat "C:\Path\To\GameClient420"
```

What it does:

- starts `Draven.exe` if it is not already running
- starts the local Maestro bridge
- launches the AIR client directly into the local stack

## Visible server launch

If you only want the server window without the direct AIR flow:

```bat
RunDravenVisible.bat "C:\Path\To\GameClient420"
```

## Troubleshooting

### Property redirection warning

If you see a warning asking for `DRAVEN_GAME_CLIENT_ROOT`, the server was started without a valid client root.

Fix:

- set `DRAVEN_GAME_CLIENT_ROOT`
- or launch the helper scripts with the 4.20 client path as the first argument

### Direct AIR script says the client was not found

Cause: wrong folder level.

Fix: pass the top-level 4.20 client folder, not the inner `deploy` folder.

### `Draven.exe` not found

Cause: server not built yet.

Fix: run `run_sql_and_draven.bat` first.
