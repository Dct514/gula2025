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
        // ����� �α� �߰�: Awake()�� ȣ��Ǵ��� Ȯ��
        Debug.Log("CardOutlineController Awake ����");

        cardImage = GetComponent<Image>();
        if (cardImage == null)
        {
            Debug.LogError("Image ������Ʈ�� ã�� �� �����ϴ�!");
            return;
        }

        // ��Ƽ���� �ν��Ͻ�ȭ �� �Ҵ�
        outlineMat = Instantiate(cardImage.material);
        cardImage.material = outlineMat;

        // �ʱ� ���� ���� (�ƿ����� ��)
        outlineMat.SetFloat("_OutlineThickness", deselectedThickness);
    }

    public void SelectCard()
    {
        if (outlineMat != null)
        {
            outlineMat.SetFloat("_OutlineThickness", selectedThickness);
            Debug.Log("SelectCard ȣ���, _OutlineThickness: " + selectedThickness);
        }
    }

    public void DeselectCard()
    {
        if (outlineMat != null)
        {
            outlineMat.SetFloat("_OutlineThickness", deselectedThickness);
            Debug.Log("DeselectCard ȣ���, _OutlineThickness: " + deselectedThickness);
        }
    }
}
