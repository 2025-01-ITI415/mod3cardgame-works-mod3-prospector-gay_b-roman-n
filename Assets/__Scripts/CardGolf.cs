using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum eGolfCardState { drawpile, tableau, target, discard }

public class CardGolf : Card
{
    [Header("Dynamic: CardGolf")]
    public eGolfCardState state = eGolfCardState.drawpile;

    // Optional: Used to track what other cards are hiding this one
    public List<CardGolf> hiddenBy = new List<CardGolf>();

    public int layoutID; // Used to link with layout data if needed
    public JsonLayoutSlot layoutSlot;

    /// <summary>
    /// Handles card click in Golf Solitaire.
    /// </summary>
    override public void OnMouseUpAsButton()
    {
        base.OnMouseUpAsButton();
        GolfSolitaire.CARD_CLICKED(this); // Call method in your new GolfSolitaire class
    }
}
