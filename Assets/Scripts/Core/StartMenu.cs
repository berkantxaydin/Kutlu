using System;
using UnityEngine;
using UnityEngine.SceneManagement;


public class StartMenu : MonoBehaviour
{
    public  void LoadGameScene()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
