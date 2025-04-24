using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Deck))]
[RequireComponent(typeof(JsonParseLayout))]
public class GolfSolitaire : MonoBehaviour
{
    private static GolfSolitaire S;

    [Header("Dynamic")]
    public List<CardGolf> drawPile;
    public List<CardGolf> discardPile;
    public List<CardGolf> tableau;
    public CardGolf target;

    private Transform layoutAnchor;
    private Deck deck;
    private JsonLayout jsonLayout;

    private Dictionary<int, CardGolf> tableauIdToCardDict;
    private Dictionary<int, CardGolf> bottomRowCards;

    void Start()
    {
        if (S != null) Debug.LogError("Attempted to set S more than once!");
        S = this;

        JsonParseLayout jsonParseLayout = GetComponent<JsonParseLayout>();
        if (jsonParseLayout.jsonLayoutFile == null)
        {
            Debug.LogError("GolfSolitaire: JsonLayoutFile is not assigned in the Inspector!");
            return;
        }

        jsonLayout = jsonParseLayout.layout;
        if (jsonLayout == null)
        {
            Debug.LogError("GolfSolitaire: jsonLayout is null! Check if the JSON file is valid.");
            return;
        }

        deck = GetComponent<Deck>();
        if (deck == null)
        {
            Debug.LogError("GolfSolitaire: Deck component not found!");
            return;
        }

        deck.InitDeck();
        Deck.Shuffle(ref deck.cards);

        // Convert regular cards to CardGolf type
        drawPile = ConvertCardsToCardGolfs(deck.cards);

        tableauIdToCardDict = new Dictionary<int, CardGolf>();
        bottomRowCards = new Dictionary<int, CardGolf>();

        LayoutTableau();

        MoveToTarget(Draw());
        UpdateDrawPile();
    }

    List<CardGolf> ConvertCardsToCardGolfs(List<Card> listCard)
    {
        List<CardGolf> listCG = new List<CardGolf>();
        CardGolf cg;
        foreach (Card card in listCard)
        {
            cg = card as CardGolf;
            listCG.Add(cg);
        }
        return listCG;
    }

    CardGolf Draw()
    {
        if (drawPile.Count == 0) return null;

        CardGolf cg = drawPile[0];
        drawPile.RemoveAt(0);
        return cg;
    }

    void LayoutTableau()
    {
        if (layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
        }

        CardGolf cg;
        tableau = new List<CardGolf>();

        Dictionary<int, List<CardGolf>> columnCards = new Dictionary<int, List<CardGolf>>();

        foreach (JsonLayoutSlot slot in jsonLayout.slots)
        {
            cg = Draw(); // Pull a card from the top of the draw pile
            cg.faceUp = true;
            cg.transform.SetParent(layoutAnchor);

            int z = 0;
            if (slot.layer != null && slot.layer.Length > 0)
            {
                z = int.Parse(slot.layer[slot.layer.Length - 1].ToString());
            }

            cg.SetLocalPos(new Vector3(
                jsonLayout.multiplier.x * slot.x,
                jsonLayout.multiplier.y * slot.y,
                -z));

            cg.layoutID = slot.id;
            cg.layoutSlot = slot;
            cg.state = eGolfCardState.tableau;
            if (slot.layer != null && slot.layer.Length > 0)
            {
                cg.SetSpriteSortingLayer(slot.layer);
            }

            tableau.Add(cg);
            tableauIdToCardDict.Add(slot.id, cg);

            int column = slot.x;
            if (!columnCards.ContainsKey(column))
            {
                columnCards[column] = new List<CardGolf>();
            }
            columnCards[column].Add(cg);
        }

        foreach (int column in columnCards.Keys)
        {
            List<CardGolf> cardsInColumn = columnCards[column];
            // Sort by ascending Y (lowest Y is bottom)
            cardsInColumn.Sort((a, b) => a.layoutSlot.y.CompareTo(b.layoutSlot.y));

            if (cardsInColumn.Count > 0)
            {
                bottomRowCards[column] = cardsInColumn[0];
            }
        }
    }

    void MoveToDiscard(CardGolf cg)
    {
        cg.state = eGolfCardState.discard;
        discardPile.Add(cg);
        cg.transform.SetParent(layoutAnchor);

        if (jsonLayout.discardPile != null)
        {
            cg.SetLocalPos(new Vector3(
                jsonLayout.multiplier.x * jsonLayout.discardPile.x,
                jsonLayout.multiplier.y * jsonLayout.discardPile.y,
                0));

            if (!string.IsNullOrEmpty(jsonLayout.discardPile.layer))
            {
                cg.SetSpriteSortingLayer(jsonLayout.discardPile.layer);
            }
            else
            {
                cg.SetSpriteSortingLayer("Default");
            }
        }

        cg.faceUp = true;
        cg.SetSortingOrder(-200 + (discardPile.Count * 3));
    }

    void MoveToTarget(CardGolf cg)
    {
        if (target != null) MoveToDiscard(target);

        if (cg == null) return;

        MoveToDiscard(cg);
        target = cg;
        cg.state = eGolfCardState.target;
        cg.SetSpriteSortingLayer("Target");
        cg.SetSortingOrder(0);
    }

    void UpdateDrawPile()
    {
        if (jsonLayout.drawPile == null)
        {
            Debug.LogError("GolfSolitaire: drawPile configuration is missing in the JSON layout!");
            return;
        }

        CardGolf cg;
        for (int i = 0; i < drawPile.Count; i++)
        {
            cg = drawPile[i];
            cg.transform.SetParent(layoutAnchor);

            Vector3 cpPos = new Vector3();
            cpPos.x = jsonLayout.multiplier.x * jsonLayout.drawPile.x;
            if (jsonLayout.drawPile != null)
            {
                cpPos.x += jsonLayout.drawPile.xStagger * i;
            }
            cpPos.y = jsonLayout.multiplier.y * jsonLayout.drawPile.y;
            cpPos.z = 0.1f * i;
            cg.SetLocalPos(cpPos);

            cg.faceUp = false;
            cg.state = eGolfCardState.drawpile;

            if (!string.IsNullOrEmpty(jsonLayout.drawPile.layer))
            {
                cg.SetSpriteSortingLayer(jsonLayout.drawPile.layer);
            }
            else
            {
                cg.SetSpriteSortingLayer("Default");
            }
            cg.SetSortingOrder(-10 * i);
        }
    }

    void UpdateBottomCards(CardGolf removedCard)
    {
        int column = removedCard.layoutSlot.x;

        List<CardGolf> cardsInColumn = new List<CardGolf>();
        foreach (CardGolf cg in tableau) // Only checking cards still in tableau
        {
            if (cg.layoutSlot.x == column)
            {
                cardsInColumn.Add(cg);
            }
        }

        // Sort ascending by Y (lowest Y is bottom row)
        cardsInColumn.Sort((a, b) => a.layoutSlot.y.CompareTo(b.layoutSlot.y));

        if (cardsInColumn.Count > 0)
        {
            bottomRowCards[column] = cardsInColumn[0]; // Lowest Y card
        }
        else
        {
            bottomRowCards.Remove(column);
        }
    }

    bool CanPlayOnTarget(CardGolf cg)
    {
        if (target == null) return false;

        // Standard check for ranks one apart
        if (Mathf.Abs(cg.rank - target.rank) == 1)
            return true;

        // Special case for King (13) and Ace (1)
        if ((cg.rank == 1 && target.rank == 13) || (cg.rank == 13 && target.rank == 1))
            return true;

        return false;
    }

    static public void CARD_CLICKED(CardGolf cg)
    {
        if (S == null) return;

        switch (cg.state)
        {
            case eGolfCardState.target:
                break;

            case eGolfCardState.drawpile:
                if (S.drawPile.Count > 0)
                {
                    S.MoveToTarget(S.Draw());
                    S.UpdateDrawPile();
                }
                break;

            case eGolfCardState.tableau:
                bool isPlayable = false;
                if (S.bottomRowCards.ContainsValue(cg))
                {
                    isPlayable = true;
                }

                if (isPlayable)
                {
                    if (S.CanPlayOnTarget(cg))
                    {
                        // Only remove from tableau and update bottom cards if we can play on target
                        S.tableau.Remove(cg);
                        S.MoveToTarget(cg);
                        S.UpdateBottomCards(cg);
                    }
                    // If we can't play on target, do nothing - card stays in place
                }
                break;
        }
    }
}