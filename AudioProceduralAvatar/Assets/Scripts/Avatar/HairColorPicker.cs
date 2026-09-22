using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Selector de color HSV completo: cuadro de Matiz (X) x Saturación (Y) a
// Valor fijo = 1, más una barra vertical de Valor (negro -> color puro
// elegido en el cuadro). Se usa UNA instancia por capa a colorear (una
// para Hair, otra independiente para SubBarba -- no comparten estado).
public class HairColorPicker : MonoBehaviour
{
    [Header("Cuadro Matiz x Saturación")]
    public RawImage hueSatSquare;
    public NormalizedDragArea squareDragArea;
    public RectTransform squareHandle;

    [Header("Barra de Valor (negro -> color puro elegido arriba)")]
    public RawImage valueBar;
    public NormalizedDragArea valueDragArea;
    public RectTransform valueHandle;

    [Header("Hex (opcional)")]
    public TMP_InputField hexInput;

    /// <summary>Se dispara con cada cambio hecho por el usuario (no al cargar vía SetColor).</summary>
    public System.Action<Color> OnColorChanged;

    private const int SquareSize = 128;
    private const int BarHeight = 128;

    private float _hue;
    private float _saturation;
    private float _value = 1f;

    private Texture2D _squareTexture;
    private Texture2D _valueTexture;

    public Color CurrentColor => Color.HSVToRGB(_hue, _saturation, _value);

    private void Awake()
    {
        _squareTexture = new Texture2D(SquareSize, SquareSize, TextureFormat.RGBA32, false);
        _valueTexture = new Texture2D(1, BarHeight, TextureFormat.RGBA32, false);

        if (hueSatSquare != null) hueSatSquare.texture = _squareTexture;
        if (valueBar != null) valueBar.texture = _valueTexture;

        if (squareDragArea != null)
            squareDragArea.OnNormalizedPositionChanged.AddListener(OnSquareChanged);

        if (valueDragArea != null)
            valueDragArea.OnNormalizedPositionChanged.AddListener(OnValueBarChanged);

        if (hexInput != null)
            hexInput.onEndEdit.AddListener(OnHexEdited);

        RebuildSquareTexture();
        RebuildValueTexture();
        UpdateHandles();
        UpdateHex();
    }

    // ================= ENTRADA DEL USUARIO =================

    private void OnSquareChanged(Vector2 normalized)
    {
        _hue = normalized.x;
        _saturation = normalized.y;
        RebuildValueTexture(); // el color puro de la barra cambia con el nuevo hue/sat
        NotifyChanged();
    }

    private void OnValueBarChanged(Vector2 normalized)
    {
        _value = normalized.y;
        NotifyChanged();
    }

    private void OnHexEdited(string hex)
    {
        string clean = hex.StartsWith("#") ? hex : "#" + hex;
        if (ColorUtility.TryParseHtmlString(clean, out Color parsed))
            ApplyColor(parsed, notify: true);
    }

    // ================= API PÚBLICA =================

    /// <summary>Fija el color sin disparar OnColorChanged -- para cargar un color ya guardado.</summary>
    public void SetColor(Color color) => ApplyColor(color, notify: false);

    // ================= INTERNO =================

    private void ApplyColor(Color color, bool notify)
    {
        Color.RGBToHSV(color, out _hue, out _saturation, out _value);
        RebuildValueTexture();
        UpdateHandles();
        UpdateHex();
        if (notify) OnColorChanged?.Invoke(CurrentColor);
    }

    private void NotifyChanged()
    {
        UpdateHandles();
        UpdateHex();
        OnColorChanged?.Invoke(CurrentColor);
    }

    private void UpdateHandles()
    {
        if (squareHandle != null && hueSatSquare != null)
        {
            Rect r = ((RectTransform)hueSatSquare.transform).rect;
            squareHandle.anchoredPosition = new Vector2(
                Mathf.Lerp(r.xMin, r.xMax, _hue),
                Mathf.Lerp(r.yMin, r.yMax, _saturation));
        }

        if (valueHandle != null && valueBar != null)
        {
            Rect r = ((RectTransform)valueBar.transform).rect;
            valueHandle.anchoredPosition = new Vector2(
                valueHandle.anchoredPosition.x,
                Mathf.Lerp(r.yMin, r.yMax, _value));
        }
    }

    private void UpdateHex()
    {
        if (hexInput == null) return;
        hexInput.SetTextWithoutNotify("#" + ColorUtility.ToHtmlStringRGB(CurrentColor));
    }

    private void RebuildSquareTexture()
    {
        for (int y = 0; y < SquareSize; y++)
        {
            float sat = y / (float)(SquareSize - 1);
            for (int x = 0; x < SquareSize; x++)
            {
                float hue = x / (float)(SquareSize - 1);
                _squareTexture.SetPixel(x, y, Color.HSVToRGB(hue, sat, 1f));
            }
        }
        _squareTexture.Apply();
    }

    private void RebuildValueTexture()
    {
        Color pure = Color.HSVToRGB(_hue, _saturation, 1f);
        for (int y = 0; y < BarHeight; y++)
        {
            float v = y / (float)(BarHeight - 1);
            _valueTexture.SetPixel(0, y, Color.Lerp(Color.black, pure, v));
        }
        _valueTexture.Apply();
    }
}
