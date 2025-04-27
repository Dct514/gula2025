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

        float blink = Mathf.PingPong(Time.time * 2f, 1f); // 2배 빠르게 (0.5초 주기)
        float alpha = Mathf.Lerp(0.5f, 1f, blink); // 알파 0.5~1.0 자연스럽게
        float colorR = Mathf.Lerp(1f, 1f, blink);  // 빨강은 항상 강하게
        float colorG = Mathf.Lerp(0.9f, 0.95f, blink); // 초록 살짝 따뜻하게
        float colorB = Mathf.Lerp(0.8f, 0.9f, blink);  // 파랑 따뜻하게 줄임

        foreach (var player in PhotonNetwork.PlayerList)
        {
            bool shouldHighlight = ShouldHighlightProfile(player.ActorNumber);

            if (player == PhotonNetwork.LocalPlayer)
            {
                if (shouldHighlight)
                {
                    myNameTagImage.color = new Color(colorR, colorG, colorB, alpha);
                }
                else
                {
                    myNameTagImage.color = new Color(1f, 1f, 1f, 1f);
                }
            }
            else
            {
                int index = otherPlayers.IndexOf(player);
                if (index >= 0 && index < otherNameTagImages.Length)
                {
                    if (shouldHighlight)
                    {
                        otherNameTagImages[index].color = new Color(colorR, colorG, colorB, alpha);
                    }
                    else
                    {
                        otherNameTagImages[index].color = new Color(1f, 1f, 1f, 1f);
                    }
                }
            }
        }
    }



    private bool ShouldHighlightProfile(int actorNumber)
    {
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
