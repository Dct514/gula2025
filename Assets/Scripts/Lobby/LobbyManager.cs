using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class LobbyManager : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public AudioSource bgmSource; // BGM AudioSource
    private int selectedModeIndex; // ���õ� ȭ�� ��� ���� ����

    public void settingPanelTrue(GameObject gameObject)
    {
        gameObject.SetActive(true);
    }

    public void settingPanelFalse(GameObject gameObject)
    {
        gameObject.SetActive(false);
    }

    void Start()
    {
        // ��Ӵٿ� ���� ����� ���� ���� ���� (��� ���� X)
        dropdown.onValueChanged.AddListener(delegate { selectedModeIndex = dropdown.value; });

        // ���� ȭ�� ��忡 �°� �⺻ ���ð� ����
        if (Screen.fullScreenMode == FullScreenMode.Windowed)
            dropdown.value = 0; // â ���
        else
            dropdown.value = 1; // ��ü ȭ��

        // ���õ� ���� ���� ������ �ʱ�ȭ
        selectedModeIndex = dropdown.value;
    }

    public void ApplyScreenMode()
    {
        if (selectedModeIndex == 0) //  "â ���"
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(1280, 720, false);
        }
        else if (selectedModeIndex == 1) // "��ü ȭ��"
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }

        Debug.Log("����� ȭ�� ���: " + Screen.fullScreenMode);
    }

  public void ToggleBGM(bool isOn)
    {
        if (bgmSource == null) return;

        if (isOn)
        {
            if (!bgmSource.isPlaying)
                bgmSource.Play();
        }
        else
        {
            bgmSource.Pause(); // 또는 Stop()
        }
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
