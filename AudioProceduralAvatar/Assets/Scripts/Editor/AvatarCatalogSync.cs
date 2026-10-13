using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// IMPORTANTE: este archivo debe ir dentro de una carpeta llamada "Editor".
//
// Copia el contenido de AvatarClothingCatalog a los sitios que ya existian
// (AvatarCreator, AvatarLayerOptionSet, AvatarPortraitLayerSet, SecretAvatarPreset)
// para que NADIE tenga que poner sprites a mano en cada uno.
//
// Seguridad:
//  - Nunca reduce una lista (los JSON guardados apuntan por indice).
//  - Las capas vacias en el catalogo no se tocan (se consideran "aun no migradas").
//  - Solo escribe cuando hay diferencias (no ensucia assets/escenas sin necesidad).
//  - Menu "Verificar" muestra lo que cambiaria SIN escribir nada.
public static class AvatarCatalogSync
{
    private const string MenuRoot = "Avatar/Catalogo/";
    private const string DefaultCatalogPath = "Assets/AvatarClothingCatalog.asset";
    private const int MaxLinesPerSection = 40;

    private static bool _running;
    private static bool _autoPending;

    private sealed class Report
    {
        public readonly List<string> Changes = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> Errors = new List<string>();
    }

    // =====================================================================
    // MENUS
    // =====================================================================

    [MenuItem(MenuRoot + "Sincronizar ahora")]
    public static void MenuSync()
    {
        RunInteractive(apply: true);
    }

    [MenuItem(MenuRoot + "Verificar (no escribe nada)")]
    public static void MenuVerify()
    {
        RunInteractive(apply: false);
    }

    [MenuItem(MenuRoot + "Importar desde configuracion actual")]
    public static void MenuImport()
    {
        AvatarCreator creator = FindLoadedCreator();
        if (creator == null)
        {
            EditorUtility.DisplayDialog(
                "Catalogo de prendas",
                "Abre la escena de personalizacion (la que tiene el AvatarCreator) y vuelve a ejecutar la importacion.",
                "OK");
            return;
        }

        AvatarClothingCatalog cat = FindCatalog(warnIfMany: true);
        if (cat == null)
        {
            cat = ScriptableObject.CreateInstance<AvatarClothingCatalog>();
            AssetDatabase.CreateAsset(cat, DefaultCatalogPath);
        }

        // Mientras importamos, el auto-sync debe estar apagado.
        cat.autoSync = false;

        List<AvatarLayerOptionSet> optionSets = LoadAll<AvatarLayerOptionSet>();
        List<AvatarPortraitLayerSet> portraitSets = LoadAll<AvatarPortraitLayerSet>();
        var importReport = new Report();

        foreach (AvatarLayer layer in creator.layers)
        {
            if (layer == null || string.IsNullOrWhiteSpace(layer.layerName)) continue;

            AvatarClothingCatalog.LayerEntries entries = cat.GetLayer(layer.layerName);
            if (entries != null && entries.options.Count > 0)
            {
                importReport.Warnings.Add($"Capa '{layer.layerName}': el catalogo ya tiene prendas, no se importa encima.");
                continue;
            }

            if (entries == null)
            {
                entries = new AvatarClothingCatalog.LayerEntries { layerName = layer.layerName };
                cat.layers.Add(entries);
            }

            AvatarLayerOptionSet optSet = optionSets.FirstOrDefault(s => s.layerName == layer.layerName);
            AvatarPortraitLayerSet portSet = portraitSets.FirstOrDefault(s => s.layerName == layer.layerName);

            int n = layer.sprites != null ? layer.sprites.Length : 0;
            int nRig = (optSet != null && optSet.options != null) ? optSet.options.Length : 0;
            int nPortrait = (portSet != null && portSet.sprites != null) ? portSet.sprites.Length : 0;

            if (optSet == null)
                importReport.Warnings.Add($"Capa '{layer.layerName}': no tiene AvatarLayerOptionSet (el rig no la conoce). Se importa solo personalizacion.");
            else if (nRig != n)
                importReport.Warnings.Add($"Capa '{layer.layerName}': personalizacion={n} prendas pero rig={nRig}. Revisa que el orden coincida.");

            if (portSet == null)
                importReport.Warnings.Add($"Capa '{layer.layerName}': no tiene AvatarPortraitLayerSet (el album no la conoce).");
            else if (nPortrait != n)
                importReport.Warnings.Add($"Capa '{layer.layerName}': personalizacion={n} prendas pero album={nPortrait}. Revisa que el orden coincida.");

            for (int i = 0; i < n; i++)
            {
                Sprite sprite = layer.sprites[i];
                entries.options.Add(new AvatarClothingCatalog.Entry
                {
                    label = sprite != null ? sprite.name : $"{layer.layerName} {i}",
                    previewSprite = sprite,
                    rigPieces = (i < nRig) ? optSet.options[i] : null,
                    locked = false
                });
            }

            int nSecretRig = (optSet != null && optSet.secretOptions != null) ? optSet.secretOptions.Length : 0;
            int nSecretPortrait = (portSet != null && portSet.secretSprites != null) ? portSet.secretSprites.Length : 0;
            int nSecret = Math.Max(nSecretRig, nSecretPortrait);

            for (int j = 0; j < nSecret; j++)
            {
                Sprite preview = (j < nSecretPortrait) ? portSet.secretSprites[j] : null;
                AvatarPieceSet rig = (j < nSecretRig) ? optSet.secretOptions[j] : null;
                entries.secretOptions.Add(new AvatarClothingCatalog.Entry
                {
                    label = preview != null ? preview.name : $"{layer.layerName} secreta {-(j + 1)}",
                    previewSprite = preview,
                    rigPieces = rig,
                    locked = false
                });
            }
        }

        EditorUtility.SetDirty(cat);
        AssetDatabase.SaveAssets();

        // Verificacion: tras importar, sincronizar NO deberia cambiar nada.
        Report verify = Execute(cat, apply: false, includeClosedScenes: true);

        bool clean = verify.Changes.Count == 0 && verify.Errors.Count == 0;
        if (clean)
        {
            cat.autoSync = true;
            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
        }

        PrintToConsole(importReport, "Importacion", applied: true);
        PrintToConsole(verify, "Verificacion posterior a la importacion", applied: false);

        EditorUtility.DisplayDialog(
            "Catalogo de prendas",
            clean
                ? "Importado y verificado: 0 diferencias. Auto-sync ACTIVADO. Desde ahora agrega prendas solo en el catalogo."
                : $"Importado, pero la verificacion encontro {verify.Changes.Count} diferencias y {verify.Errors.Count} errores (ver consola). " +
                  "Auto-sync queda APAGADO. Revisa las diferencias; si son esperadas, usa 'Sincronizar ahora' y activa autoSync en el catalogo.",
            "OK");

        Selection.activeObject = cat;
        EditorGUIUtility.PingObject(cat);
    }

    // =====================================================================
    // AUTOMATIZACION
    // =====================================================================

    [InitializeOnLoadMethod]
    private static void InitHooks()
    {
        // Antes de darle Play: sincroniza assets y escenas abiertas (con lo que haya en memoria).
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;

            AvatarClothingCatalog cat = FindCatalog(warnIfMany: false);
            if (cat == null || !cat.autoSync) return;

            PrintAuto(Execute(cat, apply: true, includeClosedScenes: false));
        };
    }

    /// <summary>Lo llama el postprocessor cuando el catalogo se guarda.</summary>
    public static void RequestAutoSync()
    {
        if (_autoPending) return;
        _autoPending = true;

        EditorApplication.delayCall += () =>
        {
            _autoPending = false;

            AvatarClothingCatalog cat = FindCatalog(warnIfMany: false);
            if (cat == null || !cat.autoSync) return;

            bool allowClosed = !EditorApplication.isPlayingOrWillChangePlaymode;
            PrintAuto(Execute(cat, apply: true, includeClosedScenes: allowClosed));
        };
    }

    // =====================================================================
    // NUCLEO
    // =====================================================================

    private static void RunInteractive(bool apply)
    {
        AvatarClothingCatalog cat = FindCatalog(warnIfMany: true);
        if (cat == null)
        {
            EditorUtility.DisplayDialog(
                "Catalogo de prendas",
                "No hay ningun AvatarClothingCatalog en el proyecto. Usa 'Importar desde configuracion actual' o crea uno con Create > Avatar > Clothing Catalog.",
                "OK");
            return;
        }

        Report report = Execute(cat, apply, includeClosedScenes: true);
        PrintToConsole(report, apply ? "Sincronizacion" : "Verificacion", applied: apply);

        EditorUtility.DisplayDialog(
            "Catalogo de prendas",
            $"{(apply ? "Cambios aplicados" : "Cambios pendientes")}: {report.Changes.Count}\n" +
            $"Avisos: {report.Warnings.Count}\n" +
            $"Errores: {report.Errors.Count}\n\nDetalle en la consola.",
            "OK");
    }

    private static Report Execute(AvatarClothingCatalog cat, bool apply, bool includeClosedScenes)
    {
        var r = new Report();
        if (_running) return r;

        _running = true;
        try
        {
            Validate(cat, r);
            SyncOptionSets(cat, apply, r);
            SyncPortraitSets(cat, apply, r);
            SyncSecretPresets(cat, apply, r);
            SyncCreators(cat, apply, r, includeClosedScenes);

            if (apply && r.Changes.Count > 0)
                AssetDatabase.SaveAssets();
        }
        catch (Exception e)
        {
            r.Errors.Add("Excepcion durante la sincronizacion: " + e.Message);
            Debug.LogException(e);
        }
        finally
        {
            _running = false;
        }

        return r;
    }

    private static void Validate(AvatarClothingCatalog cat, Report r)
    {
        var seen = new HashSet<string>();

        foreach (var layer in cat.layers)
        {
            if (layer == null || string.IsNullOrWhiteSpace(layer.layerName))
            {
                r.Errors.Add("Hay una capa sin nombre en el catalogo.");
                continue;
            }

            if (!seen.Add(layer.layerName))
                r.Errors.Add($"Capa '{layer.layerName}' repetida en el catalogo.");

            if (layer.options == null || layer.options.Count == 0)
            {
                r.Warnings.Add($"Capa '{layer.layerName}' vacia en el catalogo: no se sincroniza (aun no migrada).");
                continue;
            }

            if (layer.options[0] != null && layer.options[0].locked)
                r.Warnings.Add($"Capa '{layer.layerName}': la prenda #0 no puede estar bloqueada (la usa el avatar inicial). Se ignora el bloqueo.");

            int withoutRig = 0;
            for (int i = 0; i < layer.options.Count; i++)
            {
                var e = layer.options[i];
                if (e == null || e.previewSprite == null)
                {
                    r.Warnings.Add($"Capa '{layer.layerName}': la prenda #{i} no tiene sprite de personalizacion.");
                    continue;
                }

                if (!e.locked && e.rigPieces == null && layer.defaultRigPieces == null)
                    withoutRig++;
            }

            if (withoutRig > 0)
                r.Warnings.Add($"Capa '{layer.layerName}': {withoutRig} prenda(s) desbloqueada(s) sin rig y la capa no tiene ropa por defecto. En el rig no se vera ropa nueva.");

            if (cat.syncSecrets && layer.secretOptions != null)
            {
                for (int j = 0; j < layer.secretOptions.Count; j++)
                {
                    var e = layer.secretOptions[j];
                    if (e == null || e.previewSprite == null)
                        r.Warnings.Add($"Capa '{layer.layerName}': la prenda secreta #{-(j + 1)} no tiene sprite.");
                }
            }
        }
    }

    // ---------------------------------------------------------------------
    // Rig: AvatarLayerOptionSet
    // ---------------------------------------------------------------------
    private static void SyncOptionSets(AvatarClothingCatalog cat, bool apply, Report r)
    {
        foreach (AvatarLayerOptionSet set in LoadAll<AvatarLayerOptionSet>())
        {
            var layer = cat.GetActiveLayer(set.layerName);
            if (layer == null) continue;

            string path = AssetDatabase.GetAssetPath(set);
            AvatarPieceSet[] options = RigPieces(layer, layer.options);
            AvatarPieceSet[] secrets = RigPieces(layer, layer.secretOptions);
            bool changed = false;

            int curOptions = set.options != null ? set.options.Length : 0;
            if (!WouldShrink(curOptions, options.Length, $"Rig '{set.layerName}'.options ({path})", r, Tail(set.options, options.Length))
                && !SameObjects(set.options, options))
            {
                AddChange(r, $"Rig '{set.layerName}'.options ({path})", set.options, options);
                if (apply) set.options = options;
                changed = true;
            }

            int curSecrets = set.secretOptions != null ? set.secretOptions.Length : 0;
            if (cat.syncSecrets
                && !WouldShrink(curSecrets, secrets.Length, $"Rig '{set.layerName}'.secretOptions ({path})", r, Tail(set.secretOptions, secrets.Length))
                && !SameObjects(set.secretOptions, secrets))
            {
                AddChange(r, $"Rig '{set.layerName}'.secretOptions ({path})", set.secretOptions, secrets);
                if (apply) set.secretOptions = secrets;
                changed = true;
            }

            if (changed && apply) EditorUtility.SetDirty(set);
        }
    }

    // ---------------------------------------------------------------------
    // Album: AvatarPortraitLayerSet
    // ---------------------------------------------------------------------
    private static void SyncPortraitSets(AvatarClothingCatalog cat, bool apply, Report r)
    {
        foreach (AvatarPortraitLayerSet set in LoadAll<AvatarPortraitLayerSet>())
        {
            var layer = cat.GetActiveLayer(set.layerName);
            if (layer == null) continue;

            string path = AssetDatabase.GetAssetPath(set);
            Sprite[] sprites = Previews(layer.options);
            Sprite[] secrets = Previews(layer.secretOptions);
            bool changed = false;

            int curSprites = set.sprites != null ? set.sprites.Length : 0;
            if (!WouldShrink(curSprites, sprites.Length, $"Album '{set.layerName}'.sprites ({path})", r, Tail(set.sprites, sprites.Length))
                && !SameObjects(set.sprites, sprites))
            {
                AddChange(r, $"Album '{set.layerName}'.sprites ({path})", set.sprites, sprites);
                if (apply) set.sprites = sprites;
                changed = true;
            }

            int curSecrets = set.secretSprites != null ? set.secretSprites.Length : 0;
            if (cat.syncSecrets
                && !WouldShrink(curSecrets, secrets.Length, $"Album '{set.layerName}'.secretSprites ({path})", r, Tail(set.secretSprites, secrets.Length))
                && !SameObjects(set.secretSprites, secrets))
            {
                AddChange(r, $"Album '{set.layerName}'.secretSprites ({path})", set.secretSprites, secrets);
                if (apply) set.secretSprites = secrets;
                changed = true;
            }

            if (changed && apply) EditorUtility.SetDirty(set);
        }
    }

    // ---------------------------------------------------------------------
    // Secretos: previewSprite de cada SecretAvatarPreset
    // ---------------------------------------------------------------------
    private static void SyncSecretPresets(AvatarClothingCatalog cat, bool apply, Report r)
    {
        if (!cat.syncSecrets) return;

        foreach (var preset in LoadAll<AudioProceduralAvatar.Avatar.SecretAvatarPreset>())
        {
            if (preset.layers == null) continue;

            string path = AssetDatabase.GetAssetPath(preset);
            bool changed = false;

            for (int i = 0; i < preset.layers.Count; i++)
            {
                var sel = preset.layers[i];
                if (sel.noPiece || sel.spriteIndex >= 0) continue;

                var layer = cat.GetLayer(sel.layerName);
                if (layer == null || layer.secretOptions == null || layer.secretOptions.Count == 0) continue;

                int j = -sel.spriteIndex - 1;
                if (j >= layer.secretOptions.Count)
                {
                    r.Errors.Add($"Preset '{preset.name}' ({path}): capa '{sel.layerName}' usa indice secreto {sel.spriteIndex} pero el catalogo solo tiene {layer.secretOptions.Count} secretas.");
                    continue;
                }

                Sprite wanted = layer.secretOptions[j] != null ? layer.secretOptions[j].previewSprite : null;
                if (wanted == null)
                {
                    r.Warnings.Add($"Preset '{preset.name}': la secreta {sel.spriteIndex} de '{sel.layerName}' no tiene sprite en el catalogo; no se toca la vista previa.");
                    continue;
                }

                if ((UnityEngine.Object)sel.previewSprite != (UnityEngine.Object)wanted)
                {
                    r.Changes.Add($"Preset '{preset.name}': previewSprite de '{sel.layerName}' ({sel.spriteIndex}) ({path})");
                    if (apply)
                    {
                        sel.previewSprite = wanted;
                        preset.layers[i] = sel;
                    }
                    changed = true;
                }
            }

            if (changed && apply) EditorUtility.SetDirty(preset);
        }
    }

    // ---------------------------------------------------------------------
    // Personalizacion: AvatarCreator (en escenas y prefabs)
    // ---------------------------------------------------------------------
    private static void SyncCreators(AvatarClothingCatalog cat, bool apply, Report r, bool includeClosed)
    {
        var openScenePaths = new HashSet<string>();

        // 1) Escenas abiertas: se editan en memoria (queda sucia, tu la guardas).
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;
            openScenePaths.Add(s.path);

            foreach (GameObject root in s.GetRootGameObjects())
            {
                foreach (AvatarCreator c in root.GetComponentsInChildren<AvatarCreator>(true))
                {
                    if (SyncCreator(c, cat, apply, r, $"Escena abierta '{s.name}'") && apply)
                        EditorSceneManager.MarkSceneDirty(s);
                }
            }
        }

        if (!includeClosed) return;

        string scriptPath = FindScriptPath(typeof(AvatarCreator));
        if (scriptPath == null) return;

        // 2) Escenas cerradas que usan AvatarCreator: se abren, se editan, se guardan y se cierran.
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (openScenePaths.Contains(path)) continue;
            if (!AssetDatabase.GetDependencies(path, false).Contains(scriptPath)) continue;

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                bool changed = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (AvatarCreator c in root.GetComponentsInChildren<AvatarCreator>(true))
                        changed |= SyncCreator(c, cat, apply, r, $"Escena '{scene.name}'");

                if (changed && apply)
                    EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // 3) Prefabs que usan AvatarCreator.
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!AssetDatabase.GetDependencies(path, false).Contains(scriptPath)) continue;

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = false;
                foreach (AvatarCreator c in contents.GetComponentsInChildren<AvatarCreator>(true))
                    changed |= SyncCreator(c, cat, apply, r, $"Prefab '{contents.name}'");

                if (changed && apply)
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }

    private static bool SyncCreator(AvatarCreator creator, AvatarClothingCatalog cat, bool apply, Report r, string where)
    {
        if (creator.layers == null) return false;

        bool changed = false;

        foreach (AvatarLayer layer in creator.layers)
        {
            if (layer == null) continue;

            var entries = cat.GetActiveLayer(layer.layerName);
            if (entries == null) continue;

            Sprite[] sprites = Previews(entries.options);
            bool[] locked = LockedFlags(entries);

            int cur = layer.sprites != null ? layer.sprites.Length : 0;
            if (WouldShrink(cur, sprites.Length, $"{where}: AvatarLayer '{layer.layerName}'", r, Tail(layer.sprites, sprites.Length)))
                continue;

            bool spritesDiff = !SameObjects(layer.sprites, sprites);
            bool lockedDiff = !SameFlags(layer.locked, locked);
            if (!spritesDiff && !lockedDiff) continue;

            if (spritesDiff) AddChange(r, $"{where}: AvatarLayer '{layer.layerName}'", layer.sprites, sprites);
            if (lockedDiff) r.Changes.Add($"{where}: AvatarLayer '{layer.layerName}': bloqueos actualizados");
            changed = true;

            if (apply)
            {
                layer.sprites = sprites;
                layer.locked = locked;
            }
        }

        if (changed && apply)
            EditorUtility.SetDirty(creator);

        return changed;
    }

    // =====================================================================
    // AYUDAS
    // =====================================================================

    private static AvatarPieceSet[] RigPieces(AvatarClothingCatalog.LayerEntries layer, List<AvatarClothingCatalog.Entry> list)
    {
        if (list == null) return new AvatarPieceSet[0];

        var result = new AvatarPieceSet[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            bool hasOwn = e != null && e.rigPieces != null;
            result[i] = hasOwn ? e.rigPieces : layer.defaultRigPieces;
        }
        return result;
    }

    private static Sprite[] Previews(List<AvatarClothingCatalog.Entry> list)
    {
        if (list == null) return new Sprite[0];

        var result = new Sprite[list.Count];
        for (int i = 0; i < list.Count; i++)
            result[i] = list[i] != null ? list[i].previewSprite : null;
        return result;
    }

    private static bool[] LockedFlags(AvatarClothingCatalog.LayerEntries layer)
    {
        var flags = new bool[layer.options.Count];
        for (int i = 0; i < flags.Length; i++)
            flags[i] = layer.options[i] != null && layer.options[i].locked;

        if (flags.Length > 0) flags[0] = false; // la #0 nunca se bloquea
        return flags;
    }

    private static bool WouldShrink(int current, int desired, string what, Report r, string extra = "")
    {
        if (desired >= current) return false;

        r.Errors.Add($"{what}: el catalogo tiene {desired} prendas pero el sistema actual tiene {current}. No se toca " +
                     "(el catalogo nunca puede tener menos prendas que las existentes: los JSON guardados apuntan por indice)." + extra);
        return true;
    }

    private static string NameOf(UnityEngine.Object o)
    {
        return o != null ? o.name : "vacio";
    }

    // Nombres de lo que "sobra" en el sistema actual respecto al catalogo.
    private static string Tail(UnityEngine.Object[] current, int from)
    {
        if (current == null) return "";

        var names = new List<string>();
        for (int i = from; i < current.Length; i++)
            names.Add($"#{i} {NameOf(current[i])}");

        return names.Count == 0 ? "" : " Las que sobran en el sistema actual: " + string.Join(", ", names) + ".";
    }

    // Registra un cambio de arreglo distinguiendo prendas NUEVAS de prendas EXISTENTES que cambiarian.
    private static void AddChange<T>(Report r, string prefix, T[] current, T[] desired) where T : UnityEngine.Object
    {
        int cur = current != null ? current.Length : 0;
        int added = Math.Max(0, desired.Length - cur);

        var modified = new List<string>();
        for (int i = 0; i < Math.Min(cur, desired.Length); i++)
        {
            if ((UnityEngine.Object)current[i] != (UnityEngine.Object)desired[i])
                modified.Add($"#{i} {NameOf(current[i])} -> {NameOf(desired[i])}");
        }

        string text = $"{prefix}: {cur} -> {desired.Length} (+{added} nuevas)";
        if (modified.Count > 0)
            text += $"; ATENCION, {modified.Count} EXISTENTES CAMBIAN: " + string.Join(", ", modified);

        r.Changes.Add(text);

        if (modified.Count > 0)
            r.Warnings.Add($"{prefix}: cambiaria prendas que YA existen. Revisalo antes de sincronizar.");
    }

    private static bool SameObjects<T>(T[] current, T[] desired) where T : UnityEngine.Object
    {
        int curLen = current != null ? current.Length : 0;
        if (curLen != desired.Length) return false;

        for (int i = 0; i < desired.Length; i++)
        {
            if ((UnityEngine.Object)current[i] != (UnityEngine.Object)desired[i])
                return false;
        }
        return true;
    }

    private static bool SameFlags(bool[] current, bool[] desired)
    {
        int n = Math.Max(current != null ? current.Length : 0, desired.Length);
        for (int i = 0; i < n; i++)
        {
            bool a = current != null && i < current.Length && current[i];
            bool b = i < desired.Length && desired[i];
            if (a != b) return false;
        }
        return true;
    }

    private static List<T> LoadAll<T>() where T : UnityEngine.Object
    {
        var list = new List<T>();
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) list.Add(asset);
        }
        return list;
    }

    private static AvatarClothingCatalog FindCatalog(bool warnIfMany)
    {
        string[] guids = AssetDatabase.FindAssets("t:AvatarClothingCatalog");
        if (guids.Length == 0) return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        if (guids.Length > 1 && warnIfMany)
            Debug.LogWarning($"[AvatarCatalogSync] Hay {guids.Length} AvatarClothingCatalog en el proyecto; se usa el primero ({path}).");

        return AssetDatabase.LoadAssetAtPath<AvatarClothingCatalog>(path);
    }

    private static string FindScriptPath(Type type)
    {
        foreach (string guid in AssetDatabase.FindAssets(type.Name + " t:MonoScript"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(p);
            if (ms != null && ms.GetClass() == type) return p;
        }
        return null;
    }

    private static AvatarCreator FindLoadedCreator()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;

            foreach (GameObject root in s.GetRootGameObjects())
            {
                var c = root.GetComponentInChildren<AvatarCreator>(true);
                if (c != null) return c;
            }
        }
        return null;
    }

    // ---------------------------------------------------------------------
    // Salida
    // ---------------------------------------------------------------------
    private static void PrintAuto(Report r)
    {
        // En modo automatico solo molestamos si paso algo importante.
        if (r.Changes.Count == 0 && r.Errors.Count == 0) return;
        PrintToConsole(r, "Auto-sync", applied: true);
    }

    private static void PrintToConsole(Report r, string title, bool applied)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[AvatarCatalogSync] {title}: {r.Changes.Count} cambios{(applied ? " aplicados" : " pendientes")}, {r.Warnings.Count} avisos, {r.Errors.Count} errores.");
        AppendSection(sb, "CAMBIOS", r.Changes);
        AppendSection(sb, "AVISOS", r.Warnings);
        AppendSection(sb, "ERRORES", r.Errors);

        if (r.Errors.Count > 0) Debug.LogError(sb.ToString());
        else if (r.Warnings.Count > 0) Debug.LogWarning(sb.ToString());
        else Debug.Log(sb.ToString());
    }

    private static void AppendSection(StringBuilder sb, string title, List<string> lines)
    {
        if (lines.Count == 0) return;

        sb.AppendLine(title + ":");
        for (int i = 0; i < lines.Count && i < MaxLinesPerSection; i++)
            sb.AppendLine("  - " + lines[i]);

        if (lines.Count > MaxLinesPerSection)
            sb.AppendLine($"  ... y {lines.Count - MaxLinesPerSection} mas");
    }
}

// Se dispara cuando el catalogo se guarda (Ctrl+S / File > Save Project).
internal sealed class AvatarCatalogPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
        {
            if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(AvatarClothingCatalog))
            {
                AvatarCatalogSync.RequestAutoSync();
                return;
            }
        }
    }
}
