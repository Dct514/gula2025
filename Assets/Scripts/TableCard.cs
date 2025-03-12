using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableCard : MonoBehaviour
{
    public int playernum;
    public void TableCardClick2()
    {
        GameManager.Instance.TableCardClick(playernum);
    }

}
