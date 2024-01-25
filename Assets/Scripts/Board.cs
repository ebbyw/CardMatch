using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour {
  [SerializeField] private Card cardPrefab;
  [SerializeField] private GridLayoutGroup boardGridLayout;
  [SerializeField] public int maxCols;
  [SerializeField] public int maxRows;

  private readonly List<Card> _cards = new();

  public void SetCards (char[] deck, CardListener cardListener, bool firstTime) {
    if (!firstTime) {
      ResetBoard();
    }

    var deckSize = deck.Length;
    var rowNum = deckSize / maxCols;
    if (rowNum > maxRows) {
      Debug.LogError("There are too many cards in the deck, cannot make a properly sized board");
    }

    for (var i = 0; i < deckSize; i++) {
      var card = firstTime ?
        Instantiate(cardPrefab, boardGridLayout.transform) :
        _cards[i];
      if (firstTime) {
        card.CardListener = cardListener;
        _cards.Add(card);
      }

      card.SetFace(deck[i]);
    }
  }

  private void ResetBoard() {
    foreach (var card in _cards) {
      card.Reset();
    }
  }
}