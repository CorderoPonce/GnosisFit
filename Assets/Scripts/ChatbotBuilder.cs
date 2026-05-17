
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatbotBuilder : MonoBehaviour
{
    [ContextMenu("Build Chatbot UI")]
    public void Build()
    {
        // 1. Canvas
        GameObject canvasObj = new GameObject("ChatbotCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Fondo
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasObj.transform, false);
        bg.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;

        // 3. Scroll Chat
        GameObject scrollObj = new GameObject("ChatScroll");
        scrollObj.transform.SetParent(canvasObj.transform, false);
        ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
        sr.horizontal = false;
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.05f, 0.15f); scrollRt.anchorMax = new Vector2(0.95f, 0.95f);
        scrollRt.sizeDelta = Vector2.zero;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        viewport.AddComponent<RectMask2D>();
        sr.viewport = viewport.AddComponent<RectTransform>();
        sr.viewport.anchorMin = Vector2.zero; sr.viewport.anchorMax = Vector2.one; sr.viewport.sizeDelta = Vector2.zero;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true; vlg.childForceExpandHeight = false; vlg.spacing = 15;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = content.GetComponent<RectTransform>();
        sr.content.anchorMin = new Vector2(0, 1); sr.content.anchorMax = new Vector2(1, 1);
        sr.content.pivot = new Vector2(0.5f, 1);

        // Texto Chat
        GameObject textObj = new GameObject("ChatText");
        textObj.transform.SetParent(content.transform, false);
        var chatTxt = textObj.AddComponent<TextMeshProUGUI>();
        chatTxt.fontSize = 32; chatTxt.text = "Cargando chat...";

        // 4. Input Area
        GameObject inputObj = new GameObject("ChatInput");
        inputObj.transform.SetParent(canvasObj.transform, false);
        inputObj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
        RectTransform inputRt = inputObj.GetComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0.05f, 0.05f); inputRt.anchorMax = new Vector2(0.75f, 0.12f);
        inputRt.sizeDelta = Vector2.zero;

        GameObject textArea = new GameObject("TextArea");
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform textRt = textArea.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one; textRt.sizeDelta = new Vector2(-20, -10);

        var inputField = inputObj.AddComponent<TMP_InputField>();
        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(textArea.transform, false);
        var pText = placeholder.AddComponent<TextMeshProUGUI>();
        pText.text = "Escribe un mensaje..."; pText.color = Color.gray; pText.fontSize = 28;
        pText.alignment = TextAlignmentOptions.Left;
        RectTransform pRt = pText.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero; pRt.anchorMax = Vector2.one; pRt.sizeDelta = Vector2.zero;
        inputField.placeholder = pText;

        GameObject textComp = new GameObject("InputText");
        textComp.transform.SetParent(textArea.transform, false);
        var iText = textComp.AddComponent<TextMeshProUGUI>();
        iText.fontSize = 28; iText.color = Color.white;
        iText.alignment = TextAlignmentOptions.Left;
        RectTransform iRt = iText.GetComponent<RectTransform>();
        iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one; iRt.sizeDelta = Vector2.zero;
        inputField.textComponent = iText;
        inputField.textViewport = textRt;

        // Boton Enviar
        GameObject btnObj = new GameObject("Btn_Send");
        btnObj.transform.SetParent(canvasObj.transform, false);
        btnObj.AddComponent<Image>().color = new Color(0.2f, 0.6f, 1f);
        var btn = btnObj.AddComponent<Button>();
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.77f, 0.05f); btnRt.anchorMax = new Vector2(0.95f, 0.12f);
        btnRt.sizeDelta = Vector2.zero;

        GameObject btnTxt = new GameObject("Txt");
        btnTxt.transform.SetParent(btnObj.transform, false);
        var bTxt = btnTxt.AddComponent<TextMeshProUGUI>();
        bTxt.text = "OK"; bTxt.alignment = TextAlignmentOptions.Center;
        RectTransform bRt = btnTxt.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one; bRt.sizeDelta = Vector2.zero;

        // 5. EntrenadorLLM
        GameObject managerObj = new GameObject("ChatbotManager");
        var llm = managerObj.AddComponent<EntrenadorLLM>();
        llm.inputUsuario = inputField;
        llm.textoChat = chatTxt;
        llm.botonEnviar = btn;
        llm.scrollChat = sr;

        Debug.Log("Chatbot UI built successfully!");
    }
}
