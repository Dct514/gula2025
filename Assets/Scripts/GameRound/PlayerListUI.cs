// 📄 최종 수정: 테두리 + 이름배경 스프라이트 모두 동기화 (PlayerListUI)

using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;

public class PlayerListUI : MonoBehaviourPunCallbacks
{
    [Header("내 패널")]
    public TMP_Text myPlayerNameText;
    public Image myProfileImage;        // 내 테두리
    public Image myNameTagImage;         // 내 이름 배경

    [Header("남은 플레이어 패널 (순서대로)")]
    public TMP_Text[] otherPlayerNameTexts;
    public Image[] otherProfileImages;   // 남 테두리
    public Image[] otherNameTagImages;   // 남 이름 배경

    [Header("UI 스프라이트 세트")]
    public Sprite[] borderSprites;       // 테두리 스프라이트들
    public Sprite[] nameTagSprites;      // 이름 배경 스프라이트들

    private List<Player> otherPlayers = new List<Player>();

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignBorderIndicesToPlayers();
        }
        UpdatePlayerList();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignBorderIndicesToPlayers();
        }
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    private void AssignBorderIndicesToPlayers()
    {
        int index = 0;
        foreach (Player player in PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber))
        {
            Hashtable props = new Hashtable();
            props["borderIndex"] = index % borderSprites.Length;
            player.SetCustomProperties(props);
            index++;
        }
    }

    public void UpdatePlayerList()
    {
        if (!PhotonNetwork.InRoom)
            return;

        Debug.Log("UpdatePlayerList()");

        // 🔹 내 패널 업데이트
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("playerName"))
            myPlayerNameText.text = (string)PhotonNetwork.LocalPlayer.CustomProperties["playerName"];
        else
            myPlayerNameText.text = PhotonNetwork.LocalPlayer.NickName;

        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("borderIndex"))
        {
            int myIndex = (int)PhotonNetwork.LocalPlayer.CustomProperties["borderIndex"];
            myProfileImage.sprite = borderSprites[myIndex];
            myNameTagImage.sprite = nameTagSprites[myIndex];
        }

        // 🔹 남은 플레이어 정리
        otherPlayers = PhotonNetwork.PlayerList
            .Where(p => p != PhotonNetwork.LocalPlayer)
            .OrderBy(p => p.ActorNumber)
            .ToList();

        for (int i = 0; i < otherPlayerNameTexts.Length; i++)
        {
            if (i < otherPlayers.Count)
            {
                Player player = otherPlayers[i];

                if (player.CustomProperties.ContainsKey("playerName"))
                    otherPlayerNameTexts[i].text = (string)player.CustomProperties["playerName"];
                else
                    otherPlayerNameTexts[i].text = player.NickName;

                if (player.CustomProperties.ContainsKey("borderIndex"))
                {
                    int otherIndex = (int)player.CustomProperties["borderIndex"];
                    otherProfileImages[i].sprite = borderSprites[otherIndex];
                    otherNameTagImages[i].sprite = nameTagSprites[otherIndex];
                }
            }
            else
            {
                otherPlayerNameTexts[i].text = "";
                otherProfileImages[i].sprite = null;
                otherNameTagImages[i].sprite = null;
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("borderIndex"))
        {
            Debug.Log($"Player {targetPlayer.NickName} borderIndex 갱신됨. UpdatePlayerList 호출");
            UpdatePlayerList();
        }
    }
}
