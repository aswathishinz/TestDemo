using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Reset : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void Task2reset()
    {

        SceneManager.LoadScene("SampleScene");
    }

    public void Task2page()
    {
        SceneManager.LoadScene("Task2Updated");
    }
    public void HomePage()
    {

        SceneManager.LoadScene("Task"); 
    }

    public void AngleSelection()
    {

        SceneManager.LoadScene("Task2Updated");
    }

    public void Task3()
    {

        SceneManager.LoadScene("Task3");
    }

    public void Task4()
    {

        SceneManager.LoadScene("Task 4 updated");
    }
    public   void OnApplicationQuit()
    {
        Application.Quit();
    }
}
