using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FoodCard : MonoBehaviour, IPointerClickHandler
{
    public CardPoint cardPoint;
    public enum CardPoint
    {
        Bread = 1,
        Soup = 2,
        Fish = 3,
        Steak = 4,
        Turkey = 5,
        Cake = 7,
        deny = 0,
        tablecard = -1,
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        Image myImage = GetComponent<Image>();

        if (myImage == null)
        {
            Debug.LogWarning("Image 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        if (RefactoryGM.Instance != null && myImage.sprite.name == RefactoryGM.Instance.backSprite.name)
        {
            Debug.Log("사용한(뒷면) 카드는 클릭할 수 없습니다.");
            return;
        }

        if (RefactoryGM.Instance != null)
        {
            RefactoryGM.Instance.DeselectAllCards();
            RefactoryGM.Instance.FoodCardSelect(cardPoint);
        }
        else
        {
            Debug.LogWarning("RefactoryGM.Instance 가 null입니다.");
        }

        CardOutlineController outline = GetComponent<CardOutlineController>();
        if (outline != null)
        {
            outline.SelectCard();
        }
    }
}