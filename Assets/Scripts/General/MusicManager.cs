using UnityEngine;
using UnityEngine.SceneManagement;

// This custom struct now links a Scene Build Index directly to an Audio Clip
[System.Serializable]
public struct SceneMusicMap
{
    [Tooltip("The unique Build Index number of the scene (from File > Build Settings).")]
    public int sceneIndex;
    public AudioClip track;
}

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("References")]
    public AudioSource audioSource;

    [Header("Music Tracks")]
    [Tooltip("The music that plays if a loaded scene index isn't explicitly mapped below.")]
    public AudioClip defaultTitleTrack;

    [Tooltip("Map exact scene build indices to their specific battle tracks here.")]
    public SceneMusicMap[] battleTracks;

    private void Awake()
    {
        // Enforce the Singleton Pattern so only one Music Manager ever exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Tell Unity to call our 'OnSceneLoaded' function every time a new scene loads
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Always unsubscribe from events when disabled to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // CHANGED: Pass the scene's buildIndex instead of its string name
        SwapMusicForScene(scene.buildIndex);
    }

    public void SwapMusicForScene(int targetSceneIndex)
    {
        AudioClip nextClip = defaultTitleTrack;

        // CHANGED: Loop through our array comparing integer indices instead of strings
        foreach (var map in battleTracks)
        {
            if (map.sceneIndex == targetSceneIndex)
            {
                nextClip = map.track;
                break;
            }
        }

        // If the correct song is already playing, don't restart it.
        if (audioSource.clip == nextClip) return;

        audioSource.clip = nextClip;

        if (nextClip != null)
            audioSource.Play();
        else
            audioSource.Stop();
    }
}