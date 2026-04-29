# Fork Changes

## Local tooling

- added one-step bootstrap scripts for restore, build, database import, and launch
- added direct AIR launch helpers with a local Maestro bridge
- made the client root configurable through `DRAVEN_GAME_CLIENT_ROOT`

## Runtime behavior

- improved login payload parsing for local launch flows
- lowered the manifest privilege level to `asInvoker`
- added safer client-root detection for property redirection

## Service coverage

- added missing handlers for practice creation, tutorial creation, recent matches, rank-data lookup, and mastery save
- expanded queue and profile-related compatibility paths
- improved rune inventory and icon inventory payloads

## Data handling

- added local mastery book state handling and save plumbing
- improved profile icon normalization and compatibility inventory data
- kept direct-AIR launch focused on the 4.20 client layout

## Current limits

- practice and tutorial creation handlers are compatibility stubs, not a full game-session backend
- direct AIR launch expects the 4.20 client layout and local Windows tooling
