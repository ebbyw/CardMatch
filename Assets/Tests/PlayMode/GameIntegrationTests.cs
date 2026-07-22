using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CardMatch.Tests.PlayMode {
  /// <summary>
  /// End-to-end tests against the real MainScene. Each test reloads the scene, so
  /// nothing leaks between cases (CardListener keeps its flipped-card cache in a
  /// static field, and the win count lives in PlayerPrefs).
  /// </summary>
  public class GameIntegrationTests {
    /// <summary>Mirrors GameMistress.PLAYER_WIN_COUNT_KEY, which is private.</summary>
    private const string PLAYER_WIN_COUNT_KEY = "c495af72-66d8-49fb-b0c9-68fed6b8951d";

    private GameDriver _driver;
    private bool _hadSavedWinCount;
    private int _savedWinCount;

    [UnitySetUp]
    public IEnumerator SetUp() {
      _hadSavedWinCount = PlayerPrefs.HasKey(PLAYER_WIN_COUNT_KEY);
      _savedWinCount = PlayerPrefs.GetInt(PLAYER_WIN_COUNT_KEY, 0);
      PlayerPrefs.SetInt(PLAYER_WIN_COUNT_KEY, 0);

      _driver = new GameDriver();
      yield return _driver.LoadScene();
    }

    [TearDown]
    public void TearDown() {
      _driver.Teardown();

      // Leave the player's real win count exactly as we found it.
      if (_hadSavedWinCount) {
        PlayerPrefs.SetInt(PLAYER_WIN_COUNT_KEY, _savedWinCount);
      }
      else {
        PlayerPrefs.DeleteKey(PLAYER_WIN_COUNT_KEY);
      }

      PlayerPrefs.Save();
    }

    [UnityTest]
    public IEnumerator Scene_DealsAFullBoardOfFaceDownCards() {
      var cards = _driver.Cards();

      Assert.AreEqual(_driver.ExpectedCardCount(), cards.Length,
        "Board did not instantiate one card per grid slot");
      Assert.IsTrue(cards.All(c => !c.FaceUp), "Some cards started face up");
      Assert.IsTrue(cards.All(c => c.CardListener != null),
        "Some cards were never wired to the CardListener, so they will not respond to clicks");
      Assert.AreEqual(0, _driver.Listener.MatchedPairsCount);

      yield break;
    }

    [UnityTest]
    public IEnumerator Deal_ContainsExactlyTwoOfEachFace() {
      var facesWithWrongCount = _driver.Cards()
        .GroupBy(c => c.CardType)
        .Where(g => g.Count() != 2)
        .Select(g => $"'{g.Key}' x{g.Count()}")
        .ToList();

      Assert.IsEmpty(facesWithWrongCount,
        "Dealt board is not made of pairs: " + string.Join(", ", facesWithWrongCount));

      yield break;
    }

    [UnityTest]
    public IEnumerator FlippingOneCard_TurnsItFaceUpAndScoresNothing() {
      var card = _driver.PlayableCards().First();

      yield return _driver.Flip(card);

      Assert.IsTrue(card.FaceUp, "Card did not turn face up");
      Assert.AreEqual(0, _driver.Listener.MatchedPairsCount, "A single flip scored a pair");
      Assert.IsFalse(_driver.Listener.FlippingCardsPaused,
        "Input stayed locked after only one card was flipped");
    }

    [UnityTest]
    public IEnumerator MatchingPair_ScoresAndStaysFaceUp() {
      var (first, second) = _driver.FindMatchingPair();

      yield return _driver.FlipPair(first, second);

      Assert.AreEqual(1, _driver.Listener.MatchedPairsCount, "Matching pair did not score");
      Assert.IsTrue(first.FaceUp && second.FaceUp, "Matched cards were flipped back down");
      Assert.IsFalse(_driver.Listener.FlippingCardsPaused, "Input stayed locked after the pair resolved");
    }

    [UnityTest]
    public IEnumerator MismatchedPair_FlipsBothBackAndScoresNothing() {
      var (first, second) = _driver.FindMismatchedPair();

      yield return _driver.FlipPair(first, second);

      Assert.AreEqual(0, _driver.Listener.MatchedPairsCount, "A mismatch scored a pair");
      Assert.IsFalse(first.FaceUp, "First mismatched card was left face up");
      Assert.IsFalse(second.FaceUp, "Second mismatched card was left face up");
      Assert.IsFalse(_driver.Listener.FlippingCardsPaused, "Input stayed locked after the mismatch resolved");
    }

    /// <summary>Button-mashing guard: only two cards may be in play at a time.</summary>
    [UnityTest]
    public IEnumerator ThirdCard_CannotBeFlippedWhileAPairIsBeingEvaluated() {
      var (first, second) = _driver.FindMismatchedPair();
      yield return _driver.Flip(first);

      // Do NOT wait here - the second flip is what locks input, and we want to
      // click a third card while that lock is up.
      second.Flip();
      Assert.IsTrue(_driver.Listener.FlippingCardsPaused,
        "Flipping the second card did not lock input");

      var third = _driver.PlayableCards().First(c => c != first && c != second);
      third.Flip();

      Assert.IsFalse(third.FaceUp, "A third card was flipped while a pair was still being evaluated");

      yield return _driver.WaitForIdle();
      Assert.IsFalse(third.FaceUp, "The third card flipped once evaluation finished");
    }

    /// <summary>A card already in play should not re-register itself on a double click.</summary>
    [UnityTest]
    public IEnumerator SameCard_CannotBeFlippedTwiceIntoAPair() {
      var card = _driver.PlayableCards().First();
      yield return _driver.Flip(card);

      card.Flip();
      yield return _driver.WaitForIdle();

      Assert.AreEqual(0, _driver.Listener.MatchedPairsCount,
        "A card matched itself, so one card can fill both pair slots");
    }

    [UnityTest]
    public IEnumerator NewGame_ClearsTheScoreAndResetsEveryCard() {
      var (first, second) = _driver.FindMatchingPair();
      yield return _driver.FlipPair(first, second);
      Assert.AreEqual(1, _driver.Listener.MatchedPairsCount, "Setup failed: pair did not score");

      _driver.Mistress.NewGame();
      yield return _driver.WaitForIdle();

      Assert.AreEqual(0, _driver.Listener.MatchedPairsCount, "New game did not clear the score");
      Assert.IsTrue(_driver.Cards().All(c => !c.FaceUp), "New game left cards face up");
      Assert.IsTrue(_driver.Cards().All(c => !c.IsMatched), "New game left cards locked out of play");
      Assert.AreEqual(_driver.ExpectedCardCount(), _driver.Cards().Length,
        "New game changed the number of cards on the board");

      var facesWithWrongCount = _driver.Cards().GroupBy(c => c.CardType).Where(g => g.Count() != 2).ToList();
      Assert.IsEmpty(facesWithWrongCount, "New game dealt a board that is not made of pairs");
    }

    [UnityTest]
    public IEnumerator ClearingTheBoard_WinsTheGameAndPersistsTheWinCount() {
      yield return _driver.ClearBoard();

      Assert.AreEqual(_driver.ExpectedCardCount() / 2, _driver.Listener.MatchedPairsCount,
        "Not every pair was matched");
      Assert.AreEqual(1, PlayerPrefs.GetInt(PLAYER_WIN_COUNT_KEY, 0),
        "Winning did not persist an incremented win count");
    }

    /// <summary>Win detection must not fire again on later frames of the same game.</summary>
    [UnityTest]
    public IEnumerator Winning_IncrementsTheWinCountExactlyOnce() {
      yield return _driver.ClearBoard();

      for (var i = 0; i < 5; i++) {
        yield return null;
      }

      Assert.AreEqual(1, PlayerPrefs.GetInt(PLAYER_WIN_COUNT_KEY, 0),
        "Win count kept climbing after the game was already won");
    }

    [UnityTest]
    public IEnumerator MatchedCards_AreLockedOutOfPlay() {
      var (first, second) = _driver.FindMatchingPair();
      yield return _driver.FlipPair(first, second);

      Assert.IsTrue(first.IsMatched && second.IsMatched, "Matched cards were not marked as matched");

      first.Flip();
      yield return _driver.WaitForIdle();

      Assert.IsTrue(first.FaceUp, "An already-matched card was flipped back face down");
      Assert.AreEqual(1, _driver.Listener.MatchedPairsCount, "Re-flipping a matched card changed the score");
    }
  }
}
