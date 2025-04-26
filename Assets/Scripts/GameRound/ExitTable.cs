using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class ExitTable : MonoBehaviourPunCallbacks
{
    private bool isLeaving = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isLeaving)
        {
            LeaveRoomAndGoLobby();
        }
    }

    public void LeaveRoomAndGoLobby()
    {
        if (PhotonNetwork.InRoom)
        {
            isLeaving = true; // �ߺ� ȣ�� ����
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene("Lobby");
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("���� ���������� �������ϴ�. �κ�� �̵��մϴ�.");
        SceneManager.LoadScene("Lobby");
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.LogWarning("���� ���� ����. �κ�� �̵��մϴ�.");
        SceneManager.LoadScene("Lobby");
    }
}
