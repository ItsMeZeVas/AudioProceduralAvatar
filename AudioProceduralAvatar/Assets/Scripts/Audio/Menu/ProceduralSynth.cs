using UnityEngine;

public class ProceduralSynth
{
    private const int SampleRate = 44100;

    public AudioClip Generate(
        AudioPreset preset,
        float frequency,
        float duration)
    {
        int sampleCount =
            Mathf.CeilToInt(duration * SampleRate);

        float[] samples =
            new float[sampleCount];

        // Estado del filtro simple (6 dB/oct)
        float filterState = 0f;

        // Estado del filtro resonante (12 dB/oct)
        float svfIc1 = 0f;
        float svfIc2 = 0f;

        // Fases acumuladas (0..1) de cada oscilador.
        float phaseA = 0f;
        float phaseB = 0f;
        float phaseC = 0f;
        float phaseD = 0f;

        float multiplierA =
            SemitoneMultiplier(
                preset.oscillatorASemitones
            );

        float multiplierB =
            SemitoneMultiplier(
                preset.oscillatorBSemitones
            );

        float multiplierC =
            SemitoneMultiplier(
                preset.oscillatorCSemitones
            );

        float multiplierD =
            SemitoneMultiplier(
                preset.oscillatorDSemitones
            );

        // Fase inicial de los LFOs. Aleatoria = cada
        // sonido generado es ligeramente distinto.
        float lfoStart1 =
            preset.randomizeLfoPhase
                ? Random.value
                : 0f;

        float lfoStart2 =
            preset.randomizeLfoPhase
                ? Random.value
                : 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time =
                i / (float)SampleRate;

            // =========================================
            // FUENTES DE MODULACIÓN
            // =========================================

            float main =
                GetAmplitudeEnvelope(
                    preset,
                    time,
                    duration
                );

            float env2 =
                GetEnvelope(
                    preset.env2,
                    time,
                    duration
                );

            float env3 =
                GetEnvelope(
                    preset.env3,
                    time,
                    duration
                );

            float lfo1 =
                GenerateWave(
                    preset.lfo1.wave,
                    lfoStart1 +
                    preset.lfo1.rateHz * time
                );

            float lfo2 =
                GenerateWave(
                    preset.lfo2.wave,
                    lfoStart2 +
                    preset.lfo2.rateHz * time
                );

            // =========================================
            // PITCH ENVELOPE
            // =========================================

            float pitchOffset = 0f;

            if (preset.pitchEnvelope)
            {
                pitchOffset =
                    GetPitchEnvelope(
                        preset,
                        time
                    );
            }

            float currentFrequency =
                frequency *
                Mathf.Pow(
                    2f,
                    pitchOffset / 12f
                );

            // =========================================
            // OSCILADORES
            // Orden: D → C → B → A
            // (los moduladores se calculan primero)
            // =========================================

            // D: solo modulador. Su nivel sigue a MAIN.
            float oscD = 0f;

            if (preset.useOscillatorD)
            {
                oscD =
                    GenerateWave(
                        preset.oscillatorD,
                        phaseD
                    ) *
                    ModGain(
                        preset.ampModMainToD,
                        main
                    );
            }

            // C: su nivel sigue a LFO 1 (bipolar → 0..1)
            float oscC =
                GenerateWave(
                    preset.oscillatorC,
                    phaseC
                ) *
                ModGain(
                    preset.ampModLfo1ToC,
                    0.5f + 0.5f * lfo1
                );

            // B: modulado en fase por C
            float oscB =
                GenerateWave(
                    preset.oscillatorB,
                    phaseB +
                    preset.phaseModCToB * oscC
                );

            // A: modulado en fase por B y por D.
            // Su nivel sigue a ENV 3.
            float phaseForA = phaseA;

            if (preset.usePhaseModulation)
            {
                phaseForA +=
                    preset.phaseModAmount *
                    oscB;
            }

            phaseForA +=
                preset.phaseModDToA *
                oscD;

            float oscA =
                GenerateWave(
                    preset.oscillatorA,
                    phaseForA
                ) *
                ModGain(
                    preset.ampModEnv3ToA,
                    env3
                );

            // Avanzar fases
            phaseA +=
                currentFrequency *
                multiplierA /
                SampleRate;

            phaseB +=
                currentFrequency *
                multiplierB /
                SampleRate;

            phaseC +=
                currentFrequency *
                multiplierC /
                SampleRate;

            phaseD +=
                currentFrequency *
                multiplierD /
                SampleRate;

            phaseA -= Mathf.Floor(phaseA);
            phaseB -= Mathf.Floor(phaseB);
            phaseC -= Mathf.Floor(phaseC);
            phaseD -= Mathf.Floor(phaseD);

            // =========================================
            // MIX (solo A, B y C según su dB.
            // D nunca entra al mix)
            // =========================================

            float mixed =
                oscA *
                DbToLinear(preset.oscillatorADb);

            mixed +=
                oscB *
                DbToLinear(preset.oscillatorBDb);

            mixed +=
                oscC *
                DbToLinear(preset.oscillatorCDb);

            // =========================================
            // ADSR (MAIN)
            // =========================================

            mixed *= main;

            // =========================================
            // FILTER MODULATION
            // =========================================

            float cutoff =
                preset.filterCutoff;

            float filterMod =
                main * preset.filterModMain +
                env2 * preset.filterModEnv2 +
                env3 * preset.filterModEnv3 +
                lfo1 * preset.filterModLfo1 +
                lfo2 * preset.filterModLfo2;

            if (filterMod != 0f)
            {
                cutoff =
                    Mathf.Clamp(
                        cutoff *
                        Mathf.Pow(
                            2f,
                            filterMod *
                            preset.filterModOctaves
                        ),
                        20f,
                        SampleRate * 0.45f
                    );
            }

            // =========================================
            // LOW PASS
            // =========================================

            if (preset.useFilter)
            {
                if (preset.resonantFilter)
                {
                    mixed =
                        ResonantLowPass(
                            mixed,
                            cutoff,
                            preset.filterResonance,
                            ref svfIc1,
                            ref svfIc2
                        );
                }
                else
                {
                    mixed =
                        LowPass(
                            mixed,
                            cutoff,
                            ref filterState
                        );
                }
            }

            // =========================================
            // EQ SIMPLE
            // =========================================

            if (preset.useEQ)
            {
                mixed *=
                    DbToLinear(
                        preset.lowShelfDb
                    );
            }

            // =========================================
            // MASTER
            // =========================================

            mixed *= preset.volume;

            // Evita clipping
            samples[i] =
                Mathf.Clamp(
                    mixed,
                    -1f,
                    1f
                );
        }

        AudioClip clip =
            AudioClip.Create(
                "ProceduralSound",
                sampleCount,
                1,
                SampleRate,
                false
            );

        clip.SetData(samples, 0);

        return clip;
    }

    // =================================================
    // OSCILADORES / LFOs
    // (recibe la fase; se normaliza a 0..1 aquí)
    // =================================================

    private float GenerateWave(
        WaveType type,
        float phase)
    {
        // La modulación de fase puede sacarla de 0..1
        phase -= Mathf.Floor(phase);

        switch (type)
        {
            case WaveType.Sine:

                return Mathf.Sin(
                    2f *
                    Mathf.PI *
                    phase
                );

            case WaveType.Triangle:

                return
                    4f *
                    Mathf.Abs(
                        phase - 0.5f
                    ) - 1f;

            case WaveType.Square:

                return phase < 0.5f
                    ? 1f
                    : -1f;

            case WaveType.Saw:

                return
                    2f * phase - 1f;
        }

        return 0f;
    }

    // =================================================
    // SEMITONOS
    // =================================================

    private float SemitoneMultiplier(
        float semitones)
    {
        return Mathf.Pow(
            2f,
            semitones / 12f
        );
    }

    // =================================================
    // AMPLITUDE MODULATION
    // amount 0 → nivel 1 (sin efecto)
    // amount 1 → el nivel sigue por completo a la fuente
    // =================================================

    private float ModGain(
        float amount,
        float source01)
    {
        return Mathf.Max(
            0f,
            1f - amount + amount * source01
        );
    }

    // =================================================
    // ADSR
    // =================================================

    private float GetAmplitudeEnvelope(
        AudioPreset preset,
        float time,
        float duration)
    {
        return EvaluateAdsr(
            preset.attack,
            preset.decay,
            preset.sustain,
            preset.release,
            time,
            duration
        );
    }

    private float GetEnvelope(
        EnvelopeSettings env,
        float time,
        float duration)
    {
        return EvaluateAdsr(
            env.attack,
            env.decay,
            env.sustain,
            env.release,
            time,
            duration
        );
    }

    private float EvaluateAdsr(
        float attack,
        float decay,
        float sustain,
        float release,
        float time,
        float duration)
    {
        // ATTACK
        if (time < attack)
        {
            return Mathf.Clamp01(
                time /
                Mathf.Max(
                    attack,
                    0.0001f
                )
            );
        }

        // DECAY
        float decayStart =
            attack;

        float decayEnd =
            attack +
            decay;

        if (time < decayEnd)
        {
            float t =
                (time - decayStart) /
                Mathf.Max(
                    decay,
                    0.0001f
                );

            return Mathf.Lerp(
                1f,
                sustain,
                t
            );
        }

        // RELEASE
        float releaseStart =
            Mathf.Max(
                duration -
                release,
                decayEnd
            );

        if (time >= releaseStart)
        {
            float t =
                (time - releaseStart) /
                Mathf.Max(
                    release,
                    0.0001f
                );

            return Mathf.Lerp(
                sustain,
                0f,
                t
            );
        }

        // SUSTAIN
        return sustain;
    }

    // =================================================
    // PITCH ENVELOPE
    //
    //  0 ──pitchAttack──▶ 1 ──pitchDecay──▶ 2 (se mantiene)
    //  pitchAmount     pitchAttackLevel   pitchEndAmount
    // =================================================

    private float GetPitchEnvelope(
        AudioPreset preset,
        float time)
    {
        // ATTACK: pitchAmount → pitchAttackLevel
        if (time < preset.pitchAttack)
        {
            float t =
                time /
                Mathf.Max(
                    preset.pitchAttack,
                    0.0001f
                );

            return Mathf.Lerp(
                preset.pitchAmount,
                preset.pitchAttackLevel,
                t
            );
        }

        // DECAY: pitchAttackLevel → pitchEndAmount
        float decayTime =
            time - preset.pitchAttack;

        if (decayTime < preset.pitchDecay)
        {
            float t =
                decayTime /
                Mathf.Max(
                    preset.pitchDecay,
                    0.0001f
                );

            return Mathf.Lerp(
                preset.pitchAttackLevel,
                preset.pitchEndAmount,
                t
            );
        }

        return preset.pitchEndAmount;
    }

    // =================================================
    // LOW PASS SIMPLE (6 dB/oct, sin resonancia)
    // =================================================

    private float LowPass(
        float input,
        float cutoff,
        ref float state)
    {
        float alpha =
            1f -
            Mathf.Exp(
                -2f *
                Mathf.PI *
                cutoff /
                SampleRate
            );

        state +=
            alpha *
            (input - state);

        return state;
    }

    // =================================================
    // LOW PASS RESONANTE (12 dB/oct)
    // State Variable Filter (TPT)
    // resonance 0..1  →  Q de 0.5 a 10
    // =================================================

    private float ResonantLowPass(
        float input,
        float cutoff,
        float resonance,
        ref float ic1eq,
        ref float ic2eq)
    {
        float fc =
            Mathf.Min(
                cutoff,
                SampleRate * 0.45f
            );

        float g =
            Mathf.Tan(
                Mathf.PI *
                fc /
                SampleRate
            );

        float q =
            Mathf.Lerp(
                0.5f,
                10f,
                Mathf.Clamp01(resonance)
            );

        float k = 1f / q;

        float a1 = 1f / (1f + g * (g + k));
        float a2 = g * a1;
        float a3 = g * a2;

        float v3 = input - ic2eq;
        float v1 = a1 * ic1eq + a2 * v3;
        float v2 = ic2eq + a2 * ic1eq + a3 * v3;

        ic1eq = 2f * v1 - ic1eq;
        ic2eq = 2f * v2 - ic2eq;

        return v2;
    }

    // =================================================
    // dB → LINEAR
    // =================================================

    private float DbToLinear(
        float db)
    {
        return Mathf.Pow(
            10f,
            db / 20f
        );
    }
}
