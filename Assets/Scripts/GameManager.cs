using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Photon.Pun;
using Photon.Pun.Demo.Cockpit;
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
    public TMP_Text timer;
    public TMP_Text submitbuttontxt2;
    public TMP_Text gameovertxt;
    public TMP_Text[] scoretxt;
    public GameObject resultPannel;
    public TextMeshProUGUI[] playerListText;
    int pickedPlayerIndex = -1; // 선 플레이어가 고른카드의 플레이어 번호
    int currentPlayerIndex; // 선 플레이어 관리용 변수
    public int currentTurn = 0; // 턴 관리
    public int currentTurn2 = 1;
    float t = 5.0f;

    public bool pushFoodCard = false;
    int plchoice = -1;
    int plchoice2 = -1;
    public int[] score = new int[6] { 0, 0, 0, 0, 0, 0 }; // 내 점수가 일정 이상이면 게임 종료
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
            currentPlayerIndex = UnityEngine.Random.Range(1, PhotonNetwork.CurrentRoom.PlayerCount+1);
            photonView.RPC("SyncPlayerTurn", RpcTarget.All,currentPlayerIndex);
        }
        scoretxt = new TMP_Text[PhotonNetwork.CurrentRoom.PlayerCount];
       
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
            if (currentTurn == 0)
            {

                gamestatustxt.text = $"{mcurrentPlayerIndex}번 플레이어 차례입니다.";
            }
            }
        }

    public void ClickSubmit() // main
    {
        switch (currentTurn)
        {
        case 0: // 선 플레이어가 첫 카드를 제출해야 하는 턴
            if(currentPlayerIndex == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                pushFoodCard = true;
                photonView.RPC("SetCardValue", RpcTarget.All,(int)selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber-1], 0);
                photonView.RPC("TurnPlus", RpcTarget.All);
                photonView.RPC("SyncGamestatustxtUpdate", RpcTarget.All, "식사할 카드를 내세요.");
                gamestatustxt.text = "다른 플레이어들이 카드를 냅니다.";
                //StartCoroutine(TimerSetting(t)); //선플레이어가 타이머
                Debug.Log("current0");
            }
            else 
            {
                gamestatustxt.text = "차례가 아닙니다.";
            } 
            break;
            
        case 1: // 선 플레이어에 맞춰 식사 카드를 제출하는 턴
            Debug.Log("current1");
            if(PhotonNetwork.LocalPlayer.ActorNumber != currentPlayerIndex && pushFoodCard==false)
            {
                photonView.RPC("SetCardValue", RpcTarget.All,(int)selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber-1], PhotonNetwork.LocalPlayer.ActorNumber);
                photonView.RPC("TurnPlus2", RpcTarget.All);
                pushFoodCard = true;
                    if (currentTurn2 >= PhotonNetwork.CurrentRoom.PlayerCount) // 모든 플레이어가 제출했다면, 다음 턴으로
                    {
                    photonView.RPC("TurnPlus", RpcTarget.All);
                    }
            }
            break;

        case 2: // 선 플레이어가 식사할 카드를 고르는 턴
            Debug.Log("current2");
            
            if(currentPlayerIndex != PhotonNetwork.LocalPlayer.ActorNumber) // 선플이 아니면
            {
                gamestatustxt.text = "선 플레이어가 식사할 상대를 고르고 있습니다.";
            }
            else if(pickedPlayerIndex != -1 && PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerIndex) // 선플이면
            {
                photonView.RPC("SyncpickedPlayerIndex", RpcTarget.All, pickedPlayerIndex);
                photonView.RPC("TurnPlus", RpcTarget.All);
                photonView.RPC("SyncDinnerTimeTxt", RpcTarget.All);
                
            }

            break;

        case 3: // 선 플레이어와 선택된 플레이어가 식사/강탈을 고르는 턴 
            Debug.Log("current3");
            if (PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerIndex || PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerIndex)
            photonView.RPC("SyncChoice", RpcTarget.MasterClient, 0, PhotonNetwork.LocalPlayer.ActorNumber); //매개변수로 0은 식사, 1은 강탈입니다.
            // 추가할 것 - 클릭 후 식사 강탈 버튼 막기
            break;

        default:  // 위 조건에 해당하지 않는 경우
        // 비워 놓기 
            break;
        }
    }
    public void DenyButtonClick()
    {
        if (currentTurn==3 && (PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerIndex || PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerIndex))
        {
            photonView.RPC("SyncChoice", RpcTarget.MasterClient, 1, PhotonNetwork.LocalPlayer.ActorNumber);
            // 추가할 것 - 클릭 후 식사 강탈 버튼 막기
        }
        else
        {
            if(pushFoodCard==false && currentTurn==1)
            {
            selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber] = FoodCard.CardPoint.deny;
            photonView.RPC("TurnPlus2", RpcTarget.All);
            pushFoodCard = true;
                if (currentTurn2 >= PhotonNetwork.CurrentRoom.PlayerCount) // 모든 플레이어가 제출했다면, 다음 턴으로
                {
                    photonView.RPC("TurnPlus", RpcTarget.All);
                }
            }
        }
    }

    [PunRPC]
    public void SyncChoice(int choice, int actnum) // 마스터 클라이언트만 실행
    {
        if(actnum == currentPlayerIndex)
        {
            plchoice = choice;
        }
        else if(actnum == pickedPlayerIndex)
        {
            plchoice2 = choice;
        }

        Debug.Log($"SyncChoice : {plchoice}, {plchoice2}, pickedPlayerIndex : {pickedPlayerIndex}");
        if(plchoice != -1 && plchoice2 != -1)
        {
            Debug.Log("GetScore");
            switch ((plchoice, plchoice2))
            {
            case (0, 0):
                score[currentPlayerIndex-1] += (int)selectedFoodCard[currentPlayerIndex-1] + (int)selectedFoodCard[pickedPlayerIndex-1];
                score[pickedPlayerIndex-1] += (int)selectedFoodCard[currentPlayerIndex-1] + (int)selectedFoodCard[pickedPlayerIndex-1];
                break;
            case (1, 0): 
                score[currentPlayerIndex-1] += Math.Abs(selectedFoodCard[currentPlayerIndex-1] - selectedFoodCard[pickedPlayerIndex-1]); break;
            case (0, 1): 
                score[pickedPlayerIndex-1] += Math.Abs(selectedFoodCard[currentPlayerIndex-1] - selectedFoodCard[pickedPlayerIndex-1]); break;
            case (1, 1): 
                break; // 쓰레기통? 추가할 것
            default: ; break;
            }
        photonView.RPC("Syncscore", RpcTarget.All, score[0], score[1], score[2], score[3], score[4], score[5]);
        photonView.RPC("SyncGamestatustxtUpdate", RpcTarget.All, "턴 종료");
        RoundEnd();
        }

    }
    

    [PunRPC]
    public void SyncDinnerTimeTxt()
    {
        if(PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerIndex || PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerIndex)
        {
            submitbuttontxt.text = "식사";
            submitbuttontxt2.text = "강탈";
            gamestatustxt.text = "협상의 시간입니다.";
        }
        else
        {
            gamestatustxt.text = "협상을 기다립니다.";
        }
        Debug.Log($"SyncDinnerTimeTxt = {pickedPlayerIndex}");
    }

    IEnumerator TimerSetting(float delay)
        {
            yield return new WaitForSeconds(delay);

            while (t > 0)
            {
                t -= Time.deltaTime;
                timer.text = $"{t}";
                photonView.RPC("SyncTimer", RpcTarget.Others, t); // 다른 플레이어에게 시간 동기화
                yield return null;
            }

            if(currentTurn==1)
            photonView.RPC("TurnPlus", RpcTarget.All); // delay 시간이 끝나면 다음 턴으로(2턴으로) 
        
        }
    [PunRPC]
    public void SyncTimer(float time)
    {
        t = time;
        
    }

        [PunRPC]
    public void Syncscore(int s0, int s1, int s2, int s3, int s4, int s5)
    {
        score[0] = s0;
        score[1] = s1;
        score[2] = s2;
        score[3] = s3;
        score[4] = s4;
        score[5] = s5;
        
    }

    public void TableCardClick(int playernum) // case 2에서 선 플레이어가 식사할 카드를 고를 때 호출
    {

        if(currentTurn==2 && currentPlayerIndex == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            pickedPlayerIndex = playernum;
        }
        else if(currentTurn==3 && (currentPlayerIndex == PhotonNetwork.LocalPlayer.ActorNumber || pickedPlayerIndex == PhotonNetwork.LocalPlayer.ActorNumber))
        {
            pickedPlayerIndex = playernum;
            submitbuttontxt.text = "제출";
            submitbuttontxt2.text = "제출포기";
            photonView.RPC("TurnPlus", RpcTarget.All);
            Debug.Log("TableCardClick");
        }
    }
    [PunRPC]
    public void SyncpickedPlayerIndex(int playernum)
    {
        pickedPlayerIndex = playernum;
        Debug.Log($"syncpickedPlayerIndex : {pickedPlayerIndex}");
    }

    [PunRPC]
    public void TurnPlus()
    {
        ++currentTurn;
    }
    [PunRPC]
    public void TurnPlus2()
    {
        ++currentTurn2;
    }

   public void RoundEnd()
   {
        Debug.Log($"RoundEnd : {score[PhotonNetwork.LocalPlayer.ActorNumber]}");

        if(score[PhotonNetwork.LocalPlayer.ActorNumber]>10) // 점수 나중에 수정하기로~ 실행 겹치지 않게
        {
            photonView.RPC("GameOver", RpcTarget.All);
        }

        else
        {
            photonView.RPC("ResetRound", RpcTarget.All);
        }
    }
    
[PunRPC]
    public void ResetRound()
    {
        Debug.Log("ResetRound");
        pushFoodCard = false;
        pickedPlayerIndex = -1;
        currentTurn = 0;
        currentTurn2 = 1;
        plchoice = -1;
        plchoice2 = -1;
        PlayerListUI playerListUI = FindObjectOfType<PlayerListUI>();
        playerListUI.UpdatePlayerList();

        for(int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount+1; i++)
        {
            SetCardValue(6, i);
        }

        if(PhotonNetwork.IsMasterClient)
        {
            ++currentPlayerIndex;
            if (currentPlayerIndex > PhotonNetwork.CurrentRoom.PlayerCount) currentPlayerIndex = 1;
            photonView.RPC("SyncPlayerTurn", RpcTarget.All,currentPlayerIndex);
        }
        Debug.Log($"ResetRoundEnd {PhotonNetwork.CurrentRoom.GetPlayer(1).NickName} : {score[0]}\n{PhotonNetwork.CurrentRoom.GetPlayer(2).NickName} : {score[1]}\n{PhotonNetwork.CurrentRoom.GetPlayer(3).NickName} : {score[2]}\n{PhotonNetwork.CurrentRoom.GetPlayer(4).NickName} : {score[3]}");
  
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"플레이어 {otherPlayer.NickName} (ID: {otherPlayer.ActorNumber})가 방을 떠났습니다.");
        Debug.Log($"현재 방에 남은 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}");
        GameOver();
    }
    
[PunRPC]
   public void GameOver()
   {
    resultPannel.SetActive(true);
    gameovertxt.text = $"{PhotonNetwork.CurrentRoom.GetPlayer(1).NickName} : {score[0]}\n{PhotonNetwork.CurrentRoom.GetPlayer(2).NickName} : {score[1]}\n{PhotonNetwork.CurrentRoom.GetPlayer(3).NickName} : {score[2]}\n{PhotonNetwork.CurrentRoom.GetPlayer(4).NickName} : {score[3]}\n";
   }

   public void Gotolobby() // 게임오버 패널에서 지정
   {
    PhotonNetwork.LeaveRoom();
   }
       public void pickedCardsave(FoodCard.CardPoint pickedCard)
    {   
        if (pushFoodCard==false)
        {
            photonView.RPC("PickedCardsaveRPC", RpcTarget.All, pickedCard, PhotonNetwork.LocalPlayer.ActorNumber); // 불안하니 all으로
        }
        Debug.Log($"pickedCardsave = {selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber-1]}" );
    }

    [PunRPC]    
    public void PickedCardsaveRPC(FoodCard.CardPoint pickedCard, int who)
    {
        selectedFoodCard[who-1] = pickedCard;
        Debug.Log($"PickedCardsaveRPC, {selectedFoodCard[0]}, {selectedFoodCard[1]}, {selectedFoodCard[2]}, {selectedFoodCard[3]}");
    }
    
    [PunRPC]
    public void SetCardValue(int value, int playernum)
    {
        
        cardImages[playernum].sprite = cardSprites[GetSpriteIndex(value)];
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

    [PunRPC]
    public void SyncGamestatustxtUpdate(string txt)
    {
        gamestatustxt.text = txt;
    }
}
