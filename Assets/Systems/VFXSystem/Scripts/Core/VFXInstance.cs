using UnityEngine;
using System;
using System.Collections;

public class VFXInstance : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem[] systems;

    private Coroutine returnRoutine;

    private void Awake()
    {
        if (systems == null || systems.Length == 0)
            systems = GetComponentsInChildren<ParticleSystem>(true);
    }

    public void Play(Action onComplete = null)
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        RefreshSystemsIfNeeded();
        ResetSystems();

        foreach (ParticleSystem ps in systems)
        {
            if (ps != null)
                ps.Play();
        }

        if (onComplete != null)
            returnRoutine = StartCoroutine(ReturnWhenComplete(onComplete));
    }

    public void Stop()
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        foreach (ParticleSystem ps in systems)
        {
            if (ps != null)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private IEnumerator ReturnWhenComplete(Action onComplete)
    {
        while (AnySystemAlive())
            yield return null;

        onComplete.Invoke();
        returnRoutine = null;
    }

    private bool AnySystemAlive()
    {
        foreach (ParticleSystem ps in systems)
        {
            if (ps != null && ps.IsAlive(true))
                return true;
        }

        return false;
    }

    private void RefreshSystemsIfNeeded()
    {
        if (systems == null || systems.Length == 0)
            systems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void ResetSystems()
    {
        foreach (ParticleSystem ps in systems)
        {
            if (ps == null)
                continue;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
        }
    }
}
