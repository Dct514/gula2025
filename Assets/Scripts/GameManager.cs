using System.Collections;
using System.Collections.Generic;
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
    public GameObject submit2panel;
    public GameObject resultPannel;
    public TextMeshProUGUI[] playerListText;
    int pickedplayerIndex; // 선 플레이어가 고른카드의 플레이어 번호
    int currentPlayerIndex; // 선 플레이어 관리용 변수
    public int currentTurn=0; // 턴 관리
    int myscore; // 내 점수가 일정 이상이면 게임 종료
    public FoodCard.CardPoint selectedFoodCard;
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
        currentPlayerIndex = Random.Range(1, PhotonNetwork.CurrentRoom.PlayerCount+1);
        photonView.RPC("SyncPlayerTurn", RpcTarget.All,currentPlayerIndex);
    }
    
    [PunRPC]
    private void SyncGamestatustxtUpdate(string txt)
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
            if(currentTurn==1){gamestatustxt.text = "식사하실 분은 카드를 내세요.";}

            gamestatustxt.text = $"{mcurrentPlayerIndex}번 플레이어 차례입니다.";
        }
    }

    [PunRPC]
    public void TurnPlus()
    {
        currentTurn++;
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
                myscore = (int)selectedFoodCard;
                photonView.RPC("SetCardValue", RpcTarget.All,(int)selectedFoodCard, 1);
                photonView.RPC("TurnPlus", RpcTarget.All);
                Debug.Log("current0");
            }
            else 
            {
            gamestatustxt.text = "차례가 아닙니다.";
            } 
            break;
            
        case 1: currentTurn=1; // 선 플레이어에 맞춰 식사 카드를 제출하는 턴
            photonView.RPC("SetCardValue", RpcTarget.All,(int)selectedFoodCard, PhotonNetwork.LocalPlayer.ActorNumber);
            // (추가해야할 것) 모든 플레이어가 식사 카드를 내거나 패스를 눌렀다면, 5초가 지났다면
            photonView.RPC("TurnPlus", RpcTarget.All);

            if(currentPlayerIndex == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                gamestatustxt.text = "식사할 카드를 누르세요.";
            }

            break;

        case 2: currentTurn=2; // 선 플레이어가 식사할 카드를 고르는 턴
            if(currentPlayerIndex != PhotonNetwork.LocalPlayer.ActorNumber) 
            {
                gamestatustxt.text = "선 플레이어가 식사할 상대를 고르고 있습니다.";
            }

            break;

        case 3: currentTurn=3; // 선 플레이어와 선택된 플레이어가 식사/강탈을 고르는 턴 
            
            submit2panel.SetActive(false);
            break;

        default:  // 위 조건에 해당하지 않는 경우
        // 비워 놓기 
            break;
        }
    }
    
    public void TableCardClick(int playernum) // case 2에서 선 플레이어가 식사할 카드를 고를 때 호출
    {
        if(currentTurn==2 && currentPlayerIndex == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            pickedplayerIndex = playernum;
            submit2panel.SetActive(true);
            photonView.RPC("TurnPlus", RpcTarget.All);
        }
        else return;
    }

   void RoundEnd()
   {
    if(myscore >= 50){
        GameOver();
        
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
