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
    public Image myProfileImage;
    public Image myNameTagImage;

    [Header("남은 플레이어 패널 (순서대로)")]
    public TMP_Text[] otherPlayerNameTexts;
    public Image[] otherProfileImages;
    public Image[] otherNameTagImages;

    [Header("UI 스프라이트 세트")]
    public Sprite[] borderSprites;
    public Sprite[] nameTagSprites;

    private List<Player> otherPlayers = new List<Player>();

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignBorderIndicesToPlayers();
        }
        UpdatePlayerList();
    }

    void Update()
    {
        HighlightProfiles();
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

    private void HighlightProfiles()
    {
        if (RefactoryGM.Instance == null)
            return;

        float cycle = Time.time % 3f; // 3초 주기
        float alpha = 1f;
        float colorR = 1f, colorG = 0.9f, colorB = 0.8f;

        // 구간 0~1초: 투명 → 불투명
        if (cycle < 1f)
        {
            float t = cycle / 1f;
            alpha = Mathf.Lerp(0.3f, 1f, t);
        }
        // 구간 1~2초: 불투명 → 밝은색 (명도 올리기)
        else if (cycle < 2f)
        {
            float t = (cycle - 1f) / 1f;
            alpha = 1f;
            colorG = Mathf.Lerp(0.9f, 1f, t);
            colorB = Mathf.Lerp(0.8f, 1f, t);

            if (t > 0.7f)
            {
                colorG = 1f;
                colorB = 1f;
            }
        }
        // 구간 2~3초: 밝은색 → 불투명 → 다시 투명
        else
        {
            float t = (cycle - 2f) / 1f;
            alpha = Mathf.Lerp(1f, 0.3f, t);
            colorG = Mathf.Lerp(1f, 0.9f, t);
            colorB = Mathf.Lerp(1f, 0.8f, t);
        }

        foreach (var player in PhotonNetwork.PlayerList)
        {
            bool shouldHighlight = ShouldHighlightProfile(player.ActorNumber);

            Color highlightColor = new Color(colorR, colorG, colorB, alpha);

            if (player == PhotonNetwork.LocalPlayer)
            {
                myNameTagImage.color = shouldHighlight ? highlightColor : Color.white;
            }
            else
            {
                int index = otherPlayers.IndexOf(player);
                if (index >= 0 && index < otherNameTagImages.Length)
                {
                    otherNameTagImages[index].color = shouldHighlight ? highlightColor : Color.white;
                }
            }
        }
    }



    private bool ShouldHighlightProfile(int actorNumber)
    {
        if (RefactoryGM.Instance == null || RefactoryGM.Instance.Turn == null)
        {
            Debug.LogError("RefactoryGM.Instance 또는 Turn이 null입니다!");
            return false;
        }

        if (!RefactoryGM.Instance.Turn.ContainsKey("currentTurn") ||
            !RefactoryGM.Instance.Turn.ContainsKey("currentPlayerIndex") ||
            !RefactoryGM.Instance.Turn.ContainsKey("pickedPlayerIndex"))
        {
            Debug.LogError("Turn 딕셔너리에 필요한 키가 없습니다!");
            return false;
        }

        int currentTurn = (int)RefactoryGM.Instance.Turn["currentTurn"];
        int currentPlayer = (int)RefactoryGM.Instance.Turn["currentPlayerIndex"];
        int pickedPlayer = (int)RefactoryGM.Instance.Turn["pickedPlayerIndex"];

        switch (currentTurn)
        {
            case 0: // 선플 음식카드 제출
                return actorNumber == currentPlayer;
            case 1: // 나머지 플레이어 음식카드 제출
                return actorNumber != currentPlayer;
            case 2: // 선플 카드 선택
                return actorNumber == currentPlayer;
            case 3: // 선플 & 선택된 플레이어 식사/강탈 선택
                return actorNumber == currentPlayer || actorNumber == pickedPlayer;
            default:
                return false;
        }
    }
}
