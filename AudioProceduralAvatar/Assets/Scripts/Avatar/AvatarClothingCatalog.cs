using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CATALOGO CENTRAL DE PRENDAS.
///
/// Es el UNICO lugar donde se agregan prendas. AvatarCatalogSync (carpeta Editor)
/// copia esta informacion a los sitios que ya existian:
///   - AvatarCreator.layers[].sprites / locked   (escena de personalizacion)
///   - AvatarLayerOptionSet.options / secretOptions   (rig)
///   - AvatarPortraitLayerSet.sprites / secretSprites (album)
///   - SecretAvatarPreset.layers[].previewSprite      (secretos)
///
/// REGLAS:
///  - La POSICION en cada lista = indice guardado en los JSON de los avatares.
///    NO reordenes ni borres prendas existentes. Las nuevas van SIEMPRE al final.
///  - La prenda #0 de cada capa no se puede bloquear (es la que usa el avatar inicial).
/// </summary>
[CreateAssetMenu(fileName = "AvatarClothingCatalog", menuName = "Avatar/Clothing Catalog")]
public class AvatarClothingCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Nombre para reconocerla en la lista (solo orientativo; no se usa en el juego).")]
        public string label;

        [Tooltip("Sprite que se ve en la personalizacion y en las miniaturas del album.")]
        public Sprite previewSprite;

        [Tooltip("Piezas del rig (AvatarPieceSet). Dejalo vacio si el rig aun no esta armado: se usara la ropa por defecto de la capa.")]
        public AvatarPieceSet rigPieces;

        [Tooltip("Bloqueada = NO aparece en la personalizacion. Desbloqueala cuando quieras que los visitantes puedan elegirla. Si esta desbloqueada y no tiene rig, el rig muestra la ropa por defecto de la capa.")]
        public bool locked;
    }

    [Serializable]
    public class LayerEntries
    {
        [Tooltip("Debe coincidir EXACTO con AvatarLayer.layerName (ej. Hair, Head, UpperBody, LowerBody, Accessories, SubBoca, SubBarba).")]
        public string layerName;

        [Tooltip("Ropa por defecto del rig para esta capa. Se usa cuando una prenda desbloqueada no tiene rig propio.")]
        public AvatarPieceSet defaultRigPieces;

        [Tooltip("Prendas normales. Posicion en la lista = indice del JSON. Nuevas AL FINAL.")]
        public List<Entry> options = new List<Entry>();

        [Tooltip("Prendas secretas (solo por codigo). Posicion 0 = indice -1, posicion 1 = indice -2, etc. Nuevas AL FINAL.")]
        public List<Entry> secretOptions = new List<Entry>();
    }

    [Tooltip("Si esta activo, el catalogo se sincroniza solo al guardar el proyecto y al darle Play. Se activa automaticamente despues de 'Importar' si no hay diferencias.")]
    public bool autoSync = false;

    [Tooltip("Encendido por defecto: el sync tambien maneja los casilleros secretos (rig, album y vistas previas de los presets). Apagalo si quieres que el sync ignore todo lo de secretos.")]
    public bool syncSecrets = true;

    [Header("NO reordenes ni borres prendas: la posicion en la lista = indice guardado en los JSON. Las nuevas van SIEMPRE al final.")]
    public List<LayerEntries> layers = new List<LayerEntries>();

    public LayerEntries GetLayer(string layerName)
    {
        if (layers == null) return null;
        return layers.Find(l => l != null && l.layerName == layerName);
    }

    /// <summary>Capa que ya tiene prendas cargadas (las capas vacias se consideran "aun no migradas").</summary>
    public LayerEntries GetActiveLayer(string layerName)
    {
        var layer = GetLayer(layerName);
        return (layer != null && layer.options != null && layer.options.Count > 0) ? layer : null;
    }
}
