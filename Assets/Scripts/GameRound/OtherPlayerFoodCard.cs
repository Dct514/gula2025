using System.Collections;
using System.Collections.Generic;
using Photon.Pun.UtilityScripts;
using UnityEngine;
using UnityEngine.UI;

public class OtherPlayerFoodCard : MonoBehaviour
{
    public int playerNum;
    public FoodCard.CardPoint cardPoint;
    public void OnCardClicked()
    {
        GameManager.Instance.ChoiceFree(playerNum, cardPoint);
    }

}
