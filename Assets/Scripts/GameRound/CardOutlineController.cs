using UnityEngine;
using UnityEngine.UI;

public class CardOutlineController : MonoBehaviour
{
    public float selectedThickness = 0.1f;
    public float deselectedThickness = 0f;

    private Image cardImage;
    private Material outlineMat;

    void Awake()
    {
        // 디버그 로그 추가: Awake()가 호출되는지 확인
        Debug.Log("CardOutlineController Awake 실행");

        cardImage = GetComponent<Image>();
        if (cardImage == null)
        {
            Debug.LogError("Image 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        // 머티리얼 인스턴스화 및 할당
        outlineMat = Instantiate(cardImage.material);
        cardImage.material = outlineMat;

        // 초기 상태 설정 (아웃라인 끔)
        outlineMat.SetFloat("_OutlineThickness", deselectedThickness);
    }

    public void SelectCard()
    {
        if (outlineMat != null)
        {
            outlineMat.SetFloat("_OutlineThickness", selectedThickness);
            Debug.Log("SelectCard 호출됨, _OutlineThickness: " + selectedThickness);
        }
    }

    public void DeselectCard()
    {
        if (outlineMat != null)
        {
            outlineMat.SetFloat("_OutlineThickness", deselectedThickness);
            Debug.Log("DeselectCard 호출됨, _OutlineThickness: " + deselectedThickness);
        }
    }
}
