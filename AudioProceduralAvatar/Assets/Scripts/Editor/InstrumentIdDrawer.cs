#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AudioProceduralAvatar.Audio;

[CustomPropertyDrawer(typeof(InstrumentIdAttribute))]
public class InstrumentIdDrawer : PropertyDrawer
{
    private static string searchText = "";

    private static List<string> GetAllInstrumentIds()
    {
        HashSet<string> ids =
            new HashSet<string>();

        // ----------------------------------------------------
        // INSTRUMENTOS PROCEDURALES INTERNOS
        // ----------------------------------------------------

        foreach (
            string id
            in InstrumentPresetCatalog.BuiltInIds)
        {
            if (!string.IsNullOrEmpty(id))
                ids.Add(id);
        }

        // ----------------------------------------------------
        // INSTRUMENTOS CREADOS COMO ASSETS
        // ----------------------------------------------------

        string[] guids =
            AssetDatabase.FindAssets(
                "t:InstrumentPreset"
            );

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            InstrumentPreset preset =
                AssetDatabase.LoadAssetAtPath<InstrumentPreset>(
                    path
                );

            if (
                preset != null &&
                !string.IsNullOrEmpty(
                    preset.PresetId
                )
            )
            {
                ids.Add(
                    preset.PresetId
                );
            }
        }

        List<string> result =
            new List<string>(ids);

        result.Sort();

        return result;
    }

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        return
            EditorGUIUtility.singleLineHeight * 3f
            +
            EditorGUIUtility.standardVerticalSpacing * 2f;
    }

    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        if (
            property.propertyType !=
            SerializedPropertyType.String
        )
        {
            EditorGUI.LabelField(
                position,
                label.text,
                "InstrumentId solo funciona con string."
            );

            return;
        }

        EditorGUI.BeginProperty(
            position,
            label,
            property
        );

        float line =
            EditorGUIUtility.singleLineHeight;

        float spacing =
            EditorGUIUtility.standardVerticalSpacing;

        // ----------------------------------------------------
        // BÚSQUEDA
        // ----------------------------------------------------

        Rect searchRect =
            new Rect(
                position.x,
                position.y,
                position.width,
                line
            );

        searchText =
            EditorGUI.TextField(
                searchRect,
                "Buscar instrumento",
                searchText
            );

        // ----------------------------------------------------
        // LISTA
        // ----------------------------------------------------

        List<string> allIds =
            GetAllInstrumentIds();

        List<string> filteredIds =
            new List<string>();

        string search =
            searchText.Trim().ToLowerInvariant();

        for (int i = 0; i < allIds.Count; i++)
        {
            string id =
                allIds[i];

            if (
                string.IsNullOrEmpty(search) ||
                id.ToLowerInvariant().Contains(search)
            )
            {
                filteredIds.Add(id);
            }
        }

        // ----------------------------------------------------
        // ASEGURAR VALOR ACTUAL
        // ----------------------------------------------------

        string currentValue =
            property.stringValue;

        if (
            !string.IsNullOrEmpty(currentValue) &&
            !filteredIds.Contains(currentValue)
        )
        {
            filteredIds.Insert(
                0,
                currentValue
            );
        }

        if (filteredIds.Count == 0)
        {
            Rect emptyRect =
                new Rect(
                    position.x,
                    position.y + line + spacing,
                    position.width,
                    line
                );

            EditorGUI.HelpBox(
                emptyRect,
                "No se encontraron instrumentos.",
                MessageType.Info
            );

            EditorGUI.EndProperty();

            return;
        }

        // ----------------------------------------------------
        // POPUP
        // ----------------------------------------------------

        string[] options =
            filteredIds.ToArray();

        int selectedIndex =
            Mathf.Max(
                0,
                filteredIds.IndexOf(
                    currentValue
                )
            );

        Rect popupRect =
            new Rect(
                position.x,
                position.y +
                line +
                spacing,
                position.width,
                line
            );

        int newIndex =
            EditorGUI.Popup(
                popupRect,
                label.text,
                selectedIndex,
                options
            );

        if (
            newIndex >= 0 &&
            newIndex < filteredIds.Count
        )
        {
            property.stringValue =
                filteredIds[newIndex];
        }

        // ----------------------------------------------------
        // ID ACTUAL
        // ----------------------------------------------------

        Rect idRect =
            new Rect(
                position.x,
                position.y +
                (line + spacing) * 2f,
                position.width,
                line
            );

        EditorGUI.LabelField(
            idRect,
            "ID seleccionado:",
            property.stringValue
        );

        EditorGUI.EndProperty();
    }
}

#endif