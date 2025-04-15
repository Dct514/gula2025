using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class ProfileSyncManager : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class BorderSpriteSet
    {
        public Sprite backgroundSprite;
        public Sprite nameTagSprite;
    }

    [Header("색상 셋 (테두리 + 이름 배경)")]
    public BorderSpriteSet[] borderSpriteSets;

    [Header("플레이어 패널 (1~4번)")]
    public GameObject[] playerPanels;

    [Header("본인 UI 패널 (예: panel_MyCard)")]
    public GameObject myPlayerPanel;

    private const string BORDER_KEY = "borderIndex";

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignBorderIndicesToPlayers();
            //photonView.RPC("AssignBorderIndicesToPlayers", RpcTarget.All);
        }
        ApplyAllProfileBorders();
    }

    private void AssignBorderIndicesToPlayers()
    {
        int index = 0;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            Hashtable props = new Hashtable();
            props[BORDER_KEY] = index % borderSpriteSets.Length;
            player.SetCustomProperties(props);
            index++;
        }
    }

    public void ApplyAllProfileBorders()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            ApplyProfileBorderToPanel(player);
        }
    }

    public void ApplyProfileBorderToPanel(Player player)
    {
        int actorNum = player.ActorNumber;
        int borderIndex = 0;

        if (player.CustomProperties.ContainsKey(BORDER_KEY))
        {
            borderIndex = (int)player.CustomProperties[BORDER_KEY];
        }
        else
        {
            Debug.LogWarning($"[경고] {player.NickName}의 borderIndex 없음. 기본값 사용");
        }

        if (borderIndex < 0 || borderIndex >= borderSpriteSets.Length) return;
        var borderSet = borderSpriteSets[borderIndex];

        // 🔹 UI 타겟 결정
        int playerIndex = System.Array.IndexOf(PhotonNetwork.PlayerList, player);
        GameObject targetPanel = (PhotonNetwork.LocalPlayer == player)
        ? myPlayerPanel
        : (playerIndex < playerPanels.Length ? playerPanels[playerIndex] : null);

        // 🔹 적용
        var bg = targetPanel.transform.Find("img_ProfileBackground")?.GetComponent<Image>();
        if (bg != null) bg.sprite = borderSet.backgroundSprite;

        var nameTag = targetPanel.transform.Find("img_ProfileName")?.GetComponent<Image>();
        if (nameTag != null) nameTag.sprite = borderSet.nameTagSprite;

        Debug.Log($"[적용] {player.NickName} UI 적용됨 (borderIndex {borderIndex})");
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(BORDER_KEY))
        {
            ApplyProfileBorderToPanel(targetPlayer);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        ApplyAllProfileBorders();
    }
}
