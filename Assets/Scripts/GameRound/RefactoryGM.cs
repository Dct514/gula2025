using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Pun;
using TMPro;

public class RefactoryGM : MonoBehaviour
{
    public static RefactoryGM Instance;
    public TMP_Text gamestatustxt;
    ExitGames.Client.Photon.Hashtable Score = new ExitGames.Client.Photon.Hashtable(); // 플레이어의 커스텀 프로퍼티를 저장할 해시테이블
    ExitGames.Client.Photon.Hashtable Turn = new ExitGames.Client.Photon.Hashtable(); // 플레이어의 커스텀 프로퍼티를 저장할 해시테이블
    ExitGames.Client.Photon.Hashtable Choice = new ExitGames.Client.Photon.Hashtable(); // 플레이어의 커스텀 프로퍼티를 저장할 해시테이블
    public class PlayerData
    {
        public int playerNumber;
        public bool submited
        {
            get { return submited; }
            set { submited = value; }
        }

    }
    public PlayerData playerData = new PlayerData();
    void Start()
    {
        for (int i = 0; i < PhotonNetwork.CurrentRoom.MaxPlayers; i++)
        {
            Score["player" + (i + 1)] = 0;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            Turn["currentPlayerIndex"] = UnityEngine.Random.Range(1, PhotonNetwork.CurrentRoom.PlayerCount + 1);
            Turn["currentTurn"] = 0;
            
            PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
            PhotonNetwork.CurrentRoom.SetCustomProperties(Score);
        }
        Choice["foodSubmited"] = 0;

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
        Score["player" + playerNumber] = score;
        PhotonNetwork.CurrentRoom.SetCustomProperties(Score);
    }

    public void MainTurnStart(FoodCard.CardPoint cardPoint)
    {
        switch (Turn["currentTurn"])
        {
            case 0:
                // 선 플레이어만 진행하는, 첫 카드를 내는 턴
                if ((int)Turn["currentPlayerIndex"] == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    Turn["currentTurn"] = 1;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(Turn);
                    Debug.Log($"현재 턴 : {Turn["currentTurn"]}");
                }
                else
                {
                    gamestatustxt.text = "차례가 아닙니다.";
                }
                break;
            case 1:
                // 다른 플레이어들이 음식 카드 또는 거절 카드를 내는 턴
                UpdateScore(2, (int)cardPoint);
                if (foodSubmited()) break;
                break;
            case 2:
                // 선 플레이어만 진행, 다른 플레이어의 카드를 고르는 턴
                UpdateScore(3, (int)cardPoint);
                break;
            case 3:
                // 선택된 플레이어와 선 플레이어가 식사 / 강탈을 고르는 턴
                UpdateScore(4, (int)cardPoint);
                break;
            default :
                Debug.Log("Error: Invalid turn number.");
                break;
        }
    }

    private bool foodSubmited(){
        if ((int)Choice["foodSubmited"] == 4) return true;
        else return false;
    }


}