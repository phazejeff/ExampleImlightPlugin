using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackjackPlugin;

/// <summary>
/// One hand of blackjack between a player and the dealer, dealt from a fresh shuffled deck.
/// </summary>
internal sealed class BlackjackGame {

    // Cards are stored by rank only (1 = ace, 11-13 = face cards); suits don't matter in blackjack.
    private readonly Stack<int> deck;

    public List<int> PlayerHand { get; } = [];

    public List<int> DealerHand { get; } = [];

    public int Bet { get; set; }

    public BlackjackGame(int bet) {
        Bet = bet;

        int[] cards = new int[52];
        for (int i = 0; i < cards.Length; i++) {
            cards[i] = i % 13 + 1;
        }
        Random.Shared.Shuffle(cards);
        deck = new Stack<int>(cards);

        PlayerHand.Add(deck.Pop());
        DealerHand.Add(deck.Pop());
        PlayerHand.Add(deck.Pop());
        DealerHand.Add(deck.Pop());
    }

    public void HitPlayer() => PlayerHand.Add(deck.Pop());

    /// <summary>
    /// Draws for the dealer until they reach 17 or more. The dealer stands on every 17.
    /// </summary>
    public void PlayDealer() {
        while (Value(DealerHand) < 17) {
            DealerHand.Add(deck.Pop());
        }
    }

    public static int Value(List<int> hand) {
        int total = 0;
        int aces = 0;
        foreach (int rank in hand) {
            if (rank == 1) {
                aces++;
                total += 11;
            } else {
                total += Math.Min(rank, 10);
            }
        }

        // Count aces as 1 instead of 11 until the hand stops busting.
        while (total > 21 && aces > 0) {
            total -= 10;
            aces--;
        }
        return total;
    }

    public static bool IsBlackjack(List<int> hand) => hand.Count == 2 && Value(hand) == 21;

    public static string Describe(List<int> hand) => string.Join(", ", hand.Select(CardName));

    private static string CardName(int rank) => rank switch {
        1 => "A",
        11 => "J",
        12 => "Q",
        13 => "K",
        _ => rank.ToString()
    };

}
