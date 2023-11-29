using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("General Configuration")] 
    [SerializeField] float waitTime = 1f;
    
    [Header("Buttons")] 
    [SerializeField] Button tutorialButton;
    [SerializeField] Button PlayButton;
    [SerializeField] Button ExitButton;
    
    [Header("Target Escenes")] 
    [SerializeField] int tutorialEsceneValue;
    [SerializeField] int firstLevelEsceneValue;
    
    private bool hasPressAnyButton;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasPressAnyButton)
        {
            tutorialButton.onClick.AddListener(LoadTutorial);
            PlayButton.onClick.AddListener(LoadFirstLevel);
            ExitButton.onClick.AddListener(ExitGame);
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
