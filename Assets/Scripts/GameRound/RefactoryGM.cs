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

public class RefactoryGM : MonoBehaviourPunCallbacks
{
    public static RefactoryGM Instance;
    public TMP_Text gamestatustxt;
    public TMP_Text[] scoreTexts; // 점수 표시될 곳
    public Sprite[] cardSprites; // 카드 스프라이트
    public Image[] cardImages; // 카드 스프라이트 표시될 곳
    public TMP_Text[] goldTexts; // 골드 표시될 곳
    public TMP_Text[] silverTexts; // 실버 표시될 곳
    public TMP_Text myScoreText;
    public TMP_Text myGoldText;   
    public TMP_Text mySilverText;
    public TMP_Text myNickNameText;
    ExitGames.Client.Photon.Hashtable player = new ExitGames.Client.Photon.Hashtable();
    public ExitGames.Client.Photon.Hashtable Turn = new ExitGames.Client.Photon.Hashtable();
    public List<FoodCard> Trash = new List<FoodCard>();
    public List<int> others = new List<int>();
    public List<(int,int)> currentTurnProcess = new List<(int,int)>();
    public int choice = -1;
    public int choice2 = -1;
    public int checkFoodCard = 0; // 카드 선택 확인용
    public int[] gold = new int[6] { 0, 0, 0, 0, 0, 0 };
    public int[] silver = new int[6] { 0, 0, 0, 0, 0, 0 };
    public int[] score = new int[6] { 0, 0, 0, 0, 0, 0 };
    public Image[] myImages;
    public List<HandCard> handCards = new List<HandCard>(); // 각 플레이어의 카드들
    public List<Grade> gradeImages = new List<Grade>();
    public Sprite backSprite;
    public Sprite[] gradeSprite;
    public Image[] myGradeImage;


    public class PlayerData
    {
        public int playerNumber;
        public bool Submited { get; set; }

    }
    public List<FoodCard.CardPoint> playerHand = new List<FoodCard.CardPoint>
    {

    };
    public PlayerData playerData = new PlayerData();
    void Start()
    {
        choice = -1;
        choice2 = -1;
        
        if (PhotonNetwork.IsMasterClient)
        {
            if ((int)Turn["currentPlayerIndex"] >= PhotonNetwork.CurrentRoom.MaxPlayers) Turn["currentPlayerIndex"] = 1;
            else Turn["currentPlayerIndex"] = (int)Turn["currentPlayerIndex"] + 1;
            photonView.RPC("SetGameStatusText", RpcTarget.All, $"{PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].NickName}님의 차례입니다.");
            
            Turn["currentTurn"] = 0;
            Turn["pickedPlayerIndex"] = 0;
            Turn["foodSubmited"] = 0;
            
            
            PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
        }

        playerData.Submited = false;
        player["selectedFoodCard"] = 0;
        player["choice"] = -1;
        Debug.Log($"현재 내 점수 : {player["score"]}");
        PhotonNetwork.LocalPlayer.SetCustomProperties(player);

        for (int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount-1; i++)
        {
            scoreTexts[i].text = $"{score[others[i]-1]}";
        }
        
        for (int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount + 1; i++)
        {
            SetCardValue(0, i);
        }

        for (int i = 0; i < PhotonNetwork.CurrentRoom.PlayerCount-1; i++)
        {
            goldTexts[i].text = $"{gold[others[i]-1]}";
            silverTexts[i].text = $"{silver[others[i]-1]}";
        }

        myScoreText.text = $"{score[PhotonNetwork.LocalPlayer.ActorNumber-1]}";
        myGoldText.text = $"{gold[PhotonNetwork.LocalPlayer.ActorNumber-1]}";
        mySilverText.text = $"{silver[PhotonNetwork.LocalPlayer.ActorNumber-1]}";
        
        Debug.Log($"현재 턴 : {Turn["currentTurn"]}");
        Debug.Log($"현재 플레이어 : {Turn["currentPlayerIndex"]}");

        // todo :



        // 만약 모든 플레이어와 2번씩 식사를 마쳤거나, 더 이상 식사를 할 수 없는 상황(식사하지 않은 플레이어가 - 동일 카드만 남은 경우) - 턴 넘어가기
        // 모든 플레이어가 카드 소모 완료한 경우 - 카드 리셋
        // 이후 점수 로직으로 소프트 리셋

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
        if (PhotonNetwork.IsMasterClient)
        {
            Turn["currentPlayerIndex"] = UnityEngine.Random.Range(0, PhotonNetwork.CurrentRoom.PlayerCount-1);
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
                // 선 플레이어만 진행하는, 첫 카드를 내는 턴
                if ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber && (int)player["selectedFoodCard"] != 0)
                {
                    playerData.Submited = true;
                    Turn["currentTurn"] = 1;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
                    photonView.RPC("SetCardValue", RpcTarget.All, (int)player["selectedFoodCard"], 0);

                    Debug.Log($"현재 턴 : {Turn["currentTurn"]}");
                }
                else
                {
                    gamestatustxt.text = "차례가 아닙니다.";
                }
                photonView.RPC("SetGameStatusText", RpcTarget.All, "선 플레이어와 식사할 사람은 카드를 제출하세요.");

                break;
                
            case 1:
                // 다른 플레이어들이 음식 카드 또는 거절 카드를 내는 턴
                // 같은 플레이어와 2번 턴을 진행한 플레이어는 턴을 진행할 수 없음
           
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
                        Turn["currentTurn"] = 2;
                        PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
                        photonView.RPC("SetGameStatusText", RpcTarget.All, "선 플레이어가 같이 식사할 사람을 고르고 있습니다.");
                    }
                }
                else if ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    gamestatustxt.text = "다른 플레이어가 카드를 제출할 때까지 기다려주세요.";
                }
                else if ((int)player["selectedFoodCard"] == 0)
                {
                    gamestatustxt.text = "카드를 선택해주세요.";
                }
                else if ((int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] == (int)player["selectedFoodCard"])
                {
                    gamestatustxt.text = "같은 카드를 선택할 수 없습니다.";
                }
                else if (playerData.Submited == true)
                {
                    gamestatustxt.text = "이미 카드를 제출했습니다.";
                }
                else if (!BlockPlayerTurn((int)Turn["currentPlayerIndex"], PhotonNetwork.LocalPlayer.ActorNumber))
                {
                    gamestatustxt.text = "이 플레이어와 2번 식사를 완료했습니다.";
                }


                Debug.Log($"현재 턴 : {Turn["currentTurn"]}");
                break;
 
            case 2:
                // 선 플레이어만 진행, 다른 플레이어의 카드를 고르는 턴
                if ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber && (int)Turn["pickedPlayerIndex"] != 0 && (int)Turn["pickedPlayerIndex"] != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    Turn["currentTurn"] = 3;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
                    photonView.RPC(" ReturnUnselectedCard", RpcTarget.All, (int)Turn["pickedPlayerIndex"]);
                    photonView.RPC("SetGameStatusText", RpcTarget.All, $"{PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].NickName}님과 {PhotonNetwork.CurrentRoom.Players[(int)Turn["pickedPlayerIndex"]].NickName}님이 식사합니다.");
                }
                else
                {
                    gamestatustxt.text = "선 플레이어가 카드를 고르고 있습니다.";
                }

                Debug.Log($"현재 턴 : {Turn["currentTurn"]}");

                break;
            case 3:
                // 선택된 플레이어와 선 플레이어가 식사 / 강탈을 고르는 턴
                Debug.Log($"현재 턴 : {Turn["currentTurn"]}");
                break;
            default :
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
        int a = (int)Turn["currentTurn"];
        int max = PhotonNetwork.CurrentRoom.MaxPlayers;
        for (int i = 1; i < max+1; i++)
        {
            if (i!=a && i != selectedCard)
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
            case (0,0): // 식사
                // score = (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] + (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["pickedPlayerIndex"]].CustomProperties["selectedFoodCard"];
                photonView.RPC("CalculateScore", RpcTarget.All, pickedPlayerNum, (int)PhotonNetwork.CurrentRoom.Players[pickedPlayerNum].CustomProperties["selectedFoodCard"]);
                photonView.RPC("CalculateScore", RpcTarget.All, currentPlayerNum, (int)PhotonNetwork.CurrentRoom.Players[currentPlayerNum].CustomProperties["selectedFoodCard"]);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, (int)Turn["pickedPlayerIndex"], (int)Turn["currentPlayerIndex"]);
                photonView.RPC("UpdateMedal", RpcTarget.All, 0, currentPlayerNum);
                photonView.RPC("UpdateMedal", RpcTarget.All, 0, pickedPlayerNum);

                break;
            case (0,1):
                score = Mathf.Abs((int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] - (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["pickedPlayerIndex"]].CustomProperties["selectedFoodCard"]);
                photonView.RPC("CalculateScore", RpcTarget.All, pickedPlayerNum, score);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, (int)Turn["pickedPlayerIndex"], (int)Turn["currentPlayerIndex"]);
                photonView.RPC("UpdateMedal", RpcTarget.All, 1, pickedPlayerNum);              
                break;
            case (1,0):
                score = Mathf.Abs((int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] - (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["pickedPlayerIndex"]].CustomProperties["selectedFoodCard"]);
                photonView.RPC("CalculateScore", RpcTarget.All, currentPlayerNum, score);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, (int)Turn["pickedPlayerIndex"], (int)Turn["currentPlayerIndex"]);
                photonView.RPC("UpdateMedal", RpcTarget.All, 1, currentPlayerNum);
                break;
            case (1,1): // 쓰레기통
                Trash.Add((FoodCard)PhotonNetwork.CurrentRoom.Players[(int)Turn["pickedPlayerIndex"]].CustomProperties["selectedFoodCard"]);
                Trash.Add((FoodCard)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"]);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, (int)Turn["pickedPlayerIndex"], (int)Turn["currentPlayerIndex"]);
                break; 
            default:
                Debug.Log("Error: Invalid choice.");
                break;
        }
        photonView.RPC("RoundResetCheck", RpcTarget.All);
        photonView.RPC("ResetAllCardBacks", RpcTarget.All);
        // 턴 초기화

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
        if (goldorSilver == 0) gold[playernum-1] += 1;
        else if (goldorSilver == 1) silver[playernum-1] += 1;
    }

[PunRPC]
    public void RoundResetCheck()
    {
        if (score[PhotonNetwork.LocalPlayer.ActorNumber-1] >= 18 && (int)player["grade"] == 0)
        {
            photonView.RPC("SyncScore", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 0);
            player["grade"] = 1;
            photonView.RPC("SetGrade", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 1);
            gamestatustxt.text = "용이 자라났다!";

        }
        else if (score[PhotonNetwork.LocalPlayer.ActorNumber-1] >= 24 && (int)player["grade"] == 1)
        {
            photonView.RPC("SyncScore", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 0);
            player["grade"] = 2;
            photonView.RPC("SetGrade", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 2);
            gamestatustxt.text = "크와아아앙!!";

        }        
        else if (score[PhotonNetwork.LocalPlayer.ActorNumber-1] >= 45 && (int)player["grade"] == 2)
        {
            photonView.RPC("SyncScore", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, 0);
            player["grade"] = 3;
            gamestatustxt.text = "용이 자라났다!";

        }
        else if ((int)player["grade"] == 3 && score[PhotonNetwork.LocalPlayer.ActorNumber-1] >= 45 ) 
        {
            gamestatustxt.text = "우승!";
            return;
        }

        Debug.Log($"현재 내 점수 : {score[PhotonNetwork.LocalPlayer.ActorNumber-1]}, 다른 플레이어 점수 : {score[0]}, {score[1]}, {score[2]}, {score[3]}, {score[4]}, {score[5]}");
        PhotonNetwork.LocalPlayer.SetCustomProperties(player);
        CountOtherFoodCard();
        CountMyFoodCard((FoodCard.CardPoint)PhotonNetwork.LocalPlayer.CustomProperties["selectedFoodCard"]);
       
        Start();
    }

    [PunRPC]
    public void SetGrade(int playernum, int grade)
    {
      if (playernum == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            myGradeImage[grade - 1].sprite = gradeSprite[1];
        }
      else if (grade == 1)
        {
            int currentIndex = others.IndexOf(playernum);
            gradeImages[currentIndex].gradeImage[0].sprite = gradeSprite[1];
        }
      else if (grade == 2)
        {
            int currentIndex = others.IndexOf(playernum);
            gradeImages[currentIndex].gradeImage[1].sprite = gradeSprite[1];
        }
    }

    [PunRPC]
    public void CalculateScore(int playerNum, int scoreamount)
    {
        score[playerNum-1] += scoreamount;
        
    }
    [PunRPC]
    public void SyncScore(int playerNum, int myscore)
    {
        score[playerNum-1] = myscore;
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
        PhotonNetwork.LocalPlayer.SetCustomProperties(player);
        Debug.Log($"선택된 카드 : {player["selectedFoodCard"]}");
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
        if ((int)Turn["foodSubmited"] >= PhotonNetwork.CurrentRoom.MaxPlayers-1) return true;
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
            case 5: return 3;
            case 7: return 4;
            case 10: return 5;
            case 0 : return 6;
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

    public void WholeRoundOver()
    {

        Trash.AddRange(handCards);
        handCards.Clear();
        playerHand.Add(FoodCard.CardPoint.Bread);
        playerHand.Add(FoodCard.CardPoint.Soup);
        playerHand.Add(FoodCard.CardPoint.Fish);
        playerHand.Add(FoodCard.CardPoint.Steak);
        playerHand.Add(FoodCard.CardPoint.Turkey);
        playerHand.Add(FoodCard.CardPoint.Cake);

    }

    [PunRPC] // 마스터 플레이어만 실행, 그 이후 Trash 동기화, Trash에서 랜덤으로 카드 뽑기, Trash 비우기
    public void TrashScoring(int playernum, int cardnum)
    {
        Trash.AddRange(handCards);
    }

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


    public void CountMyFoodCard(FoodCard.CardPoint cardPoint)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == (int)Turn["currentPlayerIndex"] || PhotonNetwork.LocalPlayer.ActorNumber == (int)Turn["pickedPlayerIndex"])
        {
            if (cardPoint != FoodCard.CardPoint.deny && playerHand.Contains(cardPoint))
            {
                playerHand.Remove(cardPoint);
            }
        }

        foreach (Image card in myImages)
        {
            FoodCard.CardPoint cardType = card.GetComponent<FoodCard>().cardPoint;

            if (!playerHand.Contains(cardType))
            {
                SetCardToBack(card);
            }
            else
            {
                card.sprite = cardSprites[GetSpriteIndex((int)cardType)];
                card.color = new Color(1f, 1f, 1f, 1f); // 원래 밝게 복구
            }
        }
        Debug.Log($"CountMyFoodCard : {cardPoint}");
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

            if (currentTurnProcess.Contains(key) && currentTurnProcess.Count(p => p.Item1 == min && p.Item2 == max) > 2) return true;
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

        Debug.Log($"[SYNC] Turn 동기화됨: currentTurn = {Turn["currentTurn"]}, currentPlayerIndex = {Turn["currentPlayerIndex"]}, pickedPlayerIndex = {Turn["pickedPlayerIndex"]}, foodSubmited = {Turn["foodSubmited"]}");
    }

  public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("selectedFoodCard"))
        {
            int selectedFoodCard = (int)changedProps["selectedFoodCard"];
            Debug.Log($"[SYNC] {targetPlayer.NickName}의 selectedFoodCard 동기화됨: {selectedFoodCard}");
        }
        {
           
        }
    }
    
    public bool Isbully()
    {
        int a= 0;
       for (int i = 1; i < PhotonNetwork.CurrentRoom.MaxPlayers+1; i++)
        {
            for (int j =)
           if ((int)PhotonNetwork.CurrentRoom.Players[i].CustomProperties["selectedFoodCard"] != 0)
            {
               a++;
            }
        }

        if (a==0) return true;
        else return false;
        
    }

    public void CheckcurrentTurnProcess()
    {
        int turnCount;

        int maxPlayer = PhotonNetwork.CurrentRoom.MaxPlayers;
        for (int i = 1; i < maxPlayer+1; i++)
        {
            if (BlockPlayerTurn((int)Turn["currentTurn"], i)) turnCount++;
        }

        if (turnCount == maxPlayer-1) // todo : 턴 넘기기
    }
    
    public void DeselectAllCards()
    {
        CardOutlineController[] allCards = FindObjectsOfType<CardOutlineController>();
        foreach (var card in allCards)
        {
            card.DeselectCard();
        }
    }
}