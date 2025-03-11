using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class RobbyManager : MonoBehaviour
{
    public GameObject gameStartPannel;
    public GameObject gameSettingPannel;
    public TMP_Dropdown dropdown;
    private int selectedModeIndex; // 선택된 화면 모드 저장 변수


    public void GameStartPannelSetTrue()
    {
        gameStartPannel.SetActive(true);
    }
    public void GameStartPannelSetFalse()
    {
        gameStartPannel.SetActive(false);
    }
    
    public void GameSettingPannelSetTrue()
    {
        gameSettingPannel.SetActive(true);
    }
    public void GameSettingPannelSetFalse()
    {
        gameSettingPannel.SetActive(false);
    }
    void Start()
    {
        // 드롭다운 값이 변경될 때만 변수 저장 (즉시 적용 X)
        dropdown.onValueChanged.AddListener(delegate { selectedModeIndex = dropdown.value; });

        // 현재 화면 모드에 맞게 기본 선택값 설정
        if (Screen.fullScreenMode == FullScreenMode.Windowed)
            dropdown.value = 0; // 창 모드
        else
            dropdown.value = 1; // 전체 화면

        // 선택된 값도 현재 값으로 초기화
        selectedModeIndex = dropdown.value;
    }

       public void ApplyScreenMode()
    {
        if (selectedModeIndex == 0) // "창 모드"
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(1280, 720, false);
        }
        else if (selectedModeIndex == 1) // "전체 화면"
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }

        Debug.Log("적용된 화면 모드: " + Screen.fullScreenMode);
    }

    public void GameQuit()
    {
       /* if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        } */
        #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
        #else
                    Application.Quit();
        #endif
    }
}
