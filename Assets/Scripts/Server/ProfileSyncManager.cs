// ProfileSyncManager.cs (ActorNumber 기준으로 패널 고정 적용)

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
        }
    }

    private void AssignBorderIndicesToPlayers()
    {
        int index = 0;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int borderIndex = index % borderSpriteSets.Length;

            // CustomProperties에 저장 (기록용)
            Hashtable props = new Hashtable();
            props[BORDER_KEY] = borderIndex;
            player.SetCustomProperties(props);

            // RPC로 모든 플레이어에게 적용 명령
            photonView.RPC("ApplyProfileBorder", RpcTarget.All, borderIndex, player.ActorNumber);
            index++;
        }
    }

    [PunRPC]
    public void ApplyProfileBorder(int borderIndex, int actorNumber)
    {
        if (borderIndex < 0 || borderIndex >= borderSpriteSets.Length)
        {
            Debug.LogWarning($"[오류] 잘못된 borderIndex: {borderIndex}");
            return;
        }

        BorderSpriteSet borderSet = borderSpriteSets[borderIndex];

        GameObject targetPanel;

        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
        {
            targetPanel = myPlayerPanel;
        }
        else
        {
            int panelIndex = actorNumber - 1;
            targetPanel = (panelIndex >= 0 && panelIndex < playerPanels.Length) ? playerPanels[panelIndex] : null;
        }

        if (targetPanel == null)
        {
            Debug.LogWarning($"[오류] actorNumber {actorNumber}에 해당하는 targetPanel을 찾을 수 없습니다.");
            return;
        }

        // 🔹 스프라이트 적용
        var bg = targetPanel.transform.Find("img_ProfileBackground")?.GetComponent<Image>();
        if (bg != null) bg.sprite = borderSet.backgroundSprite;

        var nameTag = targetPanel.transform.Find("img_ProfileName")?.GetComponent<Image>();
        if (nameTag != null) nameTag.sprite = borderSet.nameTagSprite;

        Debug.Log($"[완료] {actorNumber}번 플레이어 - borderIndex {borderIndex} 적용 완료");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignBorderIndicesToPlayers();
        }
    }
}
