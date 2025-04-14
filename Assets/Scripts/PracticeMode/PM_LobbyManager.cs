using UnityEngine;
using UnityEngine.SceneManagement;

public class PM_LobbyManager : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("PracticeMode");
    }

    public void QuitGame()
    {
        Application.Quit();
    }


}
