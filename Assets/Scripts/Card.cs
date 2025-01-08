using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour {
  private static readonly Dictionary<char, int> cardTypeToFrameNo = new() {
    { 'A', 0 },
    { '2', 1 },
    { '3', 2 },
    { '4', 3 },
    { '5', 4 },
    { '6', 5 },
    { '7', 6 },
    { '8', 7 },
    { '9', 8 },
    { 'X', 9 }, // 10
    { 'J', 10 },
    { 'Q', 11 },
    { 'K', 12 },
    { '*', 39 }, // Joker
    { 'B', 27 } // the back 
  };

  [SerializeField] private Button cardButton;
  [SerializeField] private Image cardImage;

  public bool FaceUp;
  public char CardType { get; private set; }
  [HideInInspector] public CardListener CardListener;
  public bool Rotating { get; private set; } //used to prevent button mashing

  private readonly Vector3 _faceUpRotation = new(0f, 180f, 0f);
  private readonly Vector3 _faceDownRotation = new(0f, 0f, 0f);
  private static readonly int backIndex = Shader.PropertyToID("_BackIndex");
  private static readonly int frontIndex = Shader.PropertyToID("_FrontIndex");

  private void Awake() {
    var materialForThisCard = Instantiate(cardImage.material);
    cardImage.material = materialForThisCard;
    cardImage.material.SetInteger(backIndex, cardTypeToFrameNo['B']);
  }

  public void SetFace (char cardType) {
    CardType = cardType;
    cardImage.material.SetInteger(frontIndex, cardTypeToFrameNo[CardType]);
  }

  public void Flip() {
    if (CardListener == null ||
        Rotating ||
        CardListener.FlippingCardsPaused) {
      return;
    }

    FaceUp = !FaceUp;
    StartCoroutine(RotateCard(FaceUp ? _faceUpRotation : _faceDownRotation));
  }

  private IEnumerator RotateCard (Vector3 finalRotation) {
    Rotating = true;
    if (FaceUp) {
      CardListener.RegisterCardFlip(this);
    }

    CardListener.CardFlipAudioSource.Play();
    var t = 0f;
    while (true) {
      var nextRotationStep = Vector3.Lerp(transform.eulerAngles, finalRotation, t);
      transform.eulerAngles = nextRotationStep;
      t += Time.deltaTime;
      yield return new WaitForEndOfFrame();
      if (transform.eulerAngles.Equals(finalRotation)) {
        break;
      }
    }

    Rotating = false;
  }

  public void Reset() {
    FaceUp = false;
    StartCoroutine(RotateCard(_faceDownRotation));
    cardButton.interactable = true;
  }
}