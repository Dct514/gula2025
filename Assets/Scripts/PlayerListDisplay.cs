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
            }
            else
            {
                playerSlots[i].text = "-"; // 빈 자리 표시
            }
        }
    }
}
