using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Ambient Clips")]
    public AudioClip LogingAudio;
    public AudioClip LoadingAudio;
    public AudioClip SampleAudio;
    public AudioClip PlaybleAudio;

    private AudioSource ambientAudioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            ambientAudioSource = gameObject.AddComponent<AudioSource>();
            ambientAudioSource.loop = true;
            ambientAudioSource.playOnAwake = false;
            ambientAudioSource.spatialBlend = 0f;
            ambientAudioSource.volume = 0.2f;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        PlayAmbientSound(LogingAudio);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "LoadingScene":
                PlayAmbientSound(LoadingAudio);
                break;
            case "Playble":
                PlayAmbientSound(PlaybleAudio);
                break;
            case "SampleScene":
                PlayAmbientSound(SampleAudio);
                break;
            default:
                PlayAmbientSound(LogingAudio);
                break;
        }
    }

    private void PlayAmbientSound(AudioClip clip)
    {
        if (clip == null || ambientAudioSource.clip == clip) return;

        ambientAudioSource.Stop();
        ambientAudioSource.clip = clip;
        ambientAudioSource.Play();
    }

    public void Play(AudioClip clip, AudioSource audioSource)
    {
        float distance = Vector3.Distance(Camera.main.transform.position, audioSource.transform.position);
        float maxDistance = 1000f;
        audioSource.volume = Mathf.Clamp01(1 - (distance / maxDistance));

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

    public void Stop(AudioSource audioSource)
    {
        audioSource.Stop();
        audioSource.clip = null;
    }

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
