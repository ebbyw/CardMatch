using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour
{

    [SerializeField]
    private Card cardPrefab;

    [SerializeField]
    private GridLayoutGroup boardGridLayout;

    [SerializeField]
    public int maxCols;

    [SerializeField]
    public int maxRows;

    private List<Card> cards = new List<Card>();

    public void SetCards(char[] deck){
        var deckSize = deck.Length;
        var rowNum = deckSize / maxCols;
        if(rowNum > maxRows) {
            Debug.LogError("TOO MANY CARDS");
        }
        for(var i = 0; i < deckSize; i++){
            var card = Instantiate(cardPrefab,boardGridLayout.transform);
            card.SetFace(deck[i]);
            cards.Add(card);
        }
    }
}
