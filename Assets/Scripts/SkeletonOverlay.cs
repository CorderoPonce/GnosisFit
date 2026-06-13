using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class SkeletonOverlay : MonoBehaviour
{
    // ── Configuración ──────────────────────────────────────────────
    [Header("Apariencia")]
    public float radioLandmark = 12f;
    public float grosorLinea = 4f;
    public bool mostrarEsqueleto = true;

    // ── Colores por zona del cuerpo ────────────────────────────────
    private static readonly Color COLOR_CABEZA    = new Color(1f, 0.85f, 0.2f, 0.9f);    
    private static readonly Color COLOR_TORSO     = new Color(0.2f, 0.8f, 1f, 0.9f);     
    private static readonly Color COLOR_BRAZO_IZQ = new Color(0.3f, 1f, 0.5f, 0.9f);     
    private static readonly Color COLOR_BRAZO_DER = new Color(1f, 0.4f, 0.3f, 0.9f);     
    private static readonly Color COLOR_PIERNA_IZQ = new Color(0.5f, 0.8f, 1f, 0.9f);    
    private static readonly Color COLOR_PIERNA_DER = new Color(1f, 0.6f, 0.8f, 0.9f);    
    private static readonly Color COLOR_PUNTO_BAJA_CONF = new Color(0.5f, 0.5f, 0.5f, 0.4f); 

    // ── Conexiones del esqueleto de MediaPipe (33 landmarks) ───────
    private static readonly int[,] CONEXIONES = new int[,]
    {
        {0, 1}, {1, 2}, {2, 3}, {3, 7},    
        {0, 4}, {4, 5}, {5, 6}, {6, 8},    
        {9, 10},                             
        {11, 12},                            
        {11, 23}, {12, 24},                  
        {23, 24},                            
        {11, 13}, {13, 15}, {15, 17}, {15, 19}, {15, 21}, {17, 19},
        {12, 14}, {14, 16}, {16, 18}, {16, 20}, {16, 22}, {18, 20},
        {23, 25}, {25, 27}, {27, 29}, {27, 31}, {29, 31},
        {24, 26}, {26, 28}, {28, 30}, {28, 32}, {30, 32}
    };

    // ── Estado interno ─────────────────────────────────────────────
    private RectTransform _rectTransform;
    private GameObject _contenedorPuntos;
    private GameObject _contenedorLineas;
    private AnalisisPostura _analisisPostura;

    private List<Image> _puntos = new List<Image>();
    private List<Image> _lineas = new List<Image>();

    private System.Collections.Concurrent.ConcurrentQueue<PoseLandmarkerResult> _colaResultados
        = new System.Collections.Concurrent.ConcurrentQueue<PoseLandmarkerResult>();

    private float _umbralConfianza = 0.3f; 

    // Suavizado temporal de landmarks (33 puntos)
    private Vector2[] _posicionesSuavizadas = new Vector2[33];
    private float[] _confianzasSuavizadas = new float[33];
    private float[] _profundidadesSuavizadas = new float[33];
    private bool[] _inicializadoLandmark = new bool[33];

    public void Inicializar(Canvas canvas, AnalisisPostura analisis = null)
    {
        _analisisPostura = analisis;
        _rectTransform = GetComponent<RectTransform>();

        _contenedorPuntos = new GameObject("SkeletonPuntos", typeof(RectTransform));
        _contenedorPuntos.transform.SetParent(this.transform, false);
        var rtPuntos = _contenedorPuntos.GetComponent<RectTransform>();
        EstirarRectTransform(rtPuntos);

        _contenedorLineas = new GameObject("SkeletonLineas", typeof(RectTransform));
        _contenedorLineas.transform.SetParent(this.transform, false);
        var rtLineas = _contenedorLineas.GetComponent<RectTransform>();
        EstirarRectTransform(rtLineas);
        _contenedorLineas.transform.SetAsFirstSibling();

        CrearPuntos();
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

    void OnEnable()
    {
        while (_colaResultados.TryDequeue(out _)) { }
        PoseLandmarkerRunner.OnResultadoGlobal += RecibirResultado;
    }

    void OnDisable()
    {
        PoseLandmarkerRunner.OnResultadoGlobal -= RecibirResultado;
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

    private void DibujarEsqueleto(PoseLandmarkerResult result)
    {
        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
        {
            OcultarTodo();
            return;
        }

        var cuerpo = result.poseLandmarks[0].landmarks;
        var cuerpoMundo = (result.poseWorldLandmarks != null && result.poseWorldLandmarks.Count > 0) ? result.poseWorldLandmarks[0].landmarks : null;
        if (!mostrarEsqueleto || cuerpo == null || cuerpo.Count < 33)
        {
            OcultarTodo();
            return;
        }

        Vector2 canvasSize = _rectTransform.rect.size;

        // Dibujar y suavizar los 33 puntos con efecto de profundidad 3D
        for (int i = 0; i < 33 && i < _puntos.Count; i++)
        {
            var lm = cuerpo[i];
            float confianzaRaw = lm.visibility ?? lm.presence ?? 1f;

            float xRaw = lm.x * canvasSize.x;
            float yRaw = (1f - lm.y) * canvasSize.y;
            Vector2 posRaw = new Vector2(xRaw, yRaw);
            
            float zRaw = (cuerpoMundo != null && cuerpoMundo.Count > i) ? cuerpoMundo[i].z : 0f;

            if (!_inicializadoLandmark[i])
            {
                _posicionesSuavizadas[i] = posRaw;
                _confianzasSuavizadas[i] = confianzaRaw;
                _profundidadesSuavizadas[i] = zRaw;
                _inicializadoLandmark[i] = true;
            }
            else
            {
                _posicionesSuavizadas[i] = Vector2.Lerp(_posicionesSuavizadas[i], posRaw, 0.40f);
                _confianzasSuavizadas[i] = Mathf.Lerp(_confianzasSuavizadas[i], confianzaRaw, 0.25f);
                _profundidadesSuavizadas[i] = Mathf.Lerp(_profundidadesSuavizadas[i], zRaw, 0.30f);
            }

            float confianza = _confianzasSuavizadas[i];
            Vector2 pos = _posicionesSuavizadas[i];
            float z = _profundidadesSuavizadas[i];

            // Ocultar por completo si la confianza es críticamente baja para evitar que salte/se bugee
            if (confianza < 0.25f)
            {
                _puntos[i].gameObject.SetActive(false);
                continue;
            }

            var rt = _puntos[i].GetComponent<RectTransform>();
            rt.anchoredPosition = pos;

            // Escala y opacidad por profundidad 3D (Z es menor al acercarse a la cámara)
            float escala3D = Mathf.Clamp(1.0f - (z * 0.8f), 0.5f, 1.5f);
            float alpha3D = Mathf.Clamp(1.0f - (z * 0.4f), 0.4f, 1.0f);

            Color colorBase = ObtenerColorLandmark(i);

            // NUEVO: Cambiar color del punto si es una zona activa del ejercicio
            if (_analisisPostura != null && _analisisPostura.landmarksActivos.Contains(i))
            {
                if (_analisisPostura.colorFeedback != Color.white)
                {
                    colorBase = _analisisPostura.colorFeedback;
                }
            }

            // Umbral de advertencia de confianza
            if (confianza < 0.45f)
            {
                _puntos[i].color = COLOR_PUNTO_BAJA_CONF;
                rt.sizeDelta = new Vector2(radioLandmark * 0.6f * escala3D, radioLandmark * 0.6f * escala3D);
            }
            else
            {
                float alphaBase = Mathf.Lerp(0.5f, 1f, confianza);
                colorBase.a = alphaBase * alpha3D;
                _puntos[i].color = colorBase;
                rt.sizeDelta = new Vector2(radioLandmark * escala3D, radioLandmark * escala3D);
            }

            _puntos[i].gameObject.SetActive(true);
        }

        // Dibujar las conexiones usando posiciones, confianzas y profundidades suavizadas (efecto 3D)
        int numConexiones = CONEXIONES.GetLength(0);
        for (int i = 0; i < numConexiones && i < _lineas.Count; i++)
        {
            int idxA = CONEXIONES[i, 0];
            int idxB = CONEXIONES[i, 1];

            float confA = _confianzasSuavizadas[idxA];
            float confB = _confianzasSuavizadas[idxB];

            // Ocultar líneas si alguno de los extremos no es confiable (evita líneas estiradas locas)
            if (confA < 0.30f || confB < 0.30f)
            {
                _lineas[i].gameObject.SetActive(false);
                continue;
            }

            Vector2 posA = _posicionesSuavizadas[idxA];
            Vector2 posB = _posicionesSuavizadas[idxB];
            
            float zA = _profundidadesSuavizadas[idxA];
            float zB = _profundidadesSuavizadas[idxB];
            float zPromedio = (zA + zB) / 2f;

            float escala3DLinea = Mathf.Clamp(1.0f - (zPromedio * 0.8f), 0.5f, 1.5f);
            float alpha3DLinea = Mathf.Clamp(1.0f - (zPromedio * 0.4f), 0.4f, 1.0f);

            var rtLinea = _lineas[i].GetComponent<RectTransform>();
            Vector2 diff = posB - posA;
            float distancia = diff.magnitude;
            float angulo = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            rtLinea.anchoredPosition = posA;
            rtLinea.sizeDelta = new Vector2(distancia, grosorLinea * escala3DLinea);
            rtLinea.localRotation = Quaternion.Euler(0, 0, angulo);

            Color colorLinea = ObtenerColorConexion(idxA, idxB);
            
            // NUEVO: Cambiar color de la línea si conecta zonas activas
            if (_analisisPostura != null && (_analisisPostura.landmarksActivos.Contains(idxA) || _analisisPostura.landmarksActivos.Contains(idxB)))
            {
                if (_analisisPostura.colorFeedback != Color.white)
                {
                    colorLinea = _analisisPostura.colorFeedback;
                }
            }

            float confMin = Mathf.Min(confA, confB);
            float alphaBaseLinea = Mathf.Lerp(0.3f, 0.8f, confMin);
            colorLinea.a = alphaBaseLinea * alpha3DLinea;
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

        for (int i = 0; i < 33; i++)
        {
            _inicializadoLandmark[i] = false;
        }
    }

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

    private Color ObtenerColorConexion(int a, int b)
    {
        if ((a == 11 && b == 12) || (a == 23 && b == 24) || (a == 11 && b == 23) || (a == 12 && b == 24)) return COLOR_TORSO;
        if (a == 11 || b == 11) { if (a == 13 || b == 13) return COLOR_BRAZO_IZQ; }
        if (a == 13 || a == 15 || a == 17 || a == 19 || a == 21 || b == 13 || b == 15 || b == 17 || b == 19 || b == 21) return COLOR_BRAZO_IZQ;
        if (a == 12 || b == 12) { if (a == 14 || b == 14) return COLOR_BRAZO_DER; }
        if (a == 14 || a == 16 || a == 18 || a == 20 || a == 22 || b == 14 || b == 16 || b == 18 || b == 20 || b == 22) return COLOR_BRAZO_DER;
        if (a == 25 || a == 27 || a == 29 || a == 31 || b == 25 || b == 27 || b == 29 || b == 31) return COLOR_PIERNA_IZQ;
        if (a == 26 || a == 28 || a == 30 || a == 32 || b == 26 || b == 28 || b == 30 || b == 32) return COLOR_PIERNA_DER;
        if (a <= 10 && b <= 10) return COLOR_CABEZA;

        return COLOR_TORSO;
    }

    public void AlternarVisibilidad()
    {
        mostrarEsqueleto = !mostrarEsqueleto;
        if (!mostrarEsqueleto) OcultarTodo();
    }

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