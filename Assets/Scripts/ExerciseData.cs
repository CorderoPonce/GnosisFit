using UnityEngine;

[System.Serializable]
public class ExerciseData
{
    public string nombre;
    public string grupoMuscular;
    public string dificultad;
    public int idControlador;

    public ExerciseData(string nombre, string grupoMuscular, string dificultad, int idControlador)
    {
        this.nombre = nombre;
        this.grupoMuscular = grupoMuscular;
        this.dificultad = dificultad;
        this.idControlador = idControlador;
    }

    // Basado en el listado real de controllers de PlaceExample
    public static ExerciseData[] ObtenerCatalogo()
    {
        return new ExerciseData[]
        {
            new ExerciseData("Sentadillas", "Piernas", "Media", 0),
            new ExerciseData("Sentadilla y Brazos", "Piernas", "Media", 1),
            new ExerciseData("Curl de Bíceps", "Brazos", "Baja", 2),
            new ExerciseData("Crunch Bicicleta", "Core", "Media", 3),
            new ExerciseData("Burpee", "Todos", "Alta", 4),
            new ExerciseData("Crunch Circular", "Core", "Media", 5),
            new ExerciseData("Saltos Cruzados", "Piernas", "Alta", 6),
            new ExerciseData("Derribe", "Espalda", "Alta", 7),
            new ExerciseData("Jumping Jacks", "Todos", "Baja", 9),
            new ExerciseData("Caminata Pike", "Pecho", "Alta", 11),
            new ExerciseData("Flexiones de Brazo", "Pecho", "Media", 12),
            new ExerciseData("Plancha Abdominal", "Core", "Media", 13)
        };
    }
}
