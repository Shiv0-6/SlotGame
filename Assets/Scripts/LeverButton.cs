using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Handles the lever pull interaction.
/// Attach this to the lever Image GameObject.
/// When clicked, it plays the pull animation then triggers a spin.
/// Implements IPointerClickHandler for clean UI event handling.
/// </summary>
public class LeverButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Lever Sprites")]
    [Tooltip("Sprite when lever is in idle/up position (slot-machine2.png)")]
    public Sprite leverIdleSprite;

    [Tooltip("Sprite when lever is pulled down (slot-machine3.png)")]
    public Sprite leverPulledSprite;

    [Header("References")]
    public SlotMachine slotMachine;

    [Header("Animation")]
    [Tooltip("How long the lever stays in 'pulled' state before returning up.")]
    public float pullDuration = 0.3f;

    // ──────────────────────────────────────────────
    //  Private
    // ──────────────────────────────────────────────

    private Image _leverImage;
    private bool _isAnimating = false;

    private void Awake()
    {
        _leverImage = GetComponent<Image>();
        if (leverIdleSprite != null)
            _leverImage.sprite = leverIdleSprite;
    }

    // ──────────────────────────────────────────────
    //  Pointer Events
    // ──────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        // Show pulled state immediately on press
        if (!_isAnimating && leverPulledSprite != null)
            _leverImage.sprite = leverPulledSprite;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Revert on release (OnPointerClick fires between down & up)
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isAnimating) return;
        StartCoroutine(PullLeverAndSpin());
    }

    // ──────────────────────────────────────────────
    //  Lever Animation + Spin Trigger
    // ──────────────────────────────────────────────

    private IEnumerator PullLeverAndSpin()
    {
        _isAnimating = true;

        // Show pulled sprite
        if (leverPulledSprite != null)
            _leverImage.sprite = leverPulledSprite;

        yield return new WaitForSeconds(pullDuration);

        // Return to idle
        if (leverIdleSprite != null)
            _leverImage.sprite = leverIdleSprite;

        // Trigger the spin
        slotMachine.OnSpinPressed();

        _isAnimating = false;
    }
}