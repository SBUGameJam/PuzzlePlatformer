using UnityEngine.SceneManagement;
using UnityEngine;

public class ButtonHandler : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Tutorial");
    }
    public void GoToSettings()
    {
        SceneManager.LoadScene("Settings");
    }
    public void ShowLevels()
    {
        SceneManager.LoadScene("Levels");
    }
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }

}
