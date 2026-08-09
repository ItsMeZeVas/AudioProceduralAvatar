# AudioProceduralAvatar

Instalación interactiva para el Día de Ingeniería Multimedia: los participantes crean un avatar y el sistema genera automáticamente un leitmotiv musical único mediante audio procedural. Ver `docs/Propuesta_Proyecto_Avatares_Sonoros.md` para la propuesta completa.

## Stack

- **Motor:** Unity 6.3 LTS
- **Audio (fase actual):** síntesis procedural en C# puro (sin dependencias externas)
- **Audio (fase futura):** integración con FMOD Studio — la arquitectura ya está separada para ese cambio (ver `Assets/Scripts/Audio/IMusicRenderer.cs`)

## Setup — primera vez

1. Instalar **Unity Hub** y, dentro de él, **Unity 6.3 LTS** (mismo build para todo el equipo — no usar otra versión).
2. Clonar este repo.
3. Abrir Unity Hub → "Add project from disk" → seleccionar la carpeta clonada. Unity generará automáticamente `Library/`, `ProjectSettings/` y `Packages/` la primera vez que se abra (por eso no están versionados).
4. Confirmar que `Assets/Scripts/` aparece en el Project window de Unity con las subcarpetas `Audio/`, `Avatar/`, `World/`, `UI/`.

## Estructura

```
Assets/
  Scripts/
    Avatar/   -> datos y lógica de personalización del avatar
    Audio/    -> generación del leitmotiv (lógica) + renderers (síntesis)
    World/    -> mundo 2.5D, galería, spawn de avatares
    UI/       -> personalización, ficha informativa
docs/
  Propuesta_Proyecto_Avatares_Sonoros.md  -> propuesta y plan de sprints
```

## Arquitectura del audio procedural

Separada en capas para que Diseño pueda ajustar valores sin tocar código, y para poder cambiar de motor de síntesis sin tocar la lógica musical:

- `AvatarAttributes` (Avatar/) — los datos que elige el participante.
- `LeitmotivGenerator` (Audio/) — decide **qué** se toca (escala, tónica, tempo, notas) a partir de los atributos. Ya NO tiene las reglas de mapeo escritas en código — las lee de un `LeitmotivMappingConfig`.
- `LeitmotivMappingConfig` (Audio/, ScriptableObject) — **esto lo edita Diseño en el Inspector de Unity**, sin abrir código: qué escala corresponde a cada `Trait`, qué tempo a cada `Accessory`, qué instrumento a cada `Clothing`. Crear uno con clic derecho en Project → `Create → AudioProceduralAvatar → Leitmotiv Mapping Config`.
- `InstrumentPreset` (Audio/, ScriptableObject) — un timbre completo (forma de onda + volumen + ADSR). Diseño crea los que necesite con `Create → AudioProceduralAvatar → Instrument Preset`. El campo `PresetId` debe coincidir con el instrumento referenciado en `LeitmotivMappingConfig`.
- `IMusicRenderer` (Audio/) — contrato de **cómo** suena. Implementación actual: `SimpleSynthRenderer`, con osciladores (seno/cuadrada/sierra/triángulo) y envolvente ADSR real, tomando el timbre de los `InstrumentPreset` asignados. Implementación futura: `FMODRenderer`, sin tocar `LeitmotivGenerator` ni los presets.
- `MusicTheory` (Audio/) — conversión pura de grado de escala → nota MIDI → frecuencia. Sin dependencias de Unity.
- `LeitmotivDemo` (Audio/) — script de prueba: genera un avatar random y lo reproduce al presionar Play o Espacio. **Bórralo o desactívalo cuando empiece la UI real** — es solo para validar que se escucha algo.

### Cómo probarlo ahora mismo

1. Crea un GameObject vacío en una escena de prueba.
2. Agrégale `LeitmotivGenerator`, `SimpleSynthRenderer`, `LeitmotivDemo`.
3. Crea al menos un `InstrumentPreset` (Project → Create → AudioProceduralAvatar → Instrument Preset) y asígnalo en la lista `Presets` del `SimpleSynthRenderer`, además de como `Fallback Preset`.
4. (Opcional) Crea un `LeitmotivMappingConfig` y asígnalo al `LeitmotivGenerator` — si no lo asignas, usa valores por defecto internos.
5. Dale Play. Deberías escuchar un motivo corto. Presiona Espacio para generar otro avatar random y comparar.

## Modelo de datos del avatar (actualizado — capas reales, no enums)

La personalización real (hecha por el equipo) arma el avatar con `AvatarCreator` + `AvatarLayer`: capas de sprites (`Body`, `Head`, `Hair`, ...) navegables por índice, más nombre y código estudiantil vía `AvatarData`. Nuestro modelo de datos se ajustó a esto:

- `AvatarProfile` (Avatar/) — reemplaza al viejo `AvatarAttributes`. Es `Id + AvatarName + StudentCode + List<LayerSelection>`, donde cada `LayerSelection` es `(LayerName, SpriteIndex)`. No asume cuáles capas existen ni cuántas — si se agrega una capa nueva, no hay que tocar esta clase.
- `RootNoteStrategy` (Audio/, ScriptableObject abstracto) — la fuente de la tónica musical **todavía no está decidida** (no hay selector de color en la UI real). Se dejó como pieza intercambiable a propósito: hoy usa `LayerHashRootNoteStrategy` (hash determinista de las capas elegidas), y el día que se decida otra fuente, se crea una nueva estrategia y se reasigna en el Inspector del `LeitmotivGenerator` — cero cambios en el resto del sistema.
- `LeitmotivMappingConfig` — las reglas de Diseño (escala/tempo/instrumento) ahora se definen por `(nombre de capa, índice de sprite)` en vez de por categoría con nombre. Ej: "capa Hair, índice 2 → escala Dórica".

## Persistencia en JSON (Persistence/)

- `AvatarJsonStorage` — guarda/carga `AvatarProfile` como JSON en `Application.persistentDataPath/avatars/{id}.json`, y opcionalmente una imagen capturada en `{id}.png`. Desacopla personalización de galería: no se hablan en memoria, solo a través de estos archivos — así da igual si terminan en la misma pantalla o en dos PCs distintas para el evento.
- `AvatarCreationController` (Avatar/) — el pegamento con los scripts de tu compañero. Se conecta al botón "Crear avatar" (hay que agregarlo a la UI, no existe todavía): lee `AvatarCreator` + `AvatarData`, arma el `AvatarProfile`, genera el leitmotiv, captura la imagen con `AvatarCapture` (si está asignado), y guarda todo con `AvatarJsonStorage`.
- `AvatarGalleryLoader` (World/) — al iniciar la escena de galería, lee todos los avatares guardados en disco y los coloca en el mundo con `AvatarGalleryManager.CreateAndSpawn`. El leitmotiv se regenera ahí mismo a partir del perfil (no se duplica en el JSON) — mientras la configuración sea la misma en ambas escenas, el resultado es siempre igual.
- `AvatarDisplay` — ya no pinta un cubo de color: usa un `SpriteRenderer` y muestra la imagen real capturada del avatar (o un `fallbackSprite` si todavía no hay captura).

### Cómo conectar el botón "Crear avatar"

1. En la escena de personalización, agrega un componente `AvatarCreationController` a cualquier GameObject (ej. `Managers`).
2. Asigna en el Inspector: `Avatar Creator`, `Avatar Data` (los que ya existen en `Managers`), y `Leitmotiv Generator` (agrégalo si no está en la escena). `Avatar Capture` es opcional — si el render de captura no está armado todavía, simplemente se guarda sin imagen.
3. Verifica que `Layer Names` tenga exactamente `Body`, `Head`, `Hair` (deben coincidir con los `layerName` configurados en `AvatarCreator`).
4. Agrega un botón "Crear avatar" a la UI (Canvas), y en su `OnClick()` arrastra el GameObject con `AvatarCreationController` y selecciona `CreateAvatar()`.

### Cómo probar el ciclo completo (personalización → JSON → galería)

1. En la escena de personalización: personaliza un avatar, dale clic a "Crear avatar". Revisa la Console — debe loguear que se guardó con su id.
2. En la escena de galería: en vez de (o junto con) `GalleryDemo`, agrega `AvatarGalleryLoader` al `GalleryManager`, y asígnale el mismo `LeitmotivGenerator`.
3. Dale Play a la escena de galería — el avatar que acabas de crear debería aparecer ahí, con su imagen real (si `AvatarCapture` estaba asignado).

## Pendientes (MVP)

Alcance mínimo definido en la propuesta, y su estado:

- [x] Personalizar un avatar
- [x] Generar automáticamente su leitmotiv
- [x] Asignarle un nombre
- [x] Almacenar el personaje dentro del mundo interactivo
- [x] Visualizar su ficha informativa al seleccionarlo — pop-up sobre el avatar, se oculta sola. Ver sección "Ficha informativa" más abajo.
- [x] Reproducir automáticamente su identidad sonora
- [x] Validación de nombre/código (palabras prohibidas + código estudiantil único)

**MVP completo.** Quedan los pendientes de pulido/expansión listados abajo.

Otros pendientes identificados en el camino:
- Mejorar la generación melódica — hoy `LeitmotivGenerator.GenerateNotes` es un patrón determinista simple, no tiene reglas melódicas reales (evitar saltos grandes, resolver hacia la tónica, etc.).
- Decidir la fuente real de la tónica musical (`RootNoteStrategy` — hoy usa hash de capas por defecto, sin decidir si eso es lo definitivo).
- Configurar `AvatarCapture` (cámara + RenderTexture) si todavía no está armado, para que la galería muestre la imagen real en vez del `fallbackSprite`.
- Confirmar con el equipo audiovisual el número de pantallas y conectar `Display.displays[]`.
- Asignar nombres del equipo a los sprints del plan (`Propuesta_Proyecto_Avatares_Sonoros.docx`).
- Rendimiento con muchos avatares acumulados durante horas de evento (pooling / desactivar renderers fuera de cámara) — solo un riesgo identificado, no atacado aún.

## Galería / mundo 2.5D — diorama multiplano

El escenario es un **diorama de planos paralelos en profundidad (eje Z)**: cada plano es como un "piso" donde viven avatares. El jugador se mueve en **X dentro del plano actual** (side-scroller) y salta entre planos con una tecla dedicada — no es cámara libre. Los avatares son sprites 2D planos ("cartón"), sin rotación hacia la cámara — quedan siempre de frente porque la cámara solo cambia de plano en Z, nunca rodea al avatar.

- `GalleryPlane` (World/) — datos de un plano: su posición Z, capacidad, y qué avatares contiene.
- `AvatarInstance` (World/) — el "expediente" de un avatar creado: su `AvatarProfile` + su leitmotiv ya generado + la imagen capturada (si existe).
- `AvatarGalleryManager` (World/, singleton) — punto único de entrada: `CreateAndSpawn(profile, leitmotiv, capturedImage)` registra el avatar y lo coloca en el primer plano con espacio. Soporta **planos autorados por Diseño** (una lista de `Transform` colocados a mano en el editor) y **creación automática de planos nuevos** si todos los autorados ya están llenos.
- `AvatarDisplay` (World/) — vive en cada avatar instanciado. Muestra la imagen real capturada del avatar (sprite plano, sin rotación) o un `fallbackSprite` si aún no hay captura. Expone un evento `Selected` (clic sobre el avatar) como gancho para el sistema de selección/ficha.
- `GalleryPlayerController` (World/) — movimiento en X acotado a los avatares del plano actual, y salto discreto entre planos con flechas arriba/abajo (no continuo, no instantáneo del todo — una transición corta en Z). La cámara sigue al jugador en ambos ejes.
- `GalleryDemo` (World/) — SOLO pruebas: crea 14 avatares random al iniciar (con la capacidad por defecto de 6/plano, esto genera 3 planos automáticamente) para poder probar el recorrido y el salto entre planos sin esperar a la UI real.

### Cómo probar la galería

1. En una escena, crea un `Plane` (o `Cube` aplastado) como piso.
2. (Opcional pero recomendado) Crea 2-3 GameObjects vacíos como marcadores de plano — por ejemplo en Z=0, Z=5, Z=10 — y asígnalos en orden a la lista `Authored Plane Markers` del `AvatarGalleryManager`. Si no asignas ninguno, arranca con un plano en Z=0 y crea los demás automáticamente conforme se llenan.
3. Crea un GameObject `Player` con `GalleryPlayerController`, asígnale la cámara de la escena en `Camera Transform`, y el `AvatarGalleryManager` en el campo `Gallery Manager`.
4. Crea un GameObject `GalleryManager` con `AvatarGalleryManager`. Asígnale:
   - Un prefab con `AvatarDisplay` — necesita un `SpriteRenderer` y un `BoxCollider` (agrega un GameObject vacío, ponle `Sprite Renderer`, `Box Collider`, y `Avatar Display`; asígnale un `fallbackSprite` cualquiera para que se vea algo mientras no hay captura real).
   - Un `Transform` vacío como `Spawn Origin` (dónde arranca la fila de avatares en cada plano).
   - (Opcional) el `SimpleSynthRenderer` de la escena, para poder escuchar el leitmotiv al hacer clic en un avatar.
5. En el mismo GameObject (o cualquier otro), agrega `LeitmotivGenerator` + `GalleryDemo`, y asigna el generador en `GalleryDemo`.
6. Dale Play: deberías ver avatares en fila dentro del primer plano, moverte con A/D o flechas izq/der (acotado a ese plano), y saltar a los siguientes planos con flecha arriba (y volver con flecha abajo). Al hacer clic sobre un avatar se reproduce su leitmotiv (si asignaste el `SimpleSynthRenderer`).

## Ficha informativa

`AvatarInfoCardController` (UI/) — pop-up en el mundo (Canvas World Space) que aparece encima del avatar seleccionado y se oculta sola después de unos segundos. Se mueve directamente en coordenadas del mundo — sin conversiones de pantalla, así que no depende de cómo esté configurada la cámara.

### Cómo armarla (desde cero)

1. Hierarchy → `UI → Canvas`. Selecciónalo, y en el componente `Canvas`, cambia `Render Mode` a **World Space**.
2. En su `Rect Transform`: `Width = 400`, `Height = 200`. En su `Transform`: `Scale X/Y/Z = 0.01` (un Canvas World Space se mide en píxeles, hay que achicarlo para que se vea del tamaño correcto en el mundo 3D).
3. Dentro del Canvas: `UI → Image` (el fondo de la ficha — ponle un color sólido para que se lea bien encima del mundo). Dentro de esa Image: otra `UI → Image` (para la foto del avatar) y un `UI → Text - TextMeshPro` (para el nombre).
4. Selecciona el **Canvas** (no un hijo) y agrégale `Add Component → Avatar Info Card Controller`.
5. Asigna: `Panel` = el Image de fondo del paso 3, `Avatar Image`, `Name Text`.
6. Desactiva el GameObject del Canvas en el editor (el script lo activa solo).
7. Dale Play, selecciona un avatar — la ficha debería aparecer justo encima, girada hacia la cámara, y ocultarse sola a los 3 segundos.

Si el texto se ve muy grande/pequeño o mal escalado, ajusta el tamaño de fuente del `TMP_Text` directamente (es normal tener que afinarlo una vez, por el `Scale 0.01` del Canvas).

## Flujo de ramas

- `main` protegida.
- Una rama por tarea/feature: `feature/<nombre-corto>`, ejemplo `feature/leitmotiv-mapping`.
- Pull request antes de mergear a `main`.

## Git LFS

Este repo usa Git LFS para binarios pesados (audio, texturas, modelos). Tras clonar, correr una vez:

```bash
git lfs install
```
