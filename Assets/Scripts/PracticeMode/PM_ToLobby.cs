using UnityEngine;
using UnityEngine.SceneManagement;

public class PM_ToLobby : MonoBehaviour
{
    void Update()
    {
        // ESC 키 입력을 감지합니다.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 로비 씬으로 전환합니다.
            LoadLobbyScene();
        }
    }

    void LoadLobbyScene()
    {
        // 로비 씬의 이름을 "Lobby"로 가정하고 씬을 로드합니다.
        SceneManager.LoadScene("Lobby");
    }
}

