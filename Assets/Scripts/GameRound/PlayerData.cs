using System;
using System.Collections.Generic;
using Photon.Pun;

[Serializable]
public class PlayerData
{
    public int playerNumber;
    public bool Submited { get; set; }
    public int selectedFoodCard;

    public List<FoodCard.CardPoint> playerHand = new List<FoodCard.CardPoint>
    {
        FoodCard.CardPoint.Bread,
        FoodCard.CardPoint.Soup,
        FoodCard.CardPoint.Fish,
        FoodCard.CardPoint.Steak,
        FoodCard.CardPoint.Turkey,
        FoodCard.CardPoint.Cake
    };
}