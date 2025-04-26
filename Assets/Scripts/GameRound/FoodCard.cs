using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FoodCard : MonoBehaviour, IPointerClickHandler
{
    public CardPoint cardPoint;
    public enum CardPoint
    {
        Bread = 1,
        Soup = 2,
        Fish = 3,
        Steak = 5,
        Turkey = 7,
        Cake = 10,
        deny = 0,
        tablecard = -1,
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        RefactoryGM.Instance.DeselectAllCards();
        if (cardPoint != FoodCard.CardPoint.tablecard) 
        {
            RefactoryGM.Instance.FoodCardSelect(cardPoint);
        }
        // 현재 카드의 아웃라인을 켭니다.
        CardOutlineController outline = GetComponent<CardOutlineController>();
        if (outline != null)
        {
            outline.SelectCard();
        }
        //GameManager.Instance.DeselectAllCards();
        //GameManager.Instance.pickedCardsave(cardPoint);
    }
}
