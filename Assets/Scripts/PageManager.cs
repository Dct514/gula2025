using UnityEngine;

public class PageManager : MonoBehaviour
{
    public GameObject[] pages; // ������ �гε�
    private int currentPage = 0;

    void Start()
    {
        // ��� �������� ��Ȱ��ȭ
        foreach (GameObject page in pages)
        {
            page.SetActive(false);
        }

        // ������ �����ֱ� (������ �ʴ´ٸ� �� ���� �ּ� ó�� ����)
        // ShowPage(currentPage);
    }

    public void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            ShowPage(currentPage);
        }
    }

    public void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);
        }
    }

    void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index); // ���� �������� true
        }
    }
}
