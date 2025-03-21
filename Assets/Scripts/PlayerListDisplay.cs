using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListUI : MonoBehaviourPunCallbacks
{
    public TMP_Text[] playerSlots; // 왼쪽부터 오른쪽으로 닉네임을 표시할 Text 배열
    public TMP_Text[] score;

    private void Start()
    {
        UpdatePlayerList();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

private void UpdatePlayerList()
{
    if (!PhotonNetwork.InRoom)
        return;
    
    List<Player> sortedPlayers = PhotonNetwork.PlayerList
        .Where(p => p.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber) // 자신 제외
        .OrderBy(p => p.ActorNumber) // ActorNumber가 낮은 순으로 정렬
        .ToList();
    
    for (int i = 0; i < playerSlots.Length; i++)
    {
        if (i < sortedPlayers.Count)
        {
            playerSlots[i].text = sortedPlayers[i].NickName;
            score[i].text = "점수 :" +GetPlayerScore(i).ToString(); // 점수 업데이트
        }
        else
        {
            playerSlots[i].text = "-"; // 빈 자리 표시
            score[i].text = "-"; // 빈 자리 점수 표시
        }
    }
}

private int GetPlayerScore(int index)
{
    // GameManager의 Score 배열에서 점수를 가져오는 로직
    return GameManager.Instance.score[index];
}
}
