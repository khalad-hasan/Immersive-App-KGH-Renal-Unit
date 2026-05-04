

using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene switching
using System.Collections.Generic;

public class SceneChanger : MonoBehaviour
{
    // A list of scene names you can fill in the Inspector
    public List<string> sceneNames = new List<string>();

    // Change scene by the name in your list
    public void LoadSceneByIndex(int listIndex)
    {
        if (listIndex >= 0 && listIndex < sceneNames.Count)
        {
            string sceneToLoad = sceneNames[listIndex];
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("Scene index out of range in your list!");
        }
    }

    public void goBackToMainMenu()
    {
        string sceneToLoad = sceneNames[0];
        SceneManager.LoadScene(sceneToLoad);
    }






    // Direct load by name
    public void LoadSceneByName(string name)
    {
        SceneManager.LoadScene(name);
    }

    // Restart the current scene
    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}