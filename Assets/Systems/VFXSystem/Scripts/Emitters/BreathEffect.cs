using UnityEngine;

[DisallowMultipleComponent]
public sealed class BreathEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem breathParticles;
    [SerializeField] private bool coldEnvironment = true;
    [SerializeField, Min(0.1f)] private float secondsBetweenBreaths = 2.4f;

    private float breathingRateMultiplier = 1f;
    private float nextBreathTime;

    private void Awake()
    {
        if (breathParticles == null)
            breathParticles = GetComponentInChildren<ParticleSystem>(true);
    }

    private void OnEnable()
    {
        ScheduleNextBreath();
    }

    private void Update()
    {
        if (!coldEnvironment ||
            breathParticles == null ||
            Time.time < nextBreathTime)
        {
            return;
        }

        breathParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        breathParticles.Play(true);
        ScheduleNextBreath();
    }

    public void SetColdEnvironment(bool value)
    {
        coldEnvironment = value;

        if (!coldEnvironment && breathParticles != null)
        {
            breathParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void SetBreathingRateMultiplier(float multiplier)
    {
        breathingRateMultiplier = Mathf.Max(0.1f, multiplier);
        ScheduleNextBreath();
    }

    private void ScheduleNextBreath()
    {
        float interval = secondsBetweenBreaths / breathingRateMultiplier;
        nextBreathTime = Time.time + Mathf.Max(0.1f, interval);
    }

    private void OnValidate()
    {
        secondsBetweenBreaths = Mathf.Max(0.1f, secondsBetweenBreaths);
    }
}
