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

    public ExerciseData(string nombre, string grupoMuscular, string dificultad, int idControlador, TipoSupervision tipoSupervision)
    {
        this.nombre = nombre;
        this.grupoMuscular = grupoMuscular;
        this.dificultad = dificultad;
        this.idControlador = idControlador;
        this.tipoSupervision = tipoSupervision;
    }

    // Basado en el listado real de controllers de PlaceExample
    public static ExerciseData[] ObtenerCatalogo()
    {
        return new ExerciseData[]
        {
            new ExerciseData("Sentadillas",        "Piernas", "Media", 0,  TipoSupervision.AirSquat),
            new ExerciseData("Sentadilla y Brazos", "Piernas", "Media", 1,  TipoSupervision.AirSquat),
            new ExerciseData("Curl de Bíceps",      "Brazos",  "Baja",  2,  TipoSupervision.BicepCurl),
            new ExerciseData("Crunch Bicicleta",    "Core",    "Media", 3,  TipoSupervision.Situp),
            new ExerciseData("Burpee",              "Todos",   "Alta",  4,  TipoSupervision.Burpee),
            new ExerciseData("Crunch Circular",     "Core",    "Media", 5,  TipoSupervision.Situp),
            new ExerciseData("Saltos Cruzados",     "Piernas", "Alta",  6,  TipoSupervision.JumpingJack),
            new ExerciseData("Derribe",             "Espalda", "Alta",  7,  TipoSupervision.Generic),
            new ExerciseData("Jumping Jacks",       "Todos",   "Baja",  9,  TipoSupervision.JumpingJack),
            new ExerciseData("Caminata Pike",       "Pecho",   "Alta",  11, TipoSupervision.PushUp),
            new ExerciseData("Flexiones de Brazo",  "Pecho",   "Media", 12, TipoSupervision.PushUp),
            new ExerciseData("Plancha Abdominal",   "Core",    "Media", 13, TipoSupervision.Plank)
        };
    }
}
