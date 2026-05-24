using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "GenosisFit/CharacterDatabase")]
public class CharacterDatabase : ScriptableObject
{
    [Header("Lista de Personajes (Ej: Mark, X-Bot)")]
    public GameObject[] avataresPrefabs;

    [Header("Lista de Ejercicios (Archivos .controller)")]
    public RuntimeAnimatorController[] ejerciciosControllers;
}
