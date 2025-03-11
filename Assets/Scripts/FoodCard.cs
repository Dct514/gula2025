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
      Steak = 50,
      Turkey = 70,
      Cake = 100,
      deny = 0
   }

   public CardPoint cardPoint;
   public bool clicked = false;

   public void OnPointerClick(PointerEventData eventData)
   {
      GameManager.Instance.selectedFoodCard = cardPoint;
      Debug.Log($"{GameManager.Instance.selectedFoodCard}");
   }
}
