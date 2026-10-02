using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace AudioProceduralAvatar.Audio
{
    /// <summary>
    /// Controla la música del personalizador utilizando cuatro pistas
    /// independientes:
    ///
    /// Bajo
    /// Voces
    /// Otros
    /// Drums
    ///
    /// Todas las pistas se reproducen sincronizadas y en loop.
    /// Cada una puede tener su propio volumen.
    ///
    /// El volumen general de la música se controla además mediante
    /// el grupo MUSICA del Audio Mixer.
    /// </summary>
    public class ControladorMusicaPersonalizador : MonoBehaviour
    {
        public static ControladorMusicaPersonalizador Instancia
        {
            get;
            private set;
        }

        [System.Serializable]
        public class PistaMusical
        {
            [Header("IDENTIFICACIÓN")]

            public string nombre;

            [Header("AUDIO")]

            [Tooltip("AudioSource que reproduce esta pista.")]
            public AudioSource fuente;

            [Tooltip("AudioClip correspondiente a esta pista.")]
            public AudioClip clip;

            [Header("VOLUMEN")]

            [Range(0f, 1f)]
            [Tooltip("Volumen individual de esta pista.")]
            public float volumen = 0.5f;

            [Tooltip("Indica si esta pista comienza activa.")]
            public bool activaAlIniciar = true;

            [HideInInspector]
            public float volumenActual;
        }

        // ============================================================
        // MEZCLADOR
        // ============================================================

        [Header("MEZCLADOR DE AUDIO")]

        [Tooltip(
            "Audio Mixer utilizado para la música del personalizador."
        )]
        [SerializeField]
        private AudioMixer mezcladorAudio;

        [Tooltip(
            "Nombre del grupo del Mixer donde están las pistas musicales."
        )]
        [SerializeField]
        private string nombreGrupoMusica = "MUSICA";

        [Tooltip(
            "Nombre del parámetro expuesto para controlar el volumen general."
        )]
        [SerializeField]
        private string parametroVolumenMusica = "VolumenMusica";

        // ============================================================
        // PISTAS
        // ============================================================

        [Header("PISTAS MUSICALES")]

        [SerializeField]
        private PistaMusical bajo = new PistaMusical
        {
            nombre = "Bajo",
            volumen = 0.45f,
            activaAlIniciar = true
        };

        [SerializeField]
        private PistaMusical voces = new PistaMusical
        {
            nombre = "Voces",
            volumen = 0.30f,
            activaAlIniciar = true
        };

        [SerializeField]
        private PistaMusical otros = new PistaMusical
        {
            nombre = "Otros",
            volumen = 0.25f,
            activaAlIniciar = true
        };

        [SerializeField]
        private PistaMusical drums = new PistaMusical
        {
            nombre = "Drums",
            volumen = 0.20f,
            activaAlIniciar = true
        };

        // ============================================================
        // VOLUMEN GENERAL
        // ============================================================

        [Header("VOLUMEN GENERAL")]

        [Range(0f, 1f)]
        [Tooltip(
            "Volumen general de la música antes de pasar por el Mixer."
        )]
        [SerializeField]
        private float volumenGeneral = 0.75f;

        // ============================================================
        // INICIO ALEATORIO
        // ============================================================

        [Header("INICIO DE LA MÚSICA")]

        [Tooltip(
            "Hace que la música pueda comenzar desde diferentes puntos."
        )]
        [SerializeField]
        private bool comenzarDesdePosicionAleatoria = false;

        [Tooltip(
            "Tiempo mínimo desde el inicio para elegir una posición aleatoria."
        )]
        [Min(0f)]
        [SerializeField]
        private float tiempoMinimoInicio = 0f;

        [Tooltip(
            "Evita comenzar demasiado cerca del final del clip."
        )]
        [Min(0f)]
        [SerializeField]
        private float margenFinal = 5f;

        // ============================================================
        // TRANSICIONES
        // ============================================================

        [Header("TRANSICIONES")]

        [Min(0f)]
        [Tooltip(
            "Tiempo que tarda la música en entrar."
        )]
        [SerializeField]
        private float tiempoFadeEntrada = 2f;

        [Min(0f)]
        [Tooltip(
            "Tiempo que tarda la música en salir."
        )]
        [SerializeField]
        private float tiempoFadeSalida = 1.5f;

        [Min(0f)]
        [Tooltip(
            "Tiempo utilizado al cambiar de escena."
        )]
        [SerializeField]
        private float tiempoTransicionEscena = 1.5f;

        // ============================================================
        // ESCENAS
        // ============================================================

        [Header("ESCENAS")]

        [Tooltip(
            "Mantiene este objeto vivo al cambiar de escena."
        )]
        [SerializeField]
        private bool conservarEntreEscenas = true;

        // ============================================================
        // ESTADO INTERNO
        // ============================================================

        private Coroutine rutinaTransicion;

        private bool musicaIniciada;

        private bool estaCambiandoEscena;

        private AudioMixerGroup grupoMusica;

        // ============================================================
        // UNITY
        // ============================================================

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            Instancia = this;

            if (conservarEntreEscenas)
            {
                DontDestroyOnLoad(gameObject);
            }

            BuscarGrupoMusica();

            PrepararPista(bajo);
            PrepararPista(voces);
            PrepararPista(otros);
            PrepararPista(drums);
        }

        private void Start()
        {
            IniciarMusica();
        }

        // ============================================================
        // CONFIGURACIÓN DEL MIXER
        // ============================================================

        private void BuscarGrupoMusica()
        {
            if (mezcladorAudio == null)
            {
                Debug.LogWarning(
                    "ControladorMusicaPersonalizador: " +
                    "No se ha asignado el Audio Mixer."
                );

                return;
            }

            AudioMixerGroup[] grupos =
                mezcladorAudio.FindMatchingGroups(
                    nombreGrupoMusica
                );

            if (grupos != null && grupos.Length > 0)
            {
                grupoMusica = grupos[0];
            }
            else
            {
                Debug.LogWarning(
                    "ControladorMusicaPersonalizador: " +
                    "No se encontró el grupo '" +
                    nombreGrupoMusica +
                    "' en el Audio Mixer."
                );
            }
        }

        // ============================================================
        // PREPARAR PISTAS
        // ============================================================

        private void PrepararPista(
            PistaMusical pista
        )
        {
            if (pista == null)
                return;

            if (pista.fuente == null)
            {
                Debug.LogWarning(
                    "ControladorMusicaPersonalizador: " +
                    "La pista '" +
                    pista.nombre +
                    "' no tiene AudioSource."
                );

                return;
            }

            if (pista.clip == null)
            {
                Debug.LogWarning(
                    "ControladorMusicaPersonalizador: " +
                    "La pista '" +
                    pista.nombre +
                    "' no tiene AudioClip."
                );

                return;
            }

            pista.fuente.playOnAwake = false;

            pista.fuente.loop = true;

            pista.fuente.spatialBlend = 0f;

            pista.fuente.clip = pista.clip;

            if (grupoMusica != null)
            {
                pista.fuente.outputAudioMixerGroup =
                    grupoMusica;
            }

            pista.volumenActual =
                pista.activaAlIniciar
                    ? pista.volumen
                    : 0f;

            pista.fuente.volume =
                pista.volumenActual;
        }

        // ============================================================
        // INICIAR MÚSICA
        // ============================================================

        public void IniciarMusica()
        {
            if (musicaIniciada)
                return;

            musicaIniciada = true;

            PrepararPosicionInicial();

            IniciarTodasLasPistas();

            if (rutinaTransicion != null)
            {
                StopCoroutine(rutinaTransicion);
            }

            rutinaTransicion =
                StartCoroutine(
                    FadeEntrada()
                );
        }

        // ============================================================
        // POSICIÓN INICIAL
        // ============================================================

        private void PrepararPosicionInicial()
        {
            if (!comenzarDesdePosicionAleatoria)
            {
                ColocarPistaEnTiempo(
                    bajo,
                    0f
                );

                ColocarPistaEnTiempo(
                    voces,
                    0f
                );

                ColocarPistaEnTiempo(
                    otros,
                    0f
                );

                ColocarPistaEnTiempo(
                    drums,
                    0f
                );

                return;
            }

            float duracion =
                ObtenerDuracionComun();

            if (duracion <= 0f)
                return;

            float minimo =
                Mathf.Clamp(
                    tiempoMinimoInicio,
                    0f,
                    duracion
                );

            float maximo =
                Mathf.Max(
                    minimo,
                    duracion - margenFinal
                );

            float posicion =
                Random.Range(
                    minimo,
                    maximo
                );

            ColocarPistaEnTiempo(
                bajo,
                posicion
            );

            ColocarPistaEnTiempo(
                voces,
                posicion
            );

            ColocarPistaEnTiempo(
                otros,
                posicion
            );

            ColocarPistaEnTiempo(
                drums,
                posicion
            );
        }

        private void ColocarPistaEnTiempo(
            PistaMusical pista,
            float tiempo
        )
        {
            if (pista == null)
                return;

            if (pista.fuente == null)
                return;

            if (pista.clip == null)
                return;

            float tiempoSeguro =
                Mathf.Clamp(
                    tiempo,
                    0f,
                    Mathf.Max(
                        0f,
                        pista.clip.length - 0.01f
                    )
                );

            pista.fuente.time =
                tiempoSeguro;
        }

        private float ObtenerDuracionComun()
        {
            float duracion = float.MaxValue;

            bool existeUnaPista = false;

            PistaMusical[] pistas =
            {
                bajo,
                voces,
                otros,
                drums
            };

            foreach (PistaMusical pista in pistas)
            {
                if (pista == null)
                    continue;

                if (pista.clip == null)
                    continue;

                existeUnaPista = true;

                duracion =
                    Mathf.Min(
                        duracion,
                        pista.clip.length
                    );
            }

            if (!existeUnaPista)
                return 0f;

            return duracion;
        }

        // ============================================================
        // REPRODUCIR TODAS
        // ============================================================

        private void IniciarTodasLasPistas()
        {
            IniciarPista(bajo);
            IniciarPista(voces);
            IniciarPista(otros);
            IniciarPista(drums);
        }

        private void IniciarPista(
            PistaMusical pista
        )
        {
            if (pista == null)
                return;

            if (pista.fuente == null)
                return;

            if (pista.clip == null)
                return;

            if (!pista.fuente.isPlaying)
            {
                pista.fuente.Play();
            }
        }

        // ============================================================
        // FADE DE ENTRADA
        // ============================================================

        private IEnumerator FadeEntrada()
        {
            float tiempo = 0f;

            float volumenInicialBajo =
                bajo != null
                    ? bajo.fuente.volume
                    : 0f;

            float volumenInicialVoces =
                voces != null
                    ? voces.fuente.volume
                    : 0f;

            float volumenInicialOtros =
                otros != null
                    ? otros.fuente.volume
                    : 0f;

            float volumenInicialDrums =
                drums != null
                    ? drums.fuente.volume
                    : 0f;

            while (
                tiempo <
                tiempoFadeEntrada
            )
            {
                tiempo +=
                    Time.unscaledDeltaTime;

                float porcentaje =
                    Mathf.Clamp01(
                        tiempo /
                        Mathf.Max(
                            0.01f,
                            tiempoFadeEntrada
                        )
                    );

                float suavizado =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        porcentaje
                    );

                AplicarVolumenFade(
                    bajo,
                    volumenInicialBajo,
                    suavizado
                );

                AplicarVolumenFade(
                    voces,
                    volumenInicialVoces,
                    suavizado
                );

                AplicarVolumenFade(
                    otros,
                    volumenInicialOtros,
                    suavizado
                );

                AplicarVolumenFade(
                    drums,
                    volumenInicialDrums,
                    suavizado
                );

                yield return null;
            }

            AplicarVolumenFinal(bajo);
            AplicarVolumenFinal(voces);
            AplicarVolumenFinal(otros);
            AplicarVolumenFinal(drums);
        }

        private void AplicarVolumenFade(
            PistaMusical pista,
            float volumenInicial,
            float porcentaje
        )
        {
            if (pista == null)
                return;

            if (pista.fuente == null)
                return;

            float volumenObjetivo =
                pista.activaAlIniciar
                    ? pista.volumen
                    : 0f;

            pista.fuente.volume =
                Mathf.Lerp(
                    volumenInicial,
                    volumenObjetivo *
                    volumenGeneral,
                    porcentaje
                );
        }

        private void AplicarVolumenFinal(
            PistaMusical pista
        )
        {
            if (pista == null)
                return;

            if (pista.fuente == null)
                return;

            pista.fuente.volume =
                pista.activaAlIniciar
                    ? pista.volumen *
                      volumenGeneral
                    : 0f;

            pista.volumenActual =
                pista.fuente.volume;
        }

        // ============================================================
        // DETENER MÚSICA
        // ============================================================

        public void DetenerMusica()
        {
            if (!musicaIniciada)
                return;

            if (rutinaTransicion != null)
            {
                StopCoroutine(rutinaTransicion);
            }

            rutinaTransicion =
                StartCoroutine(
                    FadeSalida()
                );
        }

        private IEnumerator FadeSalida()
        {
            float tiempo = 0f;

            float inicioBajo =
                ObtenerVolumen(bajo);

            float inicioVoces =
                ObtenerVolumen(voces);

            float inicioOtros =
                ObtenerVolumen(otros);

            float inicioDrums =
                ObtenerVolumen(drums);

            while (
                tiempo <
                tiempoFadeSalida
            )
            {
                tiempo +=
                    Time.unscaledDeltaTime;

                float porcentaje =
                    Mathf.Clamp01(
                        tiempo /
                        Mathf.Max(
                            0.01f,
                            tiempoFadeSalida
                        )
                    );

                float suavizado =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        porcentaje
                    );

                AplicarVolumenDirecto(
                    bajo,
                    Mathf.Lerp(
                        inicioBajo,
                        0f,
                        suavizado
                    )
                );

                AplicarVolumenDirecto(
                    voces,
                    Mathf.Lerp(
                        inicioVoces,
                        0f,
                        suavizado
                    )
                );

                AplicarVolumenDirecto(
                    otros,
                    Mathf.Lerp(
                        inicioOtros,
                        0f,
                        suavizado
                    )
                );

                AplicarVolumenDirecto(
                    drums,
                    Mathf.Lerp(
                        inicioDrums,
                        0f,
                        suavizado
                    )
                );

                yield return null;
            }

            DetenerPista(bajo);
            DetenerPista(voces);
            DetenerPista(otros);
            DetenerPista(drums);

            musicaIniciada = false;
        }

        private float ObtenerVolumen(
            PistaMusical pista
        )
        {
            if (pista == null)
                return 0f;

            if (pista.fuente == null)
                return 0f;

            return pista.fuente.volume;
        }

        private void AplicarVolumenDirecto(
            PistaMusical pista,
            float volumen
        )
        {
            if (pista == null)
                return;

            if (pista.fuente == null)
                return;

            pista.fuente.volume =
                volumen;

            pista.volumenActual =
                volumen;
        }

        private void DetenerPista(
            PistaMusical pista
        )
        {
            if (pista == null)
                return;

            if (pista.fuente == null)
                return;

            pista.fuente.Stop();

            pista.fuente.volume = 0f;
        }

        // ============================================================
        // ACTIVAR / DESACTIVAR CAPAS
        // ============================================================

        public void ActivarBajo()
        {
            CambiarEstadoPista(
                bajo,
                true
            );
        }

        public void DesactivarBajo()
        {
            CambiarEstadoPista(
                bajo,
                false
            );
        }

        public void ActivarVoces()
        {
            CambiarEstadoPista(
                voces,
                true
            );
        }

        public void DesactivarVoces()
        {
            CambiarEstadoPista(
                voces,
                false
            );
        }

        public void ActivarOtros()
        {
            CambiarEstadoPista(
                otros,
                true
            );
        }

        public void DesactivarOtros()
        {
            CambiarEstadoPista(
                otros,
                false
            );
        }

        public void ActivarDrums()
        {
            CambiarEstadoPista(
                drums,
                true
            );
        }

        public void DesactivarDrums()
        {
            CambiarEstadoPista(
                drums,
                false
            );
        }

        private void CambiarEstadoPista(
            PistaMusical pista,
            bool activar
        )
        {
            if (pista == null)
                return;

            if (pista.fuente == null)
                return;

            pista.activaAlIniciar =
                activar;

            StartCoroutine(
                TransicionPista(
                    pista,
                    activar
                )
            );
        }

        private IEnumerator TransicionPista(
            PistaMusical pista,
            bool activar
        )
        {
            if (!pista.fuente.isPlaying)
            {
                pista.fuente.Play();
            }

            float inicio =
                pista.fuente.volume;

            float destino =
                activar
                    ? pista.volumen *
                      volumenGeneral
                    : 0f;

            float tiempo = 0f;

            while (
                tiempo <
                tiempoFadeEntrada
            )
            {
                tiempo +=
                    Time.unscaledDeltaTime;

                float porcentaje =
                    Mathf.Clamp01(
                        tiempo /
                        Mathf.Max(
                            0.01f,
                            tiempoFadeEntrada
                        )
                    );

                float suavizado =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        porcentaje
                    );

                pista.fuente.volume =
                    Mathf.Lerp(
                        inicio,
                        destino,
                        suavizado
                    );

                yield return null;
            }

            pista.fuente.volume =
                destino;

            pista.volumenActual =
                destino;
        }

        // ============================================================
        // CAMBIAR VOLUMEN INDIVIDUAL
        // ============================================================

        public void EstablecerVolumenBajo(
            float valor
        )
        {
            EstablecerVolumen(
                bajo,
                valor
            );
        }

        public void EstablecerVolumenVoces(
            float valor
        )
        {
            EstablecerVolumen(
                voces,
                valor
            );
        }

        public void EstablecerVolumenOtros(
            float valor
        )
        {
            EstablecerVolumen(
                otros,
                valor
            );
        }

        public void EstablecerVolumenDrums(
            float valor
        )
        {
            EstablecerVolumen(
                drums,
                valor
            );
        }

        private void EstablecerVolumen(
            PistaMusical pista,
            float valor
        )
        {
            if (pista == null)
                return;

            valor =
                Mathf.Clamp01(valor);

            pista.volumen =
                valor;

            if (pista.activaAlIniciar &&
                pista.fuente != null)
            {
                pista.fuente.volume =
                    valor *
                    volumenGeneral;
            }
        }

        // ============================================================
        // VOLUMEN GENERAL
        // ============================================================

        public void EstablecerVolumenGeneral(
            float valor
        )
        {
            volumenGeneral =
                Mathf.Clamp01(valor);

            ActualizarVolumenTodasLasPistas();
        }

        private void ActualizarVolumenTodasLasPistas()
        {
            ActualizarVolumenPista(bajo);
            ActualizarVolumenPista(voces);
            ActualizarVolumenPista(otros);
            ActualizarVolumenPista(drums);
        }

        private void ActualizarVolumenPista(
            PistaMusical pista
        )
        {
            if (pista == null)
                return;

            if (pista.fuente == null)
                return;

            if (!pista.activaAlIniciar)
            {
                pista.fuente.volume = 0f;
                return;
            }

            pista.fuente.volume =
                pista.volumen *
                volumenGeneral;
        }

        // ============================================================
        // REINICIAR
        // ============================================================

        public void ReiniciarMusica()
        {
            if (rutinaTransicion != null)
            {
                StopCoroutine(
                    rutinaTransicion
                );
            }

            DetenerPista(bajo);
            DetenerPista(voces);
            DetenerPista(otros);
            DetenerPista(drums);

            musicaIniciada = false;

            PrepararPosicionInicial();

            IniciarMusica();
        }

        // ============================================================
        // TRANSICIÓN DE ESCENA
        // ============================================================

        public void TransicionAntesDeCambiarEscena()
        {
            if (estaCambiandoEscena)
                return;

            StartCoroutine(
                TransicionEscena()
            );
        }

        private IEnumerator TransicionEscena()
        {
            estaCambiandoEscena = true;

            float tiempo = 0f;

            float inicioBajo =
                ObtenerVolumen(bajo);

            float inicioVoces =
                ObtenerVolumen(voces);

            float inicioOtros =
                ObtenerVolumen(otros);

            float inicioDrums =
                ObtenerVolumen(drums);

            while (
                tiempo <
                tiempoTransicionEscena
            )
            {
                tiempo +=
                    Time.unscaledDeltaTime;

                float porcentaje =
                    Mathf.Clamp01(
                        tiempo /
                        Mathf.Max(
                            0.01f,
                            tiempoTransicionEscena
                        )
                    );

                float suavizado =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        porcentaje
                    );

                AplicarVolumenDirecto(
                    bajo,
                    Mathf.Lerp(
                        inicioBajo,
                        0f,
                        suavizado
                    )
                );

                AplicarVolumenDirecto(
                    voces,
                    Mathf.Lerp(
                        inicioVoces,
                        0f,
                        suavizado
                    )
                );

                AplicarVolumenDirecto(
                    otros,
                    Mathf.Lerp(
                        inicioOtros,
                        0f,
                        suavizado
                    )
                );

                AplicarVolumenDirecto(
                    drums,
                    Mathf.Lerp(
                        inicioDrums,
                        0f,
                        suavizado
                    )
                );

                yield return null;
            }

            DetenerPista(bajo);
            DetenerPista(voces);
            DetenerPista(otros);
            DetenerPista(drums);

            musicaIniciada = false;

            estaCambiandoEscena = false;
        }

        public void CambiarEscena(
            string nombreEscena
        )
        {
            if (string.IsNullOrEmpty(nombreEscena))
            {
                Debug.LogWarning(
                    "ControladorMusicaPersonalizador: " +
                    "No se indicó el nombre de la escena."
                );

                return;
            }

            StartCoroutine(
                CambiarEscenaConTransicion(
                    nombreEscena
                )
            );
        }

        private IEnumerator CambiarEscenaConTransicion(
            string nombreEscena
        )
        {
            yield return TransicionEscena();

            SceneManager.LoadScene(
                nombreEscena
            );
        }

        // ============================================================
        // LIMPIEZA
        // ============================================================

        private void OnDestroy()
        {
            if (Instancia == this)
            {
                Instancia = null;
            }
        }
    }
}