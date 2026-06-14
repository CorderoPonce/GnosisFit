using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class MensajeChat
{
    public string remitente;
    public string contenido;
}

[System.Serializable]
public class HistorialChatWrapper
{
    public System.Collections.Generic.List<MensajeChat> mensajes = new System.Collections.Generic.List<MensajeChat>();
}

public class EntrenadorLLM : MonoBehaviour
{
    [Header("Conexión UI")]
    public TMP_InputField inputUsuario;
    public TextMeshProUGUI textoChat; // Mantener para evitar errores de referencias en inspector
    public Button botonEnviar;
    public ScrollRect scrollChat;

    [Header("Configuración Ollama")]
    public string urlOllama = "https://imply-perjury-yahoo.ngrok-free.dev/api/generate";
    public string nombreModelo = "llama3";

    private string contextoEntrenador = 
        "Eres el entrenador personal experto y profesional de Gnosis Fit. Tu tono es motivador, técnico, directo y seguro. " +
        "REGLAS: " +
        "1. SOLO responde sobre fitness, entrenamiento y salud deportiva. " +
        "2. Si preguntan otros temas, declina cortésmente. " +
        "3. Tienes acceso a este simulador 3D, recomienda SÓLO ejercicios que estén en este listado: Air Squat, Air Squat Bent Arms, Bicep Curl, Bicycle Crunch, Burpee, Circle Crunch, Cross Jumps, Jumping Jacks, Pike Walk, Push Up, y Situps. " +
        "4. Si recomiendas uno, DEBES incluir al final: <link=\"NOMBRE_EJERCICIO\"><u>[Mostrar Ejercicio en 3D]</u></link>. " +
        "5. Respuestas breves para celular.";

    private System.Collections.Generic.List<MensajeChat> listaMensajes = new System.Collections.Generic.List<MensajeChat>();
    private GameObject burbujaEscribiendo = null;
    private bool inicializado = false;

    void Start()
    {
        // En caso de que InitChatbotScene no esté en la escena y no nos inicialice,
        // ejecutamos una inicialización segura al final del frame.
        StartCoroutine(InicializarAlFinalDelFrame());
    }

    private IEnumerator InicializarAlFinalDelFrame()
    {
        yield return new WaitForEndOfFrame();
        if (!inicializado && scrollChat != null)
        {
            InicializarChat();
        }
    }

    public void InicializarChat()
    {
        if (inicializado) return;
        inicializado = true;

        if (botonEnviar != null)
        {
            botonEnviar.onClick.RemoveAllListeners();
            botonEnviar.onClick.AddListener(EnviarMensaje);
        }

        // Cargar historial si existe
        if (GenosisFitDataManager.Instance != null && !string.IsNullOrEmpty(GenosisFitDataManager.Instance.HistorialChat))
        {
            try
            {
                HistorialChatWrapper wrapper = JsonUtility.FromJson<HistorialChatWrapper>(GenosisFitDataManager.Instance.HistorialChat);
                if (wrapper != null && wrapper.mensajes != null && wrapper.mensajes.Count > 0)
                {
                    foreach (var msg in wrapper.mensajes)
                    {
                        listaMensajes.Add(msg);
                        CrearBurbujaUI(msg.remitente, msg.contenido);
                    }
                }
                else
                {
                    AgregarMensaje("Entrenador", "¡Hola! ¿En qué nos enfocamos hoy?");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[EntrenadorLLM] Error decodificando historial JSON: " + e.Message);
                // Intentar recuperar como texto plano viejo si ocurre algún error
                string oldHistory = GenosisFitDataManager.Instance.HistorialChat;
                if (!string.IsNullOrEmpty(oldHistory))
                {
                    // Separar por saltos de línea para intentar reconstruir
                    string[] lines = oldHistory.Split('\n');
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (line.Contains("<b>Tú:</b>"))
                        {
                            string raw = CleanHTMLTags(line).Replace("Tú:", "").Trim();
                            AgregarMensaje("Tú", raw);
                        }
                        else if (line.Contains("<b>Entrenador:</b>"))
                        {
                            string raw = CleanHTMLTags(line).Replace("Entrenador:", "").Trim();
                            AgregarMensaje("Entrenador", raw);
                        }
                    }
                }
                else
                {
                    AgregarMensaje("Entrenador", "¡Hola! ¿En qué nos enfocamos hoy?");
                }
            }
        }
        else
        {
            AgregarMensaje("Entrenador", "¡Hola! ¿En qué nos enfocamos hoy?");
        }

        StartCoroutine(BajarScroll());
    }

    private string CleanHTMLTags(string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
    }

    public void EnviarMensaje()
    {
        if (string.IsNullOrWhiteSpace(inputUsuario.text)) return;
  
        string mensaje = inputUsuario.text;
        AgregarMensaje("Tú", mensaje);
        inputUsuario.text = ""; 
  
        botonEnviar.interactable = false;
        
        GuardarHistorial(); // GUARDAR ANTES del indicador "Escribiendo..."

        // Burbuja temporal de Escribiendo (sin guardar en la lista de mensajes persistentes)
        burbujaEscribiendo = CrearBurbujaUI("Entrenador", "<i>Escribiendo...</i>");
  
        StartCoroutine(BajarScroll());
        StartCoroutine(PeticionOllama(mensaje));
    }

    IEnumerator PeticionOllama(string mensajeUsuario)
    {
        OllamaRequest peticion = new OllamaRequest { model = nombreModelo, prompt = contextoEntrenador + " Usuario: " + mensajeUsuario, stream = false };
        string jsonPeticion = JsonUtility.ToJson(peticion);

        UnityWebRequest request = new UnityWebRequest(urlOllama, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPeticion);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("ngrok-skip-browser-warning", "true");
        request.timeout = 120;

        yield return request.SendWebRequest();

        // Destruir burbuja de escribiendo
        if (burbujaEscribiendo != null)
        {
            Destroy(burbujaEscribiendo);
            burbujaEscribiendo = null;
        }
  
        if (request.result == UnityWebRequest.Result.Success)
        {
            OllamaResponse respuestaOllama = JsonUtility.FromJson<OllamaResponse>(request.downloadHandler.text);
            AgregarMensaje("Entrenador", respuestaOllama.response);
        }
        else
        {
            string errorDetalle = $"HTTP {request.responseCode} | {request.result} | {request.error}";
            Debug.LogError($"[EntrenadorLLM] Error de conexión: {errorDetalle}");
 
            string mensajeError;
            if (request.result == UnityWebRequest.Result.ConnectionError)
                mensajeError = "No se pudo conectar con el servidor. Verifica que Ngrok y Ollama estén corriendo.";
            else if (request.responseCode == 0)
                mensajeError = "Timeout: El servidor tardó demasiado en responder. Intenta de nuevo.";
            else if (request.responseCode == 502 || request.responseCode == 503)
                mensajeError = $"Servidor no disponible (HTTP {request.responseCode}). ¿Está Ollama corriendo?";
            else
                mensajeError = $"Error de conexión (HTTP {request.responseCode}). Revisa la consola para más detalles.";
 
            AgregarMensaje("Sistema", mensajeError);
        }
  
        GuardarHistorial();

        botonEnviar.interactable = true;
        StartCoroutine(BajarScroll());
    }

    private void AgregarMensaje(string remitente, string contenido)
    {
        listaMensajes.Add(new MensajeChat { remitente = remitente, contenido = contenido });
        CrearBurbujaUI(remitente, contenido);
    }

    private void GuardarHistorial()
    {
        if (GenosisFitDataManager.Instance != null)
        {
            HistorialChatWrapper wrapper = new HistorialChatWrapper { mensajes = listaMensajes };
            GenosisFitDataManager.Instance.HistorialChat = JsonUtility.ToJson(wrapper);
        }
    }

    IEnumerator BajarScroll()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        
        if (scrollChat != null)
        {
            scrollChat.verticalNormalizedPosition = 0f;
        }
    }

    private GameObject CrearBurbujaUI(string remitente, string texto)
    {
        if (scrollChat == null || scrollChat.content == null) return null;

        var bubble = new GameObject("Bubble_" + remitente, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bubble.transform.SetParent(scrollChat.content, false);

        var img = bubble.GetComponent<Image>();
        img.type = Image.Type.Sliced;

        Color fillColor = new Color(0.12f, 0.12f, 0.18f, 0.85f);
        Color strokeColor = Color.clear;
        float strokeWidth = 0f;

        if (remitente == "Entrenador")
        {
            strokeColor = new Color(0.70f, 0.20f, 0.80f, 1f); // Neon purple/magenta
            strokeWidth = 2.5f;
        }
        else if (remitente == "Tú")
        {
            strokeColor = new Color(0.10f, 0.75f, 0.65f, 1f); // Teal/cyan
            strokeWidth = 2.5f;
        }
        else // Sistema / Error
        {
            strokeColor = new Color(0.90f, 0.25f, 0.25f, 1f); // Rojo
            strokeWidth = 2.5f;
        }

        // Crear Sprite redondeado procedimental con borde (radio 24)
        img.sprite = CreateRoundedSprite(128, 128, 24, fillColor, strokeColor, strokeWidth);
        img.color = Color.white;

        // Auto-layout
        var le = bubble.AddComponent<LayoutElement>();
        le.minHeight = 80;
        le.flexibleWidth = 1;

        var vlg = bubble.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(25, 25, 20, 20);

        // Texto interno
        var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(bubble.transform, false);

        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = $"<b>{remitente}:</b> {texto}";
        tmp.fontSize = 28;
        tmp.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.richText = true;
        tmp.alignment = TextAlignmentOptions.TopLeft;

        if (remitente == "Entrenador")
        {
            textGO.AddComponent<EnlacesChat>();
        }

        return bubble;
    }

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

[System.Serializable]
public class OllamaRequest { public string model; public string prompt; public bool stream; }
[System.Serializable]
public class OllamaResponse { public string response; }