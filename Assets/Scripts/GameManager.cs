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
    public TextMeshProUGUI[] playerListText;
    int currentPlayerIndex; // 선 플레이어 관리용 변수
    public int currentTurn=0; // 턴 관리
    int myscore; 
    GameObject resultPannel;
    public FoodCard.CardPoint selectedFoodCard;
    public SpriteRenderer[] spriteRenderer; // 스프라이트 렌더러
    public Sprite[] cardSprites; // 카드 스프라이트 배열 (1~6점의 카드 이미지 저장)

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
        photonView.RPC("SyncPlayerTurn", RpcTarget.Others,currentPlayerIndex);
    }
    
    [PunRPC]
    public void SyncPlayerTurn(int mcurrentPlayerIndex)
    {
        Debug.Log("SyncPlayerTurn 시작");
        currentPlayerIndex = mcurrentPlayerIndex;
        if(PhotonNetwork.LocalPlayer.ActorNumber==mcurrentPlayerIndex)
        {
            gamestatustxt.text = "내 차례입니다.";
        }
        else 
        {
            gamestatustxt.text = $"{mcurrentPlayerIndex}번 플레이어 차례입니다.";
        }
    }
    
    [PunRPC]
    public void SetCardValue(int value, int playernum)
    {
    spriteRenderer[playernum-1].sprite = cardSprites[GetSpriteIndex(value)];
    }

    private int GetSpriteIndex(int value)
    {
    switch (value)
    {
        case 1: value=1; return 1;
        case 2: value=2; return 2;
        case 3: value=3; return 3;
        case 5: value=4; return 4;
        case 7: value=5; return 5;
        case 10: value=6; return 6;
        default: return 0;
    }
    }

    public void ClickSubmit() // main
    {
        switch (currentTurn)
        {
        case 1: currentTurn=0; // 선 플레이어가 첫 카드를 제출해야 하는 턴
            if(currentPlayerIndex == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                myscore = (int)selectedFoodCard;
                photonView.RPC("SetCardValue", RpcTarget.All,(int)selectedFoodCard, 1);
                // 카드의 점수를 저장, 테이블에 동기화
            }
            else 
            {
            Debug.Log("차례가 아닙니다.");
            } 
            break;
            
        case 2: currentTurn=1; // 선 플레이어에 맞춰 식사 카드를 제출하는 턴
            
            break;

        case 3: currentTurn=2; // 선 플레이어가 식사할 카드를 고르는 턴
            submit2panel.SetActive(true);
            break;

        case 4: currentTurn=3; // 선 플레이어와 선택된 플레이어가 식사/강탈을 고르는 턴 
            submit2panel.SetActive(false);
            break;

        default:  // 위 조건에 해당하지 않는 경우
            // 비워 놓기
            break;
        }
            
        
    }

    public void table()
    {

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
        
        // 방에 남아 있는 플레이어 수 확인
        Debug.Log($"현재 방에 남은 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}");

        // 방장이 나갔을 경우 새로운 방장 지정
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("새로운 방장이 되었습니다!");
            // 추가적인 방장 설정 로직 (예: 게임 관리)
        }
        GameOver();
    }

   void GameOver()
   {
    resultPannel.SetActive(true);
   }

   void gotolobby()
   {
    PhotonNetwork.LeaveRoom();
   }
}
