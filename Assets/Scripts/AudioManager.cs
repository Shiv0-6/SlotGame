using UnityEngine;

/// <summary>
/// Simple AudioManager using a singleton pattern.
/// Plays sound effects for spin start, reel stop, win, and loss.
/// Attach to a persistent GameObject with an AudioSource component.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Singleton
    // ──────────────────────────────────────────────

    public static AudioManager Instance { get; private set; }

    // ──────────────────────────────────────────────
    //  Inspector References
    // ──────────────────────────────────────────────

    [Header("Sound Effects")]
    public AudioClip spinStartClip;    // Plays when reels begin spinning
    public AudioClip reelStopClip;     // Plays each time a reel stops
    public AudioClip winClip;          // Plays on a winning combination
    public AudioClip loseClip;         // Plays on a loss
    public AudioClip nearMissClip;     // Plays on a near-miss (optional)
    public AudioClip coinClip;         // Plays when coins are added (optional)
    public AudioClip leverClip;        // Plays when lever is pulled (optional)

    [Header("Volume")]
    [Range(0f, 1f)]
    public float sfxVolume = 0.8f;

    // ──────────────────────────────────────────────
    //  Private
    // ──────────────────────────────────────────────

    private AudioSource _audioSource;

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes

        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
    }

    // ──────────────────────────────────────────────
    //  Public Play Methods
    // ──────────────────────────────────────────────

    public void PlaySpinStart()   => PlayClip(spinStartClip);
    public void PlayReelStop()    => PlayClip(reelStopClip);
    public void PlayWin()         => PlayClip(winClip);
    public void PlayLose()        => PlayClip(loseClip);
    public void PlayNearMiss()    => PlayClip(nearMissClip);
    public void PlayCoin()        => PlayClip(coinClip);
    public void PlayLever()       => PlayClip(leverClip);

    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        _audioSource.PlayOneShot(clip, sfxVolume);
    }
}