using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class InitSupervisionScene : MonoBehaviour
{
    [Header("Referencias")]
    public SupervisionLayerManager layerManager;
    public AnalisisPostura analisisPostura;

    private void Start()
    {
        Debug.Log("[SupervisionScene] Inicializando modo dedicado...");

        string nombreEjercicio = "Corrección de Postura";

        if (GenosisFitDataManager.Instance != null)
        {
            TipoSupervision tipo = GenosisFitDataManager.Instance.TipoEjercicio;
            nombreEjercicio = GenosisFitDataManager.Instance.EjercicioSeleccionado;
            Debug.Log("[SupervisionScene] Tipo de ejercicio detectado: " + tipo);

            if (analisisPostura != null)
            {
                analisisPostura.ConfigurarEjercicio(tipo);
                analisisPostura.enabled = true;
            }
        }

        // Ocultar la UI de MediaPipe (Footer con botones de config que no sirven al usuario)
        OcultarUIMediaPipe();

        if (layerManager != null)
        {
            StartCoroutine(IniciarConHUD(nombreEjercicio));
        }
        else
        {
            Debug.LogError("[SupervisionScene] No se encontró LayerManager en la escena.");
        }
    }

    private System.Collections.IEnumerator IniciarConHUD(string nombreEjercicio)
    {
        // 1. Activar el pipeline de MediaPipe
        yield return StartCoroutine(layerManager.ActivarAsync());

        // 2. Crear el HUD de supervisión sobre el canvas
        Canvas canvas = layerManager.ObtenerCanvasMaestro();
        if (canvas != null && analisisPostura != null)
        {
            var hud = canvas.gameObject.AddComponent<SupervisionHUD>();
            hud.Inicializar(canvas, analisisPostura, null, nombreEjercicio);
            Debug.Log("[SupervisionScene] HUD de supervisión creado.");
        }
        else
        {
            Debug.LogWarning("[SupervisionScene] No se pudo crear HUD. Canvas: " + (canvas != null) + " Analisis: " + (analisisPostura != null));
        }
    }

    private void OcultarUIMediaPipe()
    {
        // Ocultar el Footer (botones Graph Config y ImageSource Config)
        var footer = GameObject.Find("Footer");
        if (footer != null)
        {
            footer.SetActive(false);
            Debug.Log("[SupervisionScene] Footer de MediaPipe ocultado.");
        }

        // Ocultar el Header (MenuButton hamburguesa)
        var header = GameObject.Find("Header");
        if (header != null)
        {
            header.SetActive(false);
            Debug.Log("[SupervisionScene] Header de MediaPipe ocultado.");
        }

        // Ocultar Modal Panel si existe
        var modal = GameObject.Find("Modal Panel");
        if (modal != null)
        {
            modal.SetActive(false);
        }
    }

    public void RegresarAlMenu()
    {
        Debug.Log("[SupervisionScene] Regresando al Menú Principal...");
        SceneManager.LoadScene("Scene_Menu");
    }
}
