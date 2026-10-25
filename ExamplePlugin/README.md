# ExamplePlugin

A minimal example plugin for the Imlight Plugin API.

This plugin is not meant to do anything useful on its own. It shows the most basic features of the API so you have a simple starting point for building your own plugin. If you're new to writing plugins, read through [ExamplePlugin.cs](ExamplePlugin.cs). It's short and covers the core pieces every plugin uses.

## What it shows

### Plugin setup
Every plugin extends `ImlightPlugin`, provides a `Name` and `Version`, and does its setup in `OnEnable()`.

### Events
Subscribing to game events with `Events.Subscribe<T>()`:
- `OnPlayerZoneEnter`: greets a player when they enter a zone.
- `OnPlayerChat`: blocks chat messages containing a banned word by setting `e.IsCancelled = true`.
- `OnPlayerDamage`: tells the player how much damage they took.
- `OnPlayerKnockout`: tells the player they were knocked out.

### Commands
Registering chat commands with `Commands.Register()`:
- `ping` (alias `boing`): a basic command that replies with "Pong!".
- `broadcast <message>`: sends a message to everyone in the zone. Uses a permission (`CommandPermission.HallMonitor`) to restrict who can run it.
- `poke <first> <last>`: reads command arguments and messages a specific player in the zone.

### Players and zones
Working with `IPlayer` and `IZone`: sending messages, reading names, and looping through the players in a zone.

## Building

```
dotnet build
```

The compiled plugin will be at `bin/Debug/net10.0/ExamplePlugin.dll`. This file can be dropped into the `plugins` folder in the same directory as `Imlight.Director.exe`. If this folder doesn't exist, you can create it.
