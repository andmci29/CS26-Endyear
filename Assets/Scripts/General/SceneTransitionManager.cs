using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    // Static instance allows any other script to call SceneTransitionManager.Instance.FadeToScene()
    public static SceneTransitionManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("Attach a CanvasGroup component to your Fade Panel object.")]
    public CanvasGroup fadeCanvasGroup;

    [Header("Tuning")]
    [Tooltip("How long the fade-in and fade-out animations take in seconds.")]
    public float fadeDuration = 0.5f;

    [Header("Audio Settings")]
    [Tooltip("The AudioSource component used to play the bell effect.")]
    public AudioSource audioSource;
    [Tooltip("The audio clip of the bell that plays when a battle scene loads.")]
    public AudioClip bellClip;
    [Tooltip("The exact name of your Title Screen scene so the script knows when NOT to play the bell.")]
    public string titleSceneName = "CharacterSelect";

    private bool isTransitioning = false;

    private void Awake()
    {
        // 1. Check if an Instance already exists in the game's memory
        if (Instance != null && Instance != this)
        {
            // If one does, this new one is an accidental clone! Kill it immediately.
            Destroy(gameObject);
            return; // Stop running any more code on this object
        }

        // 2. If the slot is empty, this object is the original master copy. 
        Instance = this;

        // 3. Unparent it (just in case) and make it immortal
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // When the game first boots up, instantly fade out of black into the current scene
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = true;
        StartCoroutine(FadeInCurrentScene());
    }

    /// <summary>
    /// Call this function from your main menus or fight scripts to seamlessly transition scenes.
    /// Example: SceneTransitionManager.Instance.FadeToScene("BattleScene");
    /// </summary>
    public void FadeToScene(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionSequence(sceneName));
    }

    private IEnumerator TransitionSequence(string targetScene)
    {
        isTransitioning = true;
        fadeCanvasGroup.blocksRaycasts = true; // Blocks player clicks during the fade transition

        // 1. Fade to Black
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        // 2. Load the scene in the background
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // NEW AUDIO ADDITION: Check if the scene we just loaded is NOT the title screen
        if (targetScene != titleSceneName && audioSource != null && bellClip != null)
        {
            audioSource.PlayOneShot(bellClip);
        }

        // 3. Fade back to visibility in the new scene
        yield return StartCoroutine(FadeInCurrentScene());
        isTransitioning = false;
    }

    private IEnumerator FadeInCurrentScene()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false; // Gives input control back to the player
    }
}