using UnityEngine;
using UnityEngine.UI;
using TMPro; // Used for modern UI text
using UnityEngine.SceneManagement; // Used to reload the scene

public class HUDManager : MonoBehaviour
{
    [Header("Player 1 References")]
    public FighterController player1;
    public Slider p1HealthSlider;

    [Header("Player 2 References")]
    public FighterController player2;
    public Slider p2HealthSlider;

    [Header("End Screen UI")]
    public GameObject endScreenPanel;
    public TextMeshProUGUI winnerText;

    [Header("Audio Settings")]
    [Tooltip("The AudioSource component used to play the KO sound effect.")]
    public AudioSource audioSource;
    [Tooltip("The audio clip (like an announcer yelling 'K.O.!') that plays when a round ends.")]
    public AudioClip koSound;

    private bool isGameOver = false;

    private void Start()
    {
        // Ensure the end screen is hidden when the match starts
        if (endScreenPanel != null)
            endScreenPanel.SetActive(false);

        // Setup Player 1 Max Health Bounds
        if (player1 != null && player1.data != null)
        {
            p1HealthSlider.maxValue = player1.data.maxHealth;
            p1HealthSlider.value = player1.currentHealth;
        }

        // Setup Player 2 Max Health Bounds
        if (player2 != null && player2.data != null)
        {
            p2HealthSlider.maxValue = player2.data.maxHealth;
            p2HealthSlider.value = player2.currentHealth;
        }
    }

    private void Update()
    {
        // Constantly update the sliders to reflect current health
        if (player1 != null && p1HealthSlider != null)
        {
            p1HealthSlider.value = player1.currentHealth;
        }

        if (player2 != null && p2HealthSlider != null)
        {
            p2HealthSlider.value = player2.currentHealth;
        }

        // Monitor player health for KO states
        if (!isGameOver)
        {
            if (player1 != null && player1.currentHealth <= 0)
            {
                RevealEndScreen("PLAYER 2 WINS!");
            }
            else if (player2 != null && player2.currentHealth <= 0)
            {
                RevealEndScreen("PLAYER 1 WINS!");
            }
        }
    }

    void RevealEndScreen(string victoryText)
    {
        isGameOver = true;

        if (winnerText != null)
            winnerText.text = victoryText;

        if (endScreenPanel != null)
            endScreenPanel.SetActive(true);

        // NEW: Play the KO audio sound effect instantly when a player is defeated
        if (audioSource != null && koSound != null)
        {
            audioSource.PlayOneShot(koSound);
        }
    }

    // This method will be linked to your Rematch UI Button
    public void RematchButton()
    {
        // Reloads the currently running scene from scratch
        SceneTransitionManager.Instance.FadeToScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMenuButton()
    {
        // Transitions back to the main menu scene
        SceneManager.LoadScene(0);
    }
}