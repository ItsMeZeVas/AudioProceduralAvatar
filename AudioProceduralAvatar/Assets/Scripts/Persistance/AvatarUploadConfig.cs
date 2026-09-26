using UnityEngine;

// Config de Supabase Storage para subir los .wav y generar el link público
// que va dentro del QR. Bucket DEBE ser público (Storage -> New Bucket ->
// Public) -- si es privado, la URL no sirve sin token.
[CreateAssetMenu(fileName = "AvatarUploadConfig", menuName = "Avatar/Upload Config")]
public class AvatarUploadConfig : ScriptableObject
{
    [Tooltip("Project URL de Supabase, ej: https://xxxxx.supabase.co (SIN slash al final)")]
    public string supabaseUrl;

    [Tooltip("La 'anon public' key de Project Settings -> API. NUNCA la service_role (esa es secreta).")]
    public string supabaseAnonKey;

    [Tooltip("Nombre del bucket, debe existir y estar marcado como Public")]
    public string bucketName = "leitmotivs";
}
