using UnityEngine;
using UnityEngine.SceneManagement;

public class PM_ToLobby : MonoBehaviour
{
    void Update()
    {
        // ESC Ű �Է��� �����մϴ�.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // �κ� ������ ��ȯ�մϴ�.
            LoadLobbyScene();
        }
    }

    void LoadLobbyScene()
    {
        // �κ� ���� �̸��� "Lobby"�� �����ϰ� ���� �ε��մϴ�.
        SceneManager.LoadScene("Lobby");
    }
}

