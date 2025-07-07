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


public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    public TMP_Text gamestatustxt;
    public TMP_Text gameovertxt;    
    public GameObject resultPannel;
    public TMP_Text[] scoreTexts; // 점수 표시될 곳
    public Sprite[] cardSprites; // 카드 스프라이트
    public Image[] cardImages; // 카드 스프라이트 표시될 곳
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
    public int checkFoodCard = 0; // 카드 선택 확인용

    public int checkFalse = 0; // 모든 플레이어가 식사 거부를 한 경우, 선 플레이어에게 선택권을 주게끔 체크하는 변수입니다.

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

    [PunRPC]
    void Start()
    {
        Debug.Log("START() called");
        if (PhotonNetwork.IsMasterClient)
        {
            SetStartValue(); // 차례를 정합니다.
        }

        SetDefaultValue(); // 기본값을 초기화합니다.

        StartCoroutine(WaitForAllPlayerDatas());
    }

    private IEnumerator WaitForAllPlayerDatas()
    {
        // playerDatas가 모두 모일 때까지 대기
        while (playerDatas.Count < PhotonNetwork.CurrentRoom.PlayerCount)
        {
            yield return null; // 한 프레임 대기
        }

        // 모두 모이면 다음 코드 실행
        CheckCard();
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
                GetNewMycard();

                if (PhotonNetwork.LocalPlayer.IsMasterClient)
                {
                    TrashGotcha();
                }
                for (int i = 1; i < 7; i++)
                {
                    SetCardValue(i, playerData.playerNumber);
                }

                photonView.RPC("RoundResetCheck", RpcTarget.All); // 등급 리셋, 스코어 리셋
            }
            else if (playerData.playerHand.Count == 0)
            {
                Debug.Log("카드를 다 쓰셨군요");
                gamestatustxt.text = "카드를 다 소모해서 차례가 자동으로 넘어갑니다.";
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
                Debug.Log("카드가 남아있습니다. 식사를 진행합니다. 남은 카드 : " + playerData.playerHand.Count);

            }

        Debug.Log($"[START] 현재 턴 : {Turn["currentTurn"]}, 현재 플레이어 : {Turn["currentPlayerIndex"]}");
        
    }

    public void SetDefaultValue()
    {
        choice = -1;
        choice2 = -1;
        playerDatas.Clear();
        photonView.RPC("SendCardInfo", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, playerData);
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

        SetMyCard();
    }


    [PunRPC]
    public void SetStartValue()
    {
        if ((int)Turn["currentPlayerIndex"] >= PhotonNetwork.CurrentRoom.MaxPlayers) Turn["currentPlayerIndex"] = 1;
        else Turn["currentPlayerIndex"] = (int)Turn["currentPlayerIndex"] + 1;
        photonView.RPC("SetGameStatusText", RpcTarget.All, $"{PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].NickName}님의 차례입니다.");

        Turn["currentTurn"] = 0;
        Turn["pickedPlayerIndex"] = 0;
        Turn["foodSubmited"] = 0;

        PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
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

        ShowPlayerPositions();
        myNickNameText.text = PhotonNetwork.LocalPlayer.NickName;
        player["grade"] = 0;
        player["score"] = 0;
        PhotonNetwork.LocalPlayer.SetCustomProperties(player);

        playerData.playerHand.Clear();
        playerData.playerHand.Add(FoodCard.CardPoint.Bread);
        playerData.playerHand.Add(FoodCard.CardPoint.Soup);
        playerData.playerHand.Add(FoodCard.CardPoint.Fish);
        playerData.playerHand.Add(FoodCard.CardPoint.Steak);
        playerData.playerHand.Add(FoodCard.CardPoint.Turkey);
        playerData.playerHand.Add(FoodCard.CardPoint.Cake);

        if (PhotonNetwork.IsMasterClient)
        {
            Turn["currentPlayerIndex"] = UnityEngine.Random.Range(0, PhotonNetwork.CurrentRoom.PlayerCount - 1);
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

    public void MainTurnStart()
    {

        Debug.Log($"Local player selectedFoodCard: {player["selectedFoodCard"]}");
        Debug.Log($"Remote player selectedFoodCard: {PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"]}");
        switch (Turn["currentTurn"])
        {
            case 0:
                if ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber && (int)player["selectedFoodCard"] != 0)
                {
                    playerData.Submited = true;
                    photonView.RPC("SetCardValue", RpcTarget.All, (int)player["selectedFoodCard"], 0);
                    photonView.RPC("SetGameStatusText", RpcTarget.All, "선 플레이어와 식사할 사람은 카드를 제출하세요.");
                    Turn["currentTurn"] = 1;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
                }
                else
                {
                    gamestatustxt.text = "차례가 아닙니다.";
                }
                break;

            case 1:
                if (!BlockPlayerTurn((int)Turn["currentPlayerIndex"], PhotonNetwork.LocalPlayer.ActorNumber) &&
                playerData.Submited == false &&
                (int)player["selectedFoodCard"] != 0 &&
                (int)Turn["currentPlayerIndex"] != PhotonNetwork.LocalPlayer.ActorNumber &&
                PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] != null &&
                (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] != (int)player["selectedFoodCard"])
                {
                    playerData.Submited = true;
                    Turn["foodSubmited"] = (int)Turn["foodSubmited"] + 1;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
                    photonView.RPC("SetCardValue", RpcTarget.All, (int)player["selectedFoodCard"], PhotonNetwork.LocalPlayer.ActorNumber);
                    Debug.Log($"낸 사람 수 : {Turn["foodSubmited"]}");

                    if (FoodSubmited())
                    {
                        photonView.RPC("SetGameStatusText", RpcTarget.All, "선 플레이어님, 같이 식사할 플레이어를 고르세요.");
                        Turn["currentTurn"] = 2;
                        PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
                    }
                }
                else if (BlockPlayerTurn((int)Turn["currentPlayerIndex"] , PhotonNetwork.LocalPlayer.ActorNumber))
                {
                    gamestatustxt.text = "이 플레이어와 2번 식사를 완료했습니다. 제출 포기 버튼을 누르세요.";
                }
                else if ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    gamestatustxt.text = "당신은 선 플레이어입니다. 차례를 기다리세요.";
                }
                else if ((int)player["selectedFoodCard"] == 0)
                {
                    gamestatustxt.text = "카드가 선택되지 않았습니다.";
                }
                else if ((int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] == (int)player["selectedFoodCard"])
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
                if ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber && (int)Turn["pickedPlayerIndex"] != 0 && (int)Turn["pickedPlayerIndex"] != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    Turn["currentTurn"] = 3;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
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

    public void Denybutton()
    {
        if ((int)Turn["currentTurn"] == 1 && (int)Turn["currentPlayerIndex"] != PhotonNetwork.LocalPlayer.ActorNumber && playerData.Submited == false)
        {
            playerData.Submited = true;
            Turn["foodSubmited"] = (int)Turn["foodSubmited"] + 1;
            PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
            player["selectedFoodCard"] = 0;
            photonView.RPC("SetCardValue", RpcTarget.All, 0, PhotonNetwork.LocalPlayer.ActorNumber);
            Debug.Log($"낸 사람 수 : {Turn["foodSubmited"]}");

            if (FoodSubmited())
            {
                Turn["currentTurn"] = 2;
                PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
                photonView.RPC("SetGameStatusText", RpcTarget.All, "선 플레이어가 같이 식사할 사람을 고르고 있습니다.");
            }
        }

    }

    [PunRPC]
    public void ReturnUnselectedCard(int selectedCard)
    {
        int max = PhotonNetwork.CurrentRoom.MaxPlayers;
        for (int i = 1; i < max + 1; i++)
        {
            if (i != selectedCard)
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

    public void SetChoice(int mychoice) // 0: 식사, 1: 강탈
    {
        if ((int)Turn["currentTurn"] == 3 && ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber))
        {
            choice = mychoice;
            photonView.RPC("UpdateVariable", RpcTarget.All, mychoice, 0);

            gamestatustxt.text = "제출 완료! 상대의 선택을 기다립니다.";
        }
        else if ((int)Turn["currentTurn"] == 3 && (int)Turn["pickedPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            choice2 = mychoice;
            photonView.RPC("UpdateVariable", RpcTarget.All, mychoice, 1);

            gamestatustxt.text = "제출 완료!! 상대의 선택을 기다립니다.";
        }
        else
        {
            gamestatustxt.text = "식사를 진행하는 동안 잠시 기다려 주세요.";
        }

        if (choice != -1 && choice2 != -1) SettleChoice(choice, choice2); // 선택 두개가 모이면 실행

    }

    void SettleChoice(int choice, int choice2)
    {
        int score = 0;
        int currentPlayerNum = (int)Turn["currentPlayerIndex"];
        int pickedPlayerNum = (int)Turn["pickedPlayerIndex"];
        Debug.Log($"pickedPlayerIndex : {(int)Turn["pickedPlayerIndex"]}");
        switch (choice, choice2)
        {
            case (0, 0): // 식사
                // score = (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] + (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["pickedPlayerIndex"]].CustomProperties["selectedFoodCard"];
                photonView.RPC("CalculateScore", RpcTarget.All, pickedPlayerNum, (int)PhotonNetwork.CurrentRoom.Players[pickedPlayerNum].CustomProperties["selectedFoodCard"]);
                photonView.RPC("CalculateScore", RpcTarget.All, currentPlayerNum, (int)PhotonNetwork.CurrentRoom.Players[currentPlayerNum].CustomProperties["selectedFoodCard"]);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, (int)Turn["pickedPlayerIndex"], (int)Turn["currentPlayerIndex"]);
                photonView.RPC("UpdateMedal", RpcTarget.All, 0, currentPlayerNum);
                photonView.RPC("UpdateMedal", RpcTarget.All, 0, pickedPlayerNum);

                break;
            case (0, 1):
                score = Mathf.Abs((int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] - (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["pickedPlayerIndex"]].CustomProperties["selectedFoodCard"]);
                photonView.RPC("CalculateScore", RpcTarget.All, pickedPlayerNum, score);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, (int)Turn["pickedPlayerIndex"], (int)Turn["currentPlayerIndex"]);
                photonView.RPC("UpdateMedal", RpcTarget.All, 1, pickedPlayerNum);
                break;
            case (1, 0):
                score = Mathf.Abs((int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] - (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["pickedPlayerIndex"]].CustomProperties["selectedFoodCard"]);
                photonView.RPC("CalculateScore", RpcTarget.All, currentPlayerNum, score);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, (int)Turn["pickedPlayerIndex"], (int)Turn["currentPlayerIndex"]);
                photonView.RPC("UpdateMedal", RpcTarget.All, 1, currentPlayerNum);
                break;
            case (1, 1): // 쓰레기통
                Trash.Add((int)PhotonNetwork.CurrentRoom.Players[(int)Turn["pickedPlayerIndex"]].CustomProperties["selectedFoodCard"]);
                Trash.Add((int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"]);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, (int)Turn["pickedPlayerIndex"], (int)Turn["currentPlayerIndex"]);
                break;
            default:
                Debug.Log("Error: Invalid choice.");
                break;
        }
        photonView.RPC("CountOtherFoodCard", RpcTarget.All);
        photonView.RPC("ReturnCardToHand", RpcTarget.All);
        photonView.RPC("ResetAllCardBacks", RpcTarget.All);
        photonView.RPC("Start", RpcTarget.All);
        // 턴 초기화

    }
    [PunRPC]
    public void UpdateVariable(int value, int playernum)
    {
        if (playernum == 0) choice = value;
        else if (playernum == 1) choice2 = value;
    }

    [PunRPC]
    public void ReturnCardToHand()
    {
        if ((int)Turn["pickedPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber || (int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            playerData.playerHand.Remove((FoodCard.CardPoint)PhotonNetwork.LocalPlayer.CustomProperties["selectedFoodCard"]);
        }
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
        if (score[PhotonNetwork.LocalPlayer.ActorNumber - 1] >= 18 && (int)player["grade"] == 0)
        {
            photonView.RPC("SyncScore", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 0);
            player["grade"] = 1;
            photonView.RPC("SetGrade", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 1);
            gamestatustxt.text = "용이 자라났다!";

        }
        else if (score[PhotonNetwork.LocalPlayer.ActorNumber - 1] >= 24 && (int)player["grade"] == 1)
        {
            photonView.RPC("SyncScore", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 0);
            player["grade"] = 2;
            photonView.RPC("SetGrade", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 2);
            gamestatustxt.text = "크와아아앙!!";

        }
        else if (score[PhotonNetwork.LocalPlayer.ActorNumber - 1] >= 45 && (int)player["grade"] == 2)
        {
            photonView.RPC("SyncScore", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 0);
            player["grade"] = 3;
            gamestatustxt.text = "용이 자라났다!";

        }
        else if ((int)player["grade"] == 3 && score[PhotonNetwork.LocalPlayer.ActorNumber - 1] >= 45)
        {
            gamestatustxt.text = "우승!";
            return;
        }

        Debug.Log($"현재 내 점수 : {score[PhotonNetwork.LocalPlayer.ActorNumber - 1]}, 다른 플레이어 점수 : {score[0]}, {score[1]}, {score[2]}, {score[3]}, {score[4]}, {score[5]}");
        PhotonNetwork.LocalPlayer.SetCustomProperties(player);
        
        //CountMyFoodCard((FoodCard.CardPoint)PhotonNetwork.LocalPlayer.CustomProperties["selectedFoodCard"]);
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


    public void FoodCardSelect(FoodCard.CardPoint cardPoint)
    {
        if (cardPoint != FoodCard.CardPoint.tablecard)
        {
            if (playerData.Submited == false)
            {
                player["selectedFoodCard"] = cardPoint;
            }
            else
            {
                Debug.Log("이미 카드를 제출했습니다.");
                Debug.Log($"선택된 카드 : {player["selectedFoodCard"]}");
            }
            PhotonNetwork.LocalPlayer.SetCustomProperties(player);
        }
        else
        {
            Debug.Log("테이블 카드는 선택할 수 없습니다.");
        }
    }

    public void TableCardSelect(int playernum)
    {

        if ((int)Turn["currentTurn"] == 2 && (int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Turn["pickedPlayerIndex"] = playernum;
            gamestatustxt.text = $"{playernum}번 플레이어와 식사하려면 제출 버튼을 누르세요.";
        }
        else
        {
            gamestatustxt.text = $"{playernum}번 플레이어의 음식입니다.";
        }
    }


    private bool FoodSubmited()
    {
        if ((int)Turn["foodSubmited"] >= PhotonNetwork.CurrentRoom.MaxPlayers - 1) return true;
        else return false;
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

    public void TrashGotcha()
    {
        if (Trash.Count == 0)
        {
            Debug.LogWarning("Trash 리스트가 비어있어서 뽑기를 할 수 없습니다.");
            return;
        }

        System.Random random = new System.Random();
        int index = random.Next(Trash.Count);
        int luck = (int)Trash[index];

        int player = 1 + random.Next(PhotonNetwork.LocalPlayer.ActorNumber);

        photonView.RPC("recTrash", RpcTarget.All, player, luck);

        Debug.Log($"쓰레기통에서 뽑힌 카드 : {luck}, 플레이어 : {player}");
    }

    [PunRPC]
    public void recTrash(int playernum, int scoreamount)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == playernum)
        {
            photonView.RPC("CalculateScore", RpcTarget.All, playernum, scoreamount);
        }

        gamestatustxt.text = $"{player}번 플레이어가 쓰레기통을 차지합니다.";
        Trash.Clear(); // 쓰레기통 비우기
    }

  [PunRPC]
    public void CountOtherFoodCard()
    {
        int currentPlayerNum = (int)Turn["currentPlayerIndex"];
        int pickedPlayerNum = (int)Turn["pickedPlayerIndex"];

        Debug.Log($"선플 픽 : {PhotonNetwork.CurrentRoom.Players[currentPlayerNum].CustomProperties["selectedFoodCard"]}");
        Debug.Log($"후플 픽 : {PhotonNetwork.CurrentRoom.Players[pickedPlayerNum].CustomProperties["selectedFoodCard"]}");

        if (PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerNum)
        {
            int starterIndex = others.IndexOf(pickedPlayerNum);
            SetCardToBack(handCards[starterIndex].cards[GetSpriteIndex((int)PhotonNetwork.CurrentRoom.Players[pickedPlayerNum].CustomProperties["selectedFoodCard"])]);
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerNum)
        {
            int starterIndex = others.IndexOf(currentPlayerNum);
            SetCardToBack(handCards[starterIndex].cards[GetSpriteIndex((int)PhotonNetwork.CurrentRoom.Players[currentPlayerNum].CustomProperties["selectedFoodCard"])]);
        }
        else
        {
            int starterIndex = others.IndexOf(currentPlayerNum);
            int starterIndex2 = others.IndexOf(pickedPlayerNum);
            SetCardToBack(handCards[starterIndex2].cards[GetSpriteIndex((int)PhotonNetwork.CurrentRoom.Players[pickedPlayerNum].CustomProperties["selectedFoodCard"])]);
            SetCardToBack(handCards[starterIndex].cards[GetSpriteIndex((int)PhotonNetwork.CurrentRoom.Players[currentPlayerNum].CustomProperties["selectedFoodCard"])]);
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

        // Debug.Log($"[SYNC] Turn 동기화됨: currentTurn = {Turn["currentTurn"]}, currentPlayerIndex = {Turn["currentPlayerIndex"]}, pickedPlayerIndex = {Turn["pickedPlayerIndex"]}, foodSubmited = {Turn["foodSubmited"]}");
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
    public void SendCardInfo(int playernum, PlayerData handData)
    {
        playerDatas.Add(handData);

        Debug.Log($"{playernum}님 체크");
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
            if (pl.playerHand.Count != 0) return false;
        }
        return true;
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