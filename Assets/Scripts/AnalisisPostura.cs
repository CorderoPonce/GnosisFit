using UnityEngine;
using System;
using System.Collections.Generic;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class AnalisisPostura : MonoBehaviour
{
    // ── Estado Público (leído por SupervisionHUD) ──────────────────
    [HideInInspector] public TipoSupervision ejercicioActual = TipoSupervision.BicepCurl;
    [HideInInspector] public int repeticiones = 0;
    [HideInInspector] public float anguloActual = 0f;
    [HideInInspector] public float progreso = 0f;       // 0..1 dentro del rep actual
    [HideInInspector] public string feedback = "";
    [HideInInspector] public Color colorFeedback = Color.white;
    [HideInInspector] public bool cuerpoDetectado = false;
    [HideInInspector] public float tiempoInicio;

    // ── Eventos ────────────────────────────────────────────────────
    public event Action OnRepCompletada;
    public event Action OnDataReceived; // Notifica al HUD que hay nuevos datos

    // ── Estado interno de la máquina de estados ────────────────────
    private enum FaseRep { Inicio, Bajando, Completado }
    private FaseRep faseActual = FaseRep.Inicio;

    // Umbrales por ejercicio (se configuran en ConfigurarEjercicio)
    private float umbralInferior;  // Ángulo para considerar "abajo/contraído"
    private float umbralSuperior;  // Ángulo para considerar "arriba/extendido"

    // Suavizado
    private float anguloSuavizado = 0f;
    private const float SUAVIZADO = 0.3f; // Lerp factor

    // Plank
    private float tiempoEnPlank = 0f;
    private const float TIEMPO_REP_PLANK = 5f; // 5 segundos = 1 "rep" de plank

    // Diagnóstico
    private float _ultimoLogDiag = 0f;
    private int _framesRecibidos = 0;

    public void ConfigurarEjercicio(TipoSupervision tipo)
    {
        ejercicioActual = tipo;
        repeticiones = 0;
        faseActual = FaseRep.Inicio;
        progreso = 0f;
        anguloActual = 0f;
        feedback = "Preparándose...";
        colorFeedback = Color.white;
        tiempoInicio = Time.time;
        tiempoEnPlank = 0f;

        switch (tipo)
        {
            case TipoSupervision.BicepCurl:
                umbralInferior = 50f;   // Brazo contraído
                umbralSuperior = 140f;  // Brazo extendido
                break;
            case TipoSupervision.AirSquat:
                umbralInferior = 100f;  // Rodilla doblada
                umbralSuperior = 155f;  // De pie
                break;
            case TipoSupervision.PushUp:
                umbralInferior = 90f;   // Codo doblado
                umbralSuperior = 150f;  // Brazos extendidos
                break;
            case TipoSupervision.Situp:
                umbralInferior = 70f;   // Torso arriba
                umbralSuperior = 140f;  // Acostado
                break;
            case TipoSupervision.JumpingJack:
                umbralInferior = 0.15f; // Manos juntas (distancia normalizada)
                umbralSuperior = 0.4f;  // Manos separadas
                break;
            case TipoSupervision.Burpee:
                umbralInferior = 90f;   // En el suelo
                umbralSuperior = 150f;  // De pie
                break;
            case TipoSupervision.Plank:
                umbralInferior = 150f;  // Cuerpo recto
                umbralSuperior = 180f;
                break;
            default:
                umbralInferior = 60f;
                umbralSuperior = 150f;
                break;
        }

        Debug.Log($"[AnalisisPostura] Configurado: {tipo} | Umbrales: {umbralInferior}° - {umbralSuperior}°");
    }

    void Awake()
    {
        // Empieza desactivado — se activa cuando el usuario entra en modo supervisión
        enabled = false;
    }

    private System.Collections.Concurrent.ConcurrentQueue<PoseLandmarkerResult> _resultadosCola = new System.Collections.Concurrent.ConcurrentQueue<PoseLandmarkerResult>();

    void OnEnable()
    {
        // Limpiar cola vieja
        while (_resultadosCola.TryDequeue(out _)) { }

        // Suscribirse al evento de MediaPipe cuando este componente se activa
        PoseLandmarkerRunner.OnResultadoGlobal += ProcesarResultado;
        Debug.Log("[AnalisisPostura] Suscrito a eventos de MediaPipe.");
    }

    void OnDisable()
    {
        // Desuscribirse para evitar memory leaks
        PoseLandmarkerRunner.OnResultadoGlobal -= ProcesarResultado;
        Debug.Log("[AnalisisPostura] Desuscrito de eventos de MediaPipe.");
    }

    private void Update()
    {
        // Corazón de diagnóstico (cada 5s) para verificar que el pipeline está vivo
        if (Time.time - _ultimoLogDiag > 5f)
        {
            _ultimoLogDiag = Time.time;
            Debug.Log($"[AnalisisPostura] Pipeline Status: {_framesRecibidos} frames/5s | Detectado: {cuerpoDetectado} | Reps: {repeticiones}");
            _framesRecibidos = 0;
        }

        // Procesar todos los resultados pendientes en el hilo principal
        while (_resultadosCola.TryDequeue(out var result))
        {
            _framesRecibidos++;
            EjecutarAnalisis(result);
            OnDataReceived?.Invoke(); // Notificar al HUD de forma reactiva
        }
    }

    /// <summary>
    /// Callback del hilo de MediaPipe. Solo encola el resultado.
    /// </summary>
    private void ProcesarResultado(PoseLandmarkerResult result)
    {
        _resultadosCola.Enqueue(result);
    }

    /// <summary>
    /// Ejecuta la lógica de análisis (siempre en el hilo principal).
    /// </summary>
    private void EjecutarAnalisis(PoseLandmarkerResult result)
    {
        if (!enabled) return;

        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
        {
            cuerpoDetectado = false;
            feedback = "Buscando cuerpo...";
            colorFeedback = Color.yellow;
            return;
        }

        var cuerpo = result.poseLandmarks[0].landmarks;

        // Necesitamos al menos 33 landmarks (pose completa de MediaPipe)
        if (cuerpo == null || cuerpo.Count < 33)
        {
            cuerpoDetectado = false;
            feedback = "Cuerpo incompleto...";
            colorFeedback = Color.yellow;
            return;
        }

        cuerpoDetectado = true;

        switch (ejercicioActual)
        {
            case TipoSupervision.BicepCurl:    EvaluarBicepCurl(cuerpo);    break;
            case TipoSupervision.AirSquat:     EvaluarAirSquat(cuerpo);     break;
            case TipoSupervision.PushUp:       EvaluarPushUp(cuerpo);       break;
            case TipoSupervision.JumpingJack:  EvaluarJumpingJack(cuerpo);  break;
            case TipoSupervision.Situp:        EvaluarSitup(cuerpo);        break;
            case TipoSupervision.Burpee:       EvaluarBurpee(cuerpo);       break;
            case TipoSupervision.Plank:        EvaluarPlank(cuerpo);        break;
            default:                           EvaluarGeneric();            break;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // UTILIDADES
    // ═══════════════════════════════════════════════════════════════

    private float CalcularAngulo(Vector2 a, Vector2 b, Vector2 c)
    {
        // Ángulo en el punto B formado por los segmentos BA y BC
        return Vector2.Angle(a - b, c - b);
    }

    private Vector2 LM(IList<NormalizedLandmark> cuerpo, int idx)
    {
        return new Vector2(cuerpo[idx].x, cuerpo[idx].y);
    }

    private void ProcesarRepConAngulo(float angulo, bool invertido = false)
    {
        // Suavizar el ángulo
        anguloSuavizado = Mathf.Lerp(anguloSuavizado, angulo, SUAVIZADO);
        anguloActual = anguloSuavizado;

        // Calcular progreso (0 = inicio, 1 = completado)
        if (!invertido)
        {
            // Normal: ángulo alto = inicio, ángulo bajo = completado
            progreso = Mathf.InverseLerp(umbralSuperior, umbralInferior, anguloSuavizado);
        }
        else
        {
            // Invertido: ángulo bajo = inicio, ángulo alto = completado
            progreso = Mathf.InverseLerp(umbralInferior, umbralSuperior, anguloSuavizado);
        }
        progreso = Mathf.Clamp01(progreso);

        // Máquina de estados
        switch (faseActual)
        {
            case FaseRep.Inicio:
                if (!invertido && anguloSuavizado < umbralInferior ||
                    invertido && anguloSuavizado > umbralSuperior)
                {
                    faseActual = FaseRep.Bajando;
                }
                break;

            case FaseRep.Bajando:
                if (!invertido && anguloSuavizado > umbralSuperior ||
                    invertido && anguloSuavizado < umbralInferior)
                {
                    faseActual = FaseRep.Completado;
                    repeticiones++;
                    OnRepCompletada?.Invoke();
                    faseActual = FaseRep.Inicio; // Reset para el siguiente rep
                }
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // EVALUADORES POR EJERCICIO
    // ═══════════════════════════════════════════════════════════════

    private void EvaluarBicepCurl(IList<NormalizedLandmark> cuerpo)
    {
        // Landmarks: Hombro(12), Codo(14), Muñeca(16) — lado derecho
        float anguloDer = CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 14), LM(cuerpo, 16));
        // Lado izquierdo: Hombro(11), Codo(13), Muñeca(15)
        float anguloIzq = CalcularAngulo(LM(cuerpo, 11), LM(cuerpo, 13), LM(cuerpo, 15));

        // Usar el promedio de ambos brazos
        float angulo = (anguloDer + anguloIzq) / 2f;
        ProcesarRepConAngulo(angulo);

        // Feedback de forma
        if (anguloSuavizado > 150f)
        {
            feedback = "Brazo extendido — ¡Sube!";
            colorFeedback = Color.white;
        }
        else if (anguloSuavizado < 45f)
        {
            feedback = "¡Buena contracción!";
            colorFeedback = Color.green;
        }
        else if (anguloSuavizado < 90f)
        {
            feedback = "¡Sigue subiendo!";
            colorFeedback = new Color(0.2f, 0.8f, 1f); // Cyan
        }
        else
        {
            feedback = "Bajando...";
            colorFeedback = Color.yellow;
        }
    }

    private void EvaluarAirSquat(IList<NormalizedLandmark> cuerpo)
    {
        // Landmarks: Cadera(24), Rodilla(26), Tobillo(28)
        float anguloDer = CalcularAngulo(LM(cuerpo, 24), LM(cuerpo, 26), LM(cuerpo, 28));
        float anguloIzq = CalcularAngulo(LM(cuerpo, 23), LM(cuerpo, 25), LM(cuerpo, 27));
        float angulo = (anguloDer + anguloIzq) / 2f;

        ProcesarRepConAngulo(angulo);

        // Feedback: verificar que las rodillas no pasen de los pies
        float rodillaX = cuerpo[26].x;
        float tobilloX = cuerpo[28].x;

        if (anguloSuavizado > 160f)
        {
            feedback = "De pie — ¡Baja!";
            colorFeedback = Color.white;
        }
        else if (anguloSuavizado < 90f)
        {
            feedback = "¡Excelente profundidad!";
            colorFeedback = Color.green;
        }
        else if (anguloSuavizado < 110f)
        {
            feedback = "Buena sentadilla";
            colorFeedback = new Color(0.5f, 1f, 0.5f);
        }
        else
        {
            feedback = "Baja más la cadera";
            colorFeedback = Color.yellow;
        }
    }

    private void EvaluarPushUp(IList<NormalizedLandmark> cuerpo)
    {
        // Landmarks: Hombro(12), Codo(14), Muñeca(16)
        float anguloDer = CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 14), LM(cuerpo, 16));
        float anguloIzq = CalcularAngulo(LM(cuerpo, 11), LM(cuerpo, 13), LM(cuerpo, 15));
        float angulo = (anguloDer + anguloIzq) / 2f;

        ProcesarRepConAngulo(angulo);

        // Verificar alineación del cuerpo (hombro-cadera-tobillo)
        float anguloEspalda = CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 24), LM(cuerpo, 28));

        if (anguloSuavizado > 150f)
        {
            feedback = "Arriba — ¡Baja!";
            colorFeedback = Color.white;
        }
        else if (anguloSuavizado < 100f)
        {
            if (anguloEspalda < 150f)
            {
                feedback = "Mantén la espalda recta";
                colorFeedback = Color.red;
            }
            else
            {
                feedback = "¡Buena flexión!";
                colorFeedback = Color.green;
            }
        }
        else
        {
            feedback = "Baja más el pecho";
            colorFeedback = Color.yellow;
        }
    }

    private void EvaluarJumpingJack(IList<NormalizedLandmark> cuerpo)
    {
        // Para JumpingJack usamos la distancia entre manos Y entre pies
        float distManos = Vector2.Distance(LM(cuerpo, 16), LM(cuerpo, 15));
        float distPies = Vector2.Distance(LM(cuerpo, 28), LM(cuerpo, 27));

        // Usar distancia de manos como métrica principal
        anguloSuavizado = Mathf.Lerp(anguloSuavizado, distManos, SUAVIZADO);
        anguloActual = distManos * 100f; // Para mostrar como porcentaje

        // Progreso basado en distancia de manos
        progreso = Mathf.InverseLerp(umbralInferior, umbralSuperior, anguloSuavizado);
        progreso = Mathf.Clamp01(progreso);

        // Máquina de estados para JumpingJack
        switch (faseActual)
        {
            case FaseRep.Inicio:
                // Manos arriba (separadas)
                if (anguloSuavizado > umbralSuperior && distPies > 0.15f)
                {
                    faseActual = FaseRep.Bajando;
                }
                break;
            case FaseRep.Bajando:
                // Manos abajo (juntas)
                if (anguloSuavizado < umbralInferior && distPies < 0.1f)
                {
                    faseActual = FaseRep.Completado;
                    repeticiones++;
                    OnRepCompletada?.Invoke();
                    faseActual = FaseRep.Inicio;
                }
                break;
        }

        if (anguloSuavizado > umbralSuperior)
        {
            feedback = "¡Brazos arriba!";
            colorFeedback = Color.green;
        }
        else if (anguloSuavizado < umbralInferior)
        {
            feedback = "Brazos abajo — ¡Salta!";
            colorFeedback = Color.white;
        }
        else
        {
            feedback = "Saltando...";
            colorFeedback = Color.cyan;
        }
    }

    private void EvaluarSitup(IList<NormalizedLandmark> cuerpo)
    {
        // Landmarks: Hombro(12), Cadera(24), Rodilla(26)
        float anguloDer = CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 24), LM(cuerpo, 26));
        float anguloIzq = CalcularAngulo(LM(cuerpo, 11), LM(cuerpo, 23), LM(cuerpo, 25));
        float angulo = (anguloDer + anguloIzq) / 2f;

        ProcesarRepConAngulo(angulo);

        if (anguloSuavizado > 140f)
        {
            feedback = "Acostado — ¡Sube!";
            colorFeedback = Color.white;
        }
        else if (anguloSuavizado < 80f)
        {
            feedback = "¡Arriba! Buen crunch";
            colorFeedback = Color.green;
        }
        else
        {
            feedback = "Sigue subiendo el torso";
            colorFeedback = Color.yellow;
        }
    }

    private void EvaluarBurpee(IList<NormalizedLandmark> cuerpo)
    {
        // Burpee: medimos la altura del hombro respecto al tobillo
        // Cuando baja (push-up) y sube (salto)
        float anguloTronco = CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 24), LM(cuerpo, 28));

        ProcesarRepConAngulo(anguloTronco);

        float alturaRelativa = Mathf.Abs(cuerpo[12].y - cuerpo[28].y);

        if (anguloSuavizado > 150f && alturaRelativa > 0.3f)
        {
            feedback = "¡De pie! — Baja al suelo";
            colorFeedback = Color.white;
        }
        else if (anguloSuavizado < 100f)
        {
            feedback = "¡En el suelo! — Empuja arriba";
            colorFeedback = Color.green;
        }
        else
        {
            feedback = "Transición...";
            colorFeedback = Color.yellow;
        }
    }

    private void EvaluarPlank(IList<NormalizedLandmark> cuerpo)
    {
        // Plank: medir alineación Hombro(12)-Cadera(24)-Tobillo(28)
        float anguloAlineacion = CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 24), LM(cuerpo, 28));
        anguloSuavizado = Mathf.Lerp(anguloSuavizado, anguloAlineacion, SUAVIZADO);
        anguloActual = anguloSuavizado;

        bool buenaForma = anguloSuavizado > 150f;

        if (buenaForma)
        {
            tiempoEnPlank += Time.deltaTime;
            progreso = (tiempoEnPlank % TIEMPO_REP_PLANK) / TIEMPO_REP_PLANK;

            // Cada 5 segundos = 1 "rep"
            int repsCalculados = Mathf.FloorToInt(tiempoEnPlank / TIEMPO_REP_PLANK);
            if (repsCalculados > repeticiones)
            {
                repeticiones = repsCalculados;
                OnRepCompletada?.Invoke();
            }

            feedback = $"¡Buena plancha! {tiempoEnPlank:F0}s";
            colorFeedback = Color.green;
        }
        else
        {
            if (anguloSuavizado < 140f)
            {
                feedback = "Cadera muy baja — ¡Sube!";
                colorFeedback = Color.red;
            }
            else
            {
                feedback = "Alinea el cuerpo";
                colorFeedback = Color.yellow;
            }
        }
    }

    private void EvaluarGeneric()
    {
        anguloActual = 0f;
        progreso = 0f;
        feedback = "Ejercicio sin supervisión específica";
        colorFeedback = Color.white;
    }
}