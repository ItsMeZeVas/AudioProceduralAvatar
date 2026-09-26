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
    private readonly HashSet<string> _highlightedIds = new();
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

        // Búsqueda en vivo: dispara con cada tecla, no hace falta el botón.
        // El botón se deja igual por si alguien lo toca de costumbre / para
        // accesibilidad, pero ya no es obligatorio.
        if (searchInput != null) searchInput.onValueChanged.AddListener(_ => Search());

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

        // SetAvatar() apaga el resalte de cada slot al reusarlo -- lo
        // reaplicamos acá para que una búsqueda con varios resultados se
        // mantenga marcada aunque cambies de página con Next/Previous.
        ApplyHighlights();

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
        if (string.IsNullOrEmpty(query))
        {
            ClearHighlight();
            return;
        }

        var matches = new List<(AvatarProfile avatar, int index)>();
        for (int i = 0; i < _avatars.Count; i++)
        {
            var a = _avatars[i];
            bool matchesName = !string.IsNullOrEmpty(a.AvatarName) &&
                a.AvatarName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
            bool matchesCode = !string.IsNullOrEmpty(a.StudentCode) &&
                a.StudentCode.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

            if (matchesName || matchesCode)
                matches.Add((a, i));
        }

        if (matches.Count == 0)
        {
            if (notFoundMessage != null) notFoundMessage.SetActive(true);
            ClearHighlight();
            return;
        }

        _highlightedIds.Clear();
        foreach (var m in matches)
            _highlightedIds.Add(m.avatar.Id);

        // Salta a la página del primer resultado; el resto de coincidencias
        // queda marcado igual, se vean ya en esta página o al navegar a otra.
        int targetSpread = matches[0].index / SlotsPerSpread;
        ShowSpread(targetSpread);
    }

    private void ApplyHighlights()
    {
        foreach (var slot in leftPageSlots)
            slot.SetHighlighted(slot.AvatarId != null && _highlightedIds.Contains(slot.AvatarId));
        foreach (var slot in rightPageSlots)
            slot.SetHighlighted(slot.AvatarId != null && _highlightedIds.Contains(slot.AvatarId));
    }

    private void ClearHighlight()
    {
        _highlightedIds.Clear();
        foreach (var slot in leftPageSlots) slot.SetHighlighted(false);
        foreach (var slot in rightPageSlots) slot.SetHighlighted(false);
    }
}
