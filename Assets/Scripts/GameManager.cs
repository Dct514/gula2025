using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;


public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    public TMP_Text gamestatustxt;
    public TMP_Text submitbuttontxt;
    public TMP_Text submitbuttontxt2;
    public GameObject resultPannel;
    public TextMeshProUGUI[] playerListText;
    int pickedPlayerIndex = -1; // 선 플레이어가 고른카드의 플레이어 번호
    int currentPlayerIndex; // 선 플레이어 관리용 변수
    public int currentTurn = 0; // 턴 관리

    public bool pushFoodCard = false;
    int plchoice;
    int plchoice2;
    int[] score; // 내 점수가 일정 이상이면 게임 종료
    public FoodCard.CardPoint[] selectedFoodCard;
    public Sprite[] cardSprites; // 카드 스프라이트
    public Image[] cardImages; // 카드 스프라이트 표시될 곳

    private List<FoodCard.CardPoint> playerHand = new List<FoodCard.CardPoint> 
    {
    FoodCard.CardPoint.Bread,
    FoodCard.CardPoint.Soup,
    FoodCard.CardPoint.Fish,
    FoodCard.CardPoint.Steak,
    FoodCard.CardPoint.Turkey,
    FoodCard.CardPoint.Cake
    };
    private void Awake()
    {
        // 싱글턴 패턴 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }    

    private void Start()
    {
        if(PhotonNetwork.IsMasterClient) // 마스터 클라이언트만 실행
        {
            SettingPlayerTurn();
        }
       
    }
    public void SettingPlayerTurn() // 선플레이어 랜덤픽 + 동기화
    {
        Debug.Log("settingplayerTurn 시작");
        currentPlayerIndex = UnityEngine.Random.Range(1, PhotonNetwork.CurrentRoom.PlayerCount+1);
        photonView.RPC("SyncPlayerTurn", RpcTarget.All,currentPlayerIndex);
    }
    
    public void pickedCardsave(FoodCard.CardPoint pickedCard)
    {   
        if (pushFoodCard==false)
        {
            photonView.RPC("pickedCardsaveRPC", RpcTarget.All, pickedCard); // 불안하니 all으로
        }
        Debug.Log("pickedCardsave");
    }

    public void pickedCardsaveRPC(FoodCard.CardPoint pickedCard)
    {
        selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber-1] = pickedCard;
        Debug.Log("pickedCardsaveRPC");
    }

    [PunRPC]
    public void SyncGamestatustxtUpdate(string txt)
    {
        gamestatustxt.text = txt;
    }

    [PunRPC]
    public void SyncPlayerTurn(int mcurrentPlayerIndex)
    {
        Debug.Log("SyncPlayerTurn 실행");
        currentPlayerIndex = mcurrentPlayerIndex;
        if(PhotonNetwork.LocalPlayer.ActorNumber==mcurrentPlayerIndex)
        {
            gamestatustxt.text = "내 차례입니다.";
        }
        else 
        {
            if (currentTurn == 1)
            {

                gamestatustxt.text = $"{mcurrentPlayerIndex}번 플레이어 차례입니다.";
            }
            }
        }


    [PunRPC]
    public void SetCardValue(int value, int playernum)
    {
        cardImages[playernum-1].sprite = cardSprites[GetSpriteIndex(value)];
        Debug.Log("setCardValue");

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
        default: return 6;
    }
    }

    public void ClickSubmit() // main
    {
        Debug.Log("clicked!");
        switch (currentTurn)
        {
        case 0: // 선 플레이어가 첫 카드를 제출해야 하는 턴
            if(currentPlayerIndex == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                pushFoodCard = true;
                photonView.RPC("SetCardValue", RpcTarget.All,(int)selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber], 1);
                photonView.RPC("TurnPlus", RpcTarget.All);
                Debug.Log("current0");
            }
            else 
            {
            gamestatustxt.text = "차례가 아닙니다.";
            } 
            break;
            
        case 1: // 선 플레이어에 맞춰 식사 카드를 제출하는 턴
            photonView.RPC("SetCardValue", RpcTarget.All,(int)selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber], PhotonNetwork.LocalPlayer.ActorNumber);
            // (추가해야할 것) 모든 플레이어가 식사 카드를 내거나 패스를 눌렀다면, 5초가 지났다면
            photonView.RPC("TurnPlus", RpcTarget.All);

            if(currentPlayerIndex == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                gamestatustxt.text = "식사할 카드를 누르세요.";
            }

            break;

        case 2: // 선 플레이어가 식사할 카드를 고르는 턴
            
            
            if(currentPlayerIndex != PhotonNetwork.LocalPlayer.ActorNumber) 
            {
                gamestatustxt.text = "선 플레이어가 식사할 상대를 고르고 있습니다.";
            }
            else if(pickedPlayerIndex != -1 && PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerIndex)
            {
                photonView.RPC("TurnPlus", RpcTarget.All);
                if(PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerIndex || PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerIndex)
                {
                    submitbuttontxt.text = "식사";
                    submitbuttontxt2.text = "강탈";
                    gamestatustxt.text = "협상의 시간입니다.";
                }
            }

            break;

        case 3: // 선 플레이어와 선택된 플레이어가 식사/강탈을 고르는 턴 
            photonView.RPC("SyncChoice", RpcTarget.All, 0); // 오류날까봐 일단 ALL로... 매개변수로 0은 식사, 1은 강탈입니다.
            // 추가할 것 - 클릭 후 식사 강탈 버튼 막기
            break;

        default:  // 위 조건에 해당하지 않는 경우
        // 비워 놓기 
            break;
        }
    }
    
    [PunRPC]
    public void SyncChoice(int choice)
    {
        if(PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerIndex)
        {
            plchoice = choice;
        }
        else if(PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerIndex)
        {
            plchoice2 = choice;
        }
        
        if(plchoice != -1 && plchoice2 != -1)
        {
            photonView.RPC("GetScore", RpcTarget.All, plchoice, plchoice2);
        }
        Debug.Log("SyncChoice");
    }
    
    public void DenyButtonClick()
    {
        if (currentTurn==3 && (PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerIndex || PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerIndex))
        {
            photonView.RPC("SyncChoice", RpcTarget.All, 1);
            // 추가할 것 - 클릭 후 식사 강탈 버튼 막기
        }
    }

    [PunRPC]
    public void GetScore(int choice1, int choice2)
    {
        Debug.Log("GetScore");
        switch ((choice1, choice2))
        {
            case (0, 0):
                score[currentPlayerIndex] = (int)selectedFoodCard[currentPlayerIndex] + (int)selectedFoodCard[pickedPlayerIndex];
                score[pickedPlayerIndex] = (int)selectedFoodCard[currentPlayerIndex] + (int)selectedFoodCard[pickedPlayerIndex];
                break;
            case (1, 0): 
                score[currentPlayerIndex] = Math.Abs(selectedFoodCard[currentPlayerIndex] - selectedFoodCard[pickedPlayerIndex]); break;
            case (0, 1): 
                score[pickedPlayerIndex] = Math.Abs(selectedFoodCard[currentPlayerIndex] - selectedFoodCard[pickedPlayerIndex]); break;
            case (1, 1): 
                break; // 쓰레기통? 추가할 것
            default: ; break;
        }
        gamestatustxt.text = "턴 종료.";
        RoundEnd();
    }
    public void TableCardClick(int playernum) // case 2에서 선 플레이어가 식사할 카드를 고를 때 호출
    {
        if(currentTurn==2 && currentPlayerIndex == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            pickedPlayerIndex = playernum;
            Debug.Log("TableCardClick");
        }
        else if(currentTurn==3 && (currentPlayerIndex == PhotonNetwork.LocalPlayer.ActorNumber || pickedPlayerIndex== PhotonNetwork.LocalPlayer.ActorNumber))
        {
            pickedPlayerIndex = playernum;
            submitbuttontxt.text = "제출";
            submitbuttontxt2.text = "제출포기";
            photonView.RPC("TurnPlus", RpcTarget.All);
            Debug.Log("TableCardClick");
        }
    }

    [PunRPC]
    public void TurnPlus()
    {
        currentTurn++;
    }

   void RoundEnd()
   {
        if(score[PhotonNetwork.LocalPlayer.ActorNumber]>10) // 점수 나중에 수정하기로~
        {
            GameOver();
        }

        else if(PhotonNetwork.IsMasterClient) // 마스터 클라이언트만 실행
        {
            SettingPlayerTurn();
        }
    }
   
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"플레이어 {otherPlayer.NickName} (ID: {otherPlayer.ActorNumber})가 방을 떠났습니다.");
        Debug.Log($"현재 방에 남은 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}");
        GameOver();
    }

   void GameOver()
   {
    resultPannel.SetActive(true);
   }

   void gotolobby() // 게임오버 패널에서 지정
   {
    PhotonNetwork.LeaveRoom();
   }
}
