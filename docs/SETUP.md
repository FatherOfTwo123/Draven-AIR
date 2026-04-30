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

## What to run

- `START_LOCAL_STACK.bat` — starts the local database/bootstrap flow and launches `Draven.exe`
- `START_DIRECT_AIR_CLIENT.bat` — starts the direct AIR login flow against the local stack
- `STOP_LOCAL_STACK.bat` — closes the local helper processes

Run the direct AIR launcher from the repo root, not from the client folder.

## Folder layout

- `Draven/`
  - main application code
- `rtmp-sharp/`
  - RTMP/RTMPS transport code
- `Database/`
  - SQL files imported during local bootstrap
- `docs/`
  - setup and change notes
- repo root `.bat` / `.ps1`
  - day-to-day launch and maintenance entrypoints

## Where to configure things

### Game client root

Preferred options:

- pass the 4.20 client root as the first argument to `START_LOCAL_STACK.bat` or `START_DIRECT_AIR_CLIENT.bat`
- or set `DRAVEN_GAME_CLIENT_ROOT`

### Server host, port, and database defaults

Edit:

- `Draven/Program.cs`

Main values there:

- `host`
- `user`
- `pass`
- `database`
- `RTMPSHost`
- `RTMPSPort`
- `AuthLocations`

### Local tool download/cache folder

The helper bootstrap script uses:

- `%USERPROFILE%\tools`

That location is defined in:

- `START_LOCAL_STACK.ps1`

## First run

Run:

```bat
START_LOCAL_STACK.bat
```

This helper does the following:

- ensures local tool dependencies exist
- starts a local MySQL instance when needed
- imports the bundled database files
- restores packages
- builds `Draven.exe`
- launches the service host in the background

Server output goes to:

- `draven-live-out.log`
- `draven-live-err.log`
- `START_LOCAL_STACK.log`

If you want the helper to launch with a client root already configured:

```bat
START_LOCAL_STACK.bat "C:\Path\To\GameClient420"
```

## Direct AIR launch

Run:

```bat
START_DIRECT_AIR_CLIENT.bat "C:\Path\To\GameClient420"
```

What it does:

- starts `Draven.exe` if it is not already running
- starts the local Maestro bridge
- launches the AIR client directly into the local stack
- stops the temporary Maestro bridge when the client closes

## Troubleshooting

### Property redirection warning

If you see a warning asking for `DRAVEN_GAME_CLIENT_ROOT`, the server was started without a valid client root.

Fix:

- set `DRAVEN_GAME_CLIENT_ROOT`
- or launch the helper scripts with the 4.20 client path as the first argument
- or keep the repo beside your 4.20 client folder so auto-detect can pick it up

### Direct AIR script says the client was not found

Cause: wrong folder level.

Fix: pass the top-level 4.20 client folder, not the inner `deploy` folder.

### Closing helpers

Run:

```bat
STOP_LOCAL_STACK.bat
```

This stops the local Maestro helper, `Draven.exe`, and the AIR client if they are still running.

### `Draven.exe` not found

Cause: server not built yet.

Fix: run `START_LOCAL_STACK.bat` first.
