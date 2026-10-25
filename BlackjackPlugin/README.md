# BlackjackPlugin

An example plugin for the Imlight Plugin API that lets players bet gold on a hand of blackjack against the dealer.

This plugin builds on [ExamplePlugin](../ExamplePlugin) and shows two things that go past the basics:

- **Changing player attributes.** It takes and gives gold, a value stored on the player's character.
- **Building complex commands.** One command has subcommands, keeps track of each player's state between calls, and validates what the player typed.

Blackjack is just a fun way to show these. The same patterns work for any plugin that changes player data or needs a multi-step command.

## How to play

| Command | What it does |
| --- | --- |
| `.bj <bet>` | Deals a new hand and bets that much gold (10 to 10,000). |
| `.bj hit` | Draws another card. |
| `.bj stand` | Ends your turn and lets the dealer play. |
| `.bj double` | Doubles your bet, draws one card, then stands. Only works on your first two cards. |

`.blackjack` works as well as `.bj`. A win pays 1:1, and a natural blackjack pays 3:2. Logging out in the middle of a hand forfeits the bet.

## What it shows

### Changing player gold
The gold logic lives in [BlackjackPlugin.cs](BlackjackPlugin.cs):
- `player.RemoveGold(amount)` takes gold from the player. It returns `false` if they don't have enough, so you can check the result and refuse the bet. Nothing is taken in that case.
- `player.AddGold(amount)` gives gold back when the hand is settled. It only adds up to the player's `MaxGold`, so anything past their cap is lost.

Gold is taken when the bet is placed, not when the hand ends. That way a player can't bet gold and then spend it somewhere else mid-hand.

### Complex commands
- **Subcommands:** a single command, `blackjack`, is registered. `HandleCommand` reads `c.Args[0]` and decides whether it's a bet amount or an action like `hit` or `stand`.
- **State between commands:** each player's current hand is stored in a dictionary keyed by `CharacterId`, so `.bj hit` knows which game it belongs to.
- **Input validation:** the bet is parsed with `int.TryParse` and checked against a minimum and maximum. When the input is wrong, the command replies with usage help.
- **Cleaning up with events:** the plugin subscribes to `OnPlayerQuit` to throw away a player's hand when they log out.

## Building

```
dotnet build
```

The compiled plugin will be at `bin/Debug/net10.0/BlackjackPlugin.dll`. This file can be dropped into the `plugins` folder in the same directory as `Imlight.Director.exe`. If this folder doesn't exist, you can create it.
