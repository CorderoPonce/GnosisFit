using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class SupervisionHUD : MonoBehaviour
{
    private AnalisisPostura analisis;
    private GestorModoSupervision gestorSupervision;
    private string nombreEjercicioActual;

    private GameObject panelPrincipal;
    private GameObject panelResumen;
    private TextMeshProUGUI textoNombreEjercicio;
    private TextMeshProUGUI textoReps;
    private TextMeshProUGUI textoAngulo;
    private TextMeshProUGUI textoFeedback;
    private Image barraProgreso;
    private Image fondoBarra;
    private TextMeshProUGUI textoResumenTitulo;
    private TextMeshProUGUI textoResumenStats;
    private TextMeshProUGUI textoResumenIA; 
    private RawImage rawImgPIP; // Referencia interna de la imagen

    private static readonly Color FONDO_CARD = new Color(0.05f, 0.05f, 0.12f, 0.85f);
    private static readonly Color ACCENT = new Color(0.2f, 0.75f, 1f);
    private static readonly Color ACCENT_GREEN = new Color(0.3f, 1f, 0.5f);
    private static readonly Color BARRA_BG = new Color(0.2f, 0.2f, 0.3f, 0.8f);
    private static readonly Color BARRA_FILL = new Color(0.2f, 0.8f, 1f);

    private string urlOllama = "https://gloomily-disparity-evoke.ngrok-free.dev/api/generate";
    private string nombreModelo = "llama3";

    public void Inicializar(Canvas canvas, AnalisisPostura analisisRef, GestorModoSupervision gestorRef, string nombreEjercicio)
    {
        analisis = analisisRef;
        gestorSupervision = gestorRef;
        nombreEjercicioActual = nombreEjercicio;

        if (analisis != null) { analisis.OnRepCompletada += OnRepCompletada; analisis.OnDataReceived += ActualizarVista; }
        CrearUI(canvas.transform, nombreEjercicio);
    }

    private void CrearUI(Transform padre, string nombreEjercicio)
    {
        panelPrincipal = new GameObject("PanelSupervisionHUD", typeof(RectTransform));
        panelPrincipal.transform.SetParent(padre, false);
        var rtMain = panelPrincipal.GetComponent<RectTransform>();
        UIHelper.SetAnchorsStretch(rtMain);

        var headerPanel = UIHelper.CrearPanel("Header", panelPrincipal.transform, new Color(0, 0, 0, 0.7f), true);
        var rtHeader = headerPanel.GetComponent<RectTransform>();
        rtHeader.anchorMin = new Vector2(0, 1); rtHeader.anchorMax = new Vector2(1, 1);
        rtHeader.pivot = new Vector2(0.5f, 1); rtHeader.sizeDelta = new Vector2(0, 80);
        rtHeader.anchoredPosition = new Vector2(0, -40);

        textoNombreEjercicio = UIHelper.CrearTexto("NombreEj", headerPanel.transform, $"MODO: {nombreEjercicio.ToUpper()}", 28, Color.white, FontStyles.Bold);
        UIHelper.SetAnchorsStretch(textoNombreEjercicio.GetComponent<RectTransform>());

        // Ventana Picture-in-Picture 3D
        var pipPanel = UIHelper.CrearPanel("PIP_Panel", panelPrincipal.transform, new Color(0.1f, 0.1f, 0.15f, 0.8f), true);
        var rtPip = pipPanel.GetComponent<RectTransform>();
        rtPip.anchorMin = new Vector2(1, 1); rtPip.anchorMax = new Vector2(1, 1);
        rtPip.pivot = new Vector2(1, 1); 
        rtPip.sizeDelta = new Vector2(260, 260);
        rtPip.anchoredPosition = new Vector2(-40, -140); 
        pipPanel.AddComponent<RectMask2D>(); 

        var rawImgObj = new GameObject("PIP_RawImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        rawImgObj.transform.SetParent(pipPanel.transform, false);
        rawImgPIP = rawImgObj.GetComponent<RawImage>();
        UIHelper.SetAnchorsStretch(rawImgPIP.GetComponent<RectTransform>());

        // CORRECCIÓN: Asignación directa y segura de la textura
        if (gestorSupervision != null && gestorSupervision.renderTexturePIP != null) {
            rawImgPIP.texture = gestorSupervision.renderTexturePIP;
        }

        var repsPanel = UIHelper.CrearPanel("RepsCard", panelPrincipal.transform, FONDO_CARD, true);
        var rtReps = repsPanel.GetComponent<RectTransform>();
        rtReps.anchorMin = new Vector2(0.5f, 0.5f); rtReps.anchorMax = new Vector2(0.5f, 0.5f);
        rtReps.pivot = new Vector2(0.5f, 0.5f); rtReps.sizeDelta = new Vector2(200, 140);
        rtReps.anchoredPosition = new Vector2(0, 100);

        textoReps = UIHelper.CrearTexto("NumReps", repsPanel.transform, "0", 72, ACCENT, FontStyles.Bold);
        var rtRepsNum = textoReps.GetComponent<RectTransform>();
        rtRepsNum.anchorMin = new Vector2(0, 0.3f); rtRepsNum.anchorMax = new Vector2(1, 1);
        rtRepsNum.sizeDelta = Vector2.zero; rtRepsNum.anchoredPosition = Vector2.zero;

        var textoLabel = UIHelper.CrearTexto("LabelReps", repsPanel.transform, "REPS", 18, new Color(0.6f, 0.6f, 0.7f), FontStyles.Bold);
        var rtLabel = textoLabel.GetComponent<RectTransform>();
        rtLabel.anchorMin = new Vector2(0, 0); rtLabel.anchorMax = new Vector2(1, 0.3f);
        rtLabel.sizeDelta = Vector2.zero; rtLabel.anchoredPosition = Vector2.zero;

        var barraPanel = UIHelper.CrearPanel("BarraPanel", panelPrincipal.transform, FONDO_CARD, true);
        var rtBarraPanel = barraPanel.GetComponent<RectTransform>();
        rtBarraPanel.anchorMin = new Vector2(0.5f, 0.5f); rtBarraPanel.anchorMax = new Vector2(0.5f, 0.5f);
        rtBarraPanel.pivot = new Vector2(0.5f, 0.5f); rtBarraPanel.sizeDelta = new Vector2(320, 80);
        rtBarraPanel.anchoredPosition = new Vector2(0, -10);

        textoAngulo = UIHelper.CrearTexto("Angulo", barraPanel.transform, "0°", 22, Color.white, FontStyles.Normal);
        var rtAngulo = textoAngulo.GetComponent<RectTransform>();
        rtAngulo.anchorMin = new Vector2(0, 0.5f); rtAngulo.anchorMax = new Vector2(1, 1);
        rtAngulo.sizeDelta = Vector2.zero; rtAngulo.anchoredPosition = Vector2.zero;

        fondoBarra = UIHelper.CrearPanel("BarraFondo", barraPanel.transform, BARRA_BG, true).GetComponent<Image>();
        var rtFondoBarra = fondoBarra.GetComponent<RectTransform>();
        rtFondoBarra.anchorMin = new Vector2(0.05f, 0.1f); rtFondoBarra.anchorMax = new Vector2(0.95f, 0.45f);
        rtFondoBarra.sizeDelta = Vector2.zero; rtFondoBarra.anchoredPosition = Vector2.zero;

        var fillGO = UIHelper.CrearPanel("BarraFill", fondoBarra.transform, BARRA_FILL, true);
        barraProgreso = fillGO.GetComponent<Image>();
        var rtFill = barraProgreso.GetComponent<RectTransform>();
        rtFill.anchorMin = Vector2.zero; rtFill.anchorMax = new Vector2(0, 1);
        rtFill.pivot = new Vector2(0, 0.5f); rtFill.sizeDelta = Vector2.zero; rtFill.anchoredPosition = Vector2.zero;

        var feedbackPanel = UIHelper.CrearPanel("FeedbackCard", panelPrincipal.transform, FONDO_CARD, true);
        var rtFeedback = feedbackPanel.GetComponent<RectTransform>();
        rtFeedback.anchorMin = new Vector2(0.5f, 0.5f); rtFeedback.anchorMax = new Vector2(0.5f, 0.5f);
        rtFeedback.pivot = new Vector2(0.5f, 0.5f); rtFeedback.sizeDelta = new Vector2(800, 160);
        rtFeedback.anchoredPosition = new Vector2(0, -150);

        textoFeedback = UIHelper.CrearTexto("Feedback", feedbackPanel.transform, "Preparándose...", 60, Color.white, FontStyles.Italic);
        textoFeedback.alignment = TextAlignmentOptions.Center;
        UIHelper.SetAnchorsStretch(textoFeedback.GetComponent<RectTransform>());

        var btnTerminar = UIHelper.CrearBoton("BtnTerminar", panelPrincipal.transform, "TERMINAR", new Color(0.8f, 0.2f, 0.2f, 0.9f), Color.white, 26, true);
        var rtBtn = btnTerminar.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(0.5f, 0); rtBtn.anchorMax = new Vector2(0.5f, 0);
        rtBtn.pivot = new Vector2(0.5f, 0); rtBtn.sizeDelta = new Vector2(220, 60);
        rtBtn.anchoredPosition = new Vector2(0, 80);
        btnTerminar.onClick.AddListener(MostrarResumen);

        CrearPanelResumen(padre);
    }

    private void CrearPanelResumen(Transform padre)
    {
        panelResumen = UIHelper.CrearPanel("PanelResumen", padre, new Color(0.02f, 0.02f, 0.08f, 0.95f), false);
        var rt = panelResumen.GetComponent<RectTransform>();
        UIHelper.SetAnchorsStretch(rt);

        textoResumenTitulo = UIHelper.CrearTexto("ResumenTitulo", panelResumen.transform, "SESIÓN COMPLETA", 36, ACCENT_GREEN, FontStyles.Bold);
        var rtTitulo = textoResumenTitulo.GetComponent<RectTransform>();
        rtTitulo.anchorMin = new Vector2(0, 0.80f); rtTitulo.anchorMax = new Vector2(1, 0.95f);
        rtTitulo.sizeDelta = Vector2.zero; rtTitulo.anchoredPosition = Vector2.zero;

        textoResumenStats = UIHelper.CrearTexto("ResumenStats", panelResumen.transform, "", 26, Color.white, FontStyles.Normal);
        textoResumenStats.textWrappingMode = TextWrappingModes.Normal;
        var rtStats = textoResumenStats.GetComponent<RectTransform>();
        rtStats.anchorMin = new Vector2(0.1f, 0.55f); rtStats.anchorMax = new Vector2(0.9f, 0.80f);
        rtStats.sizeDelta = Vector2.zero; rtStats.anchoredPosition = Vector2.zero;

        var bgIA = UIHelper.CrearPanel("FondoIA", panelResumen.transform, new Color(0.1f, 0.1f, 0.15f, 0.9f), true);
        var rtBgIA = bgIA.GetComponent<RectTransform>();
        rtBgIA.anchorMin = new Vector2(0.1f, 0.25f); rtBgIA.anchorMax = new Vector2(0.9f, 0.50f);
        rtBgIA.sizeDelta = Vector2.zero; rtBgIA.anchoredPosition = Vector2.zero;

        textoResumenIA = UIHelper.CrearTexto("ResumenIA", bgIA.transform, "Generando análisis con IA...", 22, new Color(0.8f, 0.6f, 1f), FontStyles.Italic);
        textoResumenIA.textWrappingMode = TextWrappingModes.Normal;
        var rtIA = textoResumenIA.GetComponent<RectTransform>();
        rtIA.anchorMin = new Vector2(0.05f, 0.05f); rtIA.anchorMax = new Vector2(0.95f, 0.95f);
        rtIA.sizeDelta = Vector2.zero; rtIA.anchoredPosition = Vector2.zero;

        var btnVolver = UIHelper.CrearBoton("BtnVolver", panelResumen.transform, "VOLVER AL MENÚ", ACCENT, Color.black, 28, true);
        var rtBtn = btnVolver.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(0.5f, 0.1f); rtBtn.anchorMax = new Vector2(0.5f, 0.1f);
        rtBtn.pivot = new Vector2(0.5f, 0); rtBtn.sizeDelta = new Vector2(260, 70);
        rtBtn.anchoredPosition = Vector2.zero;
        btnVolver.onClick.AddListener(VolverAR);

        panelResumen.SetActive(false);
    }

    private void ActualizarVista(){
            if (analisis == null || panelPrincipal == null || !panelPrincipal.activeSelf) return;

            // Activamos el panel Picture-in-Picture solo si hay una textura 3D PIP asignada
            if (rawImgPIP != null) 
            {
                bool tieneTextura = rawImgPIP.texture != null;
                rawImgPIP.transform.parent.gameObject.SetActive(tieneTextura);
            }

            textoReps.text = analisis.repeticiones.ToString();

            if (analisis.ejercicioActual == TipoSupervision.Plank)
                textoAngulo.text = $"Tiempo: {(Time.time - analisis.tiempoInicio):F0}s";
            else if (analisis.ejercicioActual == TipoSupervision.JumpingJack)
                textoAngulo.text = $"Apertura: {analisis.anguloActual:F0}%";
            else
                textoAngulo.text = $"Ángulo: {analisis.anguloActual:F0}°";

            var rtFill = barraProgreso.GetComponent<RectTransform>();
            rtFill.anchorMax = new Vector2(analisis.progreso, 1);
            barraProgreso.color = Color.Lerp(BARRA_FILL, ACCENT_GREEN, analisis.progreso);

            textoFeedback.text = analisis.feedback;
            textoFeedback.color = analisis.colorFeedback;
    }

    private void OnRepCompletada()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
        if (textoReps != null) textoReps.color = ACCENT_GREEN;
        Invoke(nameof(RestaurarColorReps), 0.3f);
    }

    private void RestaurarColorReps() { if (textoReps != null) textoReps.color = ACCENT; }

    private void MostrarResumen()
    {
        if (panelPrincipal != null) panelPrincipal.SetActive(false);
        if (panelResumen != null) panelResumen.SetActive(true);

        float duracion = Time.time - analisis.tiempoInicio;
        int minutos = Mathf.FloorToInt(duracion / 60);
        int segundos = Mathf.FloorToInt(duracion % 60);
        string tiempoStr = minutos > 0 ? $"{minutos}m {segundos}s" : $"{segundos}s";
        float repsPerMin = duracion > 10 ? (analisis.repeticiones / (duracion / 60f)) : 0;

        textoResumenStats.text =
            $"<b>Repeticiones:</b> {analisis.repeticiones}\n\n" +
            $"<b>Duración:</b> {tiempoStr}\n\n" +
            $"<b>Ritmo:</b> {repsPerMin:F1} reps/min\n\n" +
            $"<color=#ffaa00><b>Advertencias de postura:</b> {analisis.advertenciasPostura}</color>\n" +
            $"<color=#ff7777><b>Reps muy rápidas (rebote):</b> {analisis.repeticionesRapidas}</color>";

        StartCoroutine(SolicitarFeedbackIA(duracion));
    }

    private IEnumerator SolicitarFeedbackIA(float duracion)
    {
        textoResumenIA.text = "Analizando tu rendimiento con Llama 3...";
        string promptInterno = $"Eres un entrenador personal. El usuario acaba de terminar de hacer {nombreEjercicioActual}. Hizo {analisis.repeticiones} repeticiones en {duracion:F0} segundos. ";

        if (analisis.repeticionesRapidas > 0) promptInterno += $"CUIDADO: Tuvo {analisis.repeticionesRapidas} repeticiones demasiado rápidas (rebote). ";
        if (analisis.erroresCometidos.Count > 0) promptInterno += $"Tuvo {analisis.advertenciasPostura} fallos. Errores detectados: {string.Join(", ", analisis.erroresCometidos)}. ";
        else promptInterno += $"Técnica perfecta, 0 advertencias de postura. Felicítalo. ";

        promptInterno += "Escribe un comentario directo y breve (máximo 4 líneas). Háblale directamente al usuario.";

        OllamaRequest peticion = new OllamaRequest { model = nombreModelo, prompt = promptInterno, stream = false };
        UnityWebRequest request = new UnityWebRequest(urlOllama, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(peticion)));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("ngrok-skip-browser-warning", "true");
        request.timeout = 20; 

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            OllamaResponse respuesta = JsonUtility.FromJson<OllamaResponse>(request.downloadHandler.text);
            textoResumenIA.text = $"<b>Entrenador IA:</b>\n\"{respuesta.response}\"";
        } else {
            textoResumenIA.text = $"<i>Servidor IA desconectado.</i>\n\n<b>[DEBUG PROMPT]:</b>\n<color=#aaffaa>{promptInterno}</color>";
            textoResumenIA.color = new Color(0.8f, 0.8f, 0.8f);
        }
    }

    private void VolverAR()
    {
        if (analisis != null) { analisis.OnRepCompletada -= OnRepCompletada; analisis.OnDataReceived -= ActualizarVista; }
        UnityEngine.SceneManagement.SceneManager.LoadScene("Scene_Menu");
    }

    void OnDestroy()
    {
        if (analisis != null) { analisis.OnRepCompletada -= OnRepCompletada; analisis.OnDataReceived -= ActualizarVista; }
    }
}