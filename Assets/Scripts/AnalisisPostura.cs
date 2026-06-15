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
    private const float SUAVIZADO = 0.5f; 
    private float tiempoEnPlank = 0f;
    private const float TIEMPO_REP_PLANK = 5f; 
    private bool _primerFrame = true;

    private float _ultimoLogDiag = 0f;
    private float _baselineY = -1f;
    private int _framesRecibidos = 0;
    private bool _estabaEnMalaPostura = false; 
    private float _tiempoInicioRep = 0f; // NUEVO: Cronómetro de repetición

    // Brazos independientes para Curl de Bíceps
    private float anguloSuavizadoIzq = 0f;
    private float anguloSuavizadoDer = 0f;
    private FaseRep faseActualIzq = FaseRep.Inicio;
    private FaseRep faseActualDer = FaseRep.Inicio;
    private float _tiempoInicioRepIzq = 0f;
    private float _tiempoInicioRepDer = 0f;
    private float _ultimoRepTimeDer = 0f;
    private float _ultimoRepTimeIzq = 0f;
    private Queue<float> _historiaAnguloIzq = new Queue<float>();
    private Queue<float> _historiaAnguloDer = new Queue<float>();
    private Queue<float> _historiaYCadera = new Queue<float>();

    public void ConfigurarEjercicio(TipoSupervision tipo)
    {
        ejercicioActual = tipo;
        repeticiones = 0;
        faseActual = FaseRep.Inicio;
        progreso = 0f;
        anguloActual = 0f;
        _primerFrame = true;
        feedback = "Preparándose...";
        colorFeedback = Color.white;
        tiempoInicio = Time.time;
        tiempoEnPlank = 0f;
        _baselineY = -1f;
        landmarksActivos.Clear();
        
        advertenciasPostura = 0;
        repeticionesRapidas = 0;
        _estabaEnMalaPostura = false;
        erroresCometidos.Clear();

        // Inicializar variables de seguimiento de brazos independientes
        anguloSuavizadoIzq = 0f;
        anguloSuavizadoDer = 0f;
        faseActualIzq = FaseRep.Inicio;
        faseActualDer = FaseRep.Inicio;
        _tiempoInicioRepIzq = 0f;
        _tiempoInicioRepDer = 0f;
        _ultimoRepTimeDer = 0f;
        _ultimoRepTimeIzq = 0f;
        _historiaAnguloIzq.Clear();
        _historiaAnguloDer.Clear();
        _historiaYCadera.Clear();

        switch (tipo)
        {
            case TipoSupervision.BicepCurl:
                umbralInferior = 60f; umbralSuperior = 135f;
                vistaRequerida = VistaRequerida.Frente; // Requiere estar de frente
                break;
            case TipoSupervision.AirSquat:
                umbralInferior = 105f; umbralSuperior = 145f;
                vistaRequerida = VistaRequerida.Perfil; // Requiere estar de lado
                break;
            case TipoSupervision.PushUp:
                umbralInferior = 95f; umbralSuperior = 140f;
                vistaRequerida = VistaRequerida.Perfil;
                break;
            case TipoSupervision.Situp:
                umbralInferior = 100f; umbralSuperior = 120f; // Calibrado para alta permisividad en situps/crunches (incluyendo Crunch Circular)
                vistaRequerida = VistaRequerida.Perfil;
                break;
            case TipoSupervision.JumpingJack:
                umbralInferior = 0.6f; umbralSuperior = 0.75f; // Ratios normalizados por altura de torso (Calibrado a 0.75)
                vistaRequerida = VistaRequerida.Frente;
                break;
            case TipoSupervision.SaltosCruzados:
                umbralInferior = 0.65f; umbralSuperior = 0.85f; // Ratio distancia tobillos / ancho caderas (permisivo)
                vistaRequerida = VistaRequerida.Frente;
                break;
            case TipoSupervision.Burpee:
                umbralInferior = 100f; umbralSuperior = 145f;
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

        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0 || result.poseLandmarks[0].landmarks.Count < 33 ||
            result.poseWorldLandmarks == null || result.poseWorldLandmarks.Count == 0 || result.poseWorldLandmarks[0].landmarks.Count < 33)
        {
            cuerpoDetectado = false;
            feedback = "Buscando cuerpo...";
            colorFeedback = Color.yellow;
            return;
        }

        var cuerpo = result.poseLandmarks[0].landmarks;
        var cuerpoMundo = result.poseWorldLandmarks[0].landmarks;
        cuerpoDetectado = true;

        // NUEVO: Validación de confianza mínima de los landmarks clave antes de calcular ángulos
        string msjFeedbackConfianza;
        if (!ValidarConfianzaLandmarks(cuerpo, ejercicioActual, out msjFeedbackConfianza))
        {
            feedback = msjFeedbackConfianza;
            colorFeedback = Color.yellow;
            return;
        }

        switch (ejercicioActual)
        {
            case TipoSupervision.BicepCurl:    EvaluarBicepCurl(cuerpo, cuerpoMundo);    break;
            case TipoSupervision.AirSquat:     EvaluarAirSquat(cuerpo, cuerpoMundo);     break;
            case TipoSupervision.PushUp:       EvaluarPushUp(cuerpo, cuerpoMundo);       break;
            case TipoSupervision.JumpingJack:  EvaluarJumpingJack(cuerpo, cuerpoMundo);  break;
            case TipoSupervision.Situp:        EvaluarSitup(cuerpo, cuerpoMundo);        break;
            case TipoSupervision.Burpee:       EvaluarBurpee(cuerpo, cuerpoMundo);       break;
            case TipoSupervision.Plank:        EvaluarPlank(cuerpo, cuerpoMundo);        break;
            case TipoSupervision.SaltosCruzados: EvaluarSaltosCruzados(cuerpo, cuerpoMundo); break;
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

        _primerFrame = false;
    }


    private bool ValidarConfianzaLandmarks(IList<NormalizedLandmark> cuerpo, TipoSupervision tipo, out string msjFeedback)
    {
        msjFeedback = "";
        switch (tipo)
        {
            case TipoSupervision.BicepCurl:
                {
                    float confIzq = ((cuerpo[11].visibility ?? 0f) + (cuerpo[13].visibility ?? 0f) + (cuerpo[15].visibility ?? 0f)) / 3f;
                    float confDer = ((cuerpo[12].visibility ?? 0f) + (cuerpo[14].visibility ?? 0f) + (cuerpo[16].visibility ?? 0f)) / 3f;
                    if (confIzq < 0.25f || confDer < 0.25f)
                    {
                        msjFeedback = "Ajusta tu posición — codos o manos no visibles";
                        return false;
                    }
                }
                break;
            case TipoSupervision.AirSquat:
                {
                    float confIzq = ((cuerpo[23].visibility ?? 0f) + (cuerpo[25].visibility ?? 0f) + (cuerpo[27].visibility ?? 0f)) / 3f;
                    float confDer = ((cuerpo[24].visibility ?? 0f) + (cuerpo[26].visibility ?? 0f) + (cuerpo[28].visibility ?? 0f)) / 3f;
                    if (confIzq < 0.25f && confDer < 0.25f)
                    {
                        msjFeedback = "Ajusta tu posición — rodillas o tobillos no visibles";
                        return false;
                    }
                }
                break;
            case TipoSupervision.PushUp:
                {
                    float confIzq = ((cuerpo[11].visibility ?? 0f) + (cuerpo[13].visibility ?? 0f) + (cuerpo[15].visibility ?? 0f) + (cuerpo[23].visibility ?? 0f) + (cuerpo[27].visibility ?? 0f)) / 5f;
                    float confDer = ((cuerpo[12].visibility ?? 0f) + (cuerpo[14].visibility ?? 0f) + (cuerpo[16].visibility ?? 0f) + (cuerpo[24].visibility ?? 0f) + (cuerpo[28].visibility ?? 0f)) / 5f;
                    if (confIzq < 0.25f && confDer < 0.25f)
                    {
                        msjFeedback = "Ajusta tu posición — codos o cadera no visibles";
                        return false;
                    }
                }
                break;
            case TipoSupervision.JumpingJack:
            case TipoSupervision.SaltosCruzados:
                {
                    float confIzq = ((cuerpo[11].visibility ?? 0f) + (cuerpo[13].visibility ?? 0f) + (cuerpo[15].visibility ?? 0f) + (cuerpo[23].visibility ?? 0f) + (cuerpo[25].visibility ?? 0f) + (cuerpo[27].visibility ?? 0f)) / 6f;
                    float confDer = ((cuerpo[12].visibility ?? 0f) + (cuerpo[14].visibility ?? 0f) + (cuerpo[16].visibility ?? 0f) + (cuerpo[24].visibility ?? 0f) + (cuerpo[26].visibility ?? 0f) + (cuerpo[28].visibility ?? 0f)) / 6f;
                    if (confIzq < 0.25f || confDer < 0.25f)
                    {
                        msjFeedback = "Ajusta tu posición — mantén pies y manos a la vista";
                        return false;
                    }
                }
                break;
            case TipoSupervision.Situp:
                {
                    float confIzq = ((cuerpo[11].visibility ?? 0f) + (cuerpo[23].visibility ?? 0f) + (cuerpo[25].visibility ?? 0f)) / 3f;
                    float confDer = ((cuerpo[12].visibility ?? 0f) + (cuerpo[24].visibility ?? 0f) + (cuerpo[26].visibility ?? 0f)) / 3f;
                    if (confIzq < 0.25f && confDer < 0.25f)
                    {
                        msjFeedback = "Ajusta tu posición — torso o rodillas no visibles";
                        return false;
                    }
                }
                break;
            case TipoSupervision.Burpee:
            case TipoSupervision.Plank:
                {
                    float confIzq = ((cuerpo[11].visibility ?? 0f) + (cuerpo[23].visibility ?? 0f) + (cuerpo[27].visibility ?? 0f)) / 3f;
                    float confDer = ((cuerpo[12].visibility ?? 0f) + (cuerpo[24].visibility ?? 0f) + (cuerpo[28].visibility ?? 0f)) / 3f;
                    if (confIzq < 0.25f && confDer < 0.25f)
                    {
                        msjFeedback = "Ajusta tu posición — cuerpo no visible";
                        return false;
                    }
                }
                break;
        }
        return true;
    }

    private Vector2 LM(IList<NormalizedLandmark> cuerpo, int idx) { return new Vector2(cuerpo[idx].x, cuerpo[idx].y); }

    // Helpers 3D
    private Vector3 LMMundo(IList<Landmark> cuerpoMundo, int idx)
    {
        return new Vector3(cuerpoMundo[idx].x, cuerpoMundo[idx].y, cuerpoMundo[idx].z);
    }
    private float CalcularAngulo3D(Vector3 a, Vector3 b, Vector3 c)
    {
        return Vector3.Angle(a - b, c - b);
    }
    private float CalcularAngulo2D(NormalizedLandmark a, NormalizedLandmark b, NormalizedLandmark c)
    {
        float aspect = (float)Screen.width / (float)Screen.height;
        Vector2 va = new Vector2(a.x * aspect, a.y);
        Vector2 vb = new Vector2(b.x * aspect, b.y);
        Vector2 vc = new Vector2(c.x * aspect, c.y);
        return Vector2.Angle(va - vb, vc - vb);
    }

    private void ProcesarRepConAngulo(float angulo, bool invertido = false)
    {
        if (_primerFrame) anguloSuavizado = angulo;
        else
        {
            // Clamp de outliers para mitigar glitches de MediaPipe (Máx 40° por frame)
            float delta = angulo - anguloSuavizado;
            if (Mathf.Abs(delta) > 40f)
            {
                angulo = anguloSuavizado + Mathf.Sign(delta) * 40f;
            }
            anguloSuavizado = Mathf.Lerp(anguloSuavizado, angulo, SUAVIZADO);
        }
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

    private float ObtenerRango(Queue<float> historia)
    {
        if (historia.Count == 0) return 0f;
        float min = float.MaxValue;
        float max = float.MinValue;
        foreach (var val in historia)
        {
            if (val < min) min = val;
            if (val > max) max = val;
        }
        return max - min;
    }

    private void EvaluarBicepCurl(IList<NormalizedLandmark> cuerpo, IList<Landmark> cuerpoMundo)
    {
        landmarksActivos = new List<int> { 11, 12, 13, 14, 15, 16 }; 

        // Revertido a 2D para mayor estabilidad en vista frontal (evita ruido de profundidad Z)
        float anguloDer = CalcularAngulo2D(cuerpo[12], cuerpo[14], cuerpo[16]);
        float anguloIzq = CalcularAngulo2D(cuerpo[11], cuerpo[13], cuerpo[15]);

        // Registrar historial para detectar movimiento
        _historiaAnguloDer.Enqueue(anguloDer);
        _historiaAnguloIzq.Enqueue(anguloIzq);
        if (_historiaAnguloDer.Count > 45) _historiaAnguloDer.Dequeue();
        if (_historiaAnguloIzq.Count > 45) _historiaAnguloIzq.Dequeue();

        float rangoDer = ObtenerRango(_historiaAnguloDer);
        float rangoIzq = ObtenerRango(_historiaAnguloIzq);

        // Suavizado independiente con clamp de outliers (Máx 40° por frame)
        if (_primerFrame)
        {
            anguloSuavizadoDer = anguloDer;
            anguloSuavizadoIzq = anguloIzq;
        }
        else
        {
            float deltaDer = anguloDer - anguloSuavizadoDer;
            if (Mathf.Abs(deltaDer) > 40f)
            {
                anguloDer = anguloSuavizadoDer + Mathf.Sign(deltaDer) * 40f;
            }

            float deltaIzq = anguloIzq - anguloSuavizadoIzq;
            if (Mathf.Abs(deltaIzq) > 40f)
            {
                anguloIzq = anguloSuavizadoIzq + Mathf.Sign(deltaIzq) * 40f;
            }

            anguloSuavizadoDer = Mathf.Lerp(anguloSuavizadoDer, anguloDer, SUAVIZADO);
            anguloSuavizadoIzq = Mathf.Lerp(anguloSuavizadoIzq, anguloIzq, SUAVIZADO);
        }

        // Evaluar brazo derecho de forma independiente
        switch (faseActualDer)
        {
            case FaseRep.Inicio:
                if (anguloSuavizadoDer < umbralInferior)
                {
                    faseActualDer = FaseRep.Bajando;
                    _tiempoInicioRepDer = Time.time;
                }
                break;
            case FaseRep.Bajando:
                if (anguloSuavizadoDer > umbralSuperior)
                {
                    faseActualDer = FaseRep.Completado;
                    
                    // MEJORA 7: Debounce por brazo (0.6s)
                    if (Time.time - _ultimoRepTimeDer > 0.6f)
                    {
                        repeticiones++;
                        _ultimoRepTimeDer = Time.time;
                        OnRepCompletada?.Invoke();
                    }

                    float duracionRep = Time.time - _tiempoInicioRepDer;
                    if (duracionRep < 0.8f)
                    {
                        repeticionesRapidas++;
                        erroresCometidos.Add("Repetición muy rápida (Falta control en la bajada, brazo derecho)");
                    }

                    faseActualDer = FaseRep.Inicio;
                }
                break;
        }

        // Evaluar brazo izquierdo de forma independiente
        switch (faseActualIzq)
        {
            case FaseRep.Inicio:
                if (anguloSuavizadoIzq < umbralInferior)
                {
                    faseActualIzq = FaseRep.Bajando;
                    _tiempoInicioRepIzq = Time.time;
                }
                break;
            case FaseRep.Bajando:
                if (anguloSuavizadoIzq > umbralSuperior)
                {
                    faseActualIzq = FaseRep.Completado;
                    
                    // MEJORA 7: Debounce por brazo (0.6s)
                    if (Time.time - _ultimoRepTimeIzq > 0.6f)
                    {
                        repeticiones++;
                        _ultimoRepTimeIzq = Time.time;
                        OnRepCompletada?.Invoke();
                    }

                    float duracionRep = Time.time - _tiempoInicioRepIzq;
                    if (duracionRep < 0.8f)
                    {
                        repeticionesRapidas++;
                        erroresCometidos.Add("Repetición muy rápida (Falta control en la bajada, brazo izquierdo)");
                    }

                    faseActualIzq = FaseRep.Inicio;
                }
                break;
        }

        // Seleccionar qué brazo mostrar en el HUD
        bool derActivo = rangoDer > 15f;
        bool izqActivo = rangoIzq > 15f;

        float progresoDer = Mathf.Clamp01(Mathf.InverseLerp(umbralSuperior, umbralInferior, anguloSuavizadoDer));
        float progresoIzq = Mathf.Clamp01(Mathf.InverseLerp(umbralSuperior, umbralInferior, anguloSuavizadoIzq));

        float anguloElegido;
        float progresoElegido;
        FaseRep faseElegida;

        if (derActivo && !izqActivo)
        {
            anguloElegido = anguloSuavizadoDer;
            progresoElegido = progresoDer;
            faseElegida = faseActualDer;
        }
        else if (izqActivo && !derActivo)
        {
            anguloElegido = anguloSuavizadoIzq;
            progresoElegido = progresoIzq;
            faseElegida = faseActualIzq;
        }
        else
        {
            // Si ambos se mueven o ambos están estáticos, mostramos el de mayor progreso
            if (progresoDer >= progresoIzq)
            {
                anguloElegido = anguloSuavizadoDer;
                progresoElegido = progresoDer;
                faseElegida = faseActualDer;
            }
            else
            {
                anguloElegido = anguloSuavizadoIzq;
                progresoElegido = progresoIzq;
                faseElegida = faseActualIzq;
            }
        }

        anguloActual = anguloElegido;
        progreso = progresoElegido;

        // Feedback según el brazo que estamos visualizando
        if (faseElegida == FaseRep.Inicio)
        {
            if (anguloElegido > umbralSuperior) { feedback = "Brazo extendido — ¡Sube!"; colorFeedback = Color.white; }
            else { feedback = "¡Sube con fuerza!"; colorFeedback = new Color(0.2f, 0.8f, 1f); }
        }
        else // FaseRep.Bajando
        {
            if (anguloElegido < umbralInferior) { feedback = "¡Buena contracción!"; colorFeedback = Color.green; }
            else { feedback = "Baja el peso controlado"; colorFeedback = Color.cyan; }
        }
    }

    private void EvaluarAirSquat(IList<NormalizedLandmark> cuerpo, IList<Landmark> cuerpoMundo)
    {
        landmarksActivos = new List<int> { 23, 24, 25, 26, 27, 28 }; 
        float anguloDer = CalcularAngulo3D(LMMundo(cuerpoMundo, 24), LMMundo(cuerpoMundo, 26), LMMundo(cuerpoMundo, 28));
        float anguloIzq = CalcularAngulo3D(LMMundo(cuerpoMundo, 23), LMMundo(cuerpoMundo, 25), LMMundo(cuerpoMundo, 27));
        
        // MEJORA 3: Lado más visible (mayor confianza) en perfil
        float confDer = ((cuerpo[24].visibility ?? 0f) + (cuerpo[26].visibility ?? 0f) + (cuerpo[28].visibility ?? 0f)) / 3f;
        float confIzq = ((cuerpo[23].visibility ?? 0f) + (cuerpo[25].visibility ?? 0f) + (cuerpo[27].visibility ?? 0f)) / 3f;
        float angulo = (confDer > confIzq) ? anguloDer : anguloIzq;

        ProcesarRepConAngulo(angulo);

        if (faseActual == FaseRep.Inicio)
        {
            if (anguloSuavizado > umbralSuperior) { feedback = "De pie — ¡Baja!"; colorFeedback = Color.white; }
            else { feedback = "Baja más la cadera"; colorFeedback = Color.cyan; }
        }
        else // FaseRep.Bajando (subiendo)
        {
            if (anguloSuavizado < umbralInferior) { feedback = "¡Buena profundidad!"; colorFeedback = Color.green; }
            else { feedback = "Empuja con las piernas arriba"; colorFeedback = Color.cyan; }
        }
    }

    private void EvaluarPushUp(IList<NormalizedLandmark> cuerpo, IList<Landmark> cuerpoMundo)
    {
        landmarksActivos = new List<int> { 11, 12, 13, 14, 15, 16, 23, 24, 27, 28 }; 
        float anguloDer = CalcularAngulo3D(LMMundo(cuerpoMundo, 12), LMMundo(cuerpoMundo, 14), LMMundo(cuerpoMundo, 16));
        float anguloIzq = CalcularAngulo3D(LMMundo(cuerpoMundo, 11), LMMundo(cuerpoMundo, 13), LMMundo(cuerpoMundo, 15));
        
        // MEJORA 3: Lado más visible (mayor confianza) en perfil
        float confDer = ((cuerpo[12].visibility ?? 0f) + (cuerpo[14].visibility ?? 0f) + (cuerpo[16].visibility ?? 0f)) / 3f;
        float confIzq = ((cuerpo[11].visibility ?? 0f) + (cuerpo[13].visibility ?? 0f) + (cuerpo[15].visibility ?? 0f)) / 3f;
        float angulo = (confDer > confIzq) ? anguloDer : anguloIzq;

        ProcesarRepConAngulo(angulo);

        float anguloEspaldaDer = CalcularAngulo3D(LMMundo(cuerpoMundo, 12), LMMundo(cuerpoMundo, 24), LMMundo(cuerpoMundo, 28));
        float anguloEspaldaIzq = CalcularAngulo3D(LMMundo(cuerpoMundo, 11), LMMundo(cuerpoMundo, 23), LMMundo(cuerpoMundo, 27));
        float anguloEspalda = (confDer > confIzq) ? anguloEspaldaDer : anguloEspaldaIzq;

        if (anguloEspalda < 145f)
        {
            feedback = "Mantén la espalda recta";
            colorFeedback = Color.red;
            return;
        }

        if (faseActual == FaseRep.Inicio)
        {
            if (anguloSuavizado > umbralSuperior) { feedback = "Arriba — ¡Baja!"; colorFeedback = Color.white; }
            else { feedback = "Baja más el pecho"; colorFeedback = Color.cyan; }
        }
        else // FaseRep.Bajando (subiendo)
        {
            if (anguloSuavizado < umbralInferior) { feedback = "¡Buena flexión!"; colorFeedback = Color.green; }
            else { feedback = "Empuja con fuerza arriba"; colorFeedback = Color.cyan; }
        }
    }

    private void EvaluarJumpingJack(IList<NormalizedLandmark> cuerpo, IList<Landmark> cuerpoMundo)
    {
        landmarksActivos = new List<int> { 11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27, 28 };
        
        // Usar 2D (NormalizedLandmarks) con corrección de aspecto por estabilidad frontal
        float aspect = (float)Screen.width / (float)Screen.height;
        
        Vector2 hombroIzq = new Vector2(cuerpo[11].x * aspect, cuerpo[11].y);
        Vector2 hombroDer = new Vector2(cuerpo[12].x * aspect, cuerpo[12].y);
        Vector2 caderaIzq = new Vector2(cuerpo[23].x * aspect, cuerpo[23].y);
        Vector2 caderaDer = new Vector2(cuerpo[24].x * aspect, cuerpo[24].y);
        Vector2 pieIzq = new Vector2(cuerpo[27].x * aspect, cuerpo[27].y);
        Vector2 pieDer = new Vector2(cuerpo[28].x * aspect, cuerpo[28].y);
        Vector2 manoIzq = new Vector2(cuerpo[15].x * aspect, cuerpo[15].y);
        Vector2 manoDer = new Vector2(cuerpo[16].x * aspect, cuerpo[16].y);

        float alturaTorso = (Vector2.Distance(hombroIzq, caderaIzq) + Vector2.Distance(hombroDer, caderaDer)) / 2f;
        if (alturaTorso < 0.05f) alturaTorso = 0.3f;

        // Eje Y en 2D es positivo hacia abajo: manosArriba es true si Y es menor que la del hombro
        // Calibrado a + 0.3f para ser más permisivo
        bool manosArriba = manoIzq.y < (hombroIzq.y + 0.3f * alturaTorso) && 
                           manoDer.y < (hombroDer.y + 0.3f * alturaTorso);
        
        float distPies = Vector2.Distance(pieDer, pieIzq);
        float ratioPies = distPies / alturaTorso;

        bool piesAbiertos = ratioPies > 0.75f;  // ratio > 0.75 (más permisivo)
        bool piesCerrados = ratioPies < 0.65f;  // ratio < 0.65 (más fácil de registrar al volver)
        
        // Barra de progreso y HUD
        if (_primerFrame) anguloSuavizado = ratioPies;
        else anguloSuavizado = Mathf.Lerp(anguloSuavizado, ratioPies, SUAVIZADO);
        progreso = Mathf.Clamp01(Mathf.InverseLerp(umbralInferior, umbralSuperior, anguloSuavizado));
        anguloActual = progreso * 100f; // Escala para mostrar en UI (0%-100%)

        switch (faseActual)
        {
            case FaseRep.Inicio:
                if (manosArriba && piesAbiertos)
                {
                    faseActual = FaseRep.Bajando;
                    _tiempoInicioRep = Time.time;
                }
                break;
                
            case FaseRep.Bajando:
                if (!manosArriba && piesCerrados)
                {
                    faseActual = FaseRep.Completado;
                    repeticiones++;
                    
                    float duracionRep = Time.time - _tiempoInicioRep;
                    if (duracionRep < 0.6f)
                    {
                        repeticionesRapidas++;
                        erroresCometidos.Add("Ritmo de salto muy rápido");
                    }
                    
                    OnRepCompletada?.Invoke();
                    faseActual = FaseRep.Inicio;
                }
                break;
        }

        if (manosArriba && piesAbiertos)
        {
            feedback = "¡Excelente salto!"; colorFeedback = Color.green;
        }
        else if (!manosArriba && piesCerrados)
        {
            feedback = "Posición inicial — ¡Salta!"; colorFeedback = Color.white;
        }
        else if (manosArriba && !piesAbiertos)
        {
            feedback = "Abre más las piernas al saltar"; colorFeedback = Color.yellow;
        }
        else if (!manosArriba && piesAbiertos)
        {
            feedback = "Sube los brazos sobre la cabeza"; colorFeedback = Color.yellow;
        }
        else
        {
            feedback = "Saltando..."; colorFeedback = Color.cyan;
        }
    }

    private void EvaluarSaltosCruzados(IList<NormalizedLandmark> cuerpo, IList<Landmark> cuerpoMundo)
    {
        landmarksActivos = new List<int> { 23, 24, 25, 26, 27, 28 };

        // Usar 2D (NormalizedLandmarks) con corrección de aspecto por estabilidad frontal
        float aspect = (float)Screen.width / (float)Screen.height;

        Vector2 caderaIzq = new Vector2(cuerpo[23].x * aspect, cuerpo[23].y);
        Vector2 caderaDer = new Vector2(cuerpo[24].x * aspect, cuerpo[24].y);
        Vector2 tobilloIzq = new Vector2(cuerpo[27].x * aspect, cuerpo[27].y);
        Vector2 tobilloDer = new Vector2(cuerpo[28].x * aspect, cuerpo[28].y);

        // Señal principal: distancia horizontal entre tobillos normalizada por ancho de caderas
        // Cuando los pies se cruzan, la distancia se reduce; cuando se abren, crece
        float anchoCaderas = Mathf.Abs(caderaIzq.x - caderaDer.x);
        if (anchoCaderas < 0.02f) anchoCaderas = 0.1f;

        float distTobillos = Mathf.Abs(tobilloIzq.x - tobilloDer.x);
        float ratioPies = distTobillos / anchoCaderas;

        bool piesCruzados = ratioPies < umbralInferior;  // Pies juntos o cruzados (< 0.5)
        bool piesAbiertos = ratioPies > umbralSuperior;  // Pies claramente separados (> 1.0)

        // Barra de progreso y HUD (suavizado)
        if (_primerFrame) anguloSuavizado = ratioPies;
        else anguloSuavizado = Mathf.Lerp(anguloSuavizado, ratioPies, SUAVIZADO);
        progreso = Mathf.Clamp01(Mathf.InverseLerp(umbralSuperior, umbralInferior, anguloSuavizado));
        anguloActual = progreso * 100f; // 0% pies abiertos, 100% pies cruzados

        switch (faseActual)
        {
            case FaseRep.Inicio: // Esperando que las piernas se abran
                if (piesAbiertos)
                {
                    faseActual = FaseRep.Bajando;
                    _tiempoInicioRep = Time.time;
                }
                break;

            case FaseRep.Bajando: // Piernas abiertas, esperando que se crucen
                if (piesCruzados)
                {
                    faseActual = FaseRep.Completado;
                    repeticiones++;

                    float duracionRep = Time.time - _tiempoInicioRep;
                    if (duracionRep < 0.3f)
                    {
                        repeticionesRapidas++;
                        erroresCometidos.Add("Ritmo de salto muy rápido");
                    }

                    OnRepCompletada?.Invoke();
                    faseActual = FaseRep.Inicio;
                }
                break;
        }

        // Feedback visual
        if (piesCruzados)
        {
            feedback = "¡Buen cruce!"; colorFeedback = Color.green;
        }
        else if (piesAbiertos)
        {
            feedback = "Piernas abiertas — ¡Salta y cruza!"; colorFeedback = Color.white;
        }
        else
        {
            feedback = "Saltando..."; colorFeedback = Color.cyan;
        }
    }

    private void EvaluarSitup(IList<NormalizedLandmark> cuerpo, IList<Landmark> cuerpoMundo)
    {
        landmarksActivos = new List<int> { 11, 12, 23, 24, 25, 26 };
        float anguloDer = CalcularAngulo3D(LMMundo(cuerpoMundo, 12), LMMundo(cuerpoMundo, 24), LMMundo(cuerpoMundo, 26));
        float anguloIzq = CalcularAngulo3D(LMMundo(cuerpoMundo, 11), LMMundo(cuerpoMundo, 23), LMMundo(cuerpoMundo, 25));
        
        // MEJORA 3: Lado más visible (mayor confianza) en perfil
        float confDer = ((cuerpo[12].visibility ?? 0f) + (cuerpo[24].visibility ?? 0f) + (cuerpo[26].visibility ?? 0f)) / 3f;
        float confIzq = ((cuerpo[11].visibility ?? 0f) + (cuerpo[23].visibility ?? 0f) + (cuerpo[25].visibility ?? 0f)) / 3f;
        float angulo = (confDer > confIzq) ? anguloDer : anguloIzq;

        ProcesarRepConAngulo(angulo);

        if (faseActual == FaseRep.Inicio)
        {
            if (anguloSuavizado > umbralSuperior) { feedback = "Acostado — ¡Sube el torso!"; colorFeedback = Color.white; }
            else { feedback = "Sigue subiendo el torso"; colorFeedback = Color.cyan; }
        }
        else // FaseRep.Bajando (bajando)
        {
            if (anguloSuavizado < umbralInferior) { feedback = "¡Arriba! Buen crunch"; colorFeedback = Color.green; }
            else { feedback = "Baja controlado"; colorFeedback = Color.cyan; }
        }
    }

    private void EvaluarBurpee(IList<NormalizedLandmark> cuerpo, IList<Landmark> cuerpoMundo)
    {
        landmarksActivos = new List<int> { 11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27, 28 };
        
        // MEJORA 3: Lado más visible (mayor confianza) en perfil
        float confDer = ((cuerpo[12].visibility ?? 0f) + (cuerpo[24].visibility ?? 0f) + (cuerpo[28].visibility ?? 0f)) / 3f;
        float confIzq = ((cuerpo[11].visibility ?? 0f) + (cuerpo[23].visibility ?? 0f) + (cuerpo[27].visibility ?? 0f)) / 3f;
        
        float anguloDer = CalcularAngulo3D(LMMundo(cuerpoMundo, 12), LMMundo(cuerpoMundo, 24), LMMundo(cuerpoMundo, 28));
        float anguloIzq = CalcularAngulo3D(LMMundo(cuerpoMundo, 11), LMMundo(cuerpoMundo, 23), LMMundo(cuerpoMundo, 27));
        float angulo = (confDer > confIzq) ? anguloDer : anguloIzq;

        ProcesarRepConAngulo(angulo);

        float alturaRelativa = Mathf.Abs(LMMundo(cuerpoMundo, 12).y - LMMundo(cuerpoMundo, 28).y); // Altura en metros
        float alturaTorso = Vector3.Distance(LMMundo(cuerpoMundo, 12), LMMundo(cuerpoMundo, 24));
        if (alturaTorso < 0.1f) alturaTorso = 0.5f;
        float ratioAltura = alturaRelativa / alturaTorso;

        if (anguloSuavizado > umbralSuperior && ratioAltura > 2.0f) { feedback = "¡De pie! — Baja al suelo"; colorFeedback = Color.white; }
        else if (ratioAltura < 1.0f) { feedback = "¡En el suelo! — Empuja arriba"; colorFeedback = Color.green; }
        else { feedback = "Transición..."; colorFeedback = Color.cyan; }
    }

    private void EvaluarPlank(IList<NormalizedLandmark> cuerpo, IList<Landmark> cuerpoMundo)
    {
        landmarksActivos = new List<int> { 11, 12, 23, 24, 27, 28 };

        // MEJORA 3: Lado más visible (mayor confianza) en perfil
        float confDer = ((cuerpo[12].visibility ?? 0f) + (cuerpo[24].visibility ?? 0f) + (cuerpo[28].visibility ?? 0f)) / 3f;
        float confIzq = ((cuerpo[11].visibility ?? 0f) + (cuerpo[23].visibility ?? 0f) + (cuerpo[27].visibility ?? 0f)) / 3f;
        bool usarDerecho = confDer > confIzq;

        int idxShoulder = usarDerecho ? 12 : 11;
        int idxHip = usarDerecho ? 24 : 23;
        int idxAnkle = usarDerecho ? 28 : 27;

        float angulo = CalcularAngulo3D(LMMundo(cuerpoMundo, idxShoulder), LMMundo(cuerpoMundo, idxHip), LMMundo(cuerpoMundo, idxAnkle));
        
        // MEJORA 5: Clamp de outliers (Máx 40° por frame)
        if (_primerFrame) anguloSuavizado = angulo;
        else
        {
            float delta = angulo - anguloSuavizado;
            if (Mathf.Abs(delta) > 40f)
            {
                angulo = anguloSuavizado + Mathf.Sign(delta) * 40f;
            }
            anguloSuavizado = Mathf.Lerp(anguloSuavizado, angulo, SUAVIZADO);
        }
        anguloActual = anguloSuavizado;

        float yShoulder = LMMundo(cuerpoMundo, idxShoulder).y;
        float yHip = LMMundo(cuerpoMundo, idxHip).y;
        float yAnkle = LMMundo(cuerpoMundo, idxAnkle).y;
        float yEsperado = (yShoulder + yAnkle) / 2f;
        float difY = yHip - yEsperado; // En metros: difY > 0 es cadera caída, difY < 0 es levantada

        float alturaTorso = Vector3.Distance(LMMundo(cuerpoMundo, 12), LMMundo(cuerpoMundo, 24));
        if (alturaTorso < 0.1f) alturaTorso = 0.5f;
        
        // MEJORA 2: Desviación normalizada respecto a altura del torso
        float difYNormalizado = difY / alturaTorso;

        if (anguloSuavizado > umbralInferior && Mathf.Abs(difYNormalizado) < 0.22f) // Desviación máxima de 22% del torso
        {
            tiempoEnPlank += Time.deltaTime;
            progreso = (tiempoEnPlank % TIEMPO_REP_PLANK) / TIEMPO_REP_PLANK;
            int repsCalculados = Mathf.FloorToInt(tiempoEnPlank / TIEMPO_REP_PLANK);
            if (repsCalculados > repeticiones) { repeticiones = repsCalculados; OnRepCompletada?.Invoke(); }
            feedback = $"¡Buena plancha! {tiempoEnPlank:F0}s"; colorFeedback = Color.green;
        }
        else
        {
            // Eje Y positivo es hacia arriba: difYNormalizado > 0 significa cadera levantada, difYNormalizado < 0 significa cadera caída
            if (difYNormalizado > 0.22f) { feedback = "Cadera muy alta — ¡Baja la cadera!"; colorFeedback = Color.red; }
            else if (difYNormalizado < -0.22f) { feedback = "Cadera muy baja — ¡Sube la pelvis!"; colorFeedback = Color.red; }
            else { feedback = "Alinea tu cuerpo en línea recta"; colorFeedback = Color.yellow; }
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