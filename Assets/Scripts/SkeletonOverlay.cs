using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

/// <summary>
/// Dibuja un overlay visual del esqueleto (33 landmarks + conexiones) sobre el Canvas de supervisión.
/// Se suscribe a PoseLandmarkerRunner.OnResultadoGlobal para recibir los datos de pose.
/// Independiente del sistema de anotaciones de MediaPipe para mayor robustez.
/// </summary>
public class SkeletonOverlay : MonoBehaviour
{
    // ── Configuración ──────────────────────────────────────────────
    [Header("Apariencia")]
    public float radioLandmark = 12f;
    public float grosorLinea = 4f;
    public bool mostrarEsqueleto = true;

    // ── Colores por zona del cuerpo ────────────────────────────────
    private static readonly Color COLOR_CABEZA    = new Color(1f, 0.85f, 0.2f, 0.9f);    // Amarillo dorado
    private static readonly Color COLOR_TORSO     = new Color(0.2f, 0.8f, 1f, 0.9f);     // Cyan
    private static readonly Color COLOR_BRAZO_IZQ = new Color(0.3f, 1f, 0.5f, 0.9f);     // Verde
    private static readonly Color COLOR_BRAZO_DER = new Color(1f, 0.4f, 0.3f, 0.9f);     // Rojo-naranja
    private static readonly Color COLOR_PIERNA_IZQ = new Color(0.5f, 0.8f, 1f, 0.9f);    // Azul claro
    private static readonly Color COLOR_PIERNA_DER = new Color(1f, 0.6f, 0.8f, 0.9f);    // Rosa
    private static readonly Color COLOR_PUNTO_BAJA_CONF = new Color(0.5f, 0.5f, 0.5f, 0.4f); // Gris para baja confianza

    // ── Conexiones del esqueleto de MediaPipe (33 landmarks) ───────
    // Cada par (a, b) define una línea entre dos landmarks
    private static readonly int[,] CONEXIONES = new int[,]
    {
        // Cabeza
        {0, 1}, {1, 2}, {2, 3}, {3, 7},    // Ojo derecho → oreja derecha
        {0, 4}, {4, 5}, {5, 6}, {6, 8},    // Ojo izquierdo → oreja izquierda
        {9, 10},                             // Boca

        // Torso
        {11, 12},                            // Hombros
        {11, 23}, {12, 24},                  // Hombros → caderas
        {23, 24},                            // Caderas

        // Brazo izquierdo (landmarks impares: 11, 13, 15, 17, 19, 21)
        {11, 13}, {13, 15}, {15, 17}, {15, 19}, {15, 21}, {17, 19},

        // Brazo derecho (landmarks pares: 12, 14, 16, 18, 20, 22)
        {12, 14}, {14, 16}, {16, 18}, {16, 20}, {16, 22}, {18, 20},

        // Pierna izquierda (23, 25, 27, 29, 31)
        {23, 25}, {25, 27}, {27, 29}, {27, 31}, {29, 31},

        // Pierna derecha (24, 26, 28, 30, 32)
        {24, 26}, {26, 28}, {28, 30}, {28, 32}, {30, 32}
    };

    // ── Estado interno ─────────────────────────────────────────────
    private RectTransform _rectTransform;
    private GameObject _contenedorPuntos;
    private GameObject _contenedorLineas;

    private List<Image> _puntos = new List<Image>();
    private List<Image> _lineas = new List<Image>();

    private System.Collections.Concurrent.ConcurrentQueue<PoseLandmarkerResult> _colaResultados
        = new System.Collections.Concurrent.ConcurrentQueue<PoseLandmarkerResult>();

    private float _umbralConfianza = 0.3f; // Landmarks con confianza menor se muestran tenues

    // ─────────────────────────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa el overlay dentro del canvas indicado.
    /// </summary>
    public void Inicializar(Canvas canvas)
    {
        _rectTransform = GetComponent<RectTransform>();

        // Contenedor para los puntos (landmarks)
        _contenedorPuntos = new GameObject("SkeletonPuntos", typeof(RectTransform));
        _contenedorPuntos.transform.SetParent(this.transform, false);
        var rtPuntos = _contenedorPuntos.GetComponent<RectTransform>();
        EstirarRectTransform(rtPuntos);

        // Contenedor para las líneas (conexiones) — detrás de los puntos
        _contenedorLineas = new GameObject("SkeletonLineas", typeof(RectTransform));
        _contenedorLineas.transform.SetParent(this.transform, false);
        var rtLineas = _contenedorLineas.GetComponent<RectTransform>();
        EstirarRectTransform(rtLineas);
        // Las líneas deben estar detrás de los puntos
        _contenedorLineas.transform.SetAsFirstSibling();

        // Crear los 33 puntos (landmarks)
        CrearPuntos();

        // Crear las líneas de conexión
        CrearLineas();

        Debug.Log("[SkeletonOverlay] Inicializado con " + _puntos.Count + " landmarks y " + _lineas.Count + " conexiones.");
    }

    private void CrearPuntos()
    {
        for (int i = 0; i < 33; i++)
        {
            var go = new GameObject($"LM_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_contenedorPuntos.transform, false);

            var img = go.GetComponent<Image>();
            img.color = ObtenerColorLandmark(i);
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(radioLandmark, radioLandmark);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            go.SetActive(false);
            _puntos.Add(img);
        }
    }

    private void CrearLineas()
    {
        int numConexiones = CONEXIONES.GetLength(0);
        for (int i = 0; i < numConexiones; i++)
        {
            var go = new GameObject($"Linea_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_contenedorLineas.transform, false);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;

            go.SetActive(false);
            _lineas.Add(img);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────────────────────────

    void OnEnable()
    {
        // Limpiar cola vieja
        while (_colaResultados.TryDequeue(out _)) { }

        PoseLandmarkerRunner.OnResultadoGlobal += RecibirResultado;
        Debug.Log("[SkeletonOverlay] Suscrito a eventos de MediaPipe.");
    }

    void OnDisable()
    {
        PoseLandmarkerRunner.OnResultadoGlobal -= RecibirResultado;
        Debug.Log("[SkeletonOverlay] Desuscrito de eventos de MediaPipe.");
    }

    private void RecibirResultado(PoseLandmarkerResult result)
    {
        _colaResultados.Enqueue(result);
    }

    void Update()
    {
        if (!mostrarEsqueleto)
        {
            OcultarTodo();
            return;
        }

        // Procesar el resultado más reciente, descartando los anteriores
        PoseLandmarkerResult ultimoResultado = default;
        bool hayResultado = false;

        while (_colaResultados.TryDequeue(out var result))
        {
            ultimoResultado = result;
            hayResultado = true;
        }

        if (hayResultado)
        {
            DibujarEsqueleto(ultimoResultado);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // DIBUJO
    // ─────────────────────────────────────────────────────────────────

    private void DibujarEsqueleto(PoseLandmarkerResult result)
    {
        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
        {
            OcultarTodo();
            return;
        }

        var cuerpo = result.poseLandmarks[0].landmarks;
        if (!mostrarEsqueleto || cuerpo == null || cuerpo.Count < 33)
        {
            OcultarTodo();
            return;
        }

        Vector2 canvasSize = _rectTransform.rect.size;

        // Dibujar los 33 puntos
        for (int i = 0; i < 33 && i < _puntos.Count; i++)
        {
            var lm = cuerpo[i];

            // Convertir coordenadas normalizadas (0-1) a coordenadas del canvas
            // MediaPipe: x va de izquierda a derecha, y va de arriba a abajo
            float x = lm.x * canvasSize.x;
            float y = (1f - lm.y) * canvasSize.y; // Invertir Y para Unity UI

            var rt = _puntos[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);

            // Ajustar opacidad según visibilidad/confianza
            float confianza = lm.visibility ?? lm.presence ?? 1f;
            Color colorBase = ObtenerColorLandmark(i);

            if (confianza < _umbralConfianza)
            {
                _puntos[i].color = COLOR_PUNTO_BAJA_CONF;
                rt.sizeDelta = new Vector2(radioLandmark * 0.6f, radioLandmark * 0.6f);
            }
            else
            {
                colorBase.a = Mathf.Lerp(0.5f, 1f, confianza);
                _puntos[i].color = colorBase;
                rt.sizeDelta = new Vector2(radioLandmark, radioLandmark);
            }

            _puntos[i].gameObject.SetActive(true);
        }

        // Dibujar las conexiones
        int numConexiones = CONEXIONES.GetLength(0);
        for (int i = 0; i < numConexiones && i < _lineas.Count; i++)
        {
            int idxA = CONEXIONES[i, 0];
            int idxB = CONEXIONES[i, 1];

            var lmA = cuerpo[idxA];
            var lmB = cuerpo[idxB];

            // Verificar confianza de ambos puntos
            float confA = lmA.visibility ?? lmA.presence ?? 1f;
            float confB = lmB.visibility ?? lmB.presence ?? 1f;

            if (confA < _umbralConfianza * 0.5f || confB < _umbralConfianza * 0.5f)
            {
                _lineas[i].gameObject.SetActive(false);
                continue;
            }

            // Posiciones en el canvas
            float xA = lmA.x * canvasSize.x;
            float yA = (1f - lmA.y) * canvasSize.y;
            float xB = lmB.x * canvasSize.x;
            float yB = (1f - lmB.y) * canvasSize.y;

            Vector2 posA = new Vector2(xA, yA);
            Vector2 posB = new Vector2(xB, yB);

            // Configurar la línea (Image estirada y rotada)
            var rtLinea = _lineas[i].GetComponent<RectTransform>();
            Vector2 diff = posB - posA;
            float distancia = diff.magnitude;
            float angulo = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            rtLinea.anchoredPosition = posA;
            rtLinea.sizeDelta = new Vector2(distancia, grosorLinea);
            rtLinea.localRotation = Quaternion.Euler(0, 0, angulo);

            // Color de la línea basado en la zona
            Color colorLinea = ObtenerColorConexion(idxA, idxB);
            float confMin = Mathf.Min(confA, confB);
            colorLinea.a = Mathf.Lerp(0.3f, 0.8f, confMin);
            _lineas[i].color = colorLinea;

            _lineas[i].gameObject.SetActive(true);
        }
    }

    private void OcultarTodo()
    {
        foreach (var p in _puntos)
            if (p != null) p.gameObject.SetActive(false);
        foreach (var l in _lineas)
            if (l != null) l.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────
    // COLORES
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Asigna un color al landmark según su zona corporal.
    /// MediaPipe Pose Landmarks:
    /// 0-10: Cabeza (nariz, ojos, orejas, boca)
    /// 11-12: Hombros
    /// 13,15,17,19,21: Brazo izquierdo
    /// 14,16,18,20,22: Brazo derecho
    /// 23-24: Caderas
    /// 25,27,29,31: Pierna izquierda
    /// 26,28,30,32: Pierna derecha
    /// </summary>
    private Color ObtenerColorLandmark(int idx)
    {
        if (idx <= 10) return COLOR_CABEZA;
        if (idx == 11 || idx == 12 || idx == 23 || idx == 24) return COLOR_TORSO;
        if (idx == 13 || idx == 15 || idx == 17 || idx == 19 || idx == 21) return COLOR_BRAZO_IZQ;
        if (idx == 14 || idx == 16 || idx == 18 || idx == 20 || idx == 22) return COLOR_BRAZO_DER;
        if (idx == 25 || idx == 27 || idx == 29 || idx == 31) return COLOR_PIERNA_IZQ;
        if (idx == 26 || idx == 28 || idx == 30 || idx == 32) return COLOR_PIERNA_DER;
        return Color.white;
    }

    /// <summary>
    /// Color de la línea entre dos landmarks — usa el color de la zona dominante.
    /// </summary>
    private Color ObtenerColorConexion(int a, int b)
    {
        // Torso connections
        if ((a == 11 && b == 12) || (a == 23 && b == 24) ||
            (a == 11 && b == 23) || (a == 12 && b == 24))
            return COLOR_TORSO;

        // Left arm
        if (a == 11 || b == 11)
        {
            if (a == 13 || b == 13) return COLOR_BRAZO_IZQ;
        }
        if (a == 13 || a == 15 || a == 17 || a == 19 || a == 21 ||
            b == 13 || b == 15 || b == 17 || b == 19 || b == 21)
            return COLOR_BRAZO_IZQ;

        // Right arm
        if (a == 12 || b == 12)
        {
            if (a == 14 || b == 14) return COLOR_BRAZO_DER;
        }
        if (a == 14 || a == 16 || a == 18 || a == 20 || a == 22 ||
            b == 14 || b == 16 || b == 18 || b == 20 || b == 22)
            return COLOR_BRAZO_DER;

        // Left leg
        if (a == 25 || a == 27 || a == 29 || a == 31 ||
            b == 25 || b == 27 || b == 29 || b == 31)
            return COLOR_PIERNA_IZQ;

        // Right leg
        if (a == 26 || a == 28 || a == 30 || a == 32 ||
            b == 26 || b == 28 || b == 30 || b == 32)
            return COLOR_PIERNA_DER;

        // Head
        if (a <= 10 && b <= 10) return COLOR_CABEZA;

        return COLOR_TORSO;
    }

    // ─────────────────────────────────────────────────────────────────
    // TOGGLE PÚBLICO
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Alternar la visibilidad del esqueleto.
    /// </summary>
    public void AlternarVisibilidad()
    {
        mostrarEsqueleto = !mostrarEsqueleto;
        if (!mostrarEsqueleto) OcultarTodo();
        Debug.Log("[SkeletonOverlay] Esqueleto " + (mostrarEsqueleto ? "VISIBLE" : "OCULTO"));
    }

    // ─────────────────────────────────────────────────────────────────
    // UTILIDADES
    // ─────────────────────────────────────────────────────────────────

    private void EstirarRectTransform(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void OnDestroy()
    {
        if (_contenedorPuntos != null) Destroy(_contenedorPuntos);
        if (_contenedorLineas != null) Destroy(_contenedorLineas);
    }
}
