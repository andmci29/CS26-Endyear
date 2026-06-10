// FighterAudio.cs
// Attach to each fighter alongside FighterController.
// Called by FighterController at key moments — attacks, hits, states.
// Uses PlayOneShot so sounds can overlap on a single AudioSource.
using UnityEngine;

public class FighterAudio : MonoBehaviour
{
    [Header("References")]
    public AudioSource audioSource;
    public FighterAudioData audioData;

    private void Awake()
    {
        // Auto-find AudioSource on this GameObject if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            Debug.LogWarning($"FighterAudio on {gameObject.name}: no AudioSource found.");
    }

    // -------------------------------------------------------
    // Attack sounds — called when an attack starts
    // -------------------------------------------------------

    public void PlayAttackSound(int attackIndex)
    {
        switch (attackIndex)
        {
            case 0: PlayRandom(audioData.lightAttackSounds); break;
            case 1: PlayRandom(audioData.heavyAttackSounds); break;
            case 2: PlayRandom(audioData.launcherSounds); break;
        }
    }

    public void PlaySpecialSound()
    {
        PlayRandom(audioData.specialSounds);
    }

    // -------------------------------------------------------
    // Hit sounds — called when THIS fighter receives a hit
    // -------------------------------------------------------

    public void PlayHitSound(bool wasBlocking, bool isHeavy)
    {
        if (wasBlocking)
        {
            PlayRandom(audioData.blockHitSounds);
            return;
        }

        if (isHeavy)
            PlayRandom(audioData.heavyHitSounds);
        else
            PlayRandom(audioData.lightHitSounds);
    }

    // -------------------------------------------------------
    // State sounds
    // -------------------------------------------------------

    public void PlayJumpSound() => PlayClip(audioData.jumpSound);
    public void PlayLandSound() => PlayClip(audioData.landSound);
    public void PlayKnockbackSound() => PlayRandom(audioData.lightHitSounds); // reuse hit sounds for knockback grunt
    public void PlayKOSound() => PlayClip(audioData.koSound);

    // -------------------------------------------------------
    // Footsteps — call this from Animation Events on walk clip
    // -------------------------------------------------------

    public void PlayFootstep() => PlayRandom(audioData.footstepSounds);

    // -------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;

        // Filter out any null clips the designer hasn't filled in yet
        var validClips = System.Array.FindAll(clips, c => c != null);
        if (validClips.Length == 0) return;

        AudioClip clip = validClips[Random.Range(0, validClips.Length)];
        PlayClip(clip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null || audioSource == null || audioData == null) return;

        // Slight random pitch so repeated sounds feel natural
        float pitch = 1f + Random.Range(-audioData.pitchVariance, audioData.pitchVariance);
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, audioData.volume);
    }

    public void PlayAttackLandSound(bool isHeavy)
    {
        if (isHeavy)
            PlayRandom(audioData.heavyAttackSounds);
        else
            PlayRandom(audioData.lightAttackSounds);
    }
}