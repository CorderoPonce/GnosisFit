
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIBuilderMenu : MonoBehaviour
{
    public static void BuildFilters(Transform muscleRow, Transform diffRow)
    {
        string[] muscles = { "Todos", "Pecho", "Piernas", "Espalda", "Core", "Brazos", "Full Body" };
        string[] diffs = { "Todas", "Baja", "Media", "Alta" };

        foreach (string m in muscles) CreateFilterButton(muscleRow, m);
        foreach (string d in diffs) CreateFilterButton(diffRow, d);
    }

    static void CreateFilterButton(Transform parent, string label)
    {
        GameObject btnObj = new GameObject("FilterBtn_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        
        var img = btnObj.GetComponent<Image>();
        img.color = new Color(0.9f, 0.9f, 0.9f); // Gris claro inactivo
        
        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        var tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;
        
        var rt = txtObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }
}
