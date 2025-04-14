using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableCard : MonoBehaviour
{
    public int playernum;
    private CardOutlineController outlineController;

    void Awake()
    {
        outlineController = GetComponent<CardOutlineController>();
    }

    public void TableCardClick2()
    {
        GameManager.Instance.DeselectAllCards();

        if (outlineController != null)
        {
            outlineController.SelectCard();
        }

        GameManager.Instance.TableCardClick(playernum);
    }
}
