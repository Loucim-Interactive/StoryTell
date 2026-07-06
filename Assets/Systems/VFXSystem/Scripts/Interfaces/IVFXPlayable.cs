using UnityEngine;

public interface IVFXPlayable
{
    void Play();

    void Stop();

    void SetPosition(Vector3 position);
}