using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CardMatch.Tests.PlayMode {
  /// <summary>
  /// Loads MainScene and drives it the way a player would: flip a card, wait for the
  /// flip animation, read the resulting state. Tests never poke private fields, so a
  /// green run means the real scene wiring (prefab, board, listener, UI) still works.
  /// </summary>
  public class GameDriver {
    public const string SCENE_NAME = "MainScene";

    /// <summary>Card flips are ~1s of real time. Everything runs fast-forwarded so a full
    /// 14-pair playthrough finishes in seconds instead of a minute.</summary>
    private const float TIME_SCALE = 20f;

    /// <summary>Unscaled seconds any single wait is allowed to take before the test fails.</summary>
    private const float TIMEOUT_SECONDS = 15f;

    public GameMistress Mistress { get; private set; }
    public Board Board { get; private set; }
    public CardListener Listener { get; private set; }

    public IEnumerator LoadScene() {
      Time.timeScale = TIME_SCALE;
      yield return SceneManager.LoadSceneAsync(SCENE_NAME, LoadSceneMode.Single);
      // One frame for Start() to run NewGame() and populate the board.
      yield return null;

      Mistress = UnityEngine.Object.FindAnyObjectByType<GameMistress>();
      Board = UnityEngine.Object.FindAnyObjectByType<Board>();
      Listener = UnityEngine.Object.FindAnyObjectByType<CardListener>();

      Assert.IsNotNull(Mistress, $"{SCENE_NAME} has no GameMistress");
      Assert.IsNotNull(Board, $"{SCENE_NAME} has no Board");
      Assert.IsNotNull(Listener, $"{SCENE_NAME} has no CardListener");
    }

    public void Teardown() {
      Time.timeScale = 1f;
    }

    public Card[] Cards() {
      return UnityEngine.Object.FindObjectsByType<Card>(FindObjectsInactive.Exclude);
    }

    public int ExpectedCardCount() {
      return Board.maxCols * Board.maxRows;
    }

    /// <summary>Flips one card and waits until the board is idle again (rotation done,
    /// and any pair evaluation the flip triggered has resolved).</summary>
    public IEnumerator Flip (Card card) {
      card.Flip();
      yield return WaitForIdle();
    }

    /// <summary>Flips both cards of one pair and waits for the match/mismatch verdict.</summary>
    public IEnumerator FlipPair (Card first, Card second) {
      yield return Flip(first);
      yield return Flip(second);
    }

    /// <summary>Waits until nothing is mid-rotation and the listener is accepting input again.</summary>
    public IEnumerator WaitForIdle() {
      yield return WaitUntil(
        () => !Listener.FlippingCardsPaused && Cards().All(c => !c.Rotating),
        "board never became idle (a card is stuck rotating, or pair evaluation never finished)");
    }

    public IEnumerator WaitUntil (Func<bool> condition, string failureMessage) {
      var deadline = Time.realtimeSinceStartup + TIMEOUT_SECONDS;
      while (!condition()) {
        if (Time.realtimeSinceStartup > deadline) {
          Assert.Fail($"Timed out after {TIMEOUT_SECONDS}s: {failureMessage}");
        }

        yield return null;
      }
    }

    /// <summary>Returns two face-down cards sharing a face.</summary>
    public (Card, Card) FindMatchingPair() {
      var pair = PlayableCards()
        .GroupBy(c => c.CardType)
        .FirstOrDefault(g => g.Count() >= 2);

      Assert.IsNotNull(pair, "No unmatched matching pair left on the board");
      return (pair.ElementAt(0), pair.ElementAt(1));
    }

    /// <summary>Returns two face-down cards with different faces.</summary>
    public (Card, Card) FindMismatchedPair() {
      var byFace = PlayableCards().GroupBy(c => c.CardType).ToList();

      Assert.Greater(byFace.Count, 1, "Board has only one face left, cannot build a mismatch");
      return (byFace[0].First(), byFace[1].First());
    }

    /// <summary>Cards still in play: face down and not yet matched.</summary>
    public List<Card> PlayableCards() {
      return Cards().Where(c => !c.FaceUp).ToList();
    }

    /// <summary>Plays a perfect game: matches every pair on the board.</summary>
    public IEnumerator ClearBoard() {
      var pairsNeeded = ExpectedCardCount() / 2;
      for (var i = 0; i < pairsNeeded; i++) {
        var (first, second) = FindMatchingPair();
        yield return FlipPair(first, second);
      }

      // Win detection lives in GameMistress.Update, so give it a frame to observe the score.
      yield return null;
    }
  }
}
