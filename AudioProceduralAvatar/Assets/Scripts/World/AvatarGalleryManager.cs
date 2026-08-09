using System;
using System.Collections.Generic;
using UnityEngine;
using AudioProceduralAvatar.Avatar;
using AudioProceduralAvatar.Audio;
namespace AudioProceduralAvatar.World
{
    /// <summary>
    /// Punto único de entrada para "un avatar nuevo entra a la galería".
    /// Organiza el mundo en planos de profundidad (eje Z): un número base de
    /// planos autorados por Diseño (marcadores colocados a mano en el editor),
    /// y creación automática de planos adicionales si esos se llenan.
    ///
    /// Dentro de cada plano, los avatares se acomodan en fila a lo largo del
    /// eje X (para el recorrido tipo side-scroller dentro del plano).
    /// </summary>
    public class AvatarGalleryManager : MonoBehaviour
    {
        public static AvatarGalleryManager Instance { get; private set; }

        [Header("Prefab placeholder (reemplazar cuando haya arte final)")]
        [SerializeField] private AvatarDisplay avatarPrefab;

        [Header("Planos autorados por Diseño (opcional, orden = orden de la lista)")]
        [Tooltip("Transforms colocados a mano en el editor marcando la posición Z de cada plano. Si se deja vacío, se crea un plano inicial en Z=0.")]
        [SerializeField] private List<Transform> authoredPlaneMarkers = new();

        [Header("Configuración de planos")]
        [SerializeField] private int avatarsPerPlane = 6;
        [SerializeField] private float planeSpacingZ = 5f;
        [SerializeField] private float avatarSpacingX = 3f;
        [SerializeField] private Transform spawnOrigin;

        [Header("Audio (opcional, para pruebas — la selección real se conecta en la siguiente iteración)")]
        [SerializeField] private SimpleSynthRenderer sharedRenderer;

        public event Action<AvatarDisplay> AvatarSelected;
        public event Action<GalleryPlane> PlaneCreated;

        private readonly List<AvatarInstance> _createdAvatars = new();
        private readonly List<GalleryPlane> _planes = new();

        public IReadOnlyList<AvatarInstance> CreatedAvatars => _createdAvatars;
        public IReadOnlyList<GalleryPlane> Planes => _planes;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Ya existe un AvatarGalleryManager en la escena. Destruyendo el duplicado.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePlanes();
        }

        private void InitializePlanes()
        {
            if (authoredPlaneMarkers.Count > 0)
            {
                for (int i = 0; i < authoredPlaneMarkers.Count; i++)
                {
                    float z = authoredPlaneMarkers[i] != null ? authoredPlaneMarkers[i].position.z : i * planeSpacingZ;
                    _planes.Add(new GalleryPlane(i, z, avatarsPerPlane));
                }
            }
            else
            {
                // Sin planos autorados: arrancamos con uno solo en Z=0.
                _planes.Add(new GalleryPlane(0, 0f, avatarsPerPlane));
            }
        }

        /// <summary>
        /// Crea el registro del avatar y lo instancia en el primer plano con
        /// espacio disponible. Si ninguno tiene espacio, crea un plano nuevo
        /// automáticamente después del último.
        /// </summary>
        public AvatarInstance CreateAndSpawn(AvatarProfile profile, LeitmotivData leitmotiv, Sprite capturedImage = null)
        {
            var instance = new AvatarInstance(profile, leitmotiv, capturedImage);
            _createdAvatars.Add(instance);

            var plane = GetOrCreatePlaneWithRoom();
            SpawnVisual(instance, plane);

            return instance;
        }

        private GalleryPlane GetOrCreatePlaneWithRoom()
        {
            foreach (var plane in _planes)
            {
                if (plane.HasRoom) return plane;
            }

            var last = _planes[_planes.Count - 1];
            var newPlane = new GalleryPlane(_planes.Count, last.ZPosition + planeSpacingZ, avatarsPerPlane);
            _planes.Add(newPlane);
            PlaneCreated?.Invoke(newPlane);
            return newPlane;
        }

        private void SpawnVisual(AvatarInstance instance, GalleryPlane plane)
        {
            if (avatarPrefab == null)
            {
                Debug.LogWarning("AvatarGalleryManager: no hay avatarPrefab asignado, no se puede spawnear visualmente.");
                return;
            }

            Vector3 origin = spawnOrigin != null ? spawnOrigin.position : Vector3.zero;
            int slotIndex = plane.Occupants.Count;
            Vector3 position = new Vector3(
                origin.x + slotIndex * avatarSpacingX,
                origin.y,
                plane.ZPosition);

            var display = Instantiate(avatarPrefab, position, Quaternion.identity, transform);
            display.Initialize(instance);
            display.Selected += HandleAvatarSelected;

            plane.Occupants.Add(display);

            // TODO (rendimiento, riesgo ya identificado en la propuesta):
            // si la galería acumula cientos de avatares durante horas de evento,
            // evaluar pooling o desactivar renderers de planos no visibles.
        }

        /// <summary>Extensión en X del plano actual, útil para acotar el movimiento del jugador dentro del plano.</summary>
        public (float min, float max) GetPlaneXBounds(int planeIndex, float margin = 2f)
        {
            if (planeIndex < 0 || planeIndex >= _planes.Count) return (0f, 0f);
            var plane = _planes[planeIndex];
            float originX = spawnOrigin != null ? spawnOrigin.position.x : 0f;
            float maxOccupied = plane.Occupants.Count > 0 ? (plane.Occupants.Count - 1) * avatarSpacingX : 0f;
            return (originX - margin, originX + maxOccupied + margin);
        }

        private void HandleAvatarSelected(AvatarDisplay display)
        {
            var instance = display.Data;
            Debug.Log($"[AvatarGalleryManager] Avatar seleccionado: {instance.Profile.AvatarName}");

            // Prueba rápida de audio mientras no existe el sistema de selección
            // + ficha definitivo. La siguiente iteración reemplaza esto por el
            // flujo real (mostrar ficha, luego reproducir).
            if (sharedRenderer != null)
                sharedRenderer.PlayLeitmotiv(instance.Leitmotiv);

            AvatarSelected?.Invoke(display);
        }
    }
}
