using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Photon.Pun;
using Photon.Pun.Demo.Cockpit;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    public TMP_Text[] goldtxt;
    public TMP_Text[] silvertxt;

    public GameObject resultPannel;
    public TextMeshProUGUI[] playerListText;
    int pickedPlayerIndex = -1; // 선 플레이어가 고른카드의 플레이어 번호
    int currentPlayerIndex; // 선 플레이어 관리용 변수
    public int currentTurn = 0; // 턴 관리
    public int currentTurn2 = 1;
    float t = 5.0f;
    int totalPlayers = 4;
    public bool pushFoodCard = false;
    int plchoice = -1;
    int plchoice2 = -1;
    public int[] score = new int[6] { 0, 0, 0, 0, 0, 0 }; // 내 점수가 일정 이상이면 게임 종료
    public FoodCard.CardPoint[] selectedFoodCard;
    public Sprite[] cardSprites; // 카드 스프라이트
    public Image[] cardImages; // 카드 스프라이트 표시될 곳
    public Image[] myImages; // 내 카드 스프라이트
    public int myGrade = 1; // 현재 카드 값
    Dictionary<(int, int), int> turnCount = new Dictionary<(int, int), int>();

    public List<FoodCard.CardPoint> playerHand = new List<FoodCard.CardPoint>
    {
        FoodCard.CardPoint.Bread,
        FoodCard.CardPoint.Soup,
        FoodCard.CardPoint.Fish,
        FoodCard.CardPoint.Steak,
        FoodCard.CardPoint.Turkey,
        FoodCard.CardPoint.Cake
    };

    public List<HandCard> handCards = new List<HandCard>(); // 각 플레이어의 카드들
    public List<int> others = new List<int>(); // 나를 제외한 플레이어 번호들 - 위치

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
        if (PhotonNetwork.IsMasterClient)
        {
            currentPlayerIndex = UnityEngine.Random.Range(1, PhotonNetwork.CurrentRoom.PlayerCount + 1);
            photonView.RPC("SyncPlayerTurn", RpcTarget.All, currentPlayerIndex);
        }

        ShowPlayerPositions();
    }


    [PunRPC]
    public void SyncPlayerTurn(int mcurrentPlayerIndex)
    {
        Debug.Log("SyncPlayerTurn 실행");
        currentPlayerIndex = mcurrentPlayerIndex;
        if (PhotonNetwork.LocalPlayer.ActorNumber == mcurrentPlayerIndex)
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
                if (currentPlayerIndex == PhotonNetwork.LocalPlayer.ActorNumber && pushFoodCard == false && playerHand.Contains(selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber - 1]) )
                {
                    pushFoodCard = true;
                    photonView.RPC("SetCardValue", RpcTarget.All, (int)selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber - 1], 0);
                    photonView.RPC("TurnPlus", RpcTarget.All);
                    photonView.RPC("SyncGamestatustxtUpdate", RpcTarget.All, "식사할 카드를 내세요.");
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
                if (PhotonNetwork.LocalPlayer.ActorNumber != currentPlayerIndex && pushFoodCard == false)
                {
                    if(selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber - 1] == selectedFoodCard[currentPlayerIndex - 1])
                    {
                        gamestatustxt.text = "같은 카드를 제출할 수 없습니다.";
                        return;
                    }
                    else
                    {
                    photonView.RPC("SetCardValue", RpcTarget.All, (int)selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber - 1], PhotonNetwork.LocalPlayer.ActorNumber);
                    photonView.RPC("TurnPlus2", RpcTarget.All);
                    pushFoodCard = true;
                        if (currentTurn2 >= PhotonNetwork.CurrentRoom.PlayerCount) // 모든 플레이어가 제출했다면, 다음 턴으로
                        {
                            photonView.RPC("TurnPlus", RpcTarget.All);
                        }                
                    }
                }
                break;

            case 2: // 선 플레이어가 식사할 카드를 고르는 턴
                Debug.Log("current2");

                if (currentPlayerIndex != PhotonNetwork.LocalPlayer.ActorNumber) // 선플이 아니면
                {
                    gamestatustxt.text = "차례가 아닙니다. 선 플레이어가 식사할 카드를 고르고 있습니다.";
                }
                else if (pickedPlayerIndex != -1 && PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerIndex) // 선플이면
                {
                    if(RegisterTurn(currentPlayerIndex, pickedPlayerIndex))
                    {
                        photonView.RPC("SyncpickedPlayerIndex", RpcTarget.All, pickedPlayerIndex);
                        photonView.RPC("TurnPlus", RpcTarget.All);
                        photonView.RPC("SyncDinnerTimeTxt", RpcTarget.All);
                    //todo : 카드 돌려주기...
                    }
                    else
                    {
                        gamestatustxt.text = "같은 플레이어와는 두 번까지만 턴을 진행할 수 있습니다.";
                    }
                }

                break;

            case 3: // 선 플레이어와 선택된 플레이어가 식사/강탈을 고르는 턴 
                Debug.Log("current3");
                if (PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerIndex || PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerIndex)
                    photonView.RPC("SyncChoice", RpcTarget.MasterClient, 0, PhotonNetwork.LocalPlayer.ActorNumber); //매개변수로 0은 식사, 1은 강탈입니다.
                    currentTurn++;                                                       // 추가할 것 - 클릭 후 식사 강탈 버튼 막기
                break;

            default:  // 위 조건에 해당하지 않는 경우
                      // 비워 놓기 
                break;
        }
    }
    public void DenyButtonClick()
    {
        if (currentTurn == 3 && (PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerIndex || PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerIndex))
        {
            photonView.RPC("SyncChoice", RpcTarget.MasterClient, 1, PhotonNetwork.LocalPlayer.ActorNumber);
            currentTurn++;  // 추가할 것 - 클릭 후 식사 강탈 버튼 막기
        }
        else
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber != currentPlayerIndex && pushFoodCard == false && currentTurn==1)
            {
                selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber-1] = FoodCard.CardPoint.deny;
                photonView.RPC("SetCardValue", RpcTarget.All, (int)selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber - 1], PhotonNetwork.LocalPlayer.ActorNumber);
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
        if (actnum == currentPlayerIndex)
        {
            plchoice = choice;
        }
        else if (actnum == pickedPlayerIndex)
        {
            plchoice2 = choice;
        }

        Debug.Log($"SyncChoice : {plchoice}, {plchoice2}, pickedPlayerIndex : {pickedPlayerIndex}");
        if (plchoice != -1 && plchoice2 != -1)
        {
            Debug.Log("GetScore");
            switch ((plchoice, plchoice2))
            {
                case (0, 0):
                    int scoreToAdd = Math.Abs((int)selectedFoodCard[currentPlayerIndex - 1] + (int)selectedFoodCard[pickedPlayerIndex - 1]);
                    score[currentPlayerIndex - 1] += scoreToAdd;
                    score[pickedPlayerIndex - 1] += scoreToAdd;


                    var a = goldtxt[others.IndexOf(currentPlayerIndex)];
                    var b = int.Parse(a.text);
                    b++;
                    a.text = b.ToString();

                    a = goldtxt[others.IndexOf(pickedPlayerIndex)];
                    b = int.Parse(a.text);
                    b++;
                    a.text = b.ToString();

                    break;
                case (1, 0):
                    score[currentPlayerIndex - 1] += Math.Abs(selectedFoodCard[currentPlayerIndex - 1] - selectedFoodCard[pickedPlayerIndex - 1]); 
                    a = silvertxt[others.IndexOf(currentPlayerIndex)];
                    b = int.Parse(a.text);
                    b++;
                    a.text = b.ToString(); break;

                case (0, 1):
                    a = silvertxt[others.IndexOf(pickedPlayerIndex)];
                    b = int.Parse(a.text);
                    b++;
                    a.text = b.ToString(); break;
                case (1, 1):
                    Debug.Log("쓰레기통");
                    

                    break; // 쓰레기통? 추가할 것
                default:; break;
            }
            photonView.RPC("Syncscore", RpcTarget.All, score[0], score[1], score[2], score[3], score[4], score[5]);
            photonView.RPC("SyncGamestatustxtUpdate", RpcTarget.All, "턴 종료");
            photonView.RPC("RoundEnd", RpcTarget.All);
        }

    }


    [PunRPC]
    public void SyncDinnerTimeTxt()
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerIndex || PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerIndex)
        {
            submitbuttontxt.text = "식사";
            submitbuttontxt2.text = "강탈";
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

        if (currentTurn == 1)
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

    [PunRPC]
    public void Syncscore2(int actnum, int myscore)
    {
        score[actnum - 1] = myscore;
    }

    public void TableCardClick(int playernum) // case 2에서 선 플레이어가 식사할 카드를 고를 때 호출
    {

        if (currentTurn == 2 && currentPlayerIndex == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            pickedPlayerIndex = playernum;
        }
        else 
        {
            gamestatustxt.text = $"{playernum}번 플레이어의 음식입니다.";
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
        if (currentTurn == 1)
        {
            gamestatustxt.text = "선 플레이어와 식사할 분은 카드를 제출하세요.";
        }
        else if (currentTurn == 2) 
        {
            gamestatustxt.text = "선 플레이어가 같이 식사할 플레이어를 고릅니다.";
        }
        else if (currentTurn == 3)
        {
            gamestatustxt.text = $"선 플레이어와 {pickedPlayerIndex}번 플레이어가 협상합니다.";
        }
    }
    [PunRPC]
    public void TurnPlus2()
    {
        ++currentTurn2;
    }

    [PunRPC]
    public void RoundEnd()
    {
        Debug.Log($"RoundEnd : 내 점수는 {score[PhotonNetwork.LocalPlayer.ActorNumber-1]}");

            if (score[PhotonNetwork.LocalPlayer.ActorNumber-1] >= 18) // 점수 나중에 수정하기로~ 실행 겹치지 않게
            {
                score[PhotonNetwork.LocalPlayer.ActorNumber-1] = 0;
                myGrade++;
                photonView.RPC("Syncscore2", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, score[PhotonNetwork.LocalPlayer.ActorNumber - 1]);
            }

            if (score[PhotonNetwork.LocalPlayer.ActorNumber-1] >= 24 && myGrade == 2)
            {
                score[PhotonNetwork.LocalPlayer.ActorNumber-1] = 0;
                myGrade++;
                photonView.RPC("Syncscore2", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, score[PhotonNetwork.LocalPlayer.ActorNumber - 1]);
            }

            if (score[PhotonNetwork.LocalPlayer.ActorNumber-1] >= 45 && myGrade == 3)
            {
                myGrade++;
                photonView.RPC("Syncscore2", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, score[PhotonNetwork.LocalPlayer.ActorNumber - 1]);
                photonView.RPC("GameOver", RpcTarget.All);
            }
        

        ResetRound();

    }

    [PunRPC]
    public void ResetRound()
    {
        Debug.Log("ResetRound");

        PlayerListUI playerListUI = FindObjectOfType<PlayerListUI>();
        playerListUI.UpdatePlayerList();
        CountMyFoodCard(selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber - 1]);
        CountOtherFoodCard();

        for (int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount + 1; i++)
        {
            SetCardValue(0, i);
        }

        for (int i = 0; i < others.Count; i++)
        {
            scoretxt[i].text = $"{score[others[i]-1]}"; // 점수 배열에 플레이어 번호 저장
           
            Debug.Log($"축하합니다");
        }

        UpdatePlayerTurn(currentPlayerIndex, pickedPlayerIndex); // 턴 카운트 업데이트
        pickedPlayerIndex = -1;
        currentTurn = 0;
        currentTurn2 = 1;
        plchoice = -1;
        plchoice2 = -1;
        pushFoodCard = false;
        submitbuttontxt.text = "제출";
        submitbuttontxt2.text = "제출 포기";


        if (playerHand.Count == 0){
            playerHand.Add(FoodCard.CardPoint.Bread);
            playerHand.Add(FoodCard.CardPoint.Soup);
            playerHand.Add(FoodCard.CardPoint.Fish);
            playerHand.Add(FoodCard.CardPoint.Steak);
            playerHand.Add(FoodCard.CardPoint.Turkey);
            playerHand.Add(FoodCard.CardPoint.Cake);
        }

        // 만약 상대와 내가 카드가 동일한 카드만 남아서 더 제출할 수 없다면,

        // 라운드 종료 조건 - 1. 모든 상대와 두 번씩 식사를 한 경우 2. 식사를 한 번 또는 0번 한 상대 중에 상대의 카드와 내 카드가 같은 것만 남은 경우 
         

        if (PhotonNetwork.IsMasterClient)
        {
            ++currentPlayerIndex;
            if (currentPlayerIndex > PhotonNetwork.CurrentRoom.PlayerCount) currentPlayerIndex = 1;
            photonView.RPC("SyncPlayerTurn", RpcTarget.All, currentPlayerIndex);
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
        SceneManager.LoadScene("Lobby");
        PhotonNetwork.LeaveRoom();
    }

    public void pickedCardsave(FoodCard.CardPoint pickedCard)
    {
        if (pushFoodCard == false)

        {
            photonView.RPC("PickedCardsaveRPC", RpcTarget.All, pickedCard, PhotonNetwork.LocalPlayer.ActorNumber); // 불안하니 all으로
        }
        Debug.Log($"pickedCardsave = {selectedFoodCard[PhotonNetwork.LocalPlayer.ActorNumber - 1]}");
    }

    [PunRPC]
    public void PickedCardsaveRPC(FoodCard.CardPoint pickedCard, int who)
    {
        selectedFoodCard[who - 1] = pickedCard;
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
            case 0 : return 6;
            default: return 6;
        }
    }

    [PunRPC]
    public void SyncGamestatustxtUpdate(string txt)
    {
        gamestatustxt.text = txt;
    }
    public void CountMyFoodCard(FoodCard.CardPoint cardPoint) // selectedFoodCard[actnum-1] 가져옴 - 내 사용한 음식카드 표시(흑백)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerIndex || PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerIndex)
        {
            // 카드가 선택되면 해당 카드를 playerHand에서 제거
            if (cardPoint != FoodCard.CardPoint.deny && playerHand.Contains(cardPoint))
            {
                playerHand.Remove(cardPoint);
            }
            // myImages[GetSpriteIndex((int)cardPoint)].color = new Color(1f, 1f, 1f, 1f);
        }

        foreach (Image card in myImages)
        {
            FoodCard.CardPoint cardType = card.GetComponent<FoodCard>().cardPoint;

            // playerHand에 없는 카드라면 비활성화 (또는 삭제)
            if (!playerHand.Contains(cardType))
            {
                card.gameObject.SetActive(false); // UI에서 숨김
            }
            else
            {
                card.gameObject.SetActive(true); // UI에서 표시
            }
        }
        Debug.Log($"CountMyFoodCard : {cardPoint}");
    }
    public void CountOtherFoodCard() // selectedFoodCard[actnum-1] 가져옴 - 다른 플레이어의 음식카드 표시(흑백)
    {
        if(PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerIndex)
        {
            int starterIndex = others.IndexOf(pickedPlayerIndex);
            handCards[starterIndex].cards[GetSpriteIndex((int)selectedFoodCard[pickedPlayerIndex - 1])].gameObject.SetActive(false);
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerIndex)
        {
            int starterIndex = others.IndexOf(currentPlayerIndex);
            handCards[starterIndex].cards[GetSpriteIndex((int)selectedFoodCard[currentPlayerIndex - 1])].gameObject.SetActive(false);
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber != currentPlayerIndex && PhotonNetwork.LocalPlayer.ActorNumber != pickedPlayerIndex)
        {
            int starterIndex = others.IndexOf(currentPlayerIndex);
            int starterIndex2 = others.IndexOf(pickedPlayerIndex);
            handCards[starterIndex].cards[GetSpriteIndex((int)selectedFoodCard[currentPlayerIndex - 1])].gameObject.SetActive(false);
            handCards[starterIndex2].cards[GetSpriteIndex((int)selectedFoodCard[pickedPlayerIndex - 1])].gameObject.SetActive(false);
        }
    }

    public void DeselectAllCards()
    {
        CardOutlineController[] allCards = FindObjectsOfType<CardOutlineController>();
        foreach (var card in allCards)
        {
            card.DeselectCard();
        }
    }

    public void ShowPlayerPositions()
    {
        // 1. 전체 플레이어 수
        totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

        // 2. 나를 제외하고 번호순 정렬
        
        for (int i = 1; i <= totalPlayers; i++)
        {
            if (i != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                others.Add(i);
            }
            /*
            2 3 4
            1 3 4
            1 2 4
            1 2 3
            */
        }
    }

        bool RegisterTurn(int playerA, int playerB) // 같은 플레이어와는 2번까지만 턴을 진행할 수 있음
    {
        // 항상 작은 번호 먼저로 정렬
        var key = (Math.Min(playerA, playerB), Math.Max(playerA, playerB));

        // 현재 횟수 확인
        if (!turnCount.ContainsKey(key))
        {

            Debug.Log($"처음으로 {key} 간에 턴이 진행되었습니다.");
            return true; // 첫 번째 턴 진행
        }
        else if (turnCount[key] < 2)
        {

            Debug.Log($"두 번째 턴 진행됨. 총 횟수: {turnCount[key]}");
            return true; // 첫 번째 턴 진행
        }
        else
        {
            Debug.Log($"더 이상 {key} 간에 턴을 진행할 수 없습니다.");
            return false; // 더 이상 진행할 수 없음
        }
    }


        void UpdatePlayerTurn(int playerA, int playerB)
        {
        // 항상 작은 번호 먼저로 정렬
        var key = (Math.Min(playerA, playerB), Math.Max(playerA, playerB));

        // 현재 횟수 확인
        if (!turnCount.ContainsKey(key))
        {
            turnCount[key] = 1;
            Debug.Log($"처음으로 {key} 간에 턴이 진행되었습니다.");

        }
        else if (turnCount[key] < 2)
        {
            turnCount[key]++;
            Debug.Log($"두 번째 턴 진행됨. 총 횟수: {turnCount[key]}");
        }
        else
        {

        }
    }

    void UpdateGold()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene("Lobby");
        }
    }


}