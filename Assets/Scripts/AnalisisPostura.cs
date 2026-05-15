using UnityEngine;
using TMPro;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class AnalisisPostura : MonoBehaviour
{
    [Header("Interfaz")]
    public TextMeshProUGUI textoFeedback;

    // 1. Primero definimos la lista de opciones (SIN el Header arriba)
    public enum TipoEjercicio { BicepCurl, AirSquat, PushUp, JumpingJack, Situp }

    // 2. Ahora ponemos el Header justo encima de la variable que usaremos en el Inspector
    [Header("Configuración del Ejercicio")]
    public TipoEjercicio ejercicioActual = TipoEjercicio.BicepCurl;

    void Update()
    {
        // Revisamos si hay datos de MediaPipe
        if (PoseLandmarkerRunner.ResultadoGlobal.poseLandmarks == null || PoseLandmarkerRunner.ResultadoGlobal.poseLandmarks.Count == 0)
        {
            textoFeedback.text = "<color=yellow>Buscando cuerpo completo...</color>";
            return;
        }

        var cuerpo = PoseLandmarkerRunner.ResultadoGlobal.poseLandmarks[0].landmarks;

        // El Semáforo: ¿Qué ejercicio estamos evaluando ahora mismo?
        switch (ejercicioActual)
        {
            case TipoEjercicio.BicepCurl:
                EvaluarBicepCurl(cuerpo);
                break;
            case TipoEjercicio.AirSquat:
                EvaluarAirSquat(cuerpo);
                break;
            case TipoEjercicio.PushUp:
                EvaluarPushUp(cuerpo);
                break;
            case TipoEjercicio.JumpingJack:
                EvaluarJumpingJack(cuerpo);
                break;
            case TipoEjercicio.Situp:
                EvaluarSitup(cuerpo);
                break;
        }
    }

    // --- 1. LÓGICA DEL BICEP CURL ---
    private void EvaluarBicepCurl(System.Collections.Generic.IList<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> cuerpo)
    {
        Vector2 posHombro = new Vector2(cuerpo[12].x, cuerpo[12].y);
        Vector2 posCodo = new Vector2(cuerpo[14].x, cuerpo[14].y);
        Vector2 posMuneca = new Vector2(cuerpo[16].x, cuerpo[16].y);

        float anguloCodo = Vector2.Angle(posHombro - posCodo, posMuneca - posCodo);

        if (anguloCodo > 150f) textoFeedback.text = $"Curl: {anguloCodo:F0}° - <color=yellow>Baja más</color>";
        else if (anguloCodo < 45f) textoFeedback.text = $"Curl: {anguloCodo:F0}° - <color=green>Buena contracción</color>";
        else textoFeedback.text = $"Curl: {anguloCodo:F0}° - Sigue subiendo";
    }

    // --- 2. LÓGICA DEL AIR SQUAT (SENTADILLA) ---
    private void EvaluarAirSquat(System.Collections.Generic.IList<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> cuerpo)
    {
        // Para la sentadilla usamos el tren inferior derecho (Cadera 24, Rodilla 26, Tobillo 28)
        Vector2 posCadera = new Vector2(cuerpo[24].x, cuerpo[24].y);
        Vector2 posRodilla = new Vector2(cuerpo[26].x, cuerpo[26].y);
        Vector2 posTobillo = new Vector2(cuerpo[28].x, cuerpo[28].y);

        // Ángulo de la rodilla
        float anguloRodilla = Vector2.Angle(posCadera - posRodilla, posTobillo - posRodilla);

        if (anguloRodilla > 160f) textoFeedback.text = $"Sentadilla: {anguloRodilla:F0}° - <color=white>De pie</color>";
        else if (anguloRodilla < 100f) textoFeedback.text = $"Sentadilla: {anguloRodilla:F0}° - <color=green>¡Buena profundidad!</color>";
        else textoFeedback.text = $"Sentadilla: {anguloRodilla:F0}° - <color=yellow>Baja más la cadera</color>";
    }

    // --- 3. PUSH UP ---
    private void EvaluarPushUp(System.Collections.Generic.IList<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> cuerpo)
    {
         // Usa Hombro (12), Codo (14), Muñeca (16). 
         // Lógica similar al Bicep Curl, pero buscando que el codo llegue a 90 grados al bajar.
         textoFeedback.text = "Analizando Push Up...";
    }

    // --- 4. JUMPING JACKS ---
    private void EvaluarJumpingJack(System.Collections.Generic.IList<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> cuerpo)
    {
         // Mide la distancia en el eje X entre Mano Derecha (16) y Mano Izquierda (15).
         // Si están muy cerca arriba, y los pies (27 y 28) están separados, es un salto exitoso.
         textoFeedback.text = "Analizando Jumping Jacks...";
    }

    // --- 5. SIT UPS ---
    private void EvaluarSitup(System.Collections.Generic.IList<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> cuerpo)
    {
         // Usa Hombro (12), Cadera (24) y Rodilla (26). 
         // Si el ángulo de la cadera se cierra (el torso se acerca a los muslos), el situp está completado.
         textoFeedback.text = "Analizando Sit Ups...";
    }
}