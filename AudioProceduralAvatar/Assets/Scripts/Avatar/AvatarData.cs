using TMPro;
using UnityEngine;

public class AvatarData : MonoBehaviour
{
    [Header("Nombre del Avatar")]
    public TMP_InputField nameInput;

    [Header("Código Estudiantil")]
    public TMP_InputField studentCodeInput;

    [HideInInspector]
    public string avatarName = "Avatar";

    [HideInInspector]
    public string studentCode = "";

    public void UpdateName()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text))
        {
            avatarName = "Avatar";
        }
        else
        {
            avatarName = nameInput.text.Trim();
        }

        Debug.Log("Nombre del avatar: " + avatarName);
    }

    public void UpdateStudentCode()
    {
        if (string.IsNullOrWhiteSpace(studentCodeInput.text))
        {
            studentCode = "";
        }
        else
        {
            studentCode = studentCodeInput.text.Trim();
        }

        Debug.Log("Código estudiantil: " + studentCode);
    }

    public string GetAvatarName()
    {
        return avatarName;
    }

    public string GetStudentCode()
    {
        return studentCode;
    }
}