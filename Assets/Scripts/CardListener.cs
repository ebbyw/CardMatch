using System.Collections;
using UnityEngine;

public class CardListener : MonoBehaviour {
  private static Card[] _flippedCards = new Card[2];

  // _flippedCards is static, so with Domain Reload disabled (Enter Play Mode Settings)
  // it survives between play sessions holding dead Card references. Clear it before the
  // scene loads so every play session starts from a clean slate.
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void ResetStaticState() {
    _flippedCards = new Card[2];
  }

  public bool FlippingCardsPaused;
  public int MatchedPairsCount;
  public AudioSource CardFlipAudioSource;

  public void RegisterCardFlip (Card flippedCard) {
    if (_flippedCards[0] == null) {
      _flippedCards[0] = flippedCard;
    }
    else {
      if (_flippedCards[1] == null) {
        _flippedCards[1] = flippedCard;
        FlippingCardsPaused = true;
        StartCoroutine(EvaluateFlippedCards());
      }
      else {
        Debug.LogError("More than the max # of Cards have been flipped!");
      }
    }
  }

  private IEnumerator EvaluateFlippedCards() {
    if (_flippedCards[0] == null || _flippedCards[1] == null) {
      Debug.LogError("Cannot evaluate, not enough flipped cards registered");
      yield return null;
    }

    while (true) {
      if (!_flippedCards[0].Rotating && !_flippedCards[1].Rotating) {
        if (_flippedCards[0].CardType == _flippedCards[1].CardType) {
          //MATCH!
          MatchedPairsCount++;
          _flippedCards[0].Matched();
          _flippedCards[1].Matched();
        }
        else {
          //NO MATCH!
          _flippedCards[0].Reset();
          _flippedCards[1].Reset();
        }

        //If we're here, a result has been given, we can reset our listener cache
        _flippedCards[0] = null;
        _flippedCards[1] = null;
        FlippingCardsPaused = false;
        break;
      }

      yield return new WaitForEndOfFrame();
    }
  }

  public void Reset() {
    MatchedPairsCount = 0;
    _flippedCards[0] = null;
    _flippedCards[1] = null;
  }
}