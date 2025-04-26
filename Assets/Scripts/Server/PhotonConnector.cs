using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;

public class PhotonConnector : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    public TMP_Text statusText;
    public TMP_Text statusText2;
    public TMP_InputField nicknameInput;
    public TMP_Text nicknameDisplay;
    public TMP_InputField roomNameInput;
    public GameObject cancelMatchButton;

    [Header("Lobby UI")]
    public Transform roomListContainer;
    public GameObject roomEntryPrefab;
    public TMP_Text noRoomsText;
    public TMP_Text myName;

    private Dictionary<string, GameObject> roomEntries = new Dictionary<string, GameObject>();
    private const int MaxPlayers = 4;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
        myName.text = PhotonNetwork.NickName;

        cancelMatchButton.SetActive(false);
    }

    public void StartQuickMatch()
    {
        PhotonNetwork.JoinRandomRoom();
        cancelMatchButton.SetActive(true);
    }

    public void CancelQuickMatch()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            statusText.text = "입장 대기 취소됨.";
            statusText2.text = "입장 대기 취소됨.";
        }
        cancelMatchButton.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "서버 연결 완료!";
        statusText2.text = "서버 연결 완료!";
        Debug.Log("Photon: 서버 연결 성공!");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        statusText.text = "로비에 입장했습니다.";
        Debug.Log("Photon: 로비 입장 완료!");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateRoomListUI(roomList);
    }

    private void UpdateRoomListUI(List<RoomInfo> roomList)
    {
        foreach (var entry in roomEntries.Values)
        {
            Destroy(entry);
        }
        roomEntries.Clear();

        if (roomList.Count == 0)
        {
            noRoomsText.gameObject.SetActive(true);
            return;
        }
        noRoomsText.gameObject.SetActive(false);

        float spacing = 10f;
        float itemHeight = 50f;

        for (int i = 0; i < roomList.Count; i++)
        {
            RoomInfo room = roomList[i];
            if (room.RemovedFromList) continue;

            GameObject entry = Instantiate(roomEntryPrefab, roomListContainer);
            RectTransform entryTransform = entry.GetComponent<RectTransform>();
            entryTransform.anchoredPosition = new Vector2(0, -i * (itemHeight + spacing));

            entry.transform.Find("RoomNameText").GetComponent<TMP_Text>().text = $"{room.Name} ({room.PlayerCount}/{room.MaxPlayers})";
            entry.transform.Find("JoinButton").GetComponent<Button>().onClick.AddListener(() => JoinRoom(room.Name));

            roomEntries[room.Name] = entry;
        }

        AdjustRoomListSize(roomList.Count, itemHeight, spacing);
    }

    private void AdjustRoomListSize(int roomCount, float itemHeight, float spacing)
    {
        RectTransform contentRect = roomListContainer.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, roomCount * (itemHeight + spacing));
    }

    public void CreateRoom()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("Photon에 연결되지 않았습니다!");
            return;
        }

        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName))
            roomName = $"{Random.Range(1000, 9999)}";

        RoomOptions roomOptions = new RoomOptions { MaxPlayers = MaxPlayers, IsVisible = true, IsOpen = true };
        PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    public void JoinRoom(string roomName)
    {
        roomName = roomNameInput.text;
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("Photon에 연결되지 않았습니다!");
            return;
        }

        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"방 참가 성공: {PhotonNetwork.CurrentRoom.Name}");
        statusText.text = $"[{PhotonNetwork.CurrentRoom.Name}]번 방 입장\n {PhotonNetwork.CurrentRoom.PlayerCount}/4";
        statusText2.text = $"[{PhotonNetwork.CurrentRoom.Name}]번 방 입장\n {PhotonNetwork.CurrentRoom.PlayerCount}/4";
    }

    public override void OnLeftRoom()
    {
        Debug.Log("방에서 나왔습니다.");
        statusText.text = "서버 연결 완료!";
        statusText2.text = "서버 연결 완료!";
        cancelMatchButton.SetActive(false);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"새로운 플레이어가 방에 들어왔습니다: {newPlayer.NickName}");

        if (PhotonNetwork.CurrentRoom.PlayerCount == MaxPlayers)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                cancelMatchButton.SetActive(false);
                PhotonNetwork.LoadLevel("GameRoundScene");
            }
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("빈 방이 없어서 새로운 방을 만듭니다.");
        CreateRoom();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 참가 실패: {message}");
        statusText.text = "방 참가 실패!";
    }

    public void SetNickname()
    {
        string nickname = nicknameInput.text.Trim();

        if (!string.IsNullOrEmpty(nickname))
        {
            PhotonNetwork.NickName = nickname;
            nicknameDisplay.text = "현재 닉네임: " + nickname;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "playerName", nickname }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            PlayerPrefs.SetString("PlayerNickname", nickname);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.LogWarning("닉네임을 입력하세요!");
        }

        myName.text = PhotonNetwork.NickName;
    }
}
