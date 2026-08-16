using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight, self-contained flight behaviour for distant ambient aircraft.
/// Moves in a broad circuit, banks through turns, animates named rotors/propellers,
/// and produces a spatial procedural engine sound when no authored clip is assigned.
/// </summary>
[DisallowMultipleComponent]
public sealed class AmbientAircraftFlight : MonoBehaviour
{
    public enum AircraftKind { FixedWing, Helicopter }

    [Header("Aircraft")]
    [SerializeField] private AircraftKind aircraftKind = AircraftKind.FixedWing;
    [SerializeField, Min(0f)] private float forwardSpeed = 22f;
    [SerializeField] private float turnRate = 4f;
    [SerializeField, Range(0f, 45f)] private float maximumBankAngle = 18f;
    [SerializeField, Min(0f)] private float altitudeVariation = 2f;
    [SerializeField, Min(0.01f)] private float altitudeCycleSeconds = 18f;

    [Header("Rotors / Propellers")]
    [SerializeField, Min(0f)] private float rotorSpeed = 1400f;
    [SerializeField] private Vector3 rotorLocalAxis = Vector3.up;

    [Header("Sound")]
    [Tooltip("Optional authored looping sound. A procedural engine sound is used when empty.")]
    [SerializeField] private AudioClip engineClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.35f;
    [SerializeField, Min(1f)] private float minimumDistance = 12f;
    [SerializeField, Min(2f)] private float maximumDistance = 350f;

    private readonly List<Transform> spinningParts = new List<Transform>();
    private AudioSource audioSource;
    private AudioClip proceduralClip;
    private float startingHeight;
    private float altitudePhase;
    private float bank;

    private void Awake()
    {
        startingHeight = transform.position.y;
        altitudePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        FindSpinningParts();
        ConfigureAudio();
    }

    private void OnEnable()
    {
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        transform.position += transform.forward * (forwardSpeed * dt);
        transform.Rotate(Vector3.up, turnRate * dt, Space.World);

        float targetHeight = startingHeight + Mathf.Sin((Time.time / altitudeCycleSeconds) * Mathf.PI * 2f + altitudePhase) * altitudeVariation;
        Vector3 position = transform.position;
        position.y = Mathf.Lerp(position.y, targetHeight, 1f - Mathf.Exp(-1.5f * dt));
        transform.position = position;

        float desiredBank = -Mathf.Sign(turnRate) * maximumBankAngle;
        bank = Mathf.Lerp(bank, desiredBank, 1f - Mathf.Exp(-2f * dt));
        Vector3 euler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, euler.y, bank);

        float spin = rotorSpeed * dt;
        for (int i = 0; i < spinningParts.Count; i++)
            spinningParts[i].Rotate(rotorLocalAxis, spin, Space.Self);
    }

    private void FindSpinningParts()
    {
        spinningParts.Clear();
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child == transform) continue;
            string partName = child.name.ToLowerInvariant();
            if (partName.Contains("rotor") || partName.Contains("prop"))
                spinningParts.Add(child);
        }
    }

    private void ConfigureAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = minimumDistance;
        audioSource.maxDistance = Mathf.Max(minimumDistance + 1f, maximumDistance);
        audioSource.dopplerLevel = 1f;
        audioSource.volume = volume;
        proceduralClip = engineClip == null ? CreateProceduralEngineClip() : null;
        audioSource.clip = engineClip != null ? engineClip : proceduralClip;
    }

    private AudioClip CreateProceduralEngineClip()
    {
        const int sampleRate = 22050;
        const int sampleCount = sampleRate;
        float[] samples = new float[sampleCount];
        double baseFrequency = aircraftKind == AircraftKind.Helicopter ? 24.0 : 58.0;
        double bladeFrequency = aircraftKind == AircraftKind.Helicopter ? 8.0 : 0.0;

        for (int i = 0; i < sampleCount; i++)
        {
            double time = (double)i / sampleRate;
            double pulse = Math.Sin(time * baseFrequency * Math.PI * 2.0);
            double harmonic = Math.Sin(time * baseFrequency * Math.PI * 4.0) * 0.35;
            double modulation = bladeFrequency > 0.0
                ? 0.68 + 0.32 * Math.Sin(time * bladeFrequency * Math.PI * 2.0)
                : 1.0;
            samples[i] = (float)((pulse + harmonic) * modulation * 0.09);
        }

        AudioClip clip = AudioClip.Create($"{aircraftKind} Procedural Engine", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (proceduralClip != null)
            Destroy(proceduralClip);
    }

    private void OnValidate()
    {
        altitudeCycleSeconds = Mathf.Max(0.01f, altitudeCycleSeconds);
        maximumDistance = Mathf.Max(minimumDistance + 1f, maximumDistance);
        if (audioSource != null)
        {
            audioSource.volume = volume;
            audioSource.minDistance = minimumDistance;
            audioSource.maxDistance = maximumDistance;
        }
    }
}
