using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;

public class RefactoryGM : MonoBehaviourPun
{
    public static RefactoryGM Instance;
    public TMP_Text gamestatustxt;

     public Sprite[] cardSprites; // 카드 스프라이트
    public Image[] cardImages; // 카드 스프라이트 표시될 곳
    public Image[] myImages; // 내 카드 스프라이트
    ExitGames.Client.Photon.Hashtable player = new ExitGames.Client.Photon.Hashtable();
    ExitGames.Client.Photon.Hashtable Turn = new ExitGames.Client.Photon.Hashtable(); 
    public class PlayerData
    {
        public int playerNumber;
        public bool Submited { get; set; }

    }
    public PlayerData playerData = new PlayerData();
    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Turn["currentPlayerIndex"] = UnityEngine.Random.Range(1, PhotonNetwork.CurrentRoom.PlayerCount + 1);
            Turn["currentTurn"] = 0;
            Turn["foodSubmited"] = 0;
            
            PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
        }

        player["selectedFoodCard"] = null;
        player["score"] = 0;

        PhotonNetwork.LocalPlayer.SetCustomProperties(player);
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

    }
    public void SetCustomProperty(Photon.Realtime.Player player, string key, int value)
    {
        ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable
        {
            { key, value }
        };
        player.SetCustomProperties(properties);
    }

    public void UpdateScore(int playerNumber, int score)
    {
        player["score" + playerNumber] = score;
        PhotonNetwork.CurrentRoom.SetCustomProperties(player);
    }

    public void MainTurnStart(FoodCard.CardPoint cardPoint)
    {
        switch (Turn["currentTurn"])
        {
            case 0:
                // 선 플레이어만 진행하는, 첫 카드를 내는 턴
                if ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    playerData.Submited = true;
                    Turn["currentTurn"] = 1;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
                    photonView.RPC("SetCardValue", RpcTarget.All, (int)player["selectedFoodCard"], PhotonNetwork.LocalPlayer.ActorNumber);

                    Debug.Log($"현재 턴 : {Turn["currentTurn"]}");
                }
                else
                {
                    gamestatustxt.text = "차례가 아닙니다.";
                }
                break;
                
            case 1:
                // 다른 플레이어들이 음식 카드 또는 거절 카드를 내는 턴
                if (playerData.Submited == false && player["selectedFoodCard"] != null && (int)Turn["currentPlayerIndex"] != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    playerData.Submited = true;
                    Turn["foodSubmited"] = (int)Turn["foodSubmited"] + 1;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
                    photonView.RPC("SetCardValue", RpcTarget.All, (int)player["selectedFoodCard"], PhotonNetwork.LocalPlayer.ActorNumber);
                    
                    if (FoodSubmited()) Turn["currentTurn"] = 2;
                }
                else if ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    gamestatustxt.text = "다른 플레이어가 카드를 제출할 때까지 기다려주세요.";
                }
                else if (playerData.Submited == true)
                {
                    gamestatustxt.text = "이미 카드를 제출했습니다.";
                }
                else if (player["selectedFoodCard"] == null)
                {
                    gamestatustxt.text = "카드를 선택해주세요.";
                }
                
                break;
 
            case 2:
                // 선 플레이어만 진행, 다른 플레이어의 카드를 고르는 턴
                UpdateScore(3, (int)cardPoint);
                break;
            case 3:
                // 선택된 플레이어와 선 플레이어가 식사 / 강탈을 고르는 턴
                UpdateScore(4, (int)cardPoint);
                break;
            default :
                Debug.Log("Error: Invalid turn number.");
                break;
        }
    }

    public void FoodCardSelect(FoodCard.CardPoint cardPoint)
    {
        if (playerData.Submited == false)
        {
            player["selectedFoodCard"] = cardPoint;
        }
        else
        {
            gamestatustxt.text = "이미 카드를 제출했습니다.";
        }
    }
    private bool FoodSubmited(){
        if ((int)player["foodSubmited"] == 4) return true;
        else return false;
    }

    [PunRPC]
    public void SetCardValue(int value, int playernum)
    {

        cardImages[playernum].sprite = cardSprites[GetSpriteIndex(value)];

    }
        private int GetSpriteIndex(int value)
    {
        switch (value)
        {
            case 1: return 0;
            case 2: return 1;
            case 3: return 2;
            case 5: return 3;
            case 7: return 4;
            case 10: return 5;
            case 0 : return 6;
            default: return 6;
        }
    }


}