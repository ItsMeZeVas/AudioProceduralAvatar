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

## Flujo de ramas

- `main` protegida
- No enviar commits corruptos.
- Pull request antes de mergear a `main`.
- 

## Git LFS
Este repo usa Git LFS para binarios pesados (audio, texturas, modelos). Tras clonar, correr una vez:

```bash
git lfs install
```
en caso de necesitarlo.
