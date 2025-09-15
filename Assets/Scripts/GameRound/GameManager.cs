using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;
using System;
using UnityEngine.SocialPlatforms.Impl;
using System.Linq;
using Unity.VisualScripting;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun.Demo.Cockpit;



public class GameManager : MonoBehaviourPunCallbacks
{

    public static GameManager Instance;
    public TMP_Text gamestatustxt;
    public TMP_Text gameovertxt;
    public TMP_Text timerText;
    public GameObject resultPannel;
    public TMP_Text[] scoreTexts; // 점수 표시될 곳
    public Sprite[] cardSprites; // 카드 스프라이트
    public Image[] cardImages; // 카드 스프라이트 표시될 곳 (테이블.)
    public TMP_Text[] goldTexts; // 골드 표시될 곳
    public TMP_Text[] silverTexts; // 실버 표시될 곳
    public TMP_Text myScoreText;
    public TMP_Text myGoldText;
    public TMP_Text mySilverText;
    public TMP_Text myNickNameText;

    public TMP_Text[] resultNameTexts;
    public TMP_Text[] resultScoreTexts;




    ExitGames.Client.Photon.Hashtable player = new ExitGames.Client.Photon.Hashtable();
    public ExitGames.Client.Photon.Hashtable Turn = new ExitGames.Client.Photon.Hashtable();
    public List<int> Trash = new List<int>();
    public List<int> others = new List<int>();
    public List<(int, int)> currentTurnProcess = new List<(int, int)>();
    public int choice = -1;
    public int choice2 = -1;
    public int checkDeny = 0; // 선플 전용
    public bool canFreeChoice = false; // 자유롭게 카드 고를 수 있는지 여부
    public int PlayerCount = 4;
    public int[] gold = new int[6] { 0, 0, 0, 0, 0, 0 };
    public int[] silver = new int[6] { 0, 0, 0, 0, 0, 0 };
    public int[] score = new int[6] { 0, 0, 0, 0, 0, 0 };
    public int[] grade = new int[6] { 0, 0, 0, 0, 0, 0 }; // 0: 새내기, 1: 용, 2: 대용, 3: 신룡
    public Image[] myImages;
    public List<HandCard> handCards = new List<HandCard>(); // 각 플레이어의 카드들 (이미지)
    public List<Grade> gradeImages = new List<Grade>();
    public Sprite backSprite;
    public Sprite[] gradeSprite;
    public Sprite[] WelskiSprite;
    public Image[] PlayerProfileWelski;
    public Image[] myGradeImage;
    public Sprite[] MiniFoodImages;
    public Image[] ohterMiniFoodImages;

    public SoundManager sm;
    public List<FoodCard.CardPoint> otherFoodCards = new List<FoodCard.CardPoint>();
    public PlayerData playerData = new PlayerData();
    public List<PlayerData> playerDatas = new List<PlayerData>(); // 플레이어 덱 비교를 위해서 여기에 넣겠습니다
    private Coroutine turnTimerCoroutine;
    private int foodSubmittedCount;

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

        PlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log($"현재 방 인원수: {PlayerCount}");
        ShowPlayerPositions();
        myNickNameText.text = PhotonNetwork.LocalPlayer.NickName;
        player["grade"] = 0;
        player["score"] = 0;
        PhotonNetwork.LocalPlayer.SetCustomProperties(player);

        if (PhotonNetwork.IsMasterClient)
        {
            Turn["currentPlayerIndex"] = UnityEngine.Random.Range(0, PhotonNetwork.CurrentRoom.PlayerCount - 1);
        }

        playerData.playerNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        
    }

    [PunRPC]
    void Start()
    {
        StopTurnTimer();

        if (PhotonNetwork.IsMasterClient)
        {
            SetStartValue(); // 차례를 정합니다.
        }

        SetDefaultValue(); // 기본값을 초기화합니다.
        photonView.RPC("SynTurn", RpcTarget.MasterClient, 0);
        StartCoroutine(WaitForAllPlayerDatas2());

        Debug.Log($"[Start] 현재 턴 : {Turn["currentTurn"]}, 현재 플레이어 : {Turn["currentPlayerIndex"]}");
    }


    private IEnumerator WaitForAllPlayerDatas2()
    {
        // playerDatas가 모두 모일 때까지 대기
        while (playerDatas.Count < PlayerCount)
        {
            yield return new WaitForSeconds(0.1f); // 한 프레임 대기
        }

        Debug.Log("모든 플레이어 데이터가 수신되었습니다. (playerDatas.Count: " + playerDatas.Count + "/" + PlayerCount + ") (2)");
        CheckCard();

    }

    private IEnumerator WaitFor1Sec()
    {
        yield return new WaitForSeconds(1f);
        photonView.RPC("Start", RpcTarget.All);
    }

    public void CheckCard()
    {
        if ((int)Turn["currentPlayerIndex"] != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Debug.Log("현재 차례는 " + (int)Turn["currentPlayerIndex"] + "번 플레이어입니다.");
            return;
        }

        if (CheckOtherPlayerHand() == true) // 모든 플레이어가 카드 소모 완료한 경우 
        {
            photonView.RPC("GetNewMycard", RpcTarget.All); // todo : trash가 전부 전달되지 않았는데 trash에서 뽑기할 수 있을 듯
            photonView.RPC("TrashGotcha", RpcTarget.MasterClient);
            photonView.RPC("RoundResetCheck", RpcTarget.All); // 등급 리셋, 스코어 리셋
        }
        else if (playerData.playerHand.Count == 0)
        {
            Debug.Log("카드를 다 쓰셨군요");
            gamestatustxt.text = "카드를 다 소모해서 차례가 자동으로 넘어갑니다.";
            photonView.RPC("Start", RpcTarget.All);
        }
        else if (IsFinishMyTurn() == true) // 2번씩 식사를 마쳤거나, 더 이상 식사를 할 수 없는 상황 -> 턴 넘어가기
        {
            playerData.playerHand.Clear();
            Debug.Log("모두와 2번씩 식사를 했네요 아니면 카드가 같은것만 남아있던가");
            gamestatustxt.text = "식사할 플레이어가 없어 차례가 자동으로 넘어갑니다.";

            photonView.RPC("Start", RpcTarget.All);
        }
        else
        {
            if (Turn["currentPlayerIndex"] == null)
            {
                Debug.LogWarning("currentPlayerIndex가 null입니다.");
            }
            else
            {
                Debug.Log("카드가 남아있습니다. 식사를 진행합니다. 남은 카드 : " + playerData.playerHand.Count);
            }
        }

        Debug.Log($"[CheckCard] 현재 턴 : {Turn["currentTurn"]}, 현재 플레이어 : {Turn["currentPlayerIndex"]}");
    }

    [PunRPC]
    public void GameTimer()
    {
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
        }

        turnTimerCoroutine = StartCoroutine(TurnTimerCoroutine());
    }
    private IEnumerator TurnTimerCoroutine()
    {
        float timer = 8f;
        timerText.text = $"{timer:F0}";

        while (timer > 0)
        {
            yield return new WaitForSeconds(1f);
            timer -= 1f;
            timerText.text = $"{timer:F0}";
        }
        // 8초가 지나면 턴을 자동으로 넘김
        ForceEndTurn();
    }

    public void StopTurnTimer()
    {
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }
    }
    public void ForceEndTurn()
    {
        switch (Turn["currentTurn"])
        {
            case 0:
                if ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber && playerData.Submited == false)
                {
                    if (playerData.playerHand.Count > 0)
                    {
                        Debug.Log("자동 제출 실행 (Turn 0)");
                        playerData.selectedFoodCard = (int)playerData.playerHand[0];
                        Turn["selectedFoodCard"] = playerData.selectedFoodCard;
                        MainTurnStart();
                    }
                    else Debug.LogWarning("카드가 없어서 자동 제출을 할 수 없습니다.");
                }
                break;
            case 1:
                if (playerData.Submited == false &&
                (int)Turn["currentPlayerIndex"] != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    Debug.Log("자동 제출 실행 (Turn 1)");
                    Denybutton();
                }
                break;
            case 2:
                Debug.Log("자동 제출 실행 (Turn 2)");
                gamestatustxt.text = "조금만 기다려 주세요.";
                break;
            case 3:
                if ((int)Turn["currentPlayerIndex"] == playerData.playerNumber ||
                    (int)Turn["pickedPlayerIndex"] == playerData.playerNumber)
                {
                    Debug.Log("자동 제출 실행 (Turn 3)");
                    photonView.RPC("SubmitChoiceToMaster", RpcTarget.MasterClient, playerData.playerNumber, 0);
                }
                break;
            default:
                Debug.Log("Error: Invalid turn number.");
                break;
        }
    }


    [PunRPC]
    public void SynTurn(int turn)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Turn["currentTurn"] = turn;
            PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
            photonView.RPC("SendCardInfo", RpcTarget.All);
        }
    }

    private IEnumerator WaitForAllPlayerDatas()
    {
        // playerDatas가 모두 모일 때까지 대기
        while (playerDatas.Count < PlayerCount)
        {
            yield return new WaitForSeconds(0.1f); // 한 프레임 대기
        }

        Debug.Log("모든 플레이어 데이터가 수신되었습니다. (playerDatas.Count: " + playerDatas.Count + "/" + PlayerCount + ")");
        photonView.RPC("GameTimer", RpcTarget.All);

    }

    public void MainTurnStart()
    {
        switch (Turn["currentTurn"])
        {
            case 0:
                if ((int)Turn["currentPlayerIndex"] == playerData.playerNumber && playerData.selectedFoodCard != 0 && playerData.Submited == false)
                {
                    playerData.Submited = true;
                    sm.SoundPlay(3);
                    photonView.RPC("SetCardValue", RpcTarget.All, playerData.selectedFoodCard, 0);
                    photonView.RPC("SetGameStatusText", RpcTarget.All, "선 플레이어와 식사할 사람은 카드를 제출하세요.");
                    photonView.RPC("SynTurn", RpcTarget.MasterClient, 1);
                }
                else if ((int)Turn["currentPlayerIndex"] != playerData.playerNumber)
                {
                    gamestatustxt.text = "차례가 아닙니다.";
                }
                else if (playerData.selectedFoodCard == 0)
                {
                    gamestatustxt.text = "카드가 선택되지 않았습니다.";
                }
                break;

            case 1:
                if (!BlockPlayerTurn((int)Turn["currentPlayerIndex"], playerData.playerNumber) &&
                playerData.Submited == false &&
                playerData.selectedFoodCard != 0 &&
                (int)Turn["currentPlayerIndex"] != playerData.playerNumber &&
                (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] != playerData.selectedFoodCard)
                {
                    playerData.Submited = true;
                    sm.SoundPlay(3);
                    photonView.RPC("TryIncreaseFoodSubmitedRPC", RpcTarget.MasterClient, 0);
                    photonView.RPC("SetCardValue", RpcTarget.All, playerData.selectedFoodCard, PhotonNetwork.LocalPlayer.ActorNumber);
                }
                else if (BlockPlayerTurn((int)Turn["currentPlayerIndex"], PhotonNetwork.LocalPlayer.ActorNumber))
                {
                    gamestatustxt.text = "이 플레이어와 2번 식사를 완료했습니다. 제출 포기 버튼을 누르세요.";
                }
                else if ((int)Turn["currentPlayerIndex"] == playerData.playerNumber)
                {
                    gamestatustxt.text = "당신은 선 플레이어입니다. 차례를 기다리세요.";
                }
                else if (playerData.selectedFoodCard == 0)
                {
                    gamestatustxt.text = "카드가 선택되지 않았습니다.";
                }
                else if ((int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] == playerData.selectedFoodCard)
                {
                    gamestatustxt.text = "같은 카드를 선택할 수 없습니다.";
                }
                else if (playerData.Submited == true)
                {
                    gamestatustxt.text = "이미 카드를 제출했습니다.";
                }
                break;

            case 2:
                // 선 플레이어만 진행, 다른 플레이어의 카드를 고르는 턴
                if ((int)Turn["currentPlayerIndex"] == playerData.playerNumber &&
                (int)Turn["pickedPlayerIndex"] != 0 &&
                (int)Turn["pickedPlayerIndex"] != playerData.playerNumber)
                {
                    photonView.RPC("SynTurn", RpcTarget.MasterClient, 3);
                    photonView.RPC("ReturnUnselectedCard", RpcTarget.All, (int)Turn["pickedPlayerIndex"]);
                    photonView.RPC("SetGameStatusText", RpcTarget.All, $"{PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].NickName}님과 {PhotonNetwork.CurrentRoom.Players[(int)Turn["pickedPlayerIndex"]].NickName}님이 식사합니다.");
                }
                else
                {
                    gamestatustxt.text = "선 플레이어가 카드를 고르고 있습니다.";
                }
                break;

            case 3:
                // 식사 강탈 고르는 턴
                break;
            default:
                Debug.Log("Error: Invalid turn number.");
                break;
        }
    }
    public void ChoiceFree(int pickedPlayernum, FoodCard.CardPoint cardPoint)
    {
        Debug.Log($"choicefree");
        int pickedPlayerIndex = others[pickedPlayernum - 1];
        Debug.Log($"pickedPlayerIndex: {pickedPlayerIndex}, playerData.playerNumber: {playerData.playerNumber}");

        if (canFreeChoice == true && (int)Turn["currentTurn"] == 2 &&
        (int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber &&
        BlockPlayerTurn(pickedPlayerIndex, PhotonNetwork.LocalPlayer.ActorNumber) == false &&
        (int)cardPoint != playerData.selectedFoodCard)
        {
            photonView.RPC("Picked", RpcTarget.All, pickedPlayernum, cardPoint);
            photonView.RPC("SetCardValue", RpcTarget.All, (int)cardPoint, pickedPlayerIndex);
            Turn["pickedPlayerIndex"] = pickedPlayerIndex;
            PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
            photonView.RPC("ReceivePlayerData", RpcTarget.Others, Turn["pickedPlayerIndex"], (int)cardPoint);

            Debug.Log($"(ChoiceFree) pickedPlayernum : {(int)Turn["pickedPlayerIndex"]}");
            Debug.Log($"{others[pickedPlayernum - 1]}번 플레이어와 식사하기로 선택했습니다. (choiceFree)");

            photonView.RPC("SynTurn", RpcTarget.MasterClient, 3);
            Debug.Log($"choiced : {pickedPlayernum}번 플레이어와 식사하기로 선택했습니다. 카드 포인트: {cardPoint}");

        }
        else if (BlockPlayerTurn(pickedPlayernum, PhotonNetwork.LocalPlayer.ActorNumber))
        {
            gamestatustxt.text = "이 플레이어와 2번 식사를 완료했습니다. 제출 포기 버튼을 누르세요.";
        }
        else if ((int)Turn["currentPlayerIndex"] != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            gamestatustxt.text = "선 플레이어가 카드를 고르는 동안 기다려 주세요.";
        }
        else if ((int)cardPoint == (int)player["selectedFoodCard"])
        {
            gamestatustxt.text = "같은 카드를 선택할 수 없습니다.";
        }
        else
        {
            Debug.LogWarning("자유롭게 카드를 고를 수 있는 상황이 아닙니다.");
            Debug.Log($"현재 턴 : {Turn["currentTurn"]}, 현재 플레이어 : {Turn["currentPlayerIndex"]}, 선택한 플레이어 : {Turn["pickedPlayerIndex"]}, 카드 포인트 : {cardPoint}, canFreeChoice : {canFreeChoice}");
        }
    }

    [PunRPC]
    public void Picked(int pickedPlayernum, FoodCard.CardPoint cardPoint)
    {
        if (pickedPlayernum == playerData.playerNumber)
        {
            player["selectedFoodCard"] = cardPoint;
            playerData.selectedFoodCard = (int)cardPoint;
            sm.SoundPlay(3);
        }

    }


    [PunRPC]
    public void ReceivePlayerData(int playernum, int FoodCardScore)
    {
        if (playerData.playerNumber == playernum)
        {
            playerData.selectedFoodCard = FoodCardScore;
        }
    }

    public void Denybutton()
    {
        if ((int)Turn["currentTurn"] == 1 && (int)Turn["currentPlayerIndex"] != PhotonNetwork.LocalPlayer.ActorNumber && playerData.Submited == false)
        {
            playerData.Submited = true;
            photonView.RPC("TryIncreaseFoodSubmitedRPC", RpcTarget.MasterClient, 1);
            playerData.selectedFoodCard = 0;
            photonView.RPC("SetCardValue", RpcTarget.All, 0, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }


    [PunRPC]
    public void TryIncreaseFoodSubmitedRPC(int choice = 0)
    {
        foodSubmittedCount++;
        
        if (choice == 1) {checkDeny++;}

        if (foodSubmittedCount >= PlayerCount - 1)
        {
            if (checkDeny >= PlayerCount - 1)
            photonView.RPC("SynCanFreeChoice", RpcTarget.All, true);

            photonView.RPC("SynTurn", RpcTarget.MasterClient, 2);
        }
    }

    [PunRPC]
    public void ReturnUnselectedCard(int selectedPlayer)
    {
        int max = PhotonNetwork.CurrentRoom.MaxPlayers;
        for (int i = 1; i < max + 1; i++)
        {
            if (i != selectedPlayer)
            {
                cardImages[i].sprite = backSprite;
            }
        }
    }

    [PunRPC]
    public void SetGameStatusText(string text)
    {
        gamestatustxt.text = text;
    }

    [PunRPC]
    public void SubmitChoiceToMaster(int actorNumber, int mychoice)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int currentPlayerNum = (int)Turn["currentPlayerIndex"];
            int pickedPlayerNum = (int)Turn["pickedPlayerIndex"];

            // 선플이면 앞에, 후플이면 뒤에 저장
            if (actorNumber == currentPlayerNum)
            {
                choice = mychoice;
            }
            else if (actorNumber == pickedPlayerNum)
            {
                choice2 = mychoice;
            }

            // 두 명 모두 선택했는지 체크
            if (choice != -1 && choice2 != -1)
            {
                SettleChoice(choice, choice2);
            }
        }
    }

    void SettleChoice(int choice, int choice2)
    {
        Debug.Log($"SettleChoice called with choice: {choice}, choice2: {choice2}");
        int tempscore = 0;
        int currentPlayerNum = (int)Turn["currentPlayerIndex"];
        int pickedPlayerNum = (int)Turn["pickedPlayerIndex"];
        Debug.Log($"pickedPlayerIndex : {(int)Turn["pickedPlayerIndex"]}");

        PlayerData StarterPlayer = playerDatas.Find(p => p.playerNumber == currentPlayerNum);

        if (StarterPlayer == null)
        {
            Debug.LogError($"StarterPlayer not found! currentPlayerNum={currentPlayerNum}");
            return;
        }
        else Debug.Log($"StarterPlayer found! currentPlayerNum={currentPlayerNum}, selectedFoodCard={StarterPlayer.selectedFoodCard}");

        PlayerData PickedPlayer = playerDatas.Find(p => p.playerNumber == pickedPlayerNum);

        if (PickedPlayer == null)
        {
            Debug.LogError($"PickedPlayer not found! pickedPlayerNum={pickedPlayerNum}");
            return;
        }
        else Debug.Log($"PickedPlayer found! pickedPlayerNum={pickedPlayerNum}, selectedFoodCard={PickedPlayer.selectedFoodCard}");
        
        int a = StarterPlayer.selectedFoodCard;
        int b = PickedPlayer.selectedFoodCard;

        Debug.Log($"선플 카드 : {a}, 후플 카드 : {b}");


        switch (choice, choice2)
        {
            case (0, 0): // 식사
                // photonView.RPC("CalculateScore", RpcTarget.All, pickedPlayerNum, b);
                // photonView.RPC("CalculateScore", RpcTarget.All, currentPlayerNum, a);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, pickedPlayerNum, currentPlayerNum);
                photonView.RPC("UpdateMedal", RpcTarget.All, 0, currentPlayerNum);
                photonView.RPC("UpdateMedal", RpcTarget.All, 0, pickedPlayerNum);

                sm.SoundPlay(0);
                score[currentPlayerNum - 1] += a;
                score[pickedPlayerNum - 1] += b;
                
                break;
            case (0, 1):
                tempscore = Mathf.Abs(a - b);
                // photonView.RPC("CalculateScore", RpcTarget.All, pickedPlayerNum, tempscore);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, pickedPlayerNum, currentPlayerNum);
                photonView.RPC("UpdateMedal", RpcTarget.All, 1, pickedPlayerNum);
                sm.SoundPlay(1);
                score[pickedPlayerNum - 1] += tempscore;
                break;
            case (1, 0):
                tempscore = Mathf.Abs(b - a);
                // photonView.RPC("CalculateScore", RpcTarget.All, currentPlayerNum, tempscore);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, pickedPlayerNum, currentPlayerNum);
                photonView.RPC("UpdateMedal", RpcTarget.All, 1, currentPlayerNum);
                sm.SoundPlay(1);
                score[currentPlayerNum - 1] += tempscore;
                break;
            case (1, 1): // 쓰레기통
                sm.SoundPlay(2);
                Trash.Add(a);
                Trash.Add(b);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, pickedPlayerNum, currentPlayerNum);
                break;
            default:
                Debug.Log("Error: Invalid choice.");
                break;
        }

        photonView.RPC("SyncPlayerScore", RpcTarget.All, score);
        photonView.RPC("UpdatePlayerHandCard", RpcTarget.All, currentPlayerNum, pickedPlayerNum, a, b);
        photonView.RPC("Start", RpcTarget.All);
    }

    [PunRPC]
    public void SyncPlayerScore(int[] scores)
    {
        score = scores;

        for (int i = 0; i < PlayerCount - 1; i++)
        {
            scoreTexts[i].text = $"{score[others[i] - 1]}";
        }
        
        Debug.Log($"현재 내 점수 : {score[playerData.playerNumber - 1]}, 다른 플레이어 점수 : {score[0]}, {score[1]}, {score[2]}, {score[3]}, {score[4]}, {score[5]}");

    }




    public void SetChoice(int mychoice) // 0: 식사, 1: 강탈
    {
        if ((int)Turn["currentTurn"] == 3 && ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber ||
            (int)Turn["pickedPlayerIndex"] == playerData.playerNumber))
        {
            photonView.RPC("SubmitChoiceToMaster", RpcTarget.MasterClient, playerData.playerNumber, mychoice);
            gamestatustxt.text = "제출 완료! 상대의 선택을 기다립니다.";
        }
        else if ((int)Turn["currentTurn"] != 3)
        {
            gamestatustxt.text = "아직 강탈/식사 선택을 할 수 없습니다. 차례를 기다려 주세요.";
            Debug.Log($"현재 턴 : {Turn["currentTurn"]}, 현재 플레이어 : {Turn["currentPlayerIndex"]}, 선택한 플레이어 : {Turn["pickedPlayerIndex"]}");
        }
        else if ((int)Turn["currentPlayerIndex"] != playerData.playerNumber &&
                 (int)Turn["pickedPlayerIndex"] != playerData.playerNumber)
        {
            gamestatustxt.text = "식사에 참여하고 있지 않습니다. 잠시 기다려 주세요.";
            Debug.Log($"현재 턴 : {Turn["currentTurn"]}, 현재 플레이어 : {Turn["currentPlayerIndex"]}, 선택한 플레이어 : {Turn["pickedPlayerIndex"]}");
            return;
        }
    }

    [PunRPC]
    public void UpdatePlayerHandCard(int currentPlayerNum, int pickedPlayerNum, int a, int b)
    {
        if (currentPlayerNum == playerData.playerNumber)
        {
            playerData.playerHand.Remove((FoodCard.CardPoint)a);
        }
        else if (pickedPlayerNum == playerData.playerNumber)
        {
            playerData.playerHand.Remove((FoodCard.CardPoint)b);
        }

        int pickedPlIndex = others.IndexOf(pickedPlayerNum);
        int starterIndex = others.IndexOf(currentPlayerNum);

        if (PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerNum)
        {
            SetCardToBack(handCards[pickedPlIndex].cards[GetSpriteIndex(b)]);
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerNum)
        {
            SetCardToBack(handCards[starterIndex].cards[GetSpriteIndex(a)]);
        }
        else
        {
            SetCardToBack(handCards[starterIndex].cards[GetSpriteIndex(a)]);
            SetCardToBack(handCards[pickedPlIndex].cards[GetSpriteIndex(b)]);
        }
    }

    [PunRPC]
    public void UpdateVariable(int value, int playernum)
    {
        if (playernum == 0) choice = value;
        else if (playernum == 1) choice2 = value;
    }

    [PunRPC]
    public void UpdateMedal(int goldorSilver, int playernum)
    {
        if (goldorSilver == 0) gold[playernum - 1] += 1;
        else if (goldorSilver == 1) silver[playernum - 1] += 1;
    }

    [PunRPC]
    public void RoundResetCheck()
    {
        for (int i = 0; i < PlayerCount; i++)
        {
            switch (score[i])
            {
                case int n when (n >= 0 && n <= 16):
                    grade[i] = 1;
                    photonView.RPC("SetGrade", RpcTarget.All, i+1, 1);
                    break;
                case int n when (n > 16 && n <= 21):
                    grade[i] = 2;
                    photonView.RPC("SetGrade", RpcTarget.All, i+1, 2);
                    break;
                case int n when (n > 21 && n <= 50):
                    grade[i] = 3;
                    photonView.RPC("SetGrade", RpcTarget.All, i+1, 3);
                    break;
                case int n when (n > 50):
                    // 우승
                    photonView.RPC("GameOver", RpcTarget.All, i+1, 2);
                    break;
                default:
                    Debug.LogWarning($"Invalid score value for player {i + 1}: {score[i]}");
                    break;
            }
        }


        // if (score[myNum - 1] >= 16 && (int)player["grade"] == 0)
        // {
        //     photonView.RPC("SyncScore", RpcTarget.All, myNum, 0);
        //     player["grade"] = 1;
        //     photonView.RPC("SetGrade", RpcTarget.All, myNum, 1);
        //     gamestatustxt.text = "용이 자라났다!";

        // }
        // else if (score[myNum - 1] >= 21 && (int)player["grade"] == 1)
        // {
        //     photonView.RPC("SyncScore", RpcTarget.All, myNum, 0);
        //     player["grade"] = 2;
        //     photonView.RPC("SetGrade", RpcTarget.All, myNum, 2);
        //     gamestatustxt.text = "크와아아앙!!";

        // }
        // else if (score[playerData.playerNumber - 1] >= 50 && (int)player["grade"] == 2)
        // {
        //     photonView.RPC("SyncScore", RpcTarget.All, myNum, 0);
        //     player["grade"] = 3;
        //     gamestatustxt.text = "용이 자라났다!";

        // }
        // else if ((int)player["grade"] == 3 && score[myNum - 1] > 50)
        // {
        //     gamestatustxt.text = "우승!";
        //     return; // 게임 종료부분 이어붙이기
        // }

        for (int i = 0; i < PlayerCount-1; i++) // other카드 앞판으로
        {
            for (int j = 0; j < 6; j++)
            {
                handCards[i].cards[j].sprite = MiniFoodImages[j];
                handCards[i].cards[j].color = new Color(1f, 1f, 1f, 1f);
            }
        }  

        Debug.Log($"현재 내 점수 : {score[playerData.playerNumber - 1]}, 다른 플레이어 점수 : {score[0]}, {score[1]}, {score[2]}, {score[3]}, {score[4]}, {score[5]}");
        PhotonNetwork.LocalPlayer.SetCustomProperties(player);

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(WaitFor1Sec());
        }
    }

    [PunRPC]
    public void SetGrade(int playernum, int grade)
    {
        if (playernum == playerData.playerNumber)
        {
            myGradeImage[grade - 1].sprite = gradeSprite[0];
            PlayerProfileWelski[0].sprite = WelskiSprite[grade];
        }
        else if (grade == 1)
        {
            int currentIndex = others.IndexOf(playernum);
            gradeImages[currentIndex].gradeImage[0].sprite = gradeSprite[0];
            PlayerProfileWelski[currentIndex].sprite = WelskiSprite[1];
        }
        else if (grade == 2)
        {
            int currentIndex = others.IndexOf(playernum);
            gradeImages[currentIndex].gradeImage[1].sprite = gradeSprite[0];
            PlayerProfileWelski[currentIndex].sprite = WelskiSprite[2];
        }
    }

    [PunRPC]
    public void CalculateScore(int playerNum, int scoreamount)
    {
        score[playerNum - 1] += scoreamount;
    }

    [PunRPC]
    public void SyncScore(int playerNum, int myscore)
    {
        score[playerNum - 1] = myscore;
    }

    public void FoodCardSelect(FoodCard.CardPoint cardPoint)
    {
        if (cardPoint != FoodCard.CardPoint.tablecard)
        {
            if (playerData.Submited == false)
            {
                player["selectedFoodCard"] = cardPoint;
                playerData.selectedFoodCard = (int)cardPoint;
            }
            else
            {
                Debug.Log($"카드를 이미 제출했습니다 : {player["selectedFoodCard"]}({playerData.selectedFoodCard})(제출됨)");
            }
            PhotonNetwork.LocalPlayer.SetCustomProperties(player);
        }
    }

    public void TableCardSelect(int playernum)
    {
        PlayerData selectPlayerData = playerDatas.Find(p => p.playerNumber == playernum);
        if (selectPlayerData == null)
        {
            Debug.LogWarning($"플레이어 {playernum}의 데이터가 없습니다.");
            return;
        }

        if ((int)Turn["currentTurn"] == 2 &&
        (int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber &&
        selectPlayerData.selectedFoodCard != 0
        )
        {
            Turn["pickedPlayerIndex"] = playernum;
            PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
            gamestatustxt.text = $"{playernum}번 플레이어와 식사하려면 제출 버튼을 누르세요.";
        }
        else if (selectPlayerData.selectedFoodCard == 0)
        {
            gamestatustxt.text = $"{playernum}번 플레이어는 카드를 제출하지 않았습니다.";
        }
        else
        {
            gamestatustxt.text = $"{playernum}번 플레이어의 음식입니다.";
        }

        Debug.Log($"TableCardSelect 호출됨: {playernum}번 플레이어 선택됨, 카드 포인트: {selectPlayerData.selectedFoodCard}");
    }

    [PunRPC]
    public void SynCanFreeChoice(bool canFree)
    {
        canFreeChoice = canFree;
        Debug.Log($"canFreeChoice 값이 {canFreeChoice}로 설정되었습니다.");
    }


    [PunRPC]
    public void SetCardValue(int value, int playernum)
    {
        if (value == 0)
        {
            cardImages[playernum].sprite = backSprite; // 카드 뒷면 고정
        }
        else
        {
            cardImages[playernum].sprite = cardSprites[GetSpriteIndex(value)];
        }
    }

    [PunRPC]
    public void ResetAllCardBacks()
    {
        for (int i = 0; i < cardImages.Length; i++)
        {
            cardImages[i].sprite = backSprite; // 모든 카드 뒷면으로 초기화
        }
    }

    private int GetSpriteIndex(int value)
    {
        switch (value)
        {
            case 1: return 0;
            case 2: return 1;
            case 3: return 2;
            case 4: return 3;
            case 5: return 4;
            case 7: return 5;
            case 0: return 6;
            default: return 6;
        }
    }

    public void ShowPlayerPositions()
    {
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                others.Add(player.ActorNumber);
            }
        }
        if (others.Count > 0)
            Debug.Log($"다른 플레이어들: {string.Join(", ", others)}");
    }

    [PunRPC]
    public void GetNewMycard()
    {
        photonView.RPC("AddTrash", RpcTarget.MasterClient, playerData.playerNumber);
        playerData.playerHand.Clear();
        playerData.playerHand.Add(FoodCard.CardPoint.Bread);
        playerData.playerHand.Add(FoodCard.CardPoint.Soup);
        playerData.playerHand.Add(FoodCard.CardPoint.Fish);
        playerData.playerHand.Add(FoodCard.CardPoint.Steak);
        playerData.playerHand.Add(FoodCard.CardPoint.Turkey);
        playerData.playerHand.Add(FoodCard.CardPoint.Cake);
        Debug.Log("새로운 카드를 받았습니다.");

        currentTurnProcess.Clear();
        Debug.Log($"currentTurnProcess 초기화됨: {currentTurnProcess.Count} items");
    }

    [PunRPC]
    public void AddTrash(int playernum)
    {
        PlayerData selectPlayerData = playerDatas.Find(p => p.playerNumber == playernum);
        Trash.AddRange(selectPlayerData.playerHand);
    }

    [PunRPC]
    public void TrashGotcha()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("쓰레기통에서 뽑기를 시작합니다.");
        if (Trash.Count == 0)
        {
            Debug.LogWarning("Trash 리스트가 비어있어서 뽑기를 할 수 없습니다.");
            return;
        }

        System.Random random = new System.Random();
        int index = random.Next(Trash.Count);
        int luck = (int)Trash[index];

        int player = 1 + random.Next(PlayerCount);

        photonView.RPC("recTrash", RpcTarget.All, player, luck);

        Debug.Log($"쓰레기통에서 뽑힌 카드 : {luck}, 플레이어 : {player}");
    }

    [PunRPC]
    public void recTrash(int playernum, int scoreamount)
    {
        if (playerData.playerNumber == playernum)
        {
            photonView.RPC("CalculateScore", RpcTarget.All, playernum, scoreamount);
            photonView.RPC("SetGameStatusText", RpcTarget.All, $"{playernum}번 플레이어가 쓰레기통에서 {scoreamount}점 획득!");

        }
        Trash.Clear(); // 쓰레기통 비우기
    }

    public void SetMyCard()
    {
        foreach (Image card in myImages)
        {
            var cardPoint = card.GetComponent<FoodCard>().cardPoint;

            // 해당 카드포인트가 playerHand에 있으면 앞면, 없으면 뒷면
            if (playerData.playerHand.Contains(cardPoint))
            {
                card.sprite = cardSprites[GetSpriteIndex((int)cardPoint)];
                card.color = new Color(1f, 1f, 1f, 1f);
            }
            else
            {
                SetCardToBack(card);
                Debug.Log($"카드 {cardPoint}가 플레이어의 손에 없습니다.");
            }
        }
    }


    [PunRPC]
    public void UpdatePlayerTurn(int playerA, int playerB)
    {
        // 항상 작은 번호 먼저로 정렬
        var key = (Math.Min(playerA, playerB), Math.Max(playerA, playerB));

        // 현재 횟수 확인
        if (currentTurnProcess != null)
        {
            currentTurnProcess.Add(key);
            Debug.Log($"처음으로 {key} 간에 턴이 진행되었습니다.");

        }
        else if (currentTurnProcess.Contains(key))
        {
            currentTurnProcess.Add(key);
            Debug.Log($"추가로 {key} 간에 턴이 진행되었습니다.");
        }
        else return;
    }

    private bool BlockPlayerTurn(int playerA, int playerB)
    {
        var min = Math.Min(playerA, playerB);
        var max = Math.Max(playerA, playerB);
        var key = (min, max);

        if (currentTurnProcess.Contains(key) && currentTurnProcess.Count(p => p.Item1 == min && p.Item2 == max) >= 2) return true;
        else return false;
    }

    private void SetCardToBack(Image card)
    {
        if (card != null)
        {
            card.sprite = backSprite;
            card.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        foreach (var key in propertiesThatChanged.Keys)
        {
            if (Turn.ContainsKey(key))
            {
                Turn[key] = propertiesThatChanged[key];
            }
            else
            {
                Turn.Add(key, propertiesThatChanged[key]);
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("selectedFoodCard"))
        {
            int selectedFoodCard = (int)changedProps["selectedFoodCard"];
            // Debug.Log($"[SYNC] {targetPlayer.NickName}의 selectedFoodCard 동기화됨: {selectedFoodCard}");
        }

    }


    [PunRPC]
    public void SendCardInfo()
    {
        playerDatas.Clear();
        // playerHand를 int 배열로 변환
        int[] handArray = playerData.playerHand.Select(card => (int)card).ToArray();

        // ActorNumber, 선택 카드, hand 배열 전송
        photonView.RPC("SendCardInfo2", RpcTarget.All, 
            playerData.playerNumber, 
            playerData.selectedFoodCard, 
            handArray);

        Debug.Log("SendCardInfo 호출됨");
    }

    [PunRPC]
    public void SendCardInfo2(int playerNumber, int selectedFoodCard, int[] handArray)
    {
        // 새 PlayerData 생성
        PlayerData pd = new PlayerData();
        pd.playerNumber = playerNumber;
        pd.selectedFoodCard = selectedFoodCard;

        // int 배열을 다시 CardPoint 리스트로 변환
        pd.playerHand = handArray.Select(i => (FoodCard.CardPoint)i).ToList();

        playerDatas.Add(pd);
        Debug.Log($"{playerNumber}님 체크, 카드 {pd.playerHand.Count}장 보유");
        StartCoroutine(WaitForAllPlayerDatas());

        Debug.Log($"현재 playerDatas 수: {playerDatas.Count} SendCardInfo2 종료부" );
}


    public bool CheckOtherPlayerHand() // 모든 플레이어의 카드가 소모되었으면 true
    {
        int count = 0;

        foreach (PlayerData pl in playerDatas)
        {
            if (pl.playerHand.Count != 0)
            {
                Debug.Log($"플레이어 {pl.playerNumber}의 카드가 남아있습니다. 남은 카드 수: {pl.playerHand.Count} (checkOtherPlayerHand)");
                return false;
            }
            else
            {
                Debug.Log($"플레이어 {pl.playerNumber}의 카드가 모두 소모되었습니다. (checkOtherPlayerHand)");
                count++;
            }
        }

        if (count == PlayerCount) return true;
        return false;
    }

    public bool IsFinishMyTurn() // 모든 플레이어와 두 번씩 식사 진행한 경우 true 또는 진행할 수 없으면 true
    {
        int turnCount = 0;
        int currentPlayerIndex = (int)Turn["currentPlayerIndex"];
        //int maxPlayer = PhotonNetwork.CurrentRoom.MaxPlayers;

        for (int i = 1; i < PlayerCount + 1; i++)
        {
            if (i == currentPlayerIndex) continue;
            if (BlockPlayerTurn(currentPlayerIndex, i)) turnCount++;
        }

        if (turnCount >= PlayerCount - 1)
        {
            Debug.Log($"플레이어 {currentPlayerIndex}는 모든 플레이어와 2번씩 식사를 완료했습니다. (IsFinishMyTurn)");
            return true;
        }
        else
        {
            foreach (PlayerData pl in playerDatas)
            {
                if (pl.playerNumber == currentPlayerIndex) continue;
                if (pl.playerHand.SequenceEqual(playerData.playerHand) &&
                pl.playerHand.Count == 1 &&
                !BlockPlayerTurn(pl.playerNumber, playerData.playerNumber))
                {
                    Debug.Log($"플레이어 {pl.playerNumber}와 {playerData.playerNumber}의 카드가 같습니다.");
                    return true;
                }
            }
        }
        Debug.Log($"진행 가능");
        return false;
    }

    public void DeselectAllCards()
    {
        CardOutlineController[] allCards = FindObjectsOfType<CardOutlineController>();
        foreach (var card in allCards)
        {
            card.DeselectCard();
        }
    }
    public void SetDefaultValue()
    {
        choice = -1;
        choice2 = -1;
        playerDatas.Clear();

        canFreeChoice = false;

        foodSubmittedCount = 0;
        checkDeny = 0;
        
        playerData.Submited = false;
        playerData.selectedFoodCard = -1;
        
        player["selectedFoodCard"] = 0;
        player["choice"] = -1;

        PhotonNetwork.LocalPlayer.SetCustomProperties(player);


        for (int i = 0; i < PlayerCount; i++)
        {
            SetCardValue(0, i);
        }

        for (int i = 0; i < PlayerCount-1; i++)
        {
            goldTexts[i].text = $"{gold[others[i] - 1]}";
            silverTexts[i].text = $"{silver[others[i] - 1]}";
        }

        myScoreText.text = $"{score[PhotonNetwork.LocalPlayer.ActorNumber - 1]}";
        myGoldText.text = $"{gold[PhotonNetwork.LocalPlayer.ActorNumber - 1]}";
        mySilverText.text = $"{silver[PhotonNetwork.LocalPlayer.ActorNumber - 1]}";
        photonView.RPC("ResetAllCardBacks", RpcTarget.All);

        SetMyCard();
    }

    [PunRPC]
    public void SetStartValue()
    {
        if ((int)Turn["currentPlayerIndex"] >= PhotonNetwork.CurrentRoom.MaxPlayers) Turn["currentPlayerIndex"] = 1;
        else Turn["currentPlayerIndex"] = (int)Turn["currentPlayerIndex"] + 1;
        photonView.RPC("SetGameStatusText", RpcTarget.All, $"{PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].NickName}님의 차례입니다.");

        Turn["currentTurn"] = -1;
        Turn["pickedPlayerIndex"] = 0;

        PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
    }
    public void SetCustomProperty(Photon.Realtime.Player player, string key, int value)
    {
        ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable
        {
            { key, value }
        };
        player.SetCustomProperties(properties);
    }

    [PunRPC]
    public void GameOver()
    {
        resultPannel.SetActive(true);

        for (int i = 0; i < PlayerCount; i++)
        {
            Player p = PhotonNetwork.CurrentRoom.GetPlayer(i + 1);
            resultNameTexts[i].text = p.NickName;
            resultScoreTexts[i].text = score[i].ToString();
        }
    }

    public void CloseResultPanel()
    {
        resultPannel.SetActive(false);
    }
}