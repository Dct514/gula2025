using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class ExitWhenPlayerLeaves : MonoBehaviourPunCallbacks
{
    public GameObject exitPanel;
    public Text countdownText;
    private float countdown = 15f;
    private bool isCountingDown = false;

    void Start()
    {
        if (exitPanel != null)
        {
            exitPanel.SetActive(false);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"{otherPlayer.NickName} 이(가) 방을 떠났습니다.");

        if (!isCountingDown)
        {
            StartCoroutine(CountdownAndExit());
        }
    }

    IEnumerator CountdownAndExit()
    {
        isCountingDown = true;

        if (exitPanel != null)
        {
            exitPanel.SetActive(true);
        }

        while (countdown > 0)
        {
            if (countdownText != null)
                countdownText.text = $"누군가 게임을 떠나 자동 종료됩니다... {Mathf.CeilToInt(countdown)}초";

            countdown -= Time.deltaTime;
            yield return null;
        }

        // 타이머 다 끝나면 자동으로 방 나가기
        LeaveRoom();
    }

    public void LeaveRoom() // 버튼에서도 호출할 함수
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Lobby");
    }
}
