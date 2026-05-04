using UnityEngine;
using TMPro;
using UnityEngine.UI; // Required for Button and Image

public class MyItemView : MonoBehaviour
{
    [Header("Text Elements")]
    // Assign these in the Inspector of your Prefab
    public int index;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text priceText;

    [Header("UI Controls")]
    public Button actionButton;
    public Image iconImage;
}