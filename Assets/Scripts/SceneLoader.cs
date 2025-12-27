using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadGameScreen()
    {
        SceneManager.LoadScene("GameScreen");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
