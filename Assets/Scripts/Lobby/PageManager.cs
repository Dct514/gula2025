using UnityEngine;

public class PageManager : MonoBehaviour
{
    public GameObject[] pages; // 페이지 패널들
    private int currentPage = 0;

    void Start()
    {
        // 모든 페이지를 비활성화
        foreach (GameObject page in pages)
        {
            page.SetActive(false);
        }

        // 페이지 보여주기 (원하지 않는다면 이 줄을 주석 처리 가능)
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
            pages[i].SetActive(i == index); // 현재 페이지만 true
        }
    }
}
