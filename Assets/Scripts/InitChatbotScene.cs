using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Inicializa la escena ChatbotMode construyendo toda la UI de chat por código.
/// Se conecta automáticamente al EntrenadorLLM existente en la escena.
/// </summary>
public class InitChatbotScene : MonoBehaviour
{
    // ── Paleta de colores ──
    static readonly Color BG_DARK        = new Color(0.08f, 0.08f, 0.12f, 1f);
    static readonly Color HEADER_BG      = new Color(0.06f, 0.06f, 0.10f, 1f);
    static readonly Color CHAT_AREA_BG   = new Color(0.12f, 0.12f, 0.18f, 1f);
    static readonly Color INPUT_BG       = new Color(0.18f, 0.18f, 0.24f, 1f);
    static readonly Color INPUT_BAR_BG   = new Color(0.10f, 0.10f, 0.14f, 1f);
    static readonly Color TEAL           = new Color(0.00f, 0.75f, 0.65f, 1f);
    static readonly Color TEAL_PRESSED   = new Color(0.00f, 0.55f, 0.48f, 1f);
    static readonly Color RED_BTN        = new Color(0.90f, 0.25f, 0.25f, 1f);
    static readonly Color TEXT_WHITE     = new Color(0.95f, 0.95f, 0.95f, 1f);
    static readonly Color TEXT_GRAY      = new Color(0.55f, 0.55f, 0.60f, 1f);
    static readonly Color TEXT_PURPLE    = new Color(0.53f, 0.18f, 0.58f, 1f);


    void Start()
    {
        BuildChatUI();
    }

    void BuildChatUI()
    {
        // ═══════════════════════════════════════
        // 1. CANVAS PRINCIPAL
        // ═══════════════════════════════════════
        var canvasGO = new GameObject("ChatCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ═══════════════════════════════════════
        // 2. FONDO COMPLETO
        // ═══════════════════════════════════════
        var bg = CreatePanel(canvasGO.transform, "Background", BG_DARK);
        Stretch(bg);

        // ═══════════════════════════════════════
        // 3. HEADER
        // ═══════════════════════════════════════
        var header = CreatePanel(canvasGO.transform, "Header", HEADER_BG);
        var headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 140);
        headerRT.anchoredPosition = Vector2.zero;

        // Botón Volver
        var btnVolver = CreatePanel(header.transform, "BtnVolver", RED_BTN);
        var btnVolverRT = btnVolver.GetComponent<RectTransform>();
        btnVolverRT.anchorMin = new Vector2(0, 0.5f);
        btnVolverRT.anchorMax = new Vector2(0, 0.5f);
        btnVolverRT.pivot = new Vector2(0, 0.5f);
        btnVolverRT.sizeDelta = new Vector2(120, 70);
        btnVolverRT.anchoredPosition = new Vector2(30, -10);

        var txtVolver = CreateTMP(btnVolver.transform, "✕", 36, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(txtVolver.gameObject);

        var volverBtn = btnVolver.AddComponent<Button>();
        volverBtn.targetGraphic = btnVolver.GetComponent<Image>();
        volverBtn.onClick.AddListener(() => SceneManager.LoadScene("Scene_Menu"));

        // Título
        var titulo = CreateTMP(header.transform, "Entrenador IA", 42, TEXT_WHITE, FontStyles.Bold, TextAlignmentOptions.Center);
        var tituloRT = titulo.GetComponent<RectTransform>();
        tituloRT.anchorMin = new Vector2(0.15f, 0);
        tituloRT.anchorMax = new Vector2(0.85f, 1);
        tituloRT.sizeDelta = Vector2.zero;
        tituloRT.anchoredPosition = new Vector2(0, -10);

        // Subtítulo
        var subtitulo = CreateTMP(header.transform, "Powered by Ollama · Llama 3", 22, TEXT_GRAY, FontStyles.Italic, TextAlignmentOptions.Center);
        var subRT = subtitulo.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.15f, 0);
        subRT.anchorMax = new Vector2(0.85f, 0.35f);
        subRT.sizeDelta = Vector2.zero;

        // ═══════════════════════════════════════
        // 4. ÁREA DE CHAT (ScrollRect)
        // ═══════════════════════════════════════
        var chatArea = CreatePanel(canvasGO.transform, "ChatArea", CHAT_AREA_BG);
        var chatAreaRT = chatArea.GetComponent<RectTransform>();
        chatAreaRT.anchorMin = new Vector2(0, 0);
        chatAreaRT.anchorMax = new Vector2(1, 1);
        chatAreaRT.offsetMin = new Vector2(0, 160);   // espacio para input bar
        chatAreaRT.offsetMax = new Vector2(0, -140);   // espacio para header

        // ScrollRect
        var scrollRect = chatArea.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        // Viewport
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(chatArea.transform, false);
        var vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.sizeDelta = Vector2.zero;
        vpRT.offsetMin = new Vector2(20, 10);
        vpRT.offsetMax = new Vector2(-20, -10);
        scrollRect.viewport = vpRT;

        // Content
        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

        // Texto del chat
        var chatTextGO = new GameObject("ChatText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        chatTextGO.transform.SetParent(content.transform, false);
        chatTextGO.AddComponent<EnlacesChat>();
        var chatText = chatTextGO.GetComponent<TextMeshProUGUI>();
        chatText.fontSize = 30;
        chatText.color = TEXT_WHITE;
        chatText.enableWordWrapping = true;
        chatText.richText = true;
        chatText.overflowMode = TextOverflowModes.Overflow;
        chatText.margin = new Vector4(10, 5, 10, 5);
        chatText.text = "";

        // ═══════════════════════════════════════
        // 5. BARRA DE ENTRADA (Input + Botón Enviar)
        // ═══════════════════════════════════════
        var inputBar = CreatePanel(canvasGO.transform, "InputBar", INPUT_BAR_BG);
        var inputBarRT = inputBar.GetComponent<RectTransform>();
        inputBarRT.anchorMin = new Vector2(0, 0);
        inputBarRT.anchorMax = new Vector2(1, 0);
        inputBarRT.pivot = new Vector2(0.5f, 0);
        inputBarRT.sizeDelta = new Vector2(0, 160);
        inputBarRT.anchoredPosition = Vector2.zero;

        // Centralizar el comportamiento del teclado móvil usando el script reutilizable EmpujarChat
        var empujar = inputBar.AddComponent<EmpujarChat>();
        empujar.barraEntrada = inputBarRT;
        empujar.alturaSalto = 900f;

        // Input Field
        var inputFieldGO = CreatePanel(inputBar.transform, "ChatInputField", INPUT_BG);
        var inputFieldRT = inputFieldGO.GetComponent<RectTransform>();
        inputFieldRT.anchorMin = new Vector2(0, 0.5f);
        inputFieldRT.anchorMax = new Vector2(1, 0.5f);
        inputFieldRT.pivot = new Vector2(0, 0.5f);
        inputFieldRT.sizeDelta = new Vector2(-200, 80);
        inputFieldRT.anchoredPosition = new Vector2(25, 0);

        // Text Area dentro del InputField
        var textArea = new GameObject("TextArea", typeof(RectTransform));
        textArea.transform.SetParent(inputFieldGO.transform, false);
        var textAreaRT = textArea.GetComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.sizeDelta = new Vector2(-30, -10);
        textAreaRT.anchoredPosition = Vector2.zero;

        // Placeholder
        var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        placeholderGO.transform.SetParent(textArea.transform, false);
        var placeholder = placeholderGO.GetComponent<TextMeshProUGUI>();
        placeholder.text = "Escribe un mensaje...";
        placeholder.fontSize = 28;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.color = TEXT_GRAY;
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.margin = new Vector4(15, 0, 15, 0);
        Stretch(placeholderGO);

        // Input Text
        var inputTextGO = new GameObject("InputText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        inputTextGO.transform.SetParent(textArea.transform, false);
        var inputText = inputTextGO.GetComponent<TextMeshProUGUI>();
        inputText.fontSize = 28;
        inputText.color = TEXT_WHITE;
        inputText.alignment = TextAlignmentOptions.Left;
        inputText.margin = new Vector4(15, 0, 15, 0);
        Stretch(inputTextGO);

        // TMP_InputField component
        var inputField = inputFieldGO.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRT;
        inputField.textComponent = inputText;
        inputField.placeholder = placeholder;
        inputField.fontAsset = inputText.font;
        inputField.pointSize = 28;

        // Botón Enviar
        var btnEnviar = CreatePanel(inputBar.transform, "BtnEnviar", TEAL);
        var btnEnviarRT = btnEnviar.GetComponent<RectTransform>();
        btnEnviarRT.anchorMin = new Vector2(1, 0.5f);
        btnEnviarRT.anchorMax = new Vector2(1, 0.5f);
        btnEnviarRT.pivot = new Vector2(1, 0.5f);
        btnEnviarRT.sizeDelta = new Vector2(150, 80);
        btnEnviarRT.anchoredPosition = new Vector2(-25, 0);

        var txtEnviar = CreateTMP(btnEnviar.transform, "Enviar", 28, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(txtEnviar.gameObject);

        var enviarButton = btnEnviar.AddComponent<Button>();
        enviarButton.targetGraphic = btnEnviar.GetComponent<Image>();
        var enviarColors = enviarButton.colors;
        enviarColors.highlightedColor = TEAL;
        enviarColors.pressedColor = TEAL_PRESSED;
        enviarButton.colors = enviarColors;

        // ═══════════════════════════════════════
        // 6. CONECTAR EntrenadorLLM
        // ═══════════════════════════════════════
        var llm = FindFirstObjectByType<EntrenadorLLM>();
        if (llm != null)
        {
            llm.inputUsuario = inputField;
            llm.textoChat = chatText;
            llm.botonEnviar = enviarButton;
            llm.scrollChat = scrollRect;

            // Conectar el listener y mensaje de bienvenida aquí,
            // porque EntrenadorLLM.Start() corrió antes con referencias null
            enviarButton.onClick.AddListener(llm.EnviarMensaje);
            chatText.text = "<color=#882d94><b>Entrenador:</b> ¡Hola! ¿En qué nos enfocamos hoy?</color>\n";

            Debug.Log("[InitChatbotScene] EntrenadorLLM conectado correctamente a la UI.");
        }
        else
        {
            Debug.LogError("[InitChatbotScene] No se encontró EntrenadorLLM en la escena. Asegúrate de que ChatbotManager tenga el componente.");
        }
    }


    // ═══════════════════════════════════════
    // UTILIDADES UI
    // ═══════════════════════════════════════
    GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    TMP_Text CreateTMP(Transform parent, string text, float size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
