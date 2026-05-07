using System.Collections;
using UnityEngine;

// Singleton music manager. Place an empty GameObject with this component
// in your first-loaded scene; it survives scene loads via DontDestroyOnLoad.
//
// Usage:
//   SoundManager.Instance.PlayMusic(myClip);            // crossfade in
//   SoundManager.Instance.PlayMusic(myClip, 2f);        // 2s crossfade
//   SoundManager.Instance.StopMusic();                   // fade out
//   SoundManager.Instance.SetVolume(0.5f);
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;
    [SerializeField] private float defaultFadeTime = 1f;

    private AudioSource activeSource;
    private AudioSource fadingSource;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        activeSource = gameObject.AddComponent<AudioSource>();
        fadingSource = gameObject.AddComponent<AudioSource>();
        ConfigureSource(activeSource);
        ConfigureSource(fadingSource);
    }

    private static void ConfigureSource(AudioSource source)
    {
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
    }

    public void PlayMusic(AudioClip clip, float fadeTime = -1f)
    {
        if (clip == null) return;
        if (activeSource.clip == clip && activeSource.isPlaying) return;

        if (fadeTime < 0f) fadeTime = defaultFadeTime;

        // Swap roles — old active becomes the fading-out source.
        AudioSource temp = fadingSource;
        fadingSource = activeSource;
        activeSource = temp;

        activeSource.clip = clip;
        activeSource.volume = 0f;
        activeSource.Play();

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(CrossfadeRoutine(fadeTime));
    }

    public void StopMusic(float fadeTime = -1f)
    {
        if (fadeTime < 0f) fadeTime = defaultFadeTime;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutRoutine(fadeTime));
    }

    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (activeSource != null && activeSource.isPlaying)
            activeSource.volume = musicVolume;
    }

    public float GetVolume() => musicVolume;

    private IEnumerator CrossfadeRoutine(float fadeTime)
    {
        float elapsed = 0f;
        float startFadeVol = fadingSource.volume;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            activeSource.volume = Mathf.Lerp(0f, musicVolume, t);
            fadingSource.volume = Mathf.Lerp(startFadeVol, 0f, t);
            yield return null;
        }
        activeSource.volume = musicVolume;
        fadingSource.Stop();
        fadingSource.clip = null;
        fadeRoutine = null;
    }

    private IEnumerator FadeOutRoutine(float fadeTime)
    {
        float elapsed = 0f;
        float startVol = activeSource.volume;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            activeSource.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }
        activeSource.Stop();
        activeSource.clip = null;
        fadeRoutine = null;
    }
}
