using UnityEngine;

public class EmpujarChat : MonoBehaviour
{
    [Header("Configuración")]
    public RectTransform barraEntrada;
    public float alturaSalto = 900f; // Distancia que subirá la barra

    private Vector2 posicionOriginal;

    void Start()
    {
        // Guardamos la posición original donde anclaste la barra
        if (barraEntrada != null)
        {
            posicionOriginal = barraEntrada.anchoredPosition;
        }
    }

    void Update()
    {
        if (barraEntrada == null) return;

        // Detectamos si el teclado nativo del celular está abierto
        if (TouchScreenKeyboard.visible)
        {
            // Empujamos la barra hacia arriba
            barraEntrada.anchoredPosition = new Vector2(posicionOriginal.x, posicionOriginal.y + alturaSalto);
        }
        else
        {
            // Si el teclado está cerrado, la devolvemos a su lugar
            barraEntrada.anchoredPosition = posicionOriginal;
        }
    }
}