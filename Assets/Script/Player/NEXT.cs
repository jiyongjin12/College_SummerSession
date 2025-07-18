using UnityEngine;
using UnityEngine.SceneManagement;

public class NEXT : MonoBehaviour
{
    public int nextScene;
    public void next()
    {
        SceneManager.LoadScene(nextScene);
    }
}
