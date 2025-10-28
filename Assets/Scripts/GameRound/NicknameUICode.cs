using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class NicknameUICode : MonoBehaviour
{
    public TMP_Text[] nicknameText;
    public void SetNickname(string nickname)
    {
        foreach (var text in nicknameText)
        {
            text.text = nickname;
        }

        for (int i = 0; i < GameManager.Instance.PlayerCount; i++)
        {
            Player p = PhotonNetwork.CurrentRoom.GetPlayer(i + 1);
            nicknameText[i].text = p.NickName;
        }
    }

}
