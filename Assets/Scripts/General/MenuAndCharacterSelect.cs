using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MenuAndCharacterSelect : MonoBehaviour
{
    [Header("Panel Containers")]
    public GameObject titlePanel;
    public GameObject characterSelectPanel;
    public GameObject controlsPanel;

    [Header("Title Buttons")]
    [Tooltip("Drag the Title Screen's PLAY button here.")]
    public Button playButton;
    [Tooltip("Drag the Title Screen's CONTROLS button here.")]
    public Button controlsButton;
    [Tooltip("Drag the BACK button from inside the Controls Panel here.")]
    public Button controlsBackButton;

    [Header("Columns Hierarchy Links")]
    public GameObject columnP1;
    public GameObject columnP2;
    public GameObject columnFight;

    [Header("Column P1 Buttons")]
    public Button davidButtonP1;
    public Button crashButtonP1;

    [Header("Column P2 Buttons")]
    public Button davidButtonP2;
    public Button crashButtonP2;

    [Header("Shared Back Button")]
    [Tooltip("One back button shown on the rightmost active column — reposition this in the UI to sit at the far right")]
    public Button backButton;

    [Header("Column Fight Elements")]
    public Image p1SelectionImage;
    public Image p2SelectionImage;
    public Button fightButton;

    [Header("Character Thumbnail Sprites")]
    public Sprite davidThumbnail;
    public Sprite crashThumbnail;

    [Header("Audio Configurations")]
    [Tooltip("Drag your central AudioSource component here.")]
    public AudioSource menuAudioSource;
    public AudioClip davidVoiceLine;
    public AudioClip crashVoiceLine;

    [Header("Battle Scene Target Names")]
    public string scene_DavidVsDavid = "Battle_David_vs_David";
    public string scene_DavidVsCrash = "Battle_David_vs_Crash";
    public string scene_CrashVsDavid = "Battle_Crash_vs_David";
    public string scene_CrashVsCrash = "Battle_Crash_vs_Crash";

    // Internal state
    private int p1SelectedCharacter = -1;
    private int p2SelectedCharacter = -1;

    // Which column the back button currently belongs to
    private enum BackContext { None, P1, P2, Fight }
    private BackContext currentBackContext = BackContext.None;

    private Color fullVisibility = Color.white;
    private Color dimmedVisibility = new Color(0.35f, 0.35f, 0.35f, 1f);

    // -------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------

    void Start()
    {
        RegisterButtonListeners();
        ResetToTitleScreen();
    }

    void RegisterButtonListeners()
    {
        // Automatically wires all buttons so you don't have to use the Inspector's OnClick() list
        if (playButton != null) playButton.onClick.AddListener(OnTitlePlayPressed);
        if (controlsButton != null) controlsButton.onClick.AddListener(OnControlsButtonPressed);
        if (controlsBackButton != null) controlsBackButton.onClick.AddListener(OnControlsBackPressed);

        davidButtonP1.onClick.AddListener(() => OnP1CharacterSelected(0));
        crashButtonP1.onClick.AddListener(() => OnP1CharacterSelected(1));

        davidButtonP2.onClick.AddListener(() => OnP2CharacterSelected(0));
        crashButtonP2.onClick.AddListener(() => OnP2CharacterSelected(1));

        // Single shared back button — behaviour changes based on context 
        if (backButton != null) backButton.onClick.AddListener(OnBackPressed);

        if (fightButton != null) fightButton.onClick.AddListener(OnFightButtonPressed);
    }

    // -------------------------------------------------------
    // Back button — single button, context-driven behaviour
    // -------------------------------------------------------

    void OnBackPressed()
    {
        switch (currentBackContext)
        {
            case BackContext.P1: OnP1BackPressed(); break;
            case BackContext.P2: OnP2BackPressed(); break;
            case BackContext.Fight: OnFightBackPressed(); break;
        }
    }

    // Moves the back button to the correct parent column and updates its context 
    void SetBackButton(BackContext context)
    {
        currentBackContext = context;

        if (context == BackContext.None || backButton == null)
        {
            if (backButton != null) backButton.gameObject.SetActive(false);
            return;
        }

        backButton.gameObject.SetActive(true);

        // Reparent to the rightmost active column so it sits in the correct layout 
        switch (context)
        {
            case BackContext.P1:
                backButton.transform.SetParent(columnP1.transform, false);
                break;
            case BackContext.P2:
                backButton.transform.SetParent(columnP2.transform, false);
                break;
            case BackContext.Fight:
                backButton.transform.SetParent(columnFight.transform, false);
                break;
        }

        // Push to last sibling so it sits at the bottom of the column 
        backButton.transform.SetAsLastSibling();
    }

    // -------------------------------------------------------
    // Controls panel functions
    // -------------------------------------------------------

    private void OnControlsButtonPressed()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);
        SetControllerFocus(controlsBackButton);
    }

    private void OnControlsBackPressed()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (titlePanel != null) titlePanel.SetActive(true);
        SetControllerFocus(playButton);
    }

    // -------------------------------------------------------
    // Navigation
    // -------------------------------------------------------

    public void ResetToTitleScreen()
    {
        if (titlePanel != null) titlePanel.SetActive(true);
        if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        if (columnP1 != null) columnP1.SetActive(false);
        if (columnP2 != null) columnP2.SetActive(false);
        if (columnFight != null) columnFight.SetActive(false);

        SetBackButton(BackContext.None);

        // Reset P1 column visuals 
        if (davidButtonP1 != null)
        {
            davidButtonP1.interactable = true;
            davidButtonP1.image.color = fullVisibility;
        }
        if (crashButtonP1 != null)
        {
            crashButtonP1.interactable = true;
            crashButtonP1.image.color = fullVisibility;
        }

        // Reset P2 column visuals 
        if (davidButtonP2 != null)
        {
            davidButtonP2.interactable = true;
            davidButtonP2.image.color = fullVisibility;
        }
        if (crashButtonP2 != null)
        {
            crashButtonP2.interactable = true;
            crashButtonP2.image.color = fullVisibility;
        }

        p1SelectedCharacter = -1;
        p2SelectedCharacter = -1;

        SetControllerFocus(playButton);
    }

    void OnTitlePlayPressed()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (characterSelectPanel != null) characterSelectPanel.SetActive(true);

        if (columnP1 != null) columnP1.SetActive(true);
        if (columnP2 != null) columnP2.SetActive(false);
        if (columnFight != null) columnFight.SetActive(false);

        // Back button lives on P1 column — only column visible 
        SetBackButton(BackContext.P1);
        SetControllerFocus(davidButtonP1);
    }

    void OnP1CharacterSelected(int characterID)
    {
        p1SelectedCharacter = characterID;

        if (p1SelectionImage != null)
            p1SelectionImage.sprite = characterID == 0 ? davidThumbnail : crashThumbnail;

        // Lock and dim P1 column 
        if (davidButtonP1 != null)
        {
            davidButtonP1.interactable = false;
            davidButtonP1.image.color = characterID == 0 ? fullVisibility : dimmedVisibility;
        }
        if (crashButtonP1 != null)
        {
            crashButtonP1.interactable = false;
            crashButtonP1.image.color = characterID == 1 ? fullVisibility : dimmedVisibility;
        }

        PlaySelectionVoice(characterID);

        // Reveal P2 column — back button moves here as it's now rightmost 
        if (columnP2 != null) columnP2.SetActive(true);
        if (columnFight != null) columnFight.SetActive(false);
        SetBackButton(BackContext.P2);
        SetControllerFocus(davidButtonP2);
    }

    void OnP2CharacterSelected(int characterID)
    {
        p2SelectedCharacter = characterID;

        if (p2SelectionImage != null)
            p2SelectionImage.sprite = characterID == 0 ? davidThumbnail : crashThumbnail;

        // Lock and dim P2 column 
        if (davidButtonP2 != null)
        {
            davidButtonP2.interactable = false;
            davidButtonP2.image.color = characterID == 0 ? fullVisibility : dimmedVisibility;
        }
        if (crashButtonP2 != null)
        {
            crashButtonP2.interactable = false;
            crashButtonP2.image.color = characterID == 1 ? fullVisibility : dimmedVisibility;
        }

        PlaySelectionVoice(characterID);

        // Reveal fight column — back button moves here as rightmost 
        if (columnFight != null) columnFight.SetActive(true);
        SetBackButton(BackContext.Fight);
        SetControllerFocus(fightButton);
    }

    // -------------------------------------------------------
    // Back operations 
    // -------------------------------------------------------

    void OnP1BackPressed()
    {
        ResetToTitleScreen();
    }

    void OnP2BackPressed()
    {
        if (columnP2 != null) columnP2.SetActive(false);
        if (columnFight != null) columnFight.SetActive(false);
        p2SelectedCharacter = -1;

        // Restore P1 column 
        if (davidButtonP1 != null)
        {
            davidButtonP1.interactable = true;
            davidButtonP1.image.color = fullVisibility;
        }
        if (crashButtonP1 != null)
        {
            crashButtonP1.interactable = true;
            crashButtonP1.image.color = fullVisibility;
        }

        // Back button returns to P1 column 
        SetBackButton(BackContext.P1);
        SetControllerFocus(davidButtonP1);
    }

    void OnFightBackPressed()
    {
        if (columnFight != null) columnFight.SetActive(false);
        p2SelectedCharacter = -1;

        // Restore P2 column 
        if (davidButtonP2 != null)
        {
            davidButtonP2.interactable = true;
            davidButtonP2.image.color = fullVisibility;
        }
        if (crashButtonP2 != null)
        {
            crashButtonP2.interactable = true;
            crashButtonP2.image.color = fullVisibility;
        }

        // Back button returns to P2 column 
        SetBackButton(BackContext.P2);
        SetControllerFocus(davidButtonP2);
    }

    // -------------------------------------------------------
    // Fight 
    // -------------------------------------------------------

    void OnFightButtonPressed()
    {
        if (p1SelectedCharacter == -1 || p2SelectedCharacter == -1) return;

        this.enabled = false;

        if (p1SelectedCharacter == 0 && p2SelectedCharacter == 0) SceneTransitionManager.Instance.FadeToScene(scene_DavidVsDavid);
        else if (p1SelectedCharacter == 0 && p2SelectedCharacter == 1) SceneTransitionManager.Instance.FadeToScene(scene_DavidVsCrash);
        else if (p1SelectedCharacter == 1 && p2SelectedCharacter == 0) SceneTransitionManager.Instance.FadeToScene(scene_CrashVsDavid);
        else if (p1SelectedCharacter == 1 && p2SelectedCharacter == 1) SceneTransitionManager.Instance.FadeToScene(scene_CrashVsCrash);
    }

    // -------------------------------------------------------
    // Helpers 
    // -------------------------------------------------------

    void SetControllerFocus(Button targetButton)
    {
        if (targetButton == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        targetButton.Select();
    }

    void PlaySelectionVoice(int characterID)
    {
        if (menuAudioSource == null) return;
        AudioClip clip = characterID == 0 ? davidVoiceLine : crashVoiceLine;
        if (clip == null) return;
        menuAudioSource.Stop();
        menuAudioSource.PlayOneShot(clip);
    }
}