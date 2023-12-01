using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Teleporter : MonoBehaviour
{
    [Header("Load Scenes Configuration")] 
    [SerializeField] int targetEscene = 4;
    [SerializeField] float delayBeforeLoad = 3f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.collider.tag.Equals("Player"))
        {
            StartCoroutine(LoadSceneWithDelay(0, targetEscene));
        }
    }
    
    IEnumerator LoadSceneWithDelay(int clipPositionInArray, int sceneIndex)
    {
        yield return new WaitForSeconds(delayBeforeLoad);
        LoadScene(sceneIndex);
    }
    
    void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
