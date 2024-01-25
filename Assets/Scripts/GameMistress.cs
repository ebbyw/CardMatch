using UnityEngine;
using UnityEngine.UIElements;

public class GameMistress : MonoBehaviour {
  private const string PLAYER_WIN_COUNT_KEY = "c495af72-66d8-49fb-b0c9-68fed6b8951d";
  
  [SerializeField] private Board board;
  [SerializeField] private int boardSize;
  [SerializeField] private AudioSource victorySound;
  [SerializeField] private CardListener cardListener;
  [SerializeField] private UIDocument uiDocument;

  private int WinCount {
    get => _winCount;
    set {
      _winCount = value;
      //Update Win Count in UI
      if (_winCountLabel != null) {
        _winCountLabel.text = $"Wins: {_winCount}";
      }
    }
  }
  
  private bool _firstTimeSetupComplete;
  private int _matchPairsGoal;
  private bool _gameWon;
  private int _winCount;
  private Label _winCountLabel;

  // Start is called before the first frame update
  private void Start() {
    if (uiDocument == null) {
      uiDocument = GetComponent<UIDocument>();
    }

    if (uiDocument) {
      SetUpUI();
    }

    boardSize = board.maxCols * board.maxRows;
    if (boardSize % 2 != 0) {
      Debug.LogError("board size is uneven, this will block board creation");
    }

    NewGame();
  }

  private void SetUpUI() {
    var root = uiDocument.rootVisualElement;
    var newGameBtn = root.Q<Button>("NewGameBtn");
    newGameBtn.clickable.clicked += NewGame;
    _winCountLabel = root.Q<Label>("WinCountLbl");

    if (PlayerPrefs.HasKey(PLAYER_WIN_COUNT_KEY)) {
      WinCount = PlayerPrefs.GetInt(PLAYER_WIN_COUNT_KEY);
    }
  }

  // Update is called once per frame
  private void Update() {
    if (_gameWon || cardListener.MatchedPairsCount != _matchPairsGoal) {
      return;
    }
    
    _gameWon = true;
    WinCount++;
    PlayerPrefs.SetInt(PLAYER_WIN_COUNT_KEY, _winCount);
    //Play Win Music
    victorySound.Play();
  }

  public void NewGame() {
    _matchPairsGoal = boardSize / 2;
    cardListener.Reset();
    SetupBoard();
    _gameWon = false;
  }

  private void SetupBoard() {
    var cardDeck = BoardGenerator.CreateBoard(boardSize);
    if (cardDeck == null) {
      //we can't make the board for some reason, fallback/alert the player
      return;
    }

    board.SetCards(cardDeck, cardListener, !_firstTimeSetupComplete);
    _firstTimeSetupComplete = true;
  }
}