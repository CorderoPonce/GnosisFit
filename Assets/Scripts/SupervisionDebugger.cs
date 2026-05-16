using UnityEngine;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class SupervisionDebugger : MonoBehaviour
{
    private void OnGUI()
    {
        GUI.color = Color.red;
        GUILayout.BeginArea(new Rect(10, 10, 400, 300));
        GUILayout.Label("=== SUPERVISION DIAGNOSTICS ===");
        
        var runner = FindFirstObjectByType<PoseLandmarkerRunner>();
        GUILayout.Label($"Runner: {(runner != null ? "FOUND" : "MISSING")}");
        if (runner != null)
        {
            GUILayout.Label($"Runner Active: {runner.gameObject.activeInHierarchy}");
            // Usar reflexión para ver el estado interno
            var type = typeof(VisionTaskApiRunner<>).MakeGenericType(typeof(Mediapipe.Tasks.Vision.PoseLandmarker.PoseLandmarker));
            var taskField = type.GetField("taskApi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var taskVal = taskField?.GetValue(runner);
            GUILayout.Label($"Task API: {(taskVal != null ? "INITIALIZED" : "NULL")}");
        }

        var screen = FindFirstObjectByType<Mediapipe.Unity.Screen>();
        GUILayout.Label($"Screen: {(screen != null ? "FOUND" : "MISSING")}");
        if (screen != null)
        {
            GUILayout.Label($"Screen Active: {screen.gameObject.activeInHierarchy}");
            var rawImg = screen.GetComponent<UnityEngine.UI.RawImage>();
            GUILayout.Label($"RawImage Texture: {(rawImg != null && rawImg.texture != null ? rawImg.texture.name : "NULL")}");
        }

        var imageSource = ImageSourceProvider.ImageSource;
        GUILayout.Label($"ImageSource: {(imageSource != null ? imageSource.GetType().Name : "NULL")}");
        if (imageSource != null)
        {
            GUILayout.Label($"Source Name: {imageSource.sourceName}");
            GUILayout.Label($"Resolution: {imageSource.textureWidth}x{imageSource.textureHeight}");
            GUILayout.Label($"Is Prepared: {imageSource.isPrepared}");
            GUILayout.Label($"Is Playing: {imageSource.isPlaying}");
            
            var tex = imageSource.GetCurrentTexture();
            GUILayout.Label($"Texture Object: {(tex != null ? tex.name + " (" + tex.width + "x" + tex.height + ")" : "NULL")}");
        }

        var postura = FindFirstObjectByType<AnalisisPostura>();
        if (postura != null)
        {
            GUILayout.Label($"--- Analisis Postura ---");
            GUILayout.Label($"Cuerpo Detectado: {(postura.cuerpoDetectado ? "SÍ" : "NO")}");
            GUILayout.Label($"Ejercicio: {postura.ejercicioActual}");
            GUILayout.Label($"Reps: {postura.repeticiones}");
        }

        GUILayout.EndArea();
    }
}
