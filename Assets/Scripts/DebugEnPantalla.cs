using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Muestra logs de Debug.Log en pantalla en tiempo real.
/// Agrégalo como componente a cualquier GameObject en la escena.
/// Activar/desactivar tocando con 3 dedos simultáneamente.
/// </summary>
public class DebugEnPantalla : MonoBehaviour
{
    private static DebugEnPantalla instancia;
    private static readonly List<string> mensajes = new List<string>();
    private const int MAX_MENSAJES = 12;

    private GameObject panelDebug;
    private TextMeshProUGUI textoLog;
    private bool visible = true;

    void Awake()
    {
        // Singleton
        if (instancia != null) { Destroy(gameObject); return; }
        instancia = this;
        DontDestroyOnLoad(gameObject);

        Application.logMessageReceived += CapturarLog;
        CrearPanelDebug();
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= CapturarLog;
    }

    static void CapturarLog(string mensaje, string stackTrace, LogType tipo)
    {
        // Ignorar spam de la fuente y warnings genéricos de Unity
        if (mensaje.Contains("Unicode") || mensaje.Contains("font asset") || mensaje.Contains("FindObjectOfType"))
            return;

        string prefijo = tipo == LogType.Warning ? "[W] " :
                         tipo == LogType.Error   ? "[ERR] " : "";

        // Solo mostramos logs relevantes para no saturar
        if (mensaje.StartsWith("[GestorSupervision]") || tipo == LogType.Error)
        {
            mensajes.Add(prefijo + mensaje);
            if (mensajes.Count > MAX_MENSAJES)
                mensajes.RemoveAt(0);

            if (instancia != null && instancia.textoLog != null)
                instancia.textoLog.text = string.Join("\n", mensajes);
        }
    }

    void CrearPanelDebug()
    {
        // Sin GraphicRaycaster para que el panel NO bloquee los toques
        var canvasGO = new GameObject("DebugCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        panelDebug = new GameObject("PanelDebug", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelDebug.transform.SetParent(canvasGO.transform, false);
        var img = panelDebug.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.75f);
        img.raycastTarget = false; // No interceptar toques

        var rt = panelDebug.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0.45f); // Solo ocupa la parte inferior
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var textoGO = new GameObject("TextoLog", typeof(RectTransform), typeof(TextMeshProUGUI));
        textoGO.transform.SetParent(panelDebug.transform, false);
        textoLog = textoGO.GetComponent<TextMeshProUGUI>();
        textoLog.fontSize = 20;
        textoLog.color = Color.white;
        textoLog.alignment = TextAlignmentOptions.BottomLeft;
        textoLog.textWrappingMode = TextWrappingModes.Normal;
        textoLog.raycastTarget = false; // No interceptar toques

        var trt = textoGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(20, 10);
        trt.offsetMax = new Vector2(-20, -10);

        textoLog.text = "[Debug activo — toca con 3 dedos para ocultar]";
    }

    void Update()
    {
        // Toggle con 3 dedos
        if (Input.touchCount == 3)
        {
            bool cualquierBegan = false;
            for (int i = 0; i < 3; i++)
                if (Input.GetTouch(i).phase == TouchPhase.Began) { cualquierBegan = true; break; }

            if (cualquierBegan)
            {
                visible = !visible;
                panelDebug.SetActive(visible);
            }
        }
    }
}
