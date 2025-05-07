using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Общие звуки")]
    public AudioClip deathSound;
    public AudioClip runSound;
    public AudioClip attackSound;
    public AudioClip waterSplashSound;
    public AudioClip footstepSound;

    private Dictionary<GameObject, AudioSource> activeLoops = new Dictionary<GameObject, AudioSource>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Для разовых звуков (смерть, атака)
    public void PlayAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    // Для зацикленных звуков (бег, плавание)
    public void PlayLoop(GameObject owner, AudioClip clip, float volume = 1f)
    {
        if (clip == null || activeLoops.ContainsKey(owner)) return;

        AudioSource source = owner.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = volume;
        source.Play();

        activeLoops.Add(owner, source);
    }

    public void StopLoop(GameObject owner)
    {
        if (activeLoops.TryGetValue(owner, out AudioSource source))
        {
            source.Stop();
            Destroy(source);
            activeLoops.Remove(owner);
        }
    }
}