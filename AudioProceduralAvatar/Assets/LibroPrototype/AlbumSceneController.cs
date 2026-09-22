using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AudioProceduralAvatar.Avatar;
using AudioProceduralAvatar.Persistence;

// Controlador principal de la escena Album. Carga todos los AvatarProfile
// guardados, y maneja el libro como "aperturas" de 24 avatares (12 por
// página). Los 24 AvatarThumbnailSlot ya existen en la escena (arrastrados
// a mano en el editor) -- este script solo los reutiliza (pooling real:
// nunca se instancia ni se destruye nada al cambiar de página).
public class AlbumSceneController : MonoBehaviour
{
    [Header("Datos")]
    public AvatarPortraitDatabase portraitDatabase;

    [Header("Páginas (12 slots cada una, ya armadas en la escena)")]
    public AvatarThumbnailSlot[] leftPageSlots;
    public AvatarThumbnailSlot[] rightPageSlots;

    [Header("Navegación")]
    public Button nextButton;
    public Button previousButton;

    [Header("Búsqueda")]
    public TMP_InputField searchInput;
    public Button searchButton;
    public GameObject notFoundMessage;

    [Header("Panel de detalle")]
    public AvatarDetailPanel detailPanel;

    private const int SlotsPerPage = 12;
    private const int SlotsPerSpread = SlotsPerPage * 2;

    private readonly List<AvatarProfile> _avatars = new();
    private int _spreadIndex;

    private void Start()
    {
        LoadAllAvatars();
        WireUi();
        ShowSpread(0);
    }

    private void LoadAllAvatars()
    {
        _avatars.Clear();
        foreach (string id in AvatarJsonStorage.GetAllAvatarIds())
        {
            var profile = AvatarJsonStorage.Load(id);
            if (profile != null)
                _avatars.Add(profile);
        }
    }

    private void WireUi()
    {
        if (nextButton != null) nextButton.onClick.AddListener(NextSpread);
        if (previousButton != null) previousButton.onClick.AddListener(PreviousSpread);
        if (searchButton != null) searchButton.onClick.AddListener(Search);

        foreach (var slot in leftPageSlots) slot.OnSelected += OnThumbnailSelected;
        foreach (var slot in rightPageSlots) slot.OnSelected += OnThumbnailSelected;

        if (notFoundMessage != null) notFoundMessage.SetActive(false);
    }

    private int MaxSpreadIndex =>
        _avatars.Count == 0 ? 0 : (_avatars.Count - 1) / SlotsPerSpread;

    public void NextSpread()
    {
        if (_spreadIndex < MaxSpreadIndex)
            ShowSpread(_spreadIndex + 1);
    }

    public void PreviousSpread()
    {
        if (_spreadIndex > 0)
            ShowSpread(_spreadIndex - 1);
    }

    private void ShowSpread(int spreadIndex)
    {
        _spreadIndex = Mathf.Clamp(spreadIndex, 0, MaxSpreadIndex);
        int startIndex = _spreadIndex * SlotsPerSpread;

        FillPage(leftPageSlots, startIndex);
        FillPage(rightPageSlots, startIndex + SlotsPerPage);

        if (previousButton != null) previousButton.interactable = _spreadIndex > 0;
        if (nextButton != null) nextButton.interactable = _spreadIndex < MaxSpreadIndex;
    }

    private void FillPage(AvatarThumbnailSlot[] slots, int startIndex)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int avatarIndex = startIndex + i;
            if (avatarIndex < _avatars.Count)
                slots[i].SetAvatar(_avatars[avatarIndex], portraitDatabase);
            else
                slots[i].Clear();
        }
    }

    private void OnThumbnailSelected(string avatarId)
    {
        if (string.IsNullOrEmpty(avatarId)) return;

        var profile = _avatars.FirstOrDefault(a => a.Id == avatarId);
        if (profile == null) return;

        if (detailPanel != null)
            detailPanel.Open(profile);
    }

    // ================= BÚSQUEDA =================

    public void Search()
    {
        if (notFoundMessage != null) notFoundMessage.SetActive(false);
        if (searchInput == null) return;

        string query = searchInput.text?.Trim();
        if (string.IsNullOrEmpty(query)) return;

        int foundIndex = _avatars.FindIndex(a =>
            (!string.IsNullOrEmpty(a.AvatarName) &&
             a.AvatarName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
            (!string.IsNullOrEmpty(a.StudentCode) &&
             a.StudentCode.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));

        if (foundIndex < 0)
        {
            if (notFoundMessage != null) notFoundMessage.SetActive(true);
            return;
        }

        int targetSpread = foundIndex / SlotsPerSpread;
        ShowSpread(targetSpread);
        HighlightAvatar(_avatars[foundIndex].Id);
    }

    private void HighlightAvatar(string avatarId)
    {
        foreach (var slot in leftPageSlots) slot.SetHighlighted(slot.AvatarId == avatarId);
        foreach (var slot in rightPageSlots) slot.SetHighlighted(slot.AvatarId == avatarId);
    }
}
