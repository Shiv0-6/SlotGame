using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a flashing / pulsing animation on the win effect panel.
/// Attach this to the WinEffectPanel GameObject alongside an Image component.
/// The panel scales up and glows when activated.
/// 
/// Note: The provided GIF asset (4tXlXs.gif) should be imported as a Texture2D
/// sprite sheet or used with a third-party GIF player. This script provides
/// a code-driven alternative animation that works natively in Unity.
/// </summary>
public class WinAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("How fast the panel pulses (scale in/out) per second.")]
    public float pulseSpeed = 3f;

    [Tooltip("Maximum scale multiplier at pulse peak.")]
    public float pulseMaxScale = 1.15f;

    [Tooltip("Minimum scale multiplier at pulse trough.")]
    public float pulseMinScale = 0.95f;

    [Header("Optional: Sprite Sheet Animation")]
    [Tooltip("Assign individual sprites from the win GIF if imported as a sprite sheet.")]
    public Sprite[] winFrames;

    [Tooltip("Frames per second for the sprite sheet animation.")]
    public float frameRate = 12f;

    // ──────────────────────────────────────────────
    //  Private
    // ──────────────────────────────────────────────

    private RectTransform _rectTransform;
    private Image _image;
    private Coroutine _animCoroutine;
    private Vector3 _originalScale;

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
        _originalScale = _rectTransform.localScale;
    }

    private void OnEnable()
    {
        // Start animations when panel becomes visible
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(PlayWinAnimation());
    }

    private void OnDisable()
    {
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }
        // Reset scale
        if (_rectTransform != null)
            _rectTransform.localScale = _originalScale;
    }

    // ──────────────────────────────────────────────
    //  Animation Coroutine
    // ──────────────────────────────────────────────

    private IEnumerator PlayWinAnimation()
    {
        float frameInterval = (frameRate > 0) ? 1f / frameRate : 0f;
        int frameIndex = 0;
        float frameTimer = 0f;

        while (true) // Loops until OnDisable stops it
        {
            float time = Time.time;

            // ── Pulse scale ──
            float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale,
                          (Mathf.Sin(time * pulseSpeed) + 1f) / 2f);
            _rectTransform.localScale = _originalScale * pulse;

            // ── Sprite sheet frame advance ──
            if (winFrames != null && winFrames.Length > 0 && _image != null)
            {
                frameTimer += Time.deltaTime;
                if (frameTimer >= frameInterval)
                {
                    frameTimer = 0f;
                    frameIndex = (frameIndex + 1) % winFrames.Length;
                    _image.sprite = winFrames[frameIndex];
                }
            }

            yield return null;
        }
    }
}