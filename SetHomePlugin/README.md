# SetHomePlugin

An example plugin for the Imlight Plugin API that lets players save a spot as their home and go back to it with the Go Home button.

This plugin builds on [ExamplePlugin](../ExamplePlugin) and shows two things that go past the basics:

- **Overriding game behaviour with events.** It catches the game's own Go Home travel, cancels it, and sends the player somewhere else instead.
- **Saving data persistently.** Homes are written to a JSON file in the plugin's data folder, so they survive server restarts.

Homes are just a simple way to show these. The same patterns work for any plugin that needs to replace what the game normally does, or remember something between restarts.

## How to use

| Command | What it does |
| --- | --- |
| `.sethome` | Saves where you're standing as your home. Doesn't work inside instances like dungeons or houses. |
| `.home` | Sends you to your saved home. |
| `.delhome` | Removes your saved home. |

Once a home is set, pressing Go Home takes you there instead of the preset home location. Players without a home get the normal Go Home behaviour. If the zone a home was saved in no longer exists, the home is removed and the player is told.

## What it shows

### Overriding events
The event logic lives in `OnEnable()` in [SetHomePlugin.cs](SetHomePlugin.cs):
- **Cancelling an event:** setting `e.IsCancelled = true` stops the game from doing what it was about to do. The plugin then calls `player.TransferZone()` to send the player to their home instead.
- **Checking why an event happened:** `e.Cause` says what started the travel. The plugin only steps in for `ZoneChangeCause.Dorm`, so every other kind of zone change or teleport is left alone.
- **Falling back to normal behaviour:** for Dorm, the event is only cancelled when `GoHome` succeeds. If the player has no home, the event goes through and the game handles it as usual.
- **Covering every path:** the same button can raise different events. Go Home raises `OnPlayerZoneChange` when the hub is in another zone, and `OnPlayerTeleport` when the player is already in the hub zone. The plugin subscribes to both so neither one slips through.

### Saving data
- **Where to save:** every plugin has a `DataFolder` it can write its own files to. Homes are kept in `homes.json` inside it.
- **Loading on startup:** `OnEnable()` reads the file if it exists, so homes saved before a restart are back as soon as the plugin loads.
- **Saving on change:** the file is rewritten whenever a home is set or removed, instead of only on shutdown. Nothing is lost if the server crashes.
- **Thread safety:** commands all run on one dispatcher, but event handlers run on each player's own session. Go Home events can fire for several players at once, and alongside a command, so every read and write of the homes dictionary happens inside a `lock`.

## Building

```
dotnet build
```

The compiled plugin will be at `bin/Debug/net10.0/SetHomePlugin.dll`. This file can be dropped into the `plugins` folder in the same directory as `Imlight.Director.exe`. If this folder doesn't exist, you can create it.
