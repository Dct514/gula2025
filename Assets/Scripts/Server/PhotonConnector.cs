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
    
    [Header("Lobby UI")]
    public Transform roomListContainer; // 방 목록을 담을 부모 객체
    public GameObject roomEntryPrefab; // 방 항목 UI 프리팹
    public TMP_Text noRoomsText; // "방이 없습니다" 텍스트
    public TMP_Text myName;
    
    private Dictionary<string, GameObject> roomEntries = new Dictionary<string, GameObject>();
    
    private const int MaxPlayers = 4; // 최대 플레이어 수
    
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings(); // 포톤 서버 접속
        myName.text = PhotonNetwork.NickName;
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "서버 연결 완료!";
        statusText2.text = "서버 연결 완료!";
        Debug.Log("Photon: 서버 연결 성공!");
        PhotonNetwork.JoinLobby(); // 로비 입장
    }

    public override void OnJoinedLobby()
    {
        statusText.text = "로비에 입장했습니다.";
        Debug.Log("Photon: 로비 입장 완료!");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("방 목록 업데이트!");
        UpdateRoomListUI(roomList);
    }
   private void UpdateRoomListUI(List<RoomInfo> roomList)
{
    // 기존 방 목록 삭제
    foreach (var entry in roomEntries.Values)
    {
        Destroy(entry);
    }
    roomEntries.Clear();

    // 방이 없으면 "방이 없습니다" 텍스트 표시
    if (roomList.Count == 0)
    {
        noRoomsText.gameObject.SetActive(true);
        return;
    }
    noRoomsText.gameObject.SetActive(false);

    float spacing = 10f; // 아이템 간 간격
    float itemHeight = 50f; // 방 항목 UI 높이 (프리팹 크기)

    // 방 목록 UI 생성
    for (int i = 0; i < roomList.Count; i++)
    {
        RoomInfo room = roomList[i];
        if (room.RemovedFromList) continue;

        GameObject entry = Instantiate(roomEntryPrefab, roomListContainer);
        RectTransform entryTransform = entry.GetComponent<RectTransform>();

        // 방 UI 위치 설정 (점점 아래로 배치)
        entryTransform.anchoredPosition = new Vector2(0, -i * (itemHeight + spacing));

        // UI 요소 업데이트
        entry.transform.Find("RoomNameText").GetComponent<TMP_Text>().text = $"{room.Name} ({room.PlayerCount}/{room.MaxPlayers})";
        entry.transform.Find("JoinButton").GetComponent<Button>().onClick.AddListener(() => JoinRoom(room.Name));

        roomEntries[room.Name] = entry;
    }

    // 컨테이너 크기 조절 (스크롤 뷰 대응)
    AdjustRoomListSize(roomList.Count, itemHeight, spacing);
}

// 방 목록 크기 자동 조절 (스크롤 뷰를 고려한 크기 조정)
private void AdjustRoomListSize(int roomCount, float itemHeight, float spacing)
{
    RectTransform contentRect = roomListContainer.GetComponent<RectTransform>();
    contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, roomCount * (itemHeight + spacing));
}
    public void StartQuickMatch()
    {
        PhotonNetwork.JoinRandomRoom(); // 무작위 방 참가 시도
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("빈 방이 없어서 새로운 방을 만듭니다.");
        CreateRoom();
    }
    public void CreateRoom()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("Photon에 연결되지 않았습니다!");
            return;
        }

        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName)) roomName = $"{Random.Range(1000, 9999)}"; // 방 이름이 비어있으면 랜덤으로 생성

        RoomOptions roomOptions = new RoomOptions { MaxPlayers = MaxPlayers, IsVisible = true, IsOpen = true };
        PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    public void JoinRoom(string roomName)
    {
        roomName =  roomNameInput.text;
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
        statusText.text = $"방 참가 완료: {PhotonNetwork.CurrentRoom.Name}";
        statusText2.text = $"[{PhotonNetwork.CurrentRoom.Name}]번 방 입장\n {PhotonNetwork.CurrentRoom.PlayerCount}/4";


    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        OnJoinedRoom(); // 방에 들어온 플레이어가 있을 때마다 호출
    Debug.Log($"새로운 플레이어가 방에 들어왔습니다: {newPlayer.NickName}");

    // 방에 4명이 모두 들어왔을 때, 씬 변경 시작
    if (PhotonNetwork.CurrentRoom.PlayerCount == MaxPlayers)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameRoundScene");
        }
    }
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

            // 닉네임 저장 (다음 접속 시 유지)
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
