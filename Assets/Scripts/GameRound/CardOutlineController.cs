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
        cardImage = GetComponent<Image>();
        if (cardImage == null)
        {
            Debug.LogError("Image ������Ʈ�� ã�� �� �����ϴ�!");
            return;
        }

        outlineMat = Instantiate(cardImage.material);
        cardImage.material = outlineMat;

        outlineMat.SetFloat("_OutlineThickness", deselectedThickness);
    }

    public void SelectCard()
    {
        if (outlineMat != null)
        {
            outlineMat.SetFloat("_OutlineThickness", selectedThickness);

        }
    }

    public void DeselectCard()
    {
        if (outlineMat != null)
        {
            outlineMat.SetFloat("_OutlineThickness", deselectedThickness);

        }
    }
}
