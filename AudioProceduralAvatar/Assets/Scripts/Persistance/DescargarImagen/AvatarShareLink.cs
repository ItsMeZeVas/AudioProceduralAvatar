using System;
using System.Text;

namespace AudioProceduralAvatar.Persistence
{
    // Arma la URL que va dentro del QR: apunta a la pagina de descargas
    // (GitHub Pages) con el guid del avatar y, opcionalmente, su nombre.
    // Archivo NUEVO: no modifica nada existente.
    public static class AvatarShareLink
    {
        // Tope del nombre en la URL. Ademas, si el enlace completo no cabe en el QR
        // (AvatarQrEncoder.MaxBytes), el nombre se va acortando hasta que quepa.
        private const int MaxNameLength = 24;

        // pageBaseUrl: ej. "https://usuario.github.io/avatar-descargas"
        // includeImage=false agrega "&i=0": la pagina entonces no espera ni muestra imagen.
        // Devuelve null si falta la URL base o el id (el que llama decide el fallback).
        public static string Build(string pageBaseUrl, string avatarId, string avatarName = null, bool includeImage = true)
        {
            if (string.IsNullOrWhiteSpace(pageBaseUrl) || string.IsNullOrEmpty(avatarId))
                return null;

            string baseUrl = pageBaseUrl.Trim().TrimEnd('/') + "/?id=" + Uri.EscapeDataString(avatarId);
            if (!includeImage) baseUrl += "&i=0";

            string name = string.IsNullOrWhiteSpace(avatarName) ? "" : avatarName.Trim();
            if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength);

            while (name.Length > 0)
            {
                try
                {
                    string candidate = baseUrl + "&n=" + Uri.EscapeDataString(name);
                    if (Encoding.UTF8.GetByteCount(candidate) <= AvatarQrEncoder.MaxBytes)
                        return candidate;
                }
                catch (UriFormatException)
                {
                    // Corte justo en medio de un emoji (par sustituto): se acorta un caracter mas.
                }
                name = name.Substring(0, name.Length - 1);
            }

            return baseUrl;
        }
    }
}
