using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("General Configuration")] 
    [SerializeField] float waitTime = 1f;
    
    [Header("Buttons")] 
    [SerializeField] Button tutorialButton;
    [SerializeField] Button playButton;
    [SerializeField] Button exitButton;
    
    [Header("Target Escenes")] 
    [SerializeField] int tutorialEsceneValue;
    [SerializeField] int firstLevelEsceneValue;
    
    private bool hasPressAnyButton;

    // Update is called once per frame
    void Update()
    {
        if (!hasPressAnyButton)
        {
            tutorialButton.onClick.AddListener(LoadTutorial);
            playButton.onClick.AddListener(LoadFirstLevel);
            exitButton.onClick.AddListener(ExitGame);
        }
    }

    private void LoadTutorial()
    {
        hasPressAnyButton = true;
        SceneManager.LoadScene(tutorialEsceneValue);
    }
    
    private void LoadFirstLevel()
    {
        hasPressAnyButton = true;
        SceneManager.LoadScene(firstLevelEsceneValue);
    }
    
    private void ExitGame()
    {
        hasPressAnyButton = true;
        Application.Quit();
    }
}
