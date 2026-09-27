using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton scene router that toggles between the <c>QR Code Scanning</c> scene
/// and the <c>MyArScene</c> AR scene. Persists across loads via
/// <see cref="UnityEngine.Object.DontDestroyOnLoad"/>.
/// </summary>
public class SceneManagerAr : MonoBehaviour
{

    /// <summary>The single active instance of <see cref="SceneManagerAr"/>.</summary>
    public static SceneManagerAr instance;

    /// <summary>Initialises the singleton and marks this object to persist across scene loads.</summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != null)
        {
            Destroy(this);
        }

        DontDestroyOnLoad(this);
    }

    /// <summary>
    /// Loads the other scene based on which scene is currently active.
    /// <list type="bullet">
    ///   <item><description>From <c>QR Code Scanning</c> → loads <c>MyArScene</c>.</description></item>
    ///   <item><description>From <c>MyArScene</c> → loads <c>QR Code Scanning</c>.</description></item>
    /// </list>
    /// </summary>
    public void GoToNextScene()
    {

        Scene currScene = SceneManager.GetActiveScene();
        Debug.Log(currScene.name);
        if (currScene.name == "QR Code Scanning")
        {

            SceneManager.LoadScene("MyArScene" , LoadSceneMode.Single);
        }
        else if(currScene.name == "MyArScene")
        {
            SceneManager.LoadScene("QR Code Scanning" , LoadSceneMode.Single);
        }
    }
}
