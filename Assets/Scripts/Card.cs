using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{

    static Dictionary<char,int> cardTypeToFrameNo = new Dictionary<char, int> {
        {'A', 0},
        {'2', 1},
        {'3', 2},
        {'4', 3},
        {'5', 4},
        {'6', 5},
        {'7', 6},
        {'8', 7},
        {'9', 8},
        {'X', 9}, // 10
        {'J', 10},
        {'Q', 11},
        {'K', 12},
        {'*', 13}, // Joker
        {'B', 27} // the back 
    };

    [SerializeField]
    private Button cardButton;

    [SerializeField]
    private Image cardImage;

    public bool FaceUp;
    public char CardType {get; private set;}
    public CardListener CardListener;
    public bool Rotating {get; private set;} //used to prevent button mashing
    
    private Vector3 faceUpRotation = new Vector3(0f, 180f, 0f);
    private Vector3 faceDownRotation = new Vector3(0f, 0f, 0f);

    void Awake(){
        var materialForThisCard = Instantiate(cardImage.material);
        cardImage.material = materialForThisCard;
        cardImage.material.SetFloat("_FrontFrameNo", cardTypeToFrameNo['B']);
    }

    public void SetFace(char cardType){
        CardType = cardType;
        cardImage.material.SetFloat("_BackFrameNo", cardTypeToFrameNo[CardType]);
    }

    public void Flip(){
        if (CardListener == null ||
            Rotating ||
            CardListener.FlippingCardsPaused) {
            return;
        }

        FaceUp = !FaceUp;
        StartCoroutine(RotateCard(FaceUp ? faceUpRotation : faceDownRotation));
    }

    private IEnumerator RotateCard(Vector3 finalRotation){
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
            if(transform.eulerAngles.Equals(finalRotation)){
                break;
            }
        }
        Rotating = false;
    }
    
    public void Matched(){
        cardButton.interactable = false;
    }

    public void Reset(){
        FaceUp = false;
        StartCoroutine(RotateCard(faceDownRotation));
        cardButton.interactable = true;
    }
}
