using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls a single reel: spinning animation, symbol strip, and stopping on a result.
/// 
/// IMPORTANT SETUP:
/// - Attach this script to Reel_Left / Reel_Center / Reel_Right
/// - Each reel must have 3 child Image objects named EXACTLY:
///     "Slot_Top", "Slot_Middle", "Slot_Bottom"
/// - Each reel must have a child RectTransform named "SymbolStrip"
/// - Slot images are found automatically by name — no manual Inspector dragging needed
/// </summary>
public class Reel : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector Fields
    // ──────────────────────────────────────────────

    [Header("Symbol Configuration")]
    [Tooltip("Drag all 4 SymbolData assets here")]
    public List<SymbolData> symbolPool = new List<SymbolData>();

    [Header("Spin Settings")]
    public float symbolHeight   = 150f;
    public float spinSpeed      = 1200f;
    public float spinDuration   = 1.5f;
    public float decelerateDuration = 0.4f;

    // ──────────────────────────────────────────────
    //  Private — found automatically at runtime
    // ──────────────────────────────────────────────

    private RectTransform _symbolStrip;   // The scrolling strip container
    private Image _slotTop;               // Top display image
    private Image _slotMiddle;            // Middle display image  ← the result
    private Image _slotBottom;            // Bottom display image

    private bool _isSpinning = false;
    private SymbolData _resultSymbol;
    private List<SymbolData> _weightedPool;
    private Coroutine _spinCoroutine;

    private const int STRIP_ROWS = 8;
    private Image[] _stripImages;
    private float _stripTotalHeight;
    private float _currentOffset = 0f;

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        // Step 1: Find all children automatically by name
        FindChildrenByName();

        // Step 2: Build weighted symbol pool
        BuildWeightedPool();

        // Step 3: Build the scrolling strip (hidden at start)
        BuildStripImages();

        // Step 4: Show the 3 static slots with random symbols
        ShowInitialSymbols();
    }

    // ──────────────────────────────────────────────
    //  Auto-find Children (the key fix)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Finds SymbolStrip, Slot_Top, Slot_Middle, Slot_Bottom by searching
    /// all children of this reel. No Inspector dragging required.
    /// </summary>
    private void FindChildrenByName()
    {
        // Search all children (including inactive ones)
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            switch (child.name)
            {
                case "SymbolStrip":
                    _symbolStrip = child.GetComponent<RectTransform>();
                    break;
                case "Slot_Top":
                    _slotTop = child.GetComponent<Image>();
                    break;
                case "Slot_Middle":
                    _slotMiddle = child.GetComponent<Image>();
                    break;
                case "Slot_Bottom":
                    _slotBottom = child.GetComponent<Image>();
                    break;
            }
        }

        // Log clear errors if anything is missing
        if (_symbolStrip == null) Debug.LogError($"[Reel] {name}: Could not find child named 'SymbolStrip'");
        if (_slotTop    == null) Debug.LogError($"[Reel] {name}: Could not find child named 'Slot_Top'");
        if (_slotMiddle == null) Debug.LogError($"[Reel] {name}: Could not find child named 'Slot_Middle'");
        if (_slotBottom == null) Debug.LogError($"[Reel] {name}: Could not find child named 'Slot_Bottom'");
    }

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────

    public SymbolData ResultSymbol => _resultSymbol;
    public bool IsSpinning => _isSpinning;

    /// <summary>Begin spinning the reel after an optional delay.</summary>
    public void StartSpin(float delay = 0f, SymbolData forcedResult = null)
    {
        if (_spinCoroutine != null) StopCoroutine(_spinCoroutine);
        _spinCoroutine = StartCoroutine(SpinRoutine(delay, forcedResult));
    }

    /// <summary>Called by SlotMachine just before spinning starts.</summary>
    public void PrepareForSpin()
    {
        if (_symbolStrip != null)
            _symbolStrip.gameObject.SetActive(true);

        RandomiseStrip();
        SetStaticSlotsVisible(false);
    }

    /// <summary>Force-stop the reel immediately.</summary>
    public void ForceStop()
    {
        if (_spinCoroutine != null) StopCoroutine(_spinCoroutine);
        _isSpinning = false;
        SnapToResult(_resultSymbol ?? GetRandomSymbol());
    }

    // ──────────────────────────────────────────────
    //  Initial Display
    // ──────────────────────────────────────────────

    private void ShowInitialSymbols()
    {
        // Hide strip — not needed at start
        if (_symbolStrip != null)
            _symbolStrip.gameObject.SetActive(false);

        // Show and fill each visible slot
        SetSlot(_slotTop,    GetRandomSymbol(), true);
        SetSlot(_slotMiddle, GetRandomSymbol(), true);
        SetSlot(_slotBottom, GetRandomSymbol(), true);

        // Pre-assign result so it is never null before first spin
        _resultSymbol = GetRandomSymbol();
    }

    /// <summary>Helper: sets a single slot's sprite, colour, and active state.</summary>
    private void SetSlot(Image slot, SymbolData symbol, bool active)
    {
        if (slot == null || symbol == null) return;
        slot.gameObject.SetActive(active);
        slot.color = Color.white;
        slot.sprite = symbol.sprite;
    }

    // ──────────────────────────────────────────────
    //  Weighted RNG
    // ──────────────────────────────────────────────

    private void BuildWeightedPool()
    {
        _weightedPool = new List<SymbolData>();
        foreach (var sym in symbolPool)
        {
            if (sym == null) continue;
            for (int i = 0; i < sym.weight; i++)
                _weightedPool.Add(sym);
        }

        if (_weightedPool.Count == 0)
            Debug.LogError($"[Reel] {name}: Symbol pool is empty! Assign SymbolData assets in Inspector.");
    }

    private SymbolData GetRandomSymbol()
    {
        if (_weightedPool != null && _weightedPool.Count > 0)
            return _weightedPool[Random.Range(0, _weightedPool.Count)];
        return symbolPool != null && symbolPool.Count > 0 ? symbolPool[0] : null;
    }

    // ──────────────────────────────────────────────
    //  Scrolling Strip
    // ──────────────────────────────────────────────

    private void BuildStripImages()
    {
        if (_symbolStrip == null) return;

        _stripImages = new Image[STRIP_ROWS];
        _stripTotalHeight = STRIP_ROWS * symbolHeight;

        // Clear any existing strip children
        for (int i = _symbolStrip.childCount - 1; i >= 0; i--)
            Destroy(_symbolStrip.GetChild(i).gameObject);

        for (int i = 0; i < STRIP_ROWS; i++)
        {
            GameObject cell = new GameObject($"StripCell_{i}", typeof(RectTransform), typeof(Image));
            cell.transform.SetParent(_symbolStrip, false);

            RectTransform rt = cell.GetComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(_symbolStrip.sizeDelta.x, symbolHeight);
            rt.anchoredPosition = new Vector2(0f, -i * symbolHeight);
            rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);

            Image img = cell.GetComponent<Image>();
            img.preserveAspect = true;
            _stripImages[i] = img;
        }
    }

    private void RandomiseStrip()
    {
        if (_stripImages == null) return;
        foreach (var img in _stripImages)
            if (img != null) img.sprite = GetRandomSymbol().sprite;
    }

    private void ScrollStrip(float amount)
    {
        if (_stripImages == null) return;

        _currentOffset += amount;
        if (_currentOffset >= _stripTotalHeight)
            _currentOffset -= _stripTotalHeight;

        for (int i = 0; i < STRIP_ROWS; i++)
        {
            if (_stripImages[i] == null) continue;
            float rawY     = -i * symbolHeight + _currentOffset;
            float wrappedY = ((rawY % _stripTotalHeight) + _stripTotalHeight) % _stripTotalHeight;
            _stripImages[i].rectTransform.anchoredPosition = new Vector2(0f, wrappedY - _stripTotalHeight);
        }
    }

    // ──────────────────────────────────────────────
    //  Spin Coroutine
    // ──────────────────────────────────────────────

    private IEnumerator SpinRoutine(float delay, SymbolData forcedResult)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        _isSpinning  = true;
        _resultSymbol = forcedResult ?? GetRandomSymbol();

        // Phase 1 — Full speed
        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            ScrollStrip(spinSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 2 — Ease-out deceleration
        elapsed = 0f;
        while (elapsed < decelerateDuration)
        {
            float t          = elapsed / decelerateDuration;
            float easedSpeed = Mathf.Lerp(spinSpeed, 0f, t * t);
            ScrollStrip(easedSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 3 — Snap to result
        SnapToResult(_resultSymbol);
        _isSpinning = false;
    }

    // ──────────────────────────────────────────────
    //  Snap To Result
    // ──────────────────────────────────────────────

    private void SnapToResult(SymbolData result)
    {
        if (result == null) return;

        // Hide the scrolling strip
        if (_symbolStrip != null)
            _symbolStrip.gameObject.SetActive(false);

        // Show static slots: top & bottom get random neighbours, middle gets the RESULT
        SetSlot(_slotTop,    GetRandomSymbol(), true);
        SetSlot(_slotMiddle, result,            true);  // ← actual spin result
        SetSlot(_slotBottom, GetRandomSymbol(), true);

        _currentOffset = 0f;
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private void SetStaticSlotsVisible(bool active)
    {
        if (_slotTop    != null) _slotTop.gameObject.SetActive(active);
        if (_slotMiddle != null) _slotMiddle.gameObject.SetActive(active);
        if (_slotBottom != null) _slotBottom.gameObject.SetActive(active);
    }
}