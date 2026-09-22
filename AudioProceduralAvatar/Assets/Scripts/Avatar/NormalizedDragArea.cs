using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

// Área rectangular que reporta la posición del puntero normalizada (0-1 en
// X e Y) al hacer click o arrastrar dentro de ella. La usan tanto el
// cuadro de Matiz x Saturación como la barra de Valor del color picker
// (cada una ignora el eje que no le interesa).
[RequireComponent(typeof(RectTransform))]
public class NormalizedDragArea : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public UnityEvent<Vector2> OnNormalizedPositionChanged;

    private RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData) => Report(eventData);
    public void OnDrag(PointerEventData eventData) => Report(eventData);

    private void Report(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rect, eventData.position, eventData.pressEventCamera, out Vector2 local);

        Rect rect = _rect.rect;
        float x = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
        float y = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);

        OnNormalizedPositionChanged?.Invoke(new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y)));
    }
}
