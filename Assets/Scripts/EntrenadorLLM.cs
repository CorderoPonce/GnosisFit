using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class EntrenadorLLM : MonoBehaviour
{
    [Header("Conexión UI")]
    public TMP_InputField inputUsuario;
    public TextMeshProUGUI textoChat;
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

    private string colorUsuario = "#ffffff"; 
    private string colorEntrenador = "#882d94";

    void Start()
    {
        botonEnviar.onClick.AddListener(EnviarMensaje);
        textoChat.text = $"<color={colorEntrenador}><b>Entrenador:</b> ¡Hola! ¿En qué nos enfocamos hoy?</color>\n";
    }

    public void EnviarMensaje()
    {
        if (string.IsNullOrWhiteSpace(inputUsuario.text)) return;

        string mensaje = inputUsuario.text;
        textoChat.text += $"\n<color={colorUsuario}><b>Tú:</b> {mensaje}</color>\n\n";
        inputUsuario.text = ""; 

        botonEnviar.interactable = false;
        textoChat.text += $"<color={colorEntrenador}><b>Entrenador:</b> <i>Escribiendo...</i></color>\n";

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
        request.timeout = 120; // Llama3 puede tardar en responder, especialmente en la primera invocación

        yield return request.SendWebRequest();

        textoChat.text = textoChat.text.Replace($"<color={colorEntrenador}><b>Entrenador:</b> <i>Escribiendo...</i></color>\n", "");

        if (request.result == UnityWebRequest.Result.Success)
        {
            OllamaResponse respuestaOllama = JsonUtility.FromJson<OllamaResponse>(request.downloadHandler.text);
            textoChat.text += $"<color={colorEntrenador}><b>Entrenador:</b> {respuestaOllama.response}</color>\n";
        }
        else
        {
            // Logging detallado para diagnóstico
            string errorDetalle = $"HTTP {request.responseCode} | {request.result} | {request.error}";
            Debug.LogError($"[EntrenadorLLM] Error de conexión: {errorDetalle}");
            Debug.LogError($"[EntrenadorLLM] URL: {urlOllama}");
            if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
                Debug.LogError($"[EntrenadorLLM] Response Body: {request.downloadHandler.text}");

            // Mensaje descriptivo para el usuario según el tipo de error
            string mensajeError;
            if (request.result == UnityWebRequest.Result.ConnectionError)
                mensajeError = "No se pudo conectar con el servidor. Verifica que Ngrok y Ollama estén corriendo.";
            else if (request.responseCode == 0)
                mensajeError = "Timeout: El servidor tardó demasiado en responder. Intenta de nuevo.";
            else if (request.responseCode == 502 || request.responseCode == 503)
                mensajeError = $"Servidor no disponible (HTTP {request.responseCode}). ¿Está Ollama corriendo?";
            else
                mensajeError = $"Error de conexión (HTTP {request.responseCode}). Revisa la consola para más detalles.";

            textoChat.text += $"<color=red>{mensajeError}</color>\n";
        }

        botonEnviar.interactable = true;
        StartCoroutine(BajarScroll());
    }

    IEnumerator BajarScroll()
    {
        // Esperamos dos frames para que el texto se renderice y el ContentSizeFitter actualice el tamaño
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        
        if (scrollChat != null)
        {
            scrollChat.verticalNormalizedPosition = 0f;
        }
    }
}

// ESTAS CLASES SON OBLIGATORIAS PARA QUE EL SCRIPT FUNCIONE
[System.Serializable]
public class OllamaRequest { public string model; public string prompt; public bool stream; }
[System.Serializable]
public class OllamaResponse { public string response; }