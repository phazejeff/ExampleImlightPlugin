using System.Collections.Generic;
using Imlight.PluginApi;
using Imlight.PluginApi.Commands;
using Imlight.PluginApi.Events;

namespace BlackjackPlugin;

/// <summary>
/// Lets players bet gold on a hand of blackjack against the dealer.
/// </summary>
/// <remarks>
/// .bj &lt;bet&gt; deals a hand, then .bj hit, .bj stand or .bj double plays it.
/// A win pays 1:1 and a natural blackjack pays 3:2.
/// </remarks>
public sealed class BlackjackPlugin : ImlightPlugin {

    private const int MinBet = 10;
    private const int MaxBet = 10000;

    private readonly Dictionary<ulong, BlackjackGame> games = [];
    private readonly object gamesLock = new();

    public override string Name => "BlackjackPlugin";

    public override string Version => "1.0.0";

    public override void OnEnable() {
        Commands.Register("blackjack", HandleCommand, aliases: ["bj"]);

        // Logging out mid-hand forfeits the bet.
        Events.Subscribe<OnPlayerQuit>(e => {
            lock (gamesLock) {
                games.Remove(e.Player.CharacterId);
            }
        });
    }

    private void HandleCommand(CommandInvocation c) {
        lock (gamesLock) {
            string action = c.Args.Length > 0 ? c.Args[0].ToLowerInvariant() : "";

            if (!games.TryGetValue(c.Player.CharacterId, out BlackjackGame? game)) {
                if (int.TryParse(action, out int bet)) {
                    Deal(c.Player, bet);
                } else {
                    c.Reply($"Usage: .bj <bet> to deal a hand ({MinBet}-{MaxBet} gold).");
                }
                return;
            }

            switch (action) {
                case "hit":
                    Hit(c.Player, game);
                    break;
                case "stand":
                    Stand(c.Player, game);
                    break;
                case "double":
                    Double(c.Player, game);
                    break;
                default:
                    ShowTable(c.Player, game, revealDealer: false);
                    c.Reply("You're mid-hand: .bj hit, .bj stand or .bj double.");
                    break;
            }
        }
    }

    private void Deal(IPlayer player, int bet) {
        if (bet < MinBet || bet > MaxBet) {
            player.SendMessage($"Bets must be between {MinBet} and {MaxBet} gold.");
            return;
        }
        if (!player.RemoveGold(bet)) {
            player.SendMessage($"You don't have {bet} gold to bet.");
            return;
        }

        BlackjackGame game = new(bet);
        games[player.CharacterId] = game;

        // Naturals end the hand straight away, so the dealer's hole card is only peeked at here.
        bool playerNatural = BlackjackGame.IsBlackjack(game.PlayerHand);
        bool dealerNatural = BlackjackGame.IsBlackjack(game.DealerHand);
        if (playerNatural || dealerNatural) {
            ShowTable(player, game, revealDealer: true);
            if (playerNatural && dealerNatural) {
                Settle(player, game, payout: game.Bet, "You both have blackjack. Push!");
            } else if (playerNatural) {
                Settle(player, game, payout: game.Bet + game.Bet * 3 / 2, "Blackjack! You win!");
            } else {
                Settle(player, game, payout: 0, "Dealer has blackjack. You lose.");
            }
            return;
        }

        ShowTable(player, game, revealDealer: false);
        player.SendMessage(".bj hit, .bj stand or .bj double?");
    }

    private void Hit(IPlayer player, BlackjackGame game) {
        game.HitPlayer();
        int total = BlackjackGame.Value(game.PlayerHand);

        if (total > 21) {
            ShowTable(player, game, revealDealer: true);
            Settle(player, game, payout: 0, "Bust! You lose.");
        } else if (total == 21) {
            Stand(player, game);
        } else {
            ShowTable(player, game, revealDealer: false);
        }
    }

    private void Double(IPlayer player, BlackjackGame game) {
        if (game.PlayerHand.Count != 2) {
            player.SendMessage("You can only double down on your first two cards.");
            return;
        }
        if (!player.RemoveGold(game.Bet)) {
            player.SendMessage($"You need another {game.Bet} gold to double down.");
            return;
        }

        game.Bet *= 2;
        game.HitPlayer();
        if (BlackjackGame.Value(game.PlayerHand) > 21) {
            ShowTable(player, game, revealDealer: true);
            Settle(player, game, payout: 0, "Bust! You lose.");
        } else {
            Stand(player, game);
        }
    }

    private void Stand(IPlayer player, BlackjackGame game) {
        game.PlayDealer();
        ShowTable(player, game, revealDealer: true);

        int playerTotal = BlackjackGame.Value(game.PlayerHand);
        int dealerTotal = BlackjackGame.Value(game.DealerHand);
        if (dealerTotal > 21) {
            Settle(player, game, payout: game.Bet * 2, "Dealer busts! You win!");
        } else if (playerTotal > dealerTotal) {
            Settle(player, game, payout: game.Bet * 2, "You win!");
        } else if (playerTotal == dealerTotal) {
            Settle(player, game, payout: game.Bet, "Push. Your bet is returned.");
        } else {
            Settle(player, game, payout: 0, "Dealer wins.");
        }
    }

    /// <summary>
    /// Ends the hand and pays out. The payout includes the original bet, so a 1:1 win pays twice the bet.
    /// </summary>
    private void Settle(IPlayer player, BlackjackGame game, int payout, string result) {
        games.Remove(player.CharacterId);
        if (payout > 0) {
            player.AddGold(payout);
        }

        int net = payout - game.Bet;
        string change = net switch {
            > 0 => $"+{net} gold",
            < 0 => $"{net} gold",
            _ => "no change"
        };
        player.SendMessage($"{result} ({change})");
    }

    private static void ShowTable(IPlayer player, BlackjackGame game, bool revealDealer) {
        string dealer = revealDealer
            ? $"{BlackjackGame.Describe(game.DealerHand)} ({BlackjackGame.Value(game.DealerHand)} total)"
            : $"{BlackjackGame.Describe([game.DealerHand[0]])}";
        player.SendMessage($"Dealer: {dealer}");
        player.SendMessage($"You: {BlackjackGame.Describe(game.PlayerHand)} ({BlackjackGame.Value(game.PlayerHand)} total)");
    }

}
