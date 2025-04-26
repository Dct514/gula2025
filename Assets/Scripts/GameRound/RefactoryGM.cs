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
    ExitGames.Client.Photon.Hashtable player = new ExitGames.Client.Photon.Hashtable();
    ExitGames.Client.Photon.Hashtable Turn = new ExitGames.Client.Photon.Hashtable();
    public List<FoodCard> Trash = new List<FoodCard>();
    public List<int> others = new List<int>();
    public List<(int,int)> currentTurnProcess = new List<(int,int)>();
    public int choice = -1;
    public int choice2 = -1;
    public int[] score = new int[6] { 0, 0, 0, 0, 0, 0 };
    public List<HandCard> handCards = new List<HandCard>(); // 각 플레이어의 카드들
    public class PlayerData
    {
        public int playerNumber;
        public bool Submited { get; set; }

    }
    public PlayerData playerData = new PlayerData();
    void Start()
    {
        choice = -1;
        choice2 = -1;
        
        if (PhotonNetwork.IsMasterClient)
        {
            if (Turn["currentPlayerIndex"] == null) Turn["currentPlayerIndex"] = 1;
            if ((int)Turn["currentPlayerIndex"] == 0) Turn["currentPlayerIndex"] = UnityEngine.Random.Range(1, PhotonNetwork.CurrentRoom.PlayerCount + 1);
            else {
                if ((int)Turn["currentPlayerIndex"] >= PhotonNetwork.CurrentRoom.MaxPlayers) Turn["currentPlayerIndex"] = 1;
                else Turn["currentPlayerIndex"] = (int)Turn["currentPlayerIndex"] + 1;
            }
            
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
        
        Debug.Log($"현재 턴 : {Turn["currentTurn"]}");
        Debug.Log($"현재 플레이어 : {Turn["currentPlayerIndex"]}");
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
        
        player["grade"] = 0;
        player["score"] = 0;
        PhotonNetwork.LocalPlayer.SetCustomProperties(player);
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
                SetGameStatusText("선플레이어와 식사할 사람은 카드를 제출하세요.");

                break;
                
            case 1:
                // 다른 플레이어들이 음식 카드 또는 거절 카드를 내는 턴
                // 같은 플레이어와 2번 턴을 진행한 플레이어는 턴을 진행할 수 없음
           
                if (!BlockPlayerTurn((int)Turn["currentPlayerIndex"], PhotonNetwork.LocalPlayer.ActorNumber) && 
                playerData.Submited == false && 
                player["selectedFoodCard"] != null && 
                (int)Turn["currentPlayerIndex"] != PhotonNetwork.LocalPlayer.ActorNumber && 
                PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] != null)
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
                    }
                }
                else if (player["selectedFoodCard"] == null)
                {
                    gamestatustxt.text = "카드를 선택해주세요.";
                }
                else if ((int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] == (int)player["selectedFoodCard"])
                {
                    gamestatustxt.text = "같은 카드를 선택할 수 없습니다.";
                }
                else if ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    gamestatustxt.text = "다른 플레이어가 카드를 제출할 때까지 기다려주세요.";
                }
                else if (playerData.Submited == true)
                {
                    gamestatustxt.text = "이미 카드를 제출했습니다.";
                }
                else if (!BlockPlayerTurn((int)Turn["currentPlayerIndex"], PhotonNetwork.LocalPlayer.ActorNumber))
                {
                    gamestatustxt.text = "이 플레이어와 2번 식사를 완료했습니다.";
                }
                gamestatustxt.text = "선플레이어는 같이 식사할 사람을 고르세요.";

                Debug.Log($"현재 턴 : {Turn["currentTurn"]}");
                break;
 
            case 2:
                // 선 플레이어만 진행, 다른 플레이어의 카드를 고르는 턴
                if ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber && (int)Turn["pickedPlayerIndex"] != 0 && (int)Turn["pickedPlayerIndex"] != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    Turn["currentTurn"] = 3;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
                    // TODO : 카드 돌려주기
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
                score = (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] + (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["pickedPlayerIndex"]].CustomProperties["selectedFoodCard"];
                photonView.RPC("CalculateScore", RpcTarget.All, pickedPlayerNum, score);
                photonView.RPC("CalculateScore", RpcTarget.All, currentPlayerNum, score);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, (int)Turn["pickedPlayerIndex"], (int)Turn["currentPlayerIndex"]);
                break;
            case (0,1):
                score = Mathf.Abs((int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] - (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["pickedPlayerIndex"]].CustomProperties["selectedFoodCard"]);
                photonView.RPC("CalculateScore", RpcTarget.All, pickedPlayerNum, score);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, (int)Turn["pickedPlayerIndex"], (int)Turn["currentPlayerIndex"]);                    
                break;
            case (1,0):
                score = Mathf.Abs((int)PhotonNetwork.CurrentRoom.Players[(int)Turn["currentPlayerIndex"]].CustomProperties["selectedFoodCard"] - (int)PhotonNetwork.CurrentRoom.Players[(int)Turn["pickedPlayerIndex"]].CustomProperties["selectedFoodCard"]);
                photonView.RPC("CalculateScore", RpcTarget.All, currentPlayerNum, score);
                photonView.RPC("UpdatePlayerTurn", RpcTarget.All, (int)Turn["pickedPlayerIndex"], (int)Turn["currentPlayerIndex"]);
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
            // 턴 초기화
    
}
[PunRPC]
    public void UpdateVariable(int value, int playernum)
    {
        if (playernum == 0) choice = value;
        else if (playernum == 1) choice2 = value;
    }

[PunRPC]
    public void RoundResetCheck()
    {
        if (score[PhotonNetwork.LocalPlayer.ActorNumber-1] >= 18 && (int)player["grade"] == 0)
        {
            score[PhotonNetwork.LocalPlayer.ActorNumber-1] = 0;
            player["grade"] = 1;

        }
        else if (score[PhotonNetwork.LocalPlayer.ActorNumber-1] >= 24 && (int)player["grade"] == 1)
        {
            score[PhotonNetwork.LocalPlayer.ActorNumber-1] = 0;
            player["grade"] = 2;

        }        
        else if (score[PhotonNetwork.LocalPlayer.ActorNumber-1] >= 45 && (int)player["grade"] == 2)
        {
            score[PhotonNetwork.LocalPlayer.ActorNumber-1] = 0;
            player["grade"] = 3;

        }
        else if ((int)player["grade"] == 3 && score[PhotonNetwork.LocalPlayer.ActorNumber-1] >= 45 ) 
        {
            gamestatustxt.text = "우승";
            return;
        }
        Debug.Log($"현재 내 점수 : {player["score"]}, 다른 플레이어 점수 : {score[0]}, {score[1]}, {score[2]}, {score[3]}, {score[4]}, {score[5]}");
            PhotonNetwork.LocalPlayer.SetCustomProperties(player);
        Start();
    }

[PunRPC]
    public void CalculateScore(int playerNum, int scoreamount)
    {
        score[playerNum-1] += scoreamount;
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

    public void CountOtherFoodCard() // selectedFoodCard[actnum-1] 가져옴 - 다른 플레이어의 음식카드 표시(흑백)
    {
        int currentPlayerNum = (int)Turn["currentPlayerIndex"];
        int pickedPlayerNum = (int)Turn["pickedPlayerIndex"];
        if(PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerNum)
        {
            int starterIndex = others.IndexOf(pickedPlayerNum);
            handCards[starterIndex].cards[GetSpriteIndex((int)PhotonNetwork.CurrentRoom.Players[pickedPlayerNum].CustomProperties["selectedFoodCard"])].gameObject.SetActive(false);
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == pickedPlayerNum)
        {
            int starterIndex = others.IndexOf(currentPlayerNum);
            handCards[starterIndex].cards[GetSpriteIndex((int)PhotonNetwork.CurrentRoom.Players[currentPlayerNum].CustomProperties["selectedFoodCard"])].gameObject.SetActive(false);

        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber != currentPlayerNum && PhotonNetwork.LocalPlayer.ActorNumber != pickedPlayerNum)
        {
            int starterIndex = others.IndexOf(currentPlayerNum);
            int starterIndex2 = others.IndexOf(pickedPlayerNum);
            handCards[starterIndex].cards[GetSpriteIndex((int)PhotonNetwork.CurrentRoom.Players[pickedPlayerNum].CustomProperties["selectedFoodCard"])].gameObject.SetActive(false);
            handCards[starterIndex].cards[GetSpriteIndex((int)PhotonNetwork.CurrentRoom.Players[currentPlayerNum].CustomProperties["selectedFoodCard"])].gameObject.SetActive(false);

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

            if (currentTurnProcess.Contains(key) && currentTurnProcess.Count(p => p.Item1 == min && p.Item2 == max) > 2) return true;
            else return false;

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

    
    public void DeselectAllCards()
    {
        CardOutlineController[] allCards = FindObjectsOfType<CardOutlineController>();
        foreach (var card in allCards)
        {
            card.DeselectCard();
        }
    }
}