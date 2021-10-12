using UnityEngine;
using UnityEngine.UI;

public class GameMistress : MonoBehaviour
{
    [SerializeField]
    private Board board;

    [SerializeField]
    private int boardSize; 

    [SerializeField]
    private Text WinCount;

    [SerializeField]
    private AudioSource cardFlipSound;

    [SerializeField]
    private AudioSource victorySound;

    [SerializeField]
    private CardListener cardListener;

    private bool _firstTimeSetupComplete;
    private int _matchPairsGoal;
    private bool _gameWon;
    private int _winCount;

    // Start is called before the first frame update
    void Start()
    {
        boardSize = board.maxCols * board.maxRows;
        if(boardSize % 2 != 0){
            Debug.LogError("board size is uneven, this will block board creation");
        }
        NewGame();
    }

    // Update is called once per frame
    void Update()
    {
        if (cardListener.MatchedPairsCount == _matchPairsGoal && !_gameWon) {
            _gameWon = true;
            _winCount++;
            //Play Win Music
            victorySound.Play();
            //Update win count
            WinCount.text = _winCount.ToString();
        }
    }

    public void NewGame() {
        _matchPairsGoal = boardSize/2;
        cardListener.Reset();
        _gameWon = false;
        SetupBoard();
    }

    private void SetupBoard(){
        var cardDeck = BoardGenerator.CreateBoard(boardSize);
        if (cardDeck == null) {
            //we can't make the board for some reason, fallback/alert the player
            return;
        }
        board.SetCards(cardDeck, cardListener, !_firstTimeSetupComplete);
        _firstTimeSetupComplete = true;
    }
}
