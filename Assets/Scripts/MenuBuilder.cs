
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuBuilder : MonoBehaviour
{
    [ContextMenu("Build Menu")]
    public void Build()
    {
        // 1. Canvas
        GameObject canvasObj = GameObject.Find("MainCanvas_Menu") ?? new GameObject("MainCanvas_Menu");
        Canvas canvas = canvasObj.GetComponent<Canvas>() ?? canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Scroll View
        GameObject scrollObj = new GameObject("ExerciseList");
        scrollObj.transform.SetParent(canvasObj.transform, false);
        ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
        sr.horizontal = false;
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0, 0.1f);
        scrollRt.anchorMax = new Vector2(1, 0.95f);
        scrollRt.sizeDelta = Vector2.zero;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        viewport.AddComponent<Image>().color = new Color(0,0,0,0);
        viewport.AddComponent<RectMask2D>();
        RectTransform viewRt = viewport.GetComponent<RectTransform>();
        viewRt.anchorMin = Vector2.zero; viewRt.anchorMax = Vector2.one; viewRt.sizeDelta = Vector2.zero;
        sr.viewport = viewRt;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 30; vlg.padding = new RectOffset(40, 40, 40, 40);
        vlg.childForceExpandHeight = false; vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        RectTransform contRt = content.GetComponent<RectTransform>();
        contRt.anchorMin = new Vector2(0, 1); contRt.anchorMax = new Vector2(1, 1);
        contRt.pivot = new Vector2(0.5f, 1);
        sr.content = contRt;

        // 3. Tarjeta Prefab (como hijo oculto)
        GameObject card = new GameObject("Card_Prefab");
        card.transform.SetParent(canvasObj.transform, false);
        card.SetActive(false);
        Image cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.15f, 0.15f, 0.15f); // Gris premium
        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(0, 260);

        // Borde redondeado (simulado con color de fondo)
        // Titulo
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(card.transform, false);
        var title = titleObj.AddComponent<TextMeshProUGUI>();
        title.fontSize = 42; title.fontStyle = FontStyles.Bold; title.text = "Ejercicio";
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.35f, 0.6f); titleRt.anchorMax = new Vector2(0.95f, 0.9f);
        titleRt.sizeDelta = Vector2.zero;

        // Subtitulo
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(card.transform, false);
        var sub = subObj.AddComponent<TextMeshProUGUI>();
        sub.fontSize = 28; sub.color = Color.gray; sub.text = "Piernas • Media";
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.35f, 0.35f); subRt.anchorMax = new Vector2(0.95f, 0.6f);
        subRt.sizeDelta = Vector2.zero;

        // Boton
        GameObject btnObj = new GameObject("Btn_VerAR");
        btnObj.transform.SetParent(card.transform, false);
        btnObj.AddComponent<Image>().color = new Color(0.2f, 0.6f, 1f);
        btnObj.AddComponent<Button>();
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.35f, 0.05f); btnRt.anchorMax = new Vector2(0.85f, 0.3f);
        btnRt.sizeDelta = Vector2.zero;

        GameObject btnTxt = new GameObject("Text");
        btnTxt.transform.SetParent(btnObj.transform, false);
        var txt = btnTxt.AddComponent<TextMeshProUGUI>();
        txt.text = "VER EN RA"; txt.alignment = TextAlignmentOptions.Center; txt.fontSize = 26;
        RectTransform txtRt = btnTxt.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one; txtRt.sizeDelta = Vector2.zero;

        // Imagen Placeholder
        GameObject imgObj = new GameObject("Image");
        imgObj.transform.SetParent(card.transform, false);
        imgObj.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);
        RectTransform imgRt = imgObj.GetComponent<RectTransform>();
        imgRt.anchorMin = new Vector2(0.05f, 0.1f); imgRt.anchorMax = new Vector2(0.3f, 0.9f);
        imgRt.sizeDelta = Vector2.zero;

        // 4. Configurar CatalogManager
        GameObject catalogObj = GameObject.Find("CatalogManager") ?? new GameObject("CatalogManager");
        var catalog = catalogObj.GetComponent<UIMenuCatalog>() ?? catalogObj.AddComponent<UIMenuCatalog>();
        catalog.contentPanel = contRt;
        catalog.cardPrefab = card;

        Debug.Log("Menu UI built successfully!");
    }
}
