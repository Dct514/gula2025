using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms.Impl;

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
      // 주석
   }
   public CardPoint cardPoint;
    public void OnPointerClick(PointerEventData eventData)
    {
        GameManager.Instance.pickedCardsave(cardPoint);
        Debug.Log("cardclicked");
    }

}
