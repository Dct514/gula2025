using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FoodCard : MonoBehaviour, IPointerClickHandler
{
    public enum CardPoint
    {
        Bread = 1,
        Soup = 2,
        Fish = 3,
        Steak = 5,
        Turkey = 7,
        Cake = 10,
        deny = 0
    }

    public CardPoint cardPoint;

    public void OnPointerClick(PointerEventData eventData)
    {
        GameManager.Instance.DeselectAllCards();

        GameManager.Instance.pickedCardsave(cardPoint);
        Debug.Log("cardclicked");

        // 현재 카드의 아웃라인을 켭니다.
        CardOutlineController outline = GetComponent<CardOutlineController>();
        if (outline != null)
        {
            outline.SelectCard();
        }
    }
}
