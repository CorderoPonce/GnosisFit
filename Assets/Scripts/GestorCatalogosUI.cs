using UnityEngine;

public class GestorCatalogosUI : MonoBehaviour
{
    [Header("Paneles de Catálogo")]
    public GameObject catalogoEjercicios;
    public GameObject catalogoVelocidades;
    public GameObject catalogoModelos;

    [Header("Menú Principal")]
    public GameObject menuConfiguracion; // El panel oscuro principal

    // Apaga todo de golpe (Útil para limpiar la pantalla)
    public void CerrarTodosLosCatalogos()
    {
        if (catalogoEjercicios != null) catalogoEjercicios.SetActive(false);
        if (catalogoVelocidades != null) catalogoVelocidades.SetActive(false);
        if (catalogoModelos != null) catalogoModelos.SetActive(false);
    }

    // --- LÓGICA DE LOS BOTONES ---

    public void AlternarEjercicios()
    {
        bool estabaAbierto = catalogoEjercicios.activeSelf;
        CerrarTodosLosCatalogos(); // Siempre cerramos los demás primero
        if (!estabaAbierto) catalogoEjercicios.SetActive(true); // Solo lo abrimos si estaba cerrado
    }

    public void AlternarVelocidades()
    {
        bool estabaAbierto = catalogoVelocidades.activeSelf;
        CerrarTodosLosCatalogos();
        if (!estabaAbierto) catalogoVelocidades.SetActive(true);
    }

    public void AlternarModelos()
    {
        bool estabaAbierto = catalogoModelos.activeSelf;
        CerrarTodosLosCatalogos();
        if (!estabaAbierto) catalogoModelos.SetActive(true);
    }

    // --- LÓGICA DEL BOTÓN "X" CERRAR ---
    
    public void CerrarMenuPrincipal()
    {
        CerrarTodosLosCatalogos(); // Limpiamos los submenús para que no aparezcan abiertos la próxima vez
        if (menuConfiguracion != null) menuConfiguracion.SetActive(false); // Apagamos el menú padre
    }
}