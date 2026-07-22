using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CardMatch.Tests.EditMode {
  /// <summary>
  /// Pure-logic tests for deck construction. No scene, no MonoBehaviours.
  /// </summary>
  public class BoardGeneratorTests {
    /// <summary>Number of distinct card faces BoardGenerator can deal (A-K plus Joker).</summary>
    private const int CARD_TYPE_COUNT = 14;

    /// <summary>The size MainScene actually plays with: Board.maxCols (4) * Board.maxRows (7).</summary>
    private const int SHIPPING_BOARD_SIZE = 28;

    [TestCase(2)]
    [TestCase(10)]
    [TestCase(SHIPPING_BOARD_SIZE)]
    public void CreateBoard_ReturnsRequestedNumberOfCards (int numSpaces) {
      var deck = BoardGenerator.CreateBoard(numSpaces);

      Assert.IsNotNull(deck, $"CreateBoard({numSpaces}) returned null for a legal board size");
      Assert.AreEqual(numSpaces, deck.Length);
    }

    [TestCase(2)]
    [TestCase(10)]
    [TestCase(SHIPPING_BOARD_SIZE)]
    public void CreateBoard_DealsExactlyTwoOfEachFace (int numSpaces) {
      var deck = BoardGenerator.CreateBoard(numSpaces);

      var counts = CountByFace(deck);
      Assert.AreEqual(numSpaces / 2, counts.Count, "Wrong number of distinct faces in the deck");
      foreach (var pair in counts) {
        Assert.AreEqual(2, pair.Value, $"Face '{pair.Key}' appears {pair.Value} times, expected exactly 2");
      }
    }

    [Test]
    public void CreateBoard_AtMaxSupportedSize_Succeeds() {
      var deck = BoardGenerator.CreateBoard(CARD_TYPE_COUNT * 2);

      Assert.IsNotNull(deck);
      Assert.AreEqual(CARD_TYPE_COUNT, CountByFace(deck).Count);
    }

    [Test]
    public void CreateBoard_LargerThanAvailableFaces_ReturnsNullAndLogsError() {
      LogAssert.Expect(LogType.Error, "Board Size is larger than number of available card types");

      var deck = BoardGenerator.CreateBoard((CARD_TYPE_COUNT + 1) * 2);

      Assert.IsNull(deck);
    }

    /// <summary>
    /// Guards the shuffle: an unshuffled deck is always AA22 33..., so if many
    /// independent decks all come back in that exact order the shuffle is broken.
    /// </summary>
    [Test]
    public void CreateBoard_ShufflesTheDeck() {
      var sortedDeck = BoardGenerator.CreateBoard(SHIPPING_BOARD_SIZE);
      System.Array.Sort(sortedDeck);

      var shuffledAtLeastOnce = false;
      for (var attempt = 0; attempt < 20 && !shuffledAtLeastOnce; attempt++) {
        var deck = BoardGenerator.CreateBoard(SHIPPING_BOARD_SIZE);
        shuffledAtLeastOnce = !deck.SequenceEqual(sortedDeck);
      }

      Assert.IsTrue(shuffledAtLeastOnce, "20 decks in a row came back in sorted order - shuffle is not running");
    }

    private static Dictionary<char, int> CountByFace (IEnumerable<char> deck) {
      var counts = new Dictionary<char, int>();
      foreach (var face in deck) {
        counts.TryGetValue(face, out var count);
        counts[face] = count + 1;
      }

      return counts;
    }
  }
}
