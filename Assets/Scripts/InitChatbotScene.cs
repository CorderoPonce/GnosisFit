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
    static readonly Color HEADER_BG      = new Color(0.06f, 0.06f, 0.10f, 1.0f); // Translucidez se maneja en el Sprite
    static readonly Color CHAT_AREA_BG   = new Color(0.12f, 0.12f, 0.18f, 1f);
    static readonly Color INPUT_BG       = new Color(0.12f, 0.12f, 0.16f, 1f); // Caja oscura
    static readonly Color INPUT_BAR_BG   = new Color(0.08f, 0.08f, 0.12f, 0.95f); // Translúcido
    static readonly Color TEAL           = new Color(0.10f, 0.75f, 0.65f, 1f); // Teal de Enviar
    static readonly Color TEAL_PRESSED   = new Color(0.08f, 0.65f, 0.55f, 1f);
    static readonly Color NEON_PURPLE    = new Color(0.70f, 0.20f, 0.80f, 1f); // Púrpura de cabecera/borde
    static readonly Color TEXT_WHITE     = new Color(0.95f, 0.95f, 0.95f, 1f);
    static readonly Color TEXT_GRAY      = new Color(0.55f, 0.55f, 0.60f, 1f);

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
        // 3. HEADER (Contenedor + Fondo redondeado con desbordamiento superior)
        // ═══════════════════════════════════════
        var header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(canvasGO.transform, false);
        var headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 140);
        headerRT.anchoredPosition = Vector2.zero;

        // Fondo del Header (para ocultar las esquinas redondeadas superiores)
        var headerBG = CreatePanel(header.transform, "HeaderBG", Color.white);
        var headerBGImg = headerBG.GetComponent<Image>();
        headerBGImg.sprite = CreateRoundedSprite(128, 128, 40, new Color(0.08f, 0.08f, 0.12f, 0.95f), Color.clear, 0f); // Translucidez glassmorphism
        headerBGImg.type = Image.Type.Sliced;
        var headerBGRT = headerBG.GetComponent<RectTransform>();
        headerBGRT.anchorMin = Vector2.zero;
        headerBGRT.anchorMax = Vector2.one;
        headerBGRT.offsetMin = Vector2.zero;
        headerBGRT.offsetMax = new Vector2(0, 40); // 40px overflow arriba

        // Botón Volver (Circular translúcido con contorno púrpura)
        var btnVolver = CreatePanel(header.transform, "BtnVolver", Color.white);
        var btnVolverImg = btnVolver.GetComponent<Image>();
        btnVolverImg.sprite = CreateRoundedSprite(128, 128, 64, new Color(0.12f, 0.14f, 0.18f, 0.5f), NEON_PURPLE, 2f);
        btnVolverImg.type = Image.Type.Sliced;

        var btnVolverRT = btnVolver.GetComponent<RectTransform>();
        btnVolverRT.anchorMin = new Vector2(0, 0.5f);
        btnVolverRT.anchorMax = new Vector2(0, 0.5f);
        btnVolverRT.pivot = new Vector2(0, 0.5f);
        btnVolverRT.sizeDelta = new Vector2(90, 90);
        btnVolverRT.anchoredPosition = new Vector2(40, 0); // Centrado verticalmente respecto a la cabecera visible

        var txtVolver = CreateTMP(btnVolver.transform, "<", 32, NEON_PURPLE, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(txtVolver.gameObject);

        var volverBtn = btnVolver.AddComponent<Button>();
        volverBtn.targetGraphic = btnVolverImg;
        volverBtn.onClick.AddListener(() => {
            if (GenosisFitDataManager.Instance != null && GenosisFitDataManager.Instance.VieneDeAR)
            {
                GenosisFitDataManager.Instance.VieneDeAR = false; // Reset!
                SceneManager.LoadScene("ARMode");
            }
            else
            {
                SceneManager.LoadScene("Scene_Menu");
            }
        });

        // Título
        var titulo = CreateTMP(header.transform, "Entrenador IA", 40, TEXT_WHITE, FontStyles.Bold, TextAlignmentOptions.Center);
        var tituloRT = titulo.GetComponent<RectTransform>();
        tituloRT.anchorMin = new Vector2(0.15f, 0);
        tituloRT.anchorMax = new Vector2(0.85f, 1);
        tituloRT.sizeDelta = Vector2.zero;
        tituloRT.anchoredPosition = new Vector2(0, 15);

        // Subtítulo
        var subtitulo = CreateTMP(header.transform, "Powered by Ollama - Llama 3", 20, TEXT_GRAY, FontStyles.Normal, TextAlignmentOptions.Center);
        var subRT = subtitulo.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.15f, 0);
        subRT.anchorMax = new Vector2(0.85f, 1);
        subRT.sizeDelta = Vector2.zero;
        subRT.anchoredPosition = new Vector2(0, -22);

        // ═══════════════════════════════════════
        // 4. ÁREA DE CHAT (ScrollRect)
        // ═══════════════════════════════════════
        var chatArea = CreatePanel(canvasGO.transform, "ChatArea", Color.clear); // Transparente para usar el fondo general
        var chatAreaRT = chatArea.GetComponent<RectTransform>();
        chatAreaRT.anchorMin = new Vector2(0, 0);
        chatAreaRT.anchorMax = new Vector2(1, 1);
        chatAreaRT.offsetMin = new Vector2(0, 160);   // espacio para input bar
        chatAreaRT.offsetMax = new Vector2(0, -140);  // espacio para header

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
        vpRT.offsetMin = new Vector2(40, 20); // Márgenes más amplios
        vpRT.offsetMax = new Vector2(-40, -20);
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
        vlg.spacing = 25; // Más espacio entre las burbujas individuales
        vlg.padding = new RectOffset(0, 0, 10, 10);

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

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

        // EmpujarChat para teclados de móvil
        var empujar = inputBar.AddComponent<EmpujarChat>();
        empujar.barraEntrada = inputBarRT;
        empujar.alturaSalto = 900f;

        // Input Field (Redondeado oscuro)
        var inputFieldGO = CreatePanel(inputBar.transform, "ChatInputField", Color.white);
        var inputFieldImg = inputFieldGO.GetComponent<Image>();
        inputFieldImg.sprite = CreateRoundedSprite(128, 128, 24, INPUT_BG, Color.clear, 0f);
        inputFieldImg.type = Image.Type.Sliced;

        var inputFieldRT = inputFieldGO.GetComponent<RectTransform>();
        inputFieldRT.anchorMin = new Vector2(0, 0.5f);
        inputFieldRT.anchorMax = new Vector2(1, 0.5f);
        inputFieldRT.pivot = new Vector2(0, 0.5f);
        inputFieldRT.sizeDelta = new Vector2(-310, 90); // Más ancho
        inputFieldRT.anchoredPosition = new Vector2(40, 0);

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
        placeholder.textWrappingMode = TextWrappingModes.NoWrap; // Evitar saltos de línea raros
        Stretch(placeholderGO);

        // Input Text
        var inputTextGO = new GameObject("InputText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        inputTextGO.transform.SetParent(textArea.transform, false);
        var inputText = inputTextGO.GetComponent<TextMeshProUGUI>();
        inputText.fontSize = 28;
        inputText.color = TEXT_WHITE;
        inputText.alignment = TextAlignmentOptions.Left;
        inputText.margin = new Vector4(15, 0, 15, 0);
        inputText.textWrappingMode = TextWrappingModes.NoWrap;
        Stretch(inputTextGO);

        // TMP_InputField component
        var inputField = inputFieldGO.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRT;
        inputField.textComponent = inputText;
        inputField.placeholder = placeholder;
        inputField.fontAsset = inputText.font;
        inputField.pointSize = 28;

        // Botón Enviar (Cápsula Teal con Sombra Resplandor)
        var btnEnviar = CreatePanel(inputBar.transform, "BtnEnviar", Color.white);
        var btnEnviarImg = btnEnviar.GetComponent<Image>();
        btnEnviarImg.sprite = CreateRoundedSprite(128, 128, 45, TEAL, Color.clear, 0f);
        btnEnviarImg.type = Image.Type.Sliced;

        var btnEnviarRT = btnEnviar.GetComponent<RectTransform>();
        btnEnviarRT.anchorMin = new Vector2(1, 0.5f);
        btnEnviarRT.anchorMax = new Vector2(1, 0.5f);
        btnEnviarRT.pivot = new Vector2(1, 0.5f);
        btnEnviarRT.sizeDelta = new Vector2(220, 90);
        btnEnviarRT.anchoredPosition = new Vector2(-40, 0);

        // Resplandor Teal
        var sendGlow = btnEnviar.AddComponent<Shadow>();
        sendGlow.effectColor = new Color(0.10f, 0.75f, 0.65f, 0.45f);
        sendGlow.effectDistance = new Vector2(0f, -4f);

        var txtEnviar = CreateTMP(btnEnviar.transform, "Enviar", 28, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(txtEnviar.gameObject);

        var enviarButton = btnEnviar.AddComponent<Button>();
        enviarButton.targetGraphic = btnEnviarImg;
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
            llm.botonEnviar = enviarButton;
            llm.scrollChat = scrollRect;

            enviarButton.onClick.AddListener(llm.EnviarMensaje);
            Debug.Log("[InitChatbotScene] EntrenadorLLM conectado correctamente al sistema de burbujas.");
            
            // Inicializar el chat ahora que la UI está construida y conectada
            llm.InicializarChat();
        }
        else
        {
            Debug.LogError("[InitChatbotScene] No se encontró EntrenadorLLM en la escena.");
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
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
    }

    // Generador de sprite redondeado procedimental con opción de contorno
    private Sprite CreateRoundedSprite(int width, int height, int radius, Color fillColor, Color strokeColor, float strokeWidth)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float cx = x;
                float cy = y;
                if (x < radius) cx = radius;
                else if (x >= width - radius) cx = width - radius - 1;
                
                if (y < radius) cy = radius;
                else if (y >= height - radius) cy = height - radius - 1;
                
                Color finalColor = Color.clear;
                
                if (cx != x || cy != y)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    float outerEdge = radius;
                    float innerEdge = radius - strokeWidth;
                    
                    if (dist > outerEdge + 0.5f)
                    {
                        finalColor = Color.clear;
                    }
                    else if (dist > outerEdge - 0.5f)
                    {
                        float t = (outerEdge + 0.5f - dist);
                        if (strokeWidth > 0)
                            finalColor = Color.Lerp(Color.clear, strokeColor, t);
                        else
                            finalColor = Color.Lerp(Color.clear, fillColor, t);
                    }
                    else if (strokeWidth > 0 && dist > innerEdge + 0.5f)
                    {
                        finalColor = strokeColor;
                    }
                    else if (strokeWidth > 0 && dist > innerEdge - 0.5f)
                    {
                        float t = (innerEdge + 0.5f - dist);
                        finalColor = Color.Lerp(strokeColor, fillColor, t);
                    }
                    else
                    {
                        finalColor = fillColor;
                    }
                }
                else
                {
                    if (strokeWidth > 0)
                    {
                        float minEdgeDist = Mathf.Min(x, Mathf.Min(width - 1 - x, Mathf.Min(y, height - 1 - y)));
                        if (minEdgeDist < strokeWidth - 0.5f)
                        {
                            finalColor = strokeColor;
                        }
                        else if (minEdgeDist < strokeWidth + 0.5f)
                        {
                            float t = (minEdgeDist - (strokeWidth - 0.5f));
                            finalColor = Color.Lerp(strokeColor, fillColor, t);
                        }
                        else
                        {
                            finalColor = fillColor;
                        }
                    }
                    else
                    {
                        finalColor = fillColor;
                    }
                }
                
                cols[y * width + x] = finalColor;
            }
        }
        
        tex.SetPixels(cols);
        tex.Apply();
        
        Vector4 border = new Vector4(radius, radius, radius, radius);
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.Tight, border);
        return sprite;
    }
}
