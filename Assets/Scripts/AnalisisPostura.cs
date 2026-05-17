using UnityEngine;
using System;
using System.Collections.Generic;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class AnalisisPostura : MonoBehaviour
{
    // ── Estado Público ─────────────────────────────────────────────
    [HideInInspector] public TipoSupervision ejercicioActual = TipoSupervision.BicepCurl;
    [HideInInspector] public int repeticiones = 0;
    [HideInInspector] public float anguloActual = 0f;
    [HideInInspector] public float progreso = 0f;       
    [HideInInspector] public string feedback = "";
    [HideInInspector] public Color colorFeedback = Color.white;
    [HideInInspector] public bool cuerpoDetectado = false;
    [HideInInspector] public float tiempoInicio;
    [HideInInspector] public List<int> landmarksActivos = new List<int>();
    
    // IA y Errores
    [HideInInspector] public int advertenciasPostura = 0;
    [HideInInspector] public int repeticionesRapidas = 0; // NUEVO: Contador de cadencia
    [HideInInspector] public HashSet<string> erroresCometidos = new HashSet<string>();

    // ── Audio Feedback (NUEVO) ─────────────────────────────────────
    [Header("Feedback Sonoro")]
    public AudioSource audioSource;
    public AudioClip sonidoError;

    // ── Eventos ────────────────────────────────────────────────────
    public event Action OnRepCompletada;
    public event Action OnDataReceived; 

    // ── Estado Interno ─────────────────────────────────────────────
    private enum FaseRep { Inicio, Bajando, Completado }
    private FaseRep faseActual = FaseRep.Inicio;

    private enum VistaRequerida { Frente, Perfil, Cualquiera } // NUEVO: Validación de cámara
    private VistaRequerida vistaRequerida = VistaRequerida.Cualquiera;

    private float umbralInferior;  
    private float umbralSuperior;  
    private float anguloSuavizado = 0f;
    private const float SUAVIZADO = 0.3f; 
    private float tiempoEnPlank = 0f;
    private const float TIEMPO_REP_PLANK = 5f; 

    private float _ultimoLogDiag = 0f;
    private int _framesRecibidos = 0;
    private bool _estabaEnMalaPostura = false; 
    private float _tiempoInicioRep = 0f; // NUEVO: Cronómetro de repetición

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
        landmarksActivos.Clear();
        
        advertenciasPostura = 0;
        repeticionesRapidas = 0;
        _estabaEnMalaPostura = false;
        erroresCometidos.Clear();

        switch (tipo)
        {
            case TipoSupervision.BicepCurl:
                umbralInferior = 50f; umbralSuperior = 140f;
                vistaRequerida = VistaRequerida.Frente; // Requiere estar de frente
                break;
            case TipoSupervision.AirSquat:
                umbralInferior = 100f; umbralSuperior = 155f;
                vistaRequerida = VistaRequerida.Perfil; // Requiere estar de lado
                break;
            case TipoSupervision.PushUp:
                umbralInferior = 90f; umbralSuperior = 150f;
                vistaRequerida = VistaRequerida.Perfil;
                break;
            case TipoSupervision.Situp:
                umbralInferior = 70f; umbralSuperior = 140f;
                vistaRequerida = VistaRequerida.Perfil;
                break;
            case TipoSupervision.JumpingJack:
                umbralInferior = 0.15f; umbralSuperior = 0.4f;
                vistaRequerida = VistaRequerida.Frente;
                break;
            case TipoSupervision.Burpee:
                umbralInferior = 90f; umbralSuperior = 150f;
                vistaRequerida = VistaRequerida.Perfil;
                break;
            case TipoSupervision.Plank:
                umbralInferior = 150f; umbralSuperior = 180f;
                vistaRequerida = VistaRequerida.Perfil;
                break;
            default:
                umbralInferior = 60f; umbralSuperior = 150f;
                vistaRequerida = VistaRequerida.Cualquiera;
                break;
        }
    }

    private System.Collections.Concurrent.ConcurrentQueue<PoseLandmarkerResult> _resultadosCola = new System.Collections.Concurrent.ConcurrentQueue<PoseLandmarkerResult>();

    void OnEnable()
    {
        while (_resultadosCola.TryDequeue(out _)) { }
        PoseLandmarkerRunner.OnResultadoGlobal += ProcesarResultado;
    }

    void OnDisable()
    {
        PoseLandmarkerRunner.OnResultadoGlobal -= ProcesarResultado;
    }

    private void Update()
    {
        while (_resultadosCola.TryDequeue(out var result))
        {
            _framesRecibidos++;
            EjecutarAnalisis(result);
            OnDataReceived?.Invoke();
        }
    }

    private void ProcesarResultado(PoseLandmarkerResult result)
    {
        _resultadosCola.Enqueue(result);
    }

    private void EjecutarAnalisis(PoseLandmarkerResult result)
    {
        if (!enabled) return;

        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0 || result.poseLandmarks[0].landmarks.Count < 33)
        {
            cuerpoDetectado = false;
            feedback = "Buscando cuerpo...";
            colorFeedback = Color.yellow;
            return;
        }

        var cuerpo = result.poseLandmarks[0].landmarks;
        cuerpoDetectado = true;

        // NUEVO: 1. Validación de Ángulo de Cámara
        if (vistaRequerida != VistaRequerida.Cualquiera)
        {
            float distHombros = Mathf.Abs(cuerpo[11].x - cuerpo[12].x);
            bool esPerfil = distHombros < 0.12f; // Hombros superpuestos
            bool esFrente = distHombros > 0.18f; // Hombros separados

            if (vistaRequerida == VistaRequerida.Perfil && !esPerfil)
            {
                feedback = "Gírate y colócate de PERFIL a la cámara";
                colorFeedback = Color.red;
                return; // Bloquea el análisis hasta que se voltee
            }
            else if (vistaRequerida == VistaRequerida.Frente && !esFrente)
            {
                feedback = "Gírate y colócate de FRENTE a la cámara";
                colorFeedback = Color.red;
                return;
            }
        }

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

        // Lógica de Errores y Sonido
        bool formaCorrecta = (colorFeedback != Color.yellow && colorFeedback != Color.red);
        
        if (!formaCorrecta && !_estabaEnMalaPostura)
        {
            advertenciasPostura++;
            _estabaEnMalaPostura = true;
            
            // NUEVO: 2. Feedback Sonoro al fallar
            if (audioSource != null && sonidoError != null)
            {
                audioSource.PlayOneShot(sonidoError);
            }
            
            if (!string.IsNullOrEmpty(feedback) && feedback != "Preparándose...")
            {
                string partesDelCuerpo = ObtenerNombresArticulaciones(landmarksActivos);
                string errorDetallado = $"{feedback} (Zonas afectadas: {partesDelCuerpo})";
                erroresCometidos.Add(errorDetallado);
            }
        }
        else if (formaCorrecta)
        {
            _estabaEnMalaPostura = false;
        }
    }

    private float CalcularAngulo(Vector2 a, Vector2 b, Vector2 c) { return Vector2.Angle(a - b, c - b); }
    private Vector2 LM(IList<NormalizedLandmark> cuerpo, int idx) { return new Vector2(cuerpo[idx].x, cuerpo[idx].y); }

    private void ProcesarRepConAngulo(float angulo, bool invertido = false)
    {
        anguloSuavizado = Mathf.Lerp(anguloSuavizado, angulo, SUAVIZADO);
        anguloActual = anguloSuavizado;
        progreso = Mathf.Clamp01(Mathf.InverseLerp(invertido ? umbralInferior : umbralSuperior, invertido ? umbralSuperior : umbralInferior, anguloSuavizado));

        switch (faseActual)
        {
            case FaseRep.Inicio:
                if ((!invertido && anguloSuavizado < umbralInferior) || (invertido && anguloSuavizado > umbralSuperior))
                {
                    faseActual = FaseRep.Bajando;
                    _tiempoInicioRep = Time.time; // Inicia el cronómetro de la repetición
                }
                break;

            case FaseRep.Bajando:
                if ((!invertido && anguloSuavizado > umbralSuperior) || (invertido && anguloSuavizado < umbralInferior))
                {
                    faseActual = FaseRep.Completado;
                    repeticiones++;
                    
                    // NUEVO: 3. Cadencia y Tiempo Bajo Tensión
                    float duracionRep = Time.time - _tiempoInicioRep;
                    if (duracionRep < 0.8f) // Si tardó menos de 0.8s, es rebote
                    {
                        repeticionesRapidas++;
                        erroresCometidos.Add("Repetición muy rápida (Falta control en la bajada)");
                    }

                    OnRepCompletada?.Invoke();
                    faseActual = FaseRep.Inicio; 
                }
                break;
        }
    }

    private void EvaluarBicepCurl(IList<NormalizedLandmark> cuerpo)
    {
        landmarksActivos = new List<int> { 11, 12, 13, 14, 15, 16 }; 
        float anguloActivo = Mathf.Min(CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 14), LM(cuerpo, 16)), CalcularAngulo(LM(cuerpo, 11), LM(cuerpo, 13), LM(cuerpo, 15)));
        ProcesarRepConAngulo(anguloActivo);

        if (anguloSuavizado > 150f) { feedback = "Brazo extendido — ¡Sube!"; colorFeedback = Color.white; }
        else if (anguloSuavizado < 45f) { feedback = "¡Buena contracción!"; colorFeedback = Color.green; }
        else if (anguloSuavizado < 90f) { feedback = "¡Sigue subiendo!"; colorFeedback = new Color(0.2f, 0.8f, 1f); }
        else { feedback = "Bajando..."; colorFeedback = Color.yellow; }
    }

    private void EvaluarAirSquat(IList<NormalizedLandmark> cuerpo)
    {
        landmarksActivos = new List<int> { 23, 24, 25, 26, 27, 28 }; 
        float angulo = (CalcularAngulo(LM(cuerpo, 24), LM(cuerpo, 26), LM(cuerpo, 28)) + CalcularAngulo(LM(cuerpo, 23), LM(cuerpo, 25), LM(cuerpo, 27))) / 2f;
        ProcesarRepConAngulo(angulo);

        if (anguloSuavizado > 160f) { feedback = "De pie — ¡Baja!"; colorFeedback = Color.white; }
        else if (anguloSuavizado < 90f) { feedback = "¡Excelente profundidad!"; colorFeedback = Color.green; }
        else if (anguloSuavizado < 110f) { feedback = "Buena sentadilla"; colorFeedback = new Color(0.5f, 1f, 0.5f); }
        else { feedback = "Baja más la cadera"; colorFeedback = Color.yellow; }
    }

    private void EvaluarPushUp(IList<NormalizedLandmark> cuerpo)
    {
        landmarksActivos = new List<int> { 11, 12, 13, 14, 15, 16, 23, 24, 27, 28 }; 
        float angulo = (CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 14), LM(cuerpo, 16)) + CalcularAngulo(LM(cuerpo, 11), LM(cuerpo, 13), LM(cuerpo, 15))) / 2f;
        ProcesarRepConAngulo(angulo);
        float anguloEspalda = CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 24), LM(cuerpo, 28));

        if (anguloSuavizado > 150f) { feedback = "Arriba — ¡Baja!"; colorFeedback = Color.white; }
        else if (anguloSuavizado < 100f) {
            if (anguloEspalda < 150f) { feedback = "Mantén la espalda recta"; colorFeedback = Color.red; }
            else { feedback = "¡Buena flexión!"; colorFeedback = Color.green; }
        } else { feedback = "Baja más el pecho"; colorFeedback = Color.yellow; }
    }

    private void EvaluarJumpingJack(IList<NormalizedLandmark> cuerpo)
    {
        landmarksActivos = new List<int> { 11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27, 28 };
        float distManos = Vector2.Distance(LM(cuerpo, 16), LM(cuerpo, 15));
        float distPies = Vector2.Distance(LM(cuerpo, 28), LM(cuerpo, 27));

        anguloSuavizado = Mathf.Lerp(anguloSuavizado, distManos, SUAVIZADO);
        anguloActual = distManos * 100f; 
        progreso = Mathf.Clamp01(Mathf.InverseLerp(umbralInferior, umbralSuperior, anguloSuavizado));

        switch (faseActual)
        {
            case FaseRep.Inicio:
                if (anguloSuavizado > umbralSuperior && distPies > 0.15f) { faseActual = FaseRep.Bajando; _tiempoInicioRep = Time.time; }
                break;
            case FaseRep.Bajando:
                if (anguloSuavizado < umbralInferior && distPies < 0.1f)
                {
                    faseActual = FaseRep.Completado;
                    repeticiones++;
                    if (Time.time - _tiempoInicioRep < 0.5f) { repeticionesRapidas++; erroresCometidos.Add("Ritmo de salto muy acelerado"); }
                    OnRepCompletada?.Invoke();
                    faseActual = FaseRep.Inicio;
                }
                break;
        }

        if (anguloSuavizado > umbralSuperior) { feedback = "¡Brazos arriba!"; colorFeedback = Color.green; }
        else if (anguloSuavizado < umbralInferior) { feedback = "Brazos abajo — ¡Salta!"; colorFeedback = Color.white; }
        else { feedback = "Saltando..."; colorFeedback = Color.cyan; }
    }

    private void EvaluarSitup(IList<NormalizedLandmark> cuerpo)
    {
        landmarksActivos = new List<int> { 11, 12, 23, 24, 25, 26 };
        float angulo = (CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 24), LM(cuerpo, 26)) + CalcularAngulo(LM(cuerpo, 11), LM(cuerpo, 23), LM(cuerpo, 25))) / 2f;
        ProcesarRepConAngulo(angulo);

        if (anguloSuavizado > 140f) { feedback = "Acostado — ¡Sube!"; colorFeedback = Color.white; }
        else if (anguloSuavizado < 80f) { feedback = "¡Arriba! Buen crunch"; colorFeedback = Color.green; }
        else { feedback = "Sigue subiendo el torso"; colorFeedback = Color.yellow; }
    }

    private void EvaluarBurpee(IList<NormalizedLandmark> cuerpo)
    {
        landmarksActivos = new List<int> { 11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27, 28 };
        ProcesarRepConAngulo(CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 24), LM(cuerpo, 28)));
        float alturaRelativa = Mathf.Abs(cuerpo[12].y - cuerpo[28].y);

        if (anguloSuavizado > 150f && alturaRelativa > 0.3f) { feedback = "¡De pie! — Baja al suelo"; colorFeedback = Color.white; }
        else if (anguloSuavizado < 100f) { feedback = "¡En el suelo! — Empuja arriba"; colorFeedback = Color.green; }
        else { feedback = "Transición..."; colorFeedback = Color.yellow; }
    }

    private void EvaluarPlank(IList<NormalizedLandmark> cuerpo)
    {
        landmarksActivos = new List<int> { 11, 12, 23, 24, 27, 28 };
        anguloSuavizado = Mathf.Lerp(anguloSuavizado, CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 24), LM(cuerpo, 28)), SUAVIZADO);
        anguloActual = anguloSuavizado;

        if (anguloSuavizado > 150f)
        {
            tiempoEnPlank += Time.deltaTime;
            progreso = (tiempoEnPlank % TIEMPO_REP_PLANK) / TIEMPO_REP_PLANK;
            int repsCalculados = Mathf.FloorToInt(tiempoEnPlank / TIEMPO_REP_PLANK);
            if (repsCalculados > repeticiones) { repeticiones = repsCalculados; OnRepCompletada?.Invoke(); }
            feedback = $"¡Buena plancha! {tiempoEnPlank:F0}s"; colorFeedback = Color.green;
        }
        else
        {
            if (anguloSuavizado < 140f) { feedback = "Cadera muy baja — ¡Sube!"; colorFeedback = Color.red; }
            else { feedback = "Alinea el cuerpo"; colorFeedback = Color.yellow; }
        }
    }

    private void EvaluarGeneric() { landmarksActivos.Clear(); anguloActual = 0f; progreso = 0f; feedback = "Ejercicio sin supervisión específica"; colorFeedback = Color.white; }

    private string ObtenerNombresArticulaciones(List<int> landmarks)
    {
        HashSet<string> zonas = new HashSet<string>();
        foreach (int lm in landmarks)
        {
            if (lm == 11 || lm == 12) zonas.Add("Hombros");
            else if (lm == 13 || lm == 14) zonas.Add("Codos");
            else if (lm == 15 || lm == 16) zonas.Add("Muñecas");
            else if (lm == 23 || lm == 24) zonas.Add("Caderas");
            else if (lm == 25 || lm == 26) zonas.Add("Rodillas");
            else if (lm == 27 || lm == 28) zonas.Add("Tobillos");
        }
        return string.Join(", ", zonas);
    }
}