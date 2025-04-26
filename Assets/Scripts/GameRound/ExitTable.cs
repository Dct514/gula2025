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
            isLeaving = true; // 중복 호출 방지
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene("Lobby");
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("방을 성공적으로 떠났습니다. 로비로 이동합니다.");
        SceneManager.LoadScene("Lobby");
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.LogWarning("서버 연결 끊김. 로비로 이동합니다.");
        SceneManager.LoadScene("Lobby");
    }
}
