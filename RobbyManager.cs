using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class RobbyManager : MonoBehaviour
{
    public GameObject gameStartPannel;
    public GameObject gameSettingPannel;

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
