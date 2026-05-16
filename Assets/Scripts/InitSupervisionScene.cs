using UnityEngine;
using UnityEngine.SceneManagement;

public class InitSupervisionScene : MonoBehaviour
{
    [Header("Referencias")]
    public SupervisionLayerManager layerManager;
    public AnalisisPostura analisisPostura;

    private void Start()
    {
        Debug.Log("[SupervisionScene] Inicializando modo dedicado...");
        
        if (GenosisFitDataManager.Instance != null)
        {
            TipoSupervision tipo = GenosisFitDataManager.Instance.TipoEjercicio;
            Debug.Log("[SupervisionScene] Tipo de ejercicio detectado: " + tipo);

            if (analisisPostura != null)
            {
                analisisPostura.ConfigurarEjercicio(tipo);
                analisisPostura.enabled = true;
            }
        }
        
        if (layerManager != null)
        {
            StartCoroutine(layerManager.ActivarAsync());
        }
        else
        {
            Debug.LogError("[SupervisionScene] No se encontró LayerManager en la escena.");
        }
    }

    public void RegresarA_AR()
    {
        Debug.Log("[SupervisionScene] Regresando a Modo AR...");
        SceneManager.LoadScene("ARMode");
    }
}
