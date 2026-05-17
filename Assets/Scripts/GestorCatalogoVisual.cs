using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GestorCatalogoVisual : MonoBehaviour
{
    [Header("Configuración del Catálogo")]
    public Transform contenedorTarjetas; // Arrastra aquí el objeto "Content" del Scroll View
    public GameObject prefabTarjeta;     // Arrastra aquí el Prefab azul desde la ventana Project

    [Header("Botones Grupo (Orden: Todos, Pecho, Piernas, Espalda)")]
    public Button[] botonesGrupo;

    [Header("Botones Dificultad (Orden: Baja, Media, Alta)")]
    public Button[] botonesDificultad;

    private ExerciseData[] catalogo;
    private string filtroGrupo = "Todos";
    private string filtroDificultad = "Todas";
    private List<GameObject> tarjetasInstanciadas = new List<GameObject>();
    private PlaceExample scriptAR;

    void Start()
    {
        scriptAR = FindFirstObjectByType<PlaceExample>();
        catalogo = ExerciseData.ObtenerCatalogo();

        ConfigurarBotonesFiltros();
        
        // Estado inicial por defecto
        if (botonesGrupo.Length > 0) SeleccionarGrupo("Todos", botonesGrupo[0]);
        if (botonesDificultad.Length > 1) SeleccionarDificultad("Media", botonesDificultad[1]);
    }

    void ConfigurarBotonesFiltros()
    {
        string[] grupos = { "Todos", "Pecho", "Piernas", "Espalda" };
        for (int i = 0; i < botonesGrupo.Length && i < grupos.Length; i++)
        {
            int index = i;
            botonesGrupo[i].onClick.AddListener(() => SeleccionarGrupo(grupos[index], botonesGrupo[index]));
        }

        string[] difs = { "Baja", "Media", "Alta" };
        for (int i = 0; i < botonesDificultad.Length && i < difs.Length; i++)
        {
            int index = i;
            botonesDificultad[i].onClick.AddListener(() => SeleccionarDificultad(difs[index], botonesDificultad[index]));
        }
    }

    void SeleccionarGrupo(string grupo, Button btnActivo)
    {
        filtroGrupo = grupo;
        // Pinta todos de transparente y el activo de gris (puedes ajustar estos colores)
        foreach (var b in botonesGrupo) b.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        btnActivo.GetComponent<Image>().color = UIHelper.GRIS_OSCURO;
        FiltrarEjercicios();
    }

    void SeleccionarDificultad(string dif, Button btnActivo)
    {
        if (filtroDificultad == dif)
        {
            filtroDificultad = "Todas";
            foreach (var b in botonesDificultad) b.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        }
        else
        {
            filtroDificultad = dif;
            foreach (var b in botonesDificultad) b.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            btnActivo.GetComponent<Image>().color = UIHelper.GRIS_OSCURO;
        }
        FiltrarEjercicios();
    }

    void FiltrarEjercicios()
    {
        // 1. Limpiar tarjetas anteriores
        foreach (var t in tarjetasInstanciadas) Destroy(t);
        tarjetasInstanciadas.Clear();

        if (catalogo == null) return;

        // 2. Crear las nuevas tarjetas que cumplan el filtro
        foreach (var ej in catalogo)
        {
            bool matchG = (filtroGrupo == "Todos" || ej.grupoMuscular == filtroGrupo);
            bool matchD = (filtroDificultad == "Todas" || ej.dificultad == filtroDificultad);

            if (matchG && matchD)
            {
                CrearTarjetaVisual(ej);
            }
        }
        Canvas.ForceUpdateCanvases();
    }

    void CrearTarjetaVisual(ExerciseData ej)
    {
        // Fotocopiamos el molde
        GameObject nuevaTarjeta = Instantiate(prefabTarjeta, contenedorTarjetas);
        
        // Buscamos los textos por el nombre que les pusimos y les inyectamos los datos
        nuevaTarjeta.transform.Find("TxtNombre").GetComponent<TextMeshProUGUI>().text = ej.nombre;
        nuevaTarjeta.transform.Find("TxtGrupo").GetComponent<TextMeshProUGUI>().text = ej.grupoMuscular;
        nuevaTarjeta.transform.Find("TxtDificultad").GetComponent<TextMeshProUGUI>().text = ej.dificultad;

        // Configuramos el botón
        Button btnRA = nuevaTarjeta.transform.Find("BotonVerRA").GetComponent<Button>();
        btnRA.onClick.AddListener(() => {
            if (scriptAR != null)
            {
                scriptAR.CambiarEjercicio(ej.idControlador);
                var hud = FindFirstObjectByType<ControladorHUD>();
                if (hud != null) hud.CerrarTodo();
            }
        });

        tarjetasInstanciadas.Add(nuevaTarjeta);
    }
}