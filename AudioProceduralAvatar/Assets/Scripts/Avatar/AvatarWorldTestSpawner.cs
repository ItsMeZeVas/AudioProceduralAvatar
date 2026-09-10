using UnityEngine;

public class AvatarWorldTestSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject rigPrefab;               // el Prefab del rig (con AvatarSkeletonBuilder adentro)
    public AvatarOptionsDatabase database;      // la base de datos con todas las capas y opciones

    [System.Serializable]
    public class TestLayerSelection
    {
        public string layerName;
        public int spriteIndex;
    }

    [Header("Prueba manual (mientras no leemos el JSON real)")]
    [Tooltip("Simula profile.Layers[] mientras se conecta AvatarJsonStorage")]
    public TestLayerSelection[] testSelections;

    private void Start()
    {
        GameObject instance = Instantiate(rigPrefab, Vector3.zero, Quaternion.identity);
        AvatarSkeletonBuilder builder = instance.GetComponent<AvatarSkeletonBuilder>();

        if (builder == null)
        {
            Debug.LogError("El Prefab del rig no tiene AvatarSkeletonBuilder. Agrégalo y conecta los renderers en el Inspector del Prefab.");
            return;
        }

        // TODO más adelante: reemplazar testSelections por profile.Layers
        // leído desde AvatarJsonStorage. La lógica de abajo no cambia.
        foreach (var selection in testSelections)
        {
            AvatarPieceSet piece = database.Resolve(selection.layerName, selection.spriteIndex);
            builder.Apply(piece); // solo llena los campos no nulos, no borra lo anterior
        }
    }
}
