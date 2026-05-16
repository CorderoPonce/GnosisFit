using UnityEngine;

public class GenosisFitDataManager : MonoBehaviour
{
    public static GenosisFitDataManager Instance { get; private set; }

    public string EjercicioSeleccionado = "Sentadillas";
    public TipoSupervision TipoEjercicio = TipoSupervision.Generic;
    public int IndicePersonaje = 0;
    public int IndiceEjercicio = 0;
    public bool VieneDeAR = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void EstablecerEjercicio(string nombre)
    {
        EjercicioSeleccionado = nombre;
    }
}
