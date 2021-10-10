using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMistress : MonoBehaviour
{
    [SerializeField]
    private Board board;

    [SerializeField]
    int boardSize; 
    // Start is called before the first frame update
    void Start()
    {
        boardSize = board.maxCols * board.maxRows;
        SetupBoard();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SetupBoard(){
        var cardDeck = BoardGenerator.CreateBoard(boardSize);
        if(cardDeck == null){
            //we can't make the board for some reason, fallback/alert the player
            return;
        }
        board.SetCards(cardDeck);
    }
}
