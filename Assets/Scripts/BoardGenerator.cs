using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BoardGenerator 
{
    private static readonly char[] cardTypes = new char[] {'A','2','3','4','5','6','7','8','9','X','J','Q','K','*'}; //'*' is for Joker, 'X' for 10

    //numSpaces MUST BE EVEN
    public static char[] CreateBoard(int numSpaces){
     var uniqueCards = numSpaces/2;
     if(uniqueCards > cardTypes.Length) {
         Debug.LogError("Board Size is larger than number of available card types");
         return null;
     }

     var deck = new char[numSpaces];
     for(int i = 0, j=0 ; i < deck.Length; i+=2){
         deck[i] = cardTypes[j];
         deck[i+1] = cardTypes [j];
         j++;
     }
     
     var rng = new System.Random();
     rng.Shuffle(deck);

     return deck;

 }
}
