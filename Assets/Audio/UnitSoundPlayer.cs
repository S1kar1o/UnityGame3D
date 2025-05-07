using System.ComponentModel;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UnitSoundPlayer : MonoBehaviour
{

    public static UnitSoundPlayer Instance;

    [Header("Audio Clips")]
    public AudioClip attackSound;
    public AudioClip runSound;
    public AudioClip deathSound;
    public AudioClip swimSound;
    public AudioClip standingInWaterSound;
    public AudioClip drownSound;


    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Play(AudioClip clip, AudioSource audioSource)
    {
        float distance = Vector3.Distance(Camera.main.transform.position, audioSource.transform.position);
        float maxDistance = 1000f;
        audioSource.volume = Mathf.Clamp01(1 - (distance / maxDistance));
        Debug.Log("LLLLL  " + Mathf.Clamp01(1 - (distance / maxDistance)));

        if (clip == null) return;

        if (audioSource.clip != clip)
        {
            audioSource.clip = clip;
           
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    // «упинка поточного звуку
    public void Stop(AudioSource audioSource)
    {
        audioSource.Stop();
        audioSource.clip = null;
    }

    // „и граЇ конкретний звук
    public bool IsPlayingClip(AudioClip clip, AudioSource audioSource)
    {
        return audioSource.isPlaying && audioSource.clip == clip;
    }

    public void HandleRunningSound(AudioSource audioSource, bool IsRunning)
    {
        if (IsRunning)
            Play(runSound, audioSource);
        else if (IsPlayingClip(runSound, audioSource))
            Stop(audioSource);
    }

    public void HandleDeathSound(AudioSource audioSource, bool IsDie, ref bool IsDieFlag)
    {
        if (IsDie && !IsDieFlag)
        {
            Play(deathSound, audioSource);
            IsDieFlag = true;
        }
    }

    public void HandleSwimmingSound(AudioSource audioSource, bool IsSwimming)
    {
        if (IsSwimming)
            Play(swimSound, audioSource);
        else if (IsPlayingClip(swimSound, audioSource))
            Stop(audioSource);
    }

    public void HandleStandingInWaterSound(AudioSource audioSource, bool IsStandingInWater)
    {
        if (IsStandingInWater)
            Play(standingInWaterSound, audioSource);
        else if (IsPlayingClip(standingInWaterSound, audioSource))
            Stop(audioSource);
    }

    public void HandleDrownSound(AudioSource audioSource, bool IsDrow, ref bool IsDie)
    {
        if (IsDrow && !IsDie)
        {
            Play(drownSound, audioSource);
            IsDie = true;
        }
        else if (IsPlayingClip(drownSound, audioSource))
            Stop(audioSource);
    }
}
