using UnityEngine;

public class ClickToCanvas : MonoBehaviour
{
    [Header("Canvas al que quieres ir al hacer click")]
    public GameObject canvasDestino;

    [Header("Canvas que quieres OCULTAR al hacer click (elige tú cuáles)")]
    public GameObject[] canvasesAOcultar;

    // Llama esta funcion desde el OnClick() del Button en el Inspector
    public void IrAlCanvas()
    {
        if (canvasDestino == null)
        {
            Debug.LogWarning("canvasDestino está vacío, arrastra el canvas en el Inspector");
            return;
        }

        // Oculta solo los canvas que elegiste en la lista
        foreach (GameObject canvas in canvasesAOcultar)
        {
            if (canvas != null)
            {
                canvas.SetActive(false);
            }
        }

        // Muestra el canvas destino
        canvasDestino.SetActive(true);
    }
}