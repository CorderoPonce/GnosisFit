using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class UIMenuCatalog : MonoBehaviour
{
    [Header("Referencias UI")]
    public Transform contentPanel;
    public GameObject cardPrefab;
    
    [Header("Filtros")]
    public Transform muscleFilterParent;
    public Transform diffFilterParent;

    private List<ExerciseData> allExercises;
    private string currentMuscleFilter = "Todos";
    private string currentDiffFilter = "Todas";

    private Color activeColor = new Color(0.1f, 0.1f, 0.1f, 1f); // Oscuro para activo
    private Color inactiveColor = new Color(0.9f, 0.9f, 0.9f, 1f); // Claro para inactivo

    void Start()
    {
        allExercises = ExerciseData.ObtenerCatalogo().ToList();
        
        ConfigurarFiltros();
        ActualizarVista();
    }

    void ConfigurarFiltros()
    {
        if (muscleFilterParent != null)
        {
            foreach (Transform child in muscleFilterParent)
            {
                Button btn = child.GetComponent<Button>();
                string label = child.name.Replace("FilterBtn_", "");
                if (btn) btn.onClick.AddListener(() => SetMuscleFilter(label));
            }
        }

        if (diffFilterParent != null)
        {
            foreach (Transform child in diffFilterParent)
            {
                Button btn = child.GetComponent<Button>();
                string label = child.name.Replace("FilterBtn_", "");
                if (btn) btn.onClick.AddListener(() => SetDiffFilter(label));
            }
        }
    }

    public void SetMuscleFilter(string muscle)
    {
        currentMuscleFilter = muscle;
        ActualizarBotonesVisuales(muscleFilterParent, muscle);
        ActualizarVista();
    }

    public void SetDiffFilter(string diff)
    {
        currentDiffFilter = diff;
        ActualizarBotonesVisuales(diffFilterParent, diff);
        ActualizarVista();
    }

    void ActualizarBotonesVisuales(Transform parent, string activeLabel)
    {
        foreach (Transform child in parent)
        {
            string label = child.name.Replace("FilterBtn_", "");
            Image img = child.GetComponent<Image>();
            TextMeshProUGUI txt = child.GetComponentInChildren<TextMeshProUGUI>();

            if (label == activeLabel)
            {
                if (img) img.color = activeColor;
                if (txt) txt.color = Color.white;
            }
            else
            {
                if (img) img.color = inactiveColor;
                if (txt) txt.color = Color.black;
            }
        }
    }

    void ActualizarVista()
    {
        // Limpiar panel
        foreach (Transform child in contentPanel) 
        {
            if (child.gameObject != cardPrefab) 
                Destroy(child.gameObject);
        }

        if (allExercises == null) return;
        var filtrados = allExercises.Where(e => 
            (currentMuscleFilter == "Todos" || e.grupoMuscular == currentMuscleFilter) &&
            (currentDiffFilter == "Todas" || e.dificultad == currentDiffFilter)
        );

        foreach (var ej in filtrados)
        {
            GameObject card = Instantiate(cardPrefab, contentPanel);
            card.SetActive(true);
            ConfigurarTarjeta(card, ej);
        }
    }

    void ConfigurarTarjeta(GameObject card, ExerciseData data)
    {
        var title = card.transform.Find("Info_Column/Title")?.GetComponent<TextMeshProUGUI>();
        var sub = card.transform.Find("Info_Column/Subtitle")?.GetComponent<TextMeshProUGUI>();
        var btn = card.transform.Find("Info_Column/Btn_VerAR")?.GetComponent<Button>();

        if (title) title.text = data.nombre;
        if (sub) sub.text = $"{data.grupoMuscular} • {data.dificultad}";
        
        if (btn)
        {
            btn.onClick.AddListener(() => IrAModoAR(data));
        }
    }

    void IrAModoAR(ExerciseData data)
    {
        if (GenosisFitDataManager.Instance != null)
        {
            GenosisFitDataManager.Instance.EjercicioSeleccionado = data.nombre;
            GenosisFitDataManager.Instance.TipoEjercicio = data.tipoSupervision;
            GenosisFitDataManager.Instance.IndiceEjercicio = data.idControlador;
        }
        SceneManager.LoadScene("ARMode");
    }
}
