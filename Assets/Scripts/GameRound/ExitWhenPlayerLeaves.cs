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
        Debug.Log($"{otherPlayer.NickName} ��(��) ���� �������ϴ�.");

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
                countdownText.text = $"������ ������ ���� �ڵ� ����˴ϴ�... {Mathf.CeilToInt(countdown)}��";

            countdown -= Time.deltaTime;
            yield return null;
        }

        // Ÿ�̸� �� ������ �ڵ����� �� ������
        LeaveRoom();
    }

    public void LeaveRoom() // ��ư������ ȣ���� �Լ�
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
