using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CategoryManager : MonoBehaviour
{
    public GameObject[] panels;

    public void OpenPanel(int index)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            if (i == index)
                panels[i].SetActive(true);
            else
                panels[i].SetActive(false);
        }
    }
}
