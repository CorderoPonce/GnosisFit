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
            case TipoSupervision.SaltosCruzados:
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
            float distHombros = Vector2.Distance(LM(cuerpo, 11), LM(cuerpo, 12));
            float distCaderas = Vector2.Distance(LM(cuerpo, 23), LM(cuerpo, 24));
            
            // Calculamos el largo del torso en pantalla para normalizar por la distancia del usuario
            Vector2 hombroMedio = (LM(cuerpo, 11) + LM(cuerpo, 12)) / 2f;
            Vector2 caderaMedia = (LM(cuerpo, 23) + LM(cuerpo, 24)) / 2f;
            float largoTorso = Vector2.Distance(hombroMedio, caderaMedia);
            if (largoTorso < 0.05f) largoTorso = 0.1f; // Evitar división por cero o ruido extremo

            float relacionHombros = distHombros / largoTorso;
            float relacionCaderas = distCaderas / largoTorso;

            // Detección altamente tolerante para evitar bloqueos por distancia o relación de aspecto:
            // Frente: los hombros o las caderas están ensanchados horizontalmente o la distancia absoluta es suficiente
            bool esFrente = relacionHombros > 0.38f || relacionCaderas > 0.28f || distHombros > 0.08f || distCaderas > 0.06f;
            // Perfil: tanto hombros como caderas están extremadamente comprimidos en el eje X (escala independiente)
            bool esPerfil = relacionHombros < 0.24f && relacionCaderas < 0.18f;

            if (vistaRequerida == VistaRequerida.Perfil && esFrente)
            {
                feedback = "Gírate y colócate de PERFIL a la cámara";
                colorFeedback = Color.red;
                return; // Bloquea el análisis si está claramente de frente
            }
            else if (vistaRequerida == VistaRequerida.Frente && esPerfil)
            {
                feedback = "Gírate y colócate de FRENTE a la cámara";
                colorFeedback = Color.red;
                return; // Bloquea el análisis si está claramente de perfil
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
            case TipoSupervision.SaltosCruzados: EvaluarSaltosCruzados(cuerpo); break;
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

        if (faseActual == FaseRep.Inicio)
        {
            if (anguloSuavizado > 140f) { feedback = "Brazo extendido — ¡Sube!"; colorFeedback = Color.white; }
            else if (anguloSuavizado < 90f) { feedback = "¡Sigue subiendo!"; colorFeedback = new Color(0.2f, 0.8f, 1f); }
            else { feedback = "¡Sube con fuerza!"; colorFeedback = new Color(0.2f, 0.8f, 1f); }
        }
        else // FaseRep.Bajando (retorno controlado)
        {
            if (anguloSuavizado < 50f) { feedback = "¡Buena contracción!"; colorFeedback = Color.green; }
            else { feedback = "Baja el peso controlado"; colorFeedback = Color.cyan; }
        }
    }

    private void EvaluarAirSquat(IList<NormalizedLandmark> cuerpo)
    {
        landmarksActivos = new List<int> { 23, 24, 25, 26, 27, 28 }; 
        float angulo = (CalcularAngulo(LM(cuerpo, 24), LM(cuerpo, 26), LM(cuerpo, 28)) + CalcularAngulo(LM(cuerpo, 23), LM(cuerpo, 25), LM(cuerpo, 27))) / 2f;
        ProcesarRepConAngulo(angulo);

        if (faseActual == FaseRep.Inicio)
        {
            if (anguloSuavizado > 155f) { feedback = "De pie — ¡Baja!"; colorFeedback = Color.white; }
            else if (anguloSuavizado < 100f) { feedback = "Buena sentadilla"; colorFeedback = new Color(0.5f, 1f, 0.5f); }
            else { feedback = "Baja más la cadera"; colorFeedback = Color.cyan; }
        }
        else // FaseRep.Bajando (subiendo)
        {
            if (anguloSuavizado < 90f) { feedback = "¡Excelente profundidad!"; colorFeedback = Color.green; }
            else { feedback = "Empuja con las piernas arriba"; colorFeedback = Color.cyan; }
        }
    }

    private void EvaluarPushUp(IList<NormalizedLandmark> cuerpo)
    {
        landmarksActivos = new List<int> { 11, 12, 13, 14, 15, 16, 23, 24, 27, 28 }; 
        float angulo = (CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 14), LM(cuerpo, 16)) + CalcularAngulo(LM(cuerpo, 11), LM(cuerpo, 13), LM(cuerpo, 15))) / 2f;
        ProcesarRepConAngulo(angulo);
        float anguloEspalda = CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 24), LM(cuerpo, 28));

        if (anguloEspalda < 145f)
        {
            feedback = "Mantén la espalda recta";
            colorFeedback = Color.red;
            return;
        }

        if (faseActual == FaseRep.Inicio)
        {
            if (anguloSuavizado > 145f) { feedback = "Arriba — ¡Baja!"; colorFeedback = Color.white; }
            else { feedback = "Baja más el pecho"; colorFeedback = Color.cyan; }
        }
        else // FaseRep.Bajando (subiendo)
        {
            if (anguloSuavizado < 100f) { feedback = "¡Buena flexión!"; colorFeedback = Color.green; }
            else { feedback = "Empuja con fuerza arriba"; colorFeedback = Color.cyan; }
        }
    }

    private void EvaluarJumpingJack(IList<NormalizedLandmark> cuerpo)
    {
        landmarksActivos = new List<int> { 11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27, 28 };
        
        // Determinar si las manos están arriba (por encima de los hombros con un margen cómodo de 0.08)
        bool manosArriba = cuerpo[15].y < (cuerpo[11].y + 0.08f) && cuerpo[16].y < (cuerpo[12].y + 0.08f);
        
        // Determinar si los pies están abiertos
        float distPies = Vector2.Distance(LM(cuerpo, 28), LM(cuerpo, 27));
        bool piesAbiertos = distPies > 0.22f; // Umbral de pies separados
        bool piesCerrados = distPies < 0.14f; // Umbral de pies juntos
        
        // Usamos distPies como el ánguloActual/progreso para mostrar en la barra del HUD
        anguloSuavizado = Mathf.Lerp(anguloSuavizado, distPies, SUAVIZADO);
        anguloActual = anguloSuavizado * 100f; // Escala para mostrar en UI
        progreso = Mathf.Clamp01(Mathf.InverseLerp(0.12f, 0.26f, anguloSuavizado));

        switch (faseActual)
        {
            case FaseRep.Inicio:
                // Fase de apertura: Manos arriba y pies abiertos
                if (manosArriba && piesAbiertos)
                {
                    faseActual = FaseRep.Bajando;
                    _tiempoInicioRep = Time.time;
                }
                break;
                
            case FaseRep.Bajando:
                // Fase de cierre: Manos abajo y pies cerrados
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

    private void EvaluarSaltosCruzados(IList<NormalizedLandmark> cuerpo)
    {
        landmarksActivos = new List<int> { 11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27, 28 };
        
        float distManos = Vector2.Distance(LM(cuerpo, 15), LM(cuerpo, 16));
        float distPies = Vector2.Distance(LM(cuerpo, 27), LM(cuerpo, 28));

        bool manosAbiertas = distManos > 0.38f;
        bool manosCruzadas = distManos < 0.16f;

        bool piesAbiertos = distPies > 0.20f;
        bool piesCruzados = distPies < 0.14f;

        // Para la barra de progreso
        anguloSuavizado = Mathf.Lerp(anguloSuavizado, distPies, SUAVIZADO);
        anguloActual = anguloSuavizado * 100f;
        progreso = Mathf.Clamp01(Mathf.InverseLerp(0.12f, 0.24f, anguloSuavizado));

        // Validación extra: avisar si levanta demasiado los brazos por encima de los hombros
        bool manosMuyAltas = cuerpo[15].y < cuerpo[11].y - 0.05f && cuerpo[16].y < cuerpo[12].y - 0.05f;

        switch (faseActual)
        {
            case FaseRep.Inicio:
                // Fase de apertura: Manos abiertas y pies abiertos
                if (manosAbiertas && piesAbiertos)
                {
                    faseActual = FaseRep.Bajando;
                    _tiempoInicioRep = Time.time;
                }
                break;

            case FaseRep.Bajando:
                // Fase de cruce: Manos cruzadas y pies cruzados
                if (manosCruzadas && piesCruzados)
                {
                    faseActual = FaseRep.Completado;
                    repeticiones++;

                    float duracionRep = Time.time - _tiempoInicioRep;
                    if (duracionRep < 0.6f)
                    {
                        repeticionesRapidas++;
                        erroresCometidos.Add("Ritmo de salto cruzado muy rápido");
                    }

                    OnRepCompletada?.Invoke();
                    faseActual = FaseRep.Inicio;
                }
                break;
        }

        if (manosMuyAltas)
        {
            feedback = "Mantén los brazos a la altura de los hombros";
            colorFeedback = Color.yellow;
        }
        else if (manosAbiertas && piesAbiertos)
        {
            feedback = "¡Buen salto abierto!"; colorFeedback = Color.green;
        }
        else if (manosCruzadas && piesCruzados)
        {
            feedback = "¡Buen cruce!"; colorFeedback = Color.green;
        }
        else if (manosAbiertas && !piesAbiertos)
        {
            feedback = "Abre más las piernas"; colorFeedback = Color.yellow;
        }
        else if (!manosAbiertas && piesAbiertos)
        {
            feedback = "Abre bien los brazos a los lados"; colorFeedback = Color.yellow;
        }
        else
        {
            feedback = "Cruzando..."; colorFeedback = Color.cyan;
        }
    }

    private void EvaluarSitup(IList<NormalizedLandmark> cuerpo)
    {
        landmarksActivos = new List<int> { 11, 12, 23, 24, 25, 26 };
        float angulo = (CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 24), LM(cuerpo, 26)) + CalcularAngulo(LM(cuerpo, 11), LM(cuerpo, 23), LM(cuerpo, 25))) / 2f;
        ProcesarRepConAngulo(angulo);

        if (faseActual == FaseRep.Inicio)
        {
            if (anguloSuavizado > 130f) { feedback = "Acostado — ¡Sube el torso!"; colorFeedback = Color.white; }
            else { feedback = "Sigue subiendo el torso"; colorFeedback = Color.cyan; }
        }
        else // FaseRep.Bajando (bajando)
        {
            if (anguloSuavizado < 80f) { feedback = "¡Arriba! Buen crunch"; colorFeedback = Color.green; }
            else { feedback = "Baja controlado"; colorFeedback = Color.cyan; }
        }
    }

    private void EvaluarBurpee(IList<NormalizedLandmark> cuerpo)
    {
        landmarksActivos = new List<int> { 11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27, 28 };
        ProcesarRepConAngulo(CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 24), LM(cuerpo, 28)));
        float alturaRelativa = Mathf.Abs(cuerpo[12].y - cuerpo[28].y);

        if (anguloSuavizado > 150f && alturaRelativa > 0.3f) { feedback = "¡De pie! — Baja al suelo"; colorFeedback = Color.white; }
        else if (anguloSuavizado < 100f) { feedback = "¡En el suelo! — Empuja arriba"; colorFeedback = Color.green; }
        else { feedback = "Transición..."; colorFeedback = Color.cyan; }
    }

    private void EvaluarPlank(IList<NormalizedLandmark> cuerpo)
    {
        landmarksActivos = new List<int> { 11, 12, 23, 24, 27, 28 };
        anguloSuavizado = Mathf.Lerp(anguloSuavizado, CalcularAngulo(LM(cuerpo, 12), LM(cuerpo, 24), LM(cuerpo, 28)), SUAVIZADO);
        anguloActual = anguloSuavizado;

        float yShoulder = cuerpo[12].y;
        float yHip = cuerpo[24].y;
        float yAnkle = cuerpo[28].y;
        float yEsperado = (yShoulder + yAnkle) / 2f;
        float difY = yHip - yEsperado; // difY > 0 es cadera caída, difY < 0 es cadera levantada

        if (anguloSuavizado > 155f && Mathf.Abs(difY) < 0.08f)
        {
            tiempoEnPlank += Time.deltaTime;
            progreso = (tiempoEnPlank % TIEMPO_REP_PLANK) / TIEMPO_REP_PLANK;
            int repsCalculados = Mathf.FloorToInt(tiempoEnPlank / TIEMPO_REP_PLANK);
            if (repsCalculados > repeticiones) { repeticiones = repsCalculados; OnRepCompletada?.Invoke(); }
            feedback = $"¡Buena plancha! {tiempoEnPlank:F0}s"; colorFeedback = Color.green;
        }
        else
        {
            if (difY > 0.05f) { feedback = "Cadera muy baja — ¡Sube la pelvis!"; colorFeedback = Color.red; }
            else if (difY < -0.05f) { feedback = "Cadera muy alta — ¡Baja la cadera!"; colorFeedback = Color.red; }
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