using UnityEngine;
using UnityEngine.SceneManagement;

public class SkipIntro : MonoBehaviour
{
    [Header("General Configuration")] 
    [SerializeField] float waitTime = 3f;
    [SerializeField] int nextEsceneValue;
    
    // Start is called before the first frame update
    void Start()
    {
        Invoke("LoadNextEscene", waitTime);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            LoadNextEscene();
        }
    }

    private void LoadNextEscene()
    {
        SceneManager.LoadScene(nextEsceneValue);
    }
}
