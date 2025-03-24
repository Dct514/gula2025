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
    public TMP_Text[] scoretxt;

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

public void UpdatePlayerList()
{
    Debug.Log("UpdatePlayerList");
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
            scoretxt[i].text = $"점수 : {GetPlayerScore(sortedPlayers[i])}"; // GameManager에서 점수 가져오기
        }
        else
        {
            playerSlots[i].text = "-"; // 빈 자리 표시
            scoretxt[i].text = "-"; // 빈 자리 점수 표시
        }
    }
}

// GameManager의 score 배열에서 점수 가져오기
private int GetPlayerScore(Player player)
{
    int actorNumber = player.ActorNumber;

    // GameManager의 인스턴스에서 score 배열을 가져옴 (배열 크기 검사 추가)
    if (GameManager.Instance != null && actorNumber >= 0 && actorNumber < GameManager.Instance.score.Length)
    {
        return GameManager.Instance.score[actorNumber]; 
    }
    return 0; // 기본값
}

}
