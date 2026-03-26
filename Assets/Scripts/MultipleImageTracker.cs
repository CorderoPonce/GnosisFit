using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class MultipleImageTracker : MonoBehaviour
{
    // Una clase auxiliar para guardar en Unity qué objeto va con qué nombre de imagen
    [Serializable]
    public struct TrackableImage
    {
        public string imageName; // El nombre exacto de la imagen en tu biblioteca
        public GameObject prefabToSpawn; // El mensaje/cofre/objeto que quieres mostrar
    }

    [SerializeField]
    private List<TrackableImage> libraryOfImages; // Esta lista aparecerá en el Inspector

    private ARTrackedImageManager trackedImageManager;
    
    // Un diccionario (lista rápida de buscar) para guardar los objetos que ya creamos y no repetirlos
    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();

    private void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        // Nos suscribimos al evento de cuando cambian las imágenes detectadas
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        // Nos desuscribimos al apagar el script para evitar errores
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Cuando se detecta una imagen nueva por primera vez
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            SpawnOrActivatePrefab(trackedImage);
        }

        // Cuando el rastreo se actualiza (cada frame que la cámara se mueve)
        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            UpdatePrefabPosition(trackedImage);
        }

        // Cuando la cámara deja de ver la imagen (puedes elegir ocultar el objeto o dejarlo ahí)
        foreach (ARTrackedImage trackedImage in eventArgs.removed)
        {
            if (spawnedPrefabs.ContainsKey(trackedImage.referenceImage.name))
            {
                // Dejamos el objeto ahí, pero podrías usar SetActive(false) para ocultarlo
            }
        }
    }

    private void SpawnOrActivatePrefab(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (spawnedPrefabs.ContainsKey(imageName))
        {
            spawnedPrefabs[imageName].SetActive(true);
            UpdatePrefabPosition(trackedImage);
            return;
        }

        foreach (TrackableImage item in libraryOfImages)
        {
            if (item.imageName == imageName && item.prefabToSpawn != null)
            {
                // CREAMOS EL OBJETO
                GameObject newPrefab = Instantiate(item.prefabToSpawn, trackedImage.transform.position, trackedImage.transform.rotation);
                
                newPrefab.transform.parent = null; 
                newPrefab.transform.localScale = Vector3.one; 

                // --- ¡LA PRUEBA DEFINITIVA! ---
                Handheld.Vibrate(); // Tu celular vibrará físicamente al detectarlo
                // ------------------------------

                spawnedPrefabs.Add(imageName, newPrefab);
                break;
            }
        }
    }

    private void UpdatePrefabPosition(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // Si tenemos el objeto y la imagen se está rastreando bien...
        if (spawnedPrefabs.ContainsKey(imageName) && trackedImage.trackingState == TrackingState.Tracking)
        {
            GameObject currentPrefab = spawnedPrefabs[imageName];
            currentPrefab.SetActive(true);

            // Actualizamos su posición para que siga a la imagen
            currentPrefab.transform.position = trackedImage.transform.position;
            currentPrefab.transform.rotation = trackedImage.transform.rotation;
        }
        else if (spawnedPrefabs.ContainsKey(imageName) && trackedImage.trackingState != TrackingState.Tracking)
        {
            // Opcional: Si el rastreo es malo, podrías ocultar el objeto
            // spawnedPrefabs[imageName].SetActive(false);
        }
    }
}
