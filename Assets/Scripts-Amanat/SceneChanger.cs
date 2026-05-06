

using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene switching
using UnityEngine.Video;
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
            CleanupActiveVideoPlayers();
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
        CleanupActiveVideoPlayers();
        SceneManager.LoadScene(sceneToLoad);
    }






    // Direct load by name
    public void LoadSceneByName(string name)
    {
        CleanupActiveVideoPlayers();
        SceneManager.LoadScene(name);
    }

    // Restart the current scene
    public void RestartScene()
    {
        CleanupActiveVideoPlayers();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void CleanupActiveVideoPlayers()
    {
        VideoPlayer[] players = FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None);
        foreach (VideoPlayer player in players)
        {
            if (player == null) continue;

            RenderTexture targetTexture = player.targetTexture;
            player.Stop();
            player.url = "";

            if (targetTexture == null) continue;

            RenderTexture previousActiveTexture = RenderTexture.active;
            if (targetTexture.IsCreated())
            {
                RenderTexture.active = targetTexture;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = previousActiveTexture;
                targetTexture.Release();
            }
        }
    }
}
