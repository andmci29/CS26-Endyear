// FighterAudioData.cs
// ScriptableObject — one asset per character.
// Fill in clips as the sound designer delivers them.
// Arrays allow multiple variations per event — system picks randomly for variety.
using UnityEngine;

[CreateAssetMenu(fileName = "FighterAudioData", menuName = "Musical Fighters/Fighter Audio Data")]
public class FighterAudioData : ScriptableObject
{
    [Header("Attack — plays on confirmed hit")]
    public AudioClip[] lightAttackSounds;   // e.g. drum crack, guitar sting, sax note
    public AudioClip[] heavyAttackSounds;   // heavier version
    public AudioClip[] launcherSounds;
    public AudioClip[] specialSounds;

    [Header("Hits — incoming")]
    public AudioClip[] lightHitSounds;
    public AudioClip[] heavyHitSounds;
    public AudioClip[] blockHitSounds;

    [Header("Swing — plays on button press (optional)")]
    public AudioClip[] lightSwingSounds;    // whoosh, wind-up sound — can leave empty
    public AudioClip[] heavySwingSounds;

    [Header("States")]
    public AudioClip jumpSound;
    public AudioClip landSound;             // thud when landing from jump
    public AudioClip knockbackSound;        // played when entering knockback
    public AudioClip koSound;               // played on KO

    [Header("Movement")]
    public AudioClip[] footstepSounds;      // optional — played via animation events

    [Header("Tuning")]
    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0f, 0.15f)]
    [Tooltip("Random pitch variance added per clip — keeps repeated sounds from feeling robotic")]
    public float pitchVariance = 0.08f;
}