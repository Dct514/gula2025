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
    public Image[] myImages;
    public List<HandCard> handCards = new List<HandCard>(); // 각 플레이어의 카드들 (이미지)
    public List<Grade> gradeImages = new List<Grade>();
    public Sprite backSprite;
    public Sprite[] gradeSprite;
    public Image[] myGradeImage;
    public List<FoodCard.CardPoint> otherFoodCards = new List<FoodCard.CardPoint>();
    public PlayerData playerData = new PlayerData();
    public List<PlayerData> playerDatas = new List<PlayerData>(); // 플레이어 덱 비교를 위해서 여기에 넣겠습니다
    private Coroutine turnTimerCoroutine;
    private Dictionary<int, int> receivedChoices = new Dictionary<int, int>();
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
        CheckCard();

        Debug.Log($"[Start] 현재 턴 : {Turn["currentTurn"]}, 현재 플레이어 : {Turn["currentPlayerIndex"]}");
    }



    private IEnumerator WaitFor1Sec()
    {
        yield return new WaitForSeconds(1f);
    }


    // 선플일때만 정상적으로 타이머가 작동하는 오류
    public void CheckCard()
    {
        if ((int)Turn["currentPlayerIndex"] != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Debug.Log("현재 차례는 " + (int)Turn["currentPlayerIndex"] + "번 플레이어입니다.");
            return;
        }

        if (CheckOtherPlayerHand() == true) // 모든 플레이어가 카드 소모 완료한 경우 
        {
            GetNewMycard();
            photonView.RPC("TrashGotcha", RpcTarget.MasterClient);
            photonView.RPC("RoundResetCheck", RpcTarget.All); // 등급 리셋, 스코어 리셋
            Debug.Log("모든 플레이어가 카드를 소모했습니다. 새로운 카드를 받습니다.");
        }
        else if (playerData.playerHand.Count == 0)
        {
            Debug.Log("카드를 다 쓰셨군요");
            gamestatustxt.text = "카드를 다 소모해서 차례가 자동으로 넘어갑니다.";
            StopTurnTimer();
            photonView.RPC("Start", RpcTarget.All);
        }
        else if (IsFinishMyTurn() || IsCannot()) // 2번씩 식사를 마쳤거나, 더 이상 식사를 할 수 없는 상황 -> 턴 넘어가기
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
                        playerData.selectedFoodCard = (int)playerData.playerHand[0];
                        MainTurnStart();
                    }
                    else Debug.LogWarning("카드가 없어서 자동 제출을 할 수 없습니다.");
                }
                break;
            case 1:
                if (playerData.Submited == false &&
                (int)Turn["currentPlayerIndex"] != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    Denybutton();
                }
                break;
            case 2:
                gamestatustxt.text = "조금만 기다려 주세요.";
                break;
            case 3:
                if ((int)Turn["currentPlayerIndex"] == playerData.playerNumber ||
                    (int)Turn["pickedPlayerIndex"] == playerData.playerNumber)
                {
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
                (int)Turn["currentPlayerIndex"] != PhotonNetwork.LocalPlayer.ActorNumber &&
                (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] != playerData.selectedFoodCard)
                {
                    playerData.Submited = true;
                    photonView.RPC("TryIncreaseFoodSubmitedRPC", RpcTarget.MasterClient, 0);
                    photonView.RPC("SetCardValue", RpcTarget.All, playerData.selectedFoodCard, PhotonNetwork.LocalPlayer.ActorNumber);
                }
                else if (BlockPlayerTurn((int)Turn["currentPlayerIndex"], PhotonNetwork.LocalPlayer.ActorNumber))
                {
                    gamestatustxt.text = "이 플레이어와 2번 식사를 완료했습니다. 제출 포기 버튼을 누르세요.";
                }
                else if ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber)
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
        Debug.Log($"pickedPlayernum: {pickedPlayernum}, cardPoint: {cardPoint}, currentTurn: {Turn["currentTurn"]}, currentPlayerIndex: {Turn["currentPlayerIndex"]}");

        if (canFreeChoice == true && (int)Turn["currentTurn"] == 2 &&
        (int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber &&
        BlockPlayerTurn(pickedPlayernum, PhotonNetwork.LocalPlayer.ActorNumber) == false &&
        (int)cardPoint != (int)player["selectedFoodCard"])
        {
            player["selectedFoodCard"] = cardPoint;
            playerData.selectedFoodCard = (int)cardPoint;
            photonView.RPC("SetCardValue", RpcTarget.All, (int)cardPoint, pickedPlayernum);
            Turn["pickedPlayerIndex"] = others[pickedPlayernum - 1];
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
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("MasterClient가 아니므로 무시됨");
            return;
        }

        // Master 전용 변수 증가
        foodSubmittedCount++;
        
        if (choice == 1) {checkDeny++;}

        FoodSubmitted();

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
        int score = 0;
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
                photonView.RPC("CalculateScore", RpcTarget.All, pickedPlayerNum, PickedPlayer.selectedFoodCard);
                photonView.RPC("CalculateScore", RpcTarget.All, currentPlayerNum, a);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, pickedPlayerNum, currentPlayerNum);
                photonView.RPC("UpdateMedal", RpcTarget.All, 0, currentPlayerNum);
                photonView.RPC("UpdateMedal", RpcTarget.All, 0, pickedPlayerNum);
                break;
            case (0, 1):
                score = Mathf.Abs(a - b);
                photonView.RPC("CalculateScore", RpcTarget.All, pickedPlayerNum, score);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, pickedPlayerNum, currentPlayerNum);
                photonView.RPC("UpdateMedal", RpcTarget.All, 1, pickedPlayerNum);
                break;
            case (1, 0):
                score = Mathf.Abs(b - a);
                photonView.RPC("CalculateScore", RpcTarget.All, currentPlayerNum, score);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, pickedPlayerNum, currentPlayerNum);
                photonView.RPC("UpdateMedal", RpcTarget.All, 1, currentPlayerNum);
                break;
            case (1, 1): // 쓰레기통
                Trash.Add(a);
                Trash.Add(b);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, pickedPlayerNum, currentPlayerNum);
                break;
            default:
                Debug.Log("Error: Invalid choice.");
                break;
        }

        photonView.RPC("CountOtherFoodCard", RpcTarget.All, currentPlayerNum, pickedPlayerNum, a, b);

        if (currentPlayerNum == playerData.playerNumber)
        {
            playerData.playerHand.Remove((FoodCard.CardPoint)a);
        }
        else if (pickedPlayerNum == playerData.playerNumber)
        {
            playerData.playerHand.Remove((FoodCard.CardPoint)b);
        }

        photonView.RPC("Start", RpcTarget.All);
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
    public void RoundResetCheck() // todo : 카드 소모 다 된 다음 등급 적용 if()
    {
        if (score[PhotonNetwork.LocalPlayer.ActorNumber - 1] >= 16 && (int)player["grade"] == 0)
        {
            photonView.RPC("SyncScore", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 0);
            player["grade"] = 1;
            photonView.RPC("SetGrade", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 1);
            gamestatustxt.text = "용이 자라났다!";

        }
        else if (score[PhotonNetwork.LocalPlayer.ActorNumber - 1] >= 21 && (int)player["grade"] == 1)
        {
            photonView.RPC("SyncScore", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 0);
            player["grade"] = 2;
            photonView.RPC("SetGrade", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 2);
            gamestatustxt.text = "크와아아앙!!";

        }
        else if (score[PhotonNetwork.LocalPlayer.ActorNumber - 1] >= 50 && (int)player["grade"] == 2)
        {
            photonView.RPC("SyncScore", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 0);
            player["grade"] = 3;
            gamestatustxt.text = "용이 자라났다!";

        }
        else if ((int)player["grade"] == 3 && score[PhotonNetwork.LocalPlayer.ActorNumber - 1] >= 50)
        {
            gamestatustxt.text = "우승!";
            return;
        }

        Debug.Log($"현재 내 점수 : {score[PhotonNetwork.LocalPlayer.ActorNumber - 1]}, 다른 플레이어 점수 : {score[0]}, {score[1]}, {score[2]}, {score[3]}, {score[4]}, {score[5]}");
        PhotonNetwork.LocalPlayer.SetCustomProperties(player);

        photonView.RPC("Start", RpcTarget.All);
    }

    [PunRPC]
    public void SetGrade(int playernum, int grade)
    {
        if (playernum == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            myGradeImage[grade - 1].sprite = gradeSprite[0];
        }
        else if (grade == 1)
        {
            int currentIndex = others.IndexOf(playernum);
            gradeImages[currentIndex].gradeImage[0].sprite = gradeSprite[0];
        }
        else if (grade == 2)
        {
            int currentIndex = others.IndexOf(playernum);
            gradeImages[currentIndex].gradeImage[1].sprite = gradeSprite[0];
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

    [PunRPC]
    public void SyncVar(int variable, int myamount)
    {
        variable = myamount;
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


    public void FoodSubmitted()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (foodSubmittedCount >= PlayerCount - 1)
        {
            if (checkDeny >= PlayerCount - 1)
            {
                photonView.RPC("SynCanFreeChoice", RpcTarget.All, true);
            }
            
            photonView.RPC("SynTurn", RpcTarget.MasterClient, 2);
        }
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

    public void GetNewMycard()
    {
        photonView.RPC("AddTrash", RpcTarget.All, playerData);
        playerData.playerHand.Clear();
        playerData.playerHand.Add(FoodCard.CardPoint.Bread);
        playerData.playerHand.Add(FoodCard.CardPoint.Soup);
        playerData.playerHand.Add(FoodCard.CardPoint.Fish);
        playerData.playerHand.Add(FoodCard.CardPoint.Steak);
        playerData.playerHand.Add(FoodCard.CardPoint.Turkey);
        playerData.playerHand.Add(FoodCard.CardPoint.Cake);
    }

    [PunRPC]
    public void AddTrash(PlayerData playerdata)
    {
        Trash.AddRange(playerdata.playerHand);
    }

    [PunRPC]
    public void TrashGotcha()
    {
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
        }

        gamestatustxt.text = $"{player}번 플레이어가 쓰레기통을 차지합니다.";
        Trash.Clear(); // 쓰레기통 비우기
    }

    [PunRPC]
    public void CountOtherFoodCard(int currentPlayerNum, int pickedPlayerNum, int a, int b)
    {
        int pickedPlIndex = others.IndexOf(pickedPlayerNum);
        int starterIndex = others.IndexOf(currentPlayerNum);

        if (PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerNum)
        {
            SetCardToBack(handCards[pickedPlIndex].cards[GetSpriteIndex(b)]);
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerNum)
        {
            SetCardToBack(handCards[starterIndex].cards[GetSpriteIndex(a)]);
            // pickedPlayer는 currentPlayerNum의 카드가 비활성화되도록.
        }
        else
        {
            // SetCardToBack(handCards[starterIndex2].cards[GetSpriteIndex((int)PhotonNetwork.CurrentRoom.Players[pickedPlayerNum].CustomProperties["selectedFoodCard"])]);
            // SetCardToBack(handCards[starterIndex].cards[GetSpriteIndex((int)PhotonNetwork.CurrentRoom.Players[currentPlayerNum].CustomProperties["selectedFoodCard"])]);
            SetCardToBack(handCards[starterIndex].cards[GetSpriteIndex(a)]);
            SetCardToBack(handCards[pickedPlIndex].cards[GetSpriteIndex(b)]);
         }
    }

    public void SetMyCard(/*FoodCard.CardPoint cardPoint*/)
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

    public bool IsCannot() // 식사할 수 있는 플레이어 중, 남은 카드의 종류가 내 카드의 종류와 같은 경우 -> 턴을 넘길 것
    {

        foreach (PlayerData pl in playerDatas)
        {
            if (pl.playerHand == playerData.playerHand && pl.playerNumber != playerData.playerNumber && playerData.playerHand.Count == 1 && !BlockPlayerTurn(pl.playerNumber, playerData.playerNumber))
            {
                Debug.Log($"플레이어 {pl.playerNumber}와 {playerData.playerNumber}의 카드가 같습니다.");
                return true;
            }
        }
        return false;

    }

    public bool CheckOtherPlayerHand() // 모든 플레이어의 카드가 소모되었으면 true
    {

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
                return true;
            }
        }
        Debug.Log("오류 : playerDatas가 비어있습니다. (checkOtherPlayerHand)");
        return false;
    }

    public bool IsFinishMyTurn() // 모든 플레이어와 두 번씩 식사 진행한 경우 true
    {
        int turnCount = 0;
        int maxPlayer = PhotonNetwork.CurrentRoom.MaxPlayers;

        for (int i = 1; i < maxPlayer + 1; i++)
        {
            if (BlockPlayerTurn((int)Turn["currentPlayerIndex"], i)) turnCount++;
        }

        if (turnCount >= maxPlayer - 1) return true;
        else return false;
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
        checkDeny = 0;
        canFreeChoice = false;
        receivedChoices.Clear();
        
        playerData.Submited = false;
        player["selectedFoodCard"] = 0;
        player["choice"] = -1;
        Debug.Log($"현재 내 점수 : {player["score"]}");
        PhotonNetwork.LocalPlayer.SetCustomProperties(player);

        for (int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount - 1; i++)
        {
            scoreTexts[i].text = $"{score[others[i] - 1]}";
        }

        for (int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount + 1; i++)
        {
            SetCardValue(0, i);
        }

        for (int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount - 1; i++)
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

    public void UpdateScore(int playerNumber, int score)
    {
        player["score" + playerNumber] = score;
        PhotonNetwork.CurrentRoom.SetCustomProperties(player);
    }
    [PunRPC]
    public void GameOver()
    {
        resultPannel.SetActive(true);

        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        for (int i = 0; i < playerCount; i++)
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