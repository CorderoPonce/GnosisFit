using UnityEngine;

public enum TipoSupervision
{
    BicepCurl,
    AirSquat,
    PushUp,
    JumpingJack,
    Situp,
    Burpee,
    Plank,
    SaltosCruzados,
    CaminataPike,
    Generic // Sin análisis específico
}

[System.Serializable]
public class ExerciseData
{
    public string nombre;
    public string grupoMuscular;
    public string dificultad;
    public int idControlador;
    public TipoSupervision tipoSupervision;

    // Parámetros de cámara PIP específicos por ejercicio
    public Vector3 offsetCamara;
    public Vector3 targetCamara;
    public float fovCamara;

    public ExerciseData(string nombre, string grupoMuscular, string dificultad, int idControlador, TipoSupervision tipoSupervision, Vector3 offsetCamara = default, Vector3 targetCamara = default, float fovCamara = 45f)
    {
        this.nombre = nombre;
        this.grupoMuscular = grupoMuscular;
        this.dificultad = dificultad;
        this.idControlador = idControlador;
        this.tipoSupervision = tipoSupervision;
        this.offsetCamara = (offsetCamara == default) ? new Vector3(0f, 0.9f, 1.8f) : offsetCamara;
        this.targetCamara = (targetCamara == default) ? new Vector3(0f, 0.75f, 0f) : targetCamara;
        this.fovCamara = fovCamara;
    }

    // Basado en el listado real de controllers de PlaceExample
    public static ExerciseData[] ObtenerCatalogo()
    {
        return new ExerciseData[]
        {
            // Sentadillas (Squats) - Centradas verticalmente para la flexión del movimiento
            new ExerciseData("Sentadillas",        "Piernas", "Media", 0,  TipoSupervision.AirSquat, new Vector3(0f, 0.75f, 1.8f), new Vector3(0f, 0.45f, 0f), 45f),
            new ExerciseData("Sentadilla y Brazos", "Piernas", "Media", 1,  TipoSupervision.AirSquat, new Vector3(0f, 0.75f, 1.8f), new Vector3(0f, 0.45f, 0f), 45f),

            // Curl de Bíceps (Standing Bicep Curls) - Cámara a nivel de pecho/cintura mirando arriba
            new ExerciseData("Curl de Bíceps",      "Brazos",  "Baja",  2,  TipoSupervision.BicepCurl, new Vector3(0f, 0.9f, 1.8f), new Vector3(0f, 0.75f, 0f), 45f),

            // Crunches / Situps (Crunches y Abdominales) - Cámara baja al ras del suelo
            new ExerciseData("Crunch Bicicleta",    "Core",    "Media", 3,  TipoSupervision.Situp, new Vector3(0f, 0.4f, 1.8f), new Vector3(0f, 0.2f, 0f), 45f),
            new ExerciseData("Crunch Circular",     "Core",    "Media", 5,  TipoSupervision.Situp, new Vector3(0f, 0.4f, 1.8f), new Vector3(0f, 0.2f, 0f), 45f),

            // Burpees (Movimiento completo cuerpo entero)
            new ExerciseData("Burpee",              "Todos",   "Alta",  4,  TipoSupervision.Burpee, new Vector3(0f, 0.5f, 1.8f), new Vector3(0f, 0.3f, 0f), 45f),

            // Saltos Cruzados / Jumping Jacks (Standing jumping motion)
            new ExerciseData("Saltos Cruzados",     "Piernas", "Alta",  6,  TipoSupervision.SaltosCruzados, new Vector3(0f, 0.9f, 1.8f), new Vector3(0f, 0.75f, 0f), 45f),
            new ExerciseData("Jumping Jacks",       "Todos",   "Baja",  9,  TipoSupervision.JumpingJack, new Vector3(0f, 0.9f, 1.8f), new Vector3(0f, 0.75f, 0f), 45f),

            // Flexiones / Pecho en el suelo (PushUps)
            new ExerciseData("Caminata Pike",       "Pecho",   "Alta",  11, TipoSupervision.CaminataPike, new Vector3(0f, 0.45f, 1.8f), new Vector3(0f, 0.25f, 0f), 45f),
            new ExerciseData("Flexiones de Brazo",  "Pecho",   "Media", 12, TipoSupervision.PushUp, new Vector3(0f, 0.45f, 1.8f), new Vector3(0f, 0.25f, 0f), 45f),

            // Abdominales (Situps - Crunches y Abdominales)
            new ExerciseData("Abdominales",         "Core",    "Media", 13, TipoSupervision.Situp, new Vector3(0f, 0.4f, 1.8f), new Vector3(0f, 0.2f, 0f), 45f),

            // Derribe / Genérico
            new ExerciseData("Derribe",             "Espalda", "Alta",  7,  TipoSupervision.Generic, new Vector3(0f, 0.9f, 1.8f), new Vector3(0f, 0.75f, 0f), 45f)
        };
    }
}
