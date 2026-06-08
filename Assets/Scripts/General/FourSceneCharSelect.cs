using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class FourSceneCharSelect : MonoBehaviour
{
    public TextMeshProUGUI p1Text;
    public TextMeshProUGUI p2Text;

    [Header("Scene Names (Must match Build Settings exactly)")]
    public string scene_AvsA = "A vs A";
    public string scene_AvsB = "A vs B";
    public string scene_BvsA = "B vs A";
    public string scene_BvsB = "B vs B";

    private int p1Choice = 0; // 0 = Character A, 1 = Character B
    private int p2Choice = 0;

    private FighterInputActions actions;

    private void Awake()
    {
        actions = new FighterInputActions();
        actions.Fighter.Enable();
        actions.FighterP2.Enable();
    }

    private void OnDestroy()
    {
        actions.Fighter.Disable();
        actions.FighterP2.Disable();
        actions.Dispose();
    }

    void Update()
    {
        // Player 1 - Left/Right to toggle between 0 and 1
        if (actions.Fighter.Move.WasPressedThisFrame())
        {
            float x = actions.Fighter.Move.ReadValue<Vector2>().x;
            if (x > 0.5f) p1Choice = 1;
            if (x < -0.5f) p1Choice = 0;
        }

        // Player 2 - Left/Right to toggle between 0 and 1
        if (actions.FighterP2.Move.WasPressedThisFrame())
        {
            float x = actions.FighterP2.Move.ReadValue<Vector2>().x;
            if (x > 0.5f) p2Choice = 1;
            if (x < -0.5f) p2Choice = 0;
        }

        // Update Text feedback
        p1Text.text = "P1: " + (p1Choice == 0 ? "Character A" : "Character B");
        p2Text.text = "P2: " + (p2Choice == 0 ? "Character A" : "Character B");

        // Player 1 presses Light Attack to confirm and load the correct match scene
        if (actions.Fighter.LightAttack.WasPressedThisFrame())
        {
            LoadSelectedMatch();
        }
    }

    void LoadSelectedMatch()
    {
        // Disable this script so players can't spam the button during loading
        this.enabled = false;

        if (p1Choice == 0 && p2Choice == 0) SceneManager.LoadScene(scene_AvsA);
        else if (p1Choice == 0 && p2Choice == 1) SceneManager.LoadScene(scene_AvsB);
        else if (p1Choice == 1 && p2Choice == 0) SceneManager.LoadScene(scene_BvsA);
        else if (p1Choice == 1 && p2Choice == 1) SceneManager.LoadScene(scene_BvsB);
    }
}