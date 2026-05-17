using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls a single reel: spinning animation, symbol strip, and stopping on a result.
/// On startup, visible slots are shown immediately with random symbols.
/// </summary>
public class Reel : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector References
    // ──────────────────────────────────────────────

    [Header("Symbol Configuration")]
    public List<SymbolData> symbolPool = new List<SymbolData>();

    [Header("Reel Strip Setup")]
    public RectTransform symbolStrip;
    public Image[] visibleSymbolImages = new Image[3]; // 0=Top 1=Middle(result) 2=Bottom
    public float symbolHeight = 150f;

    [Header("Spin Settings")]
    public float spinSpeed = 1200f;
    public float spinDuration = 1.5f;
    public float decelerateDuration = 0.4f;

    // ──────────────────────────────────────────────
    //  Private State
    // ──────────────────────────────────────────────

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
        BuildWeightedPool();
        BuildStripImages();

        // Hide scrolling strip at start — only visible during spin
        if (symbolStrip != null)
            symbolStrip.gameObject.SetActive(false);

        // Immediately show the 3 static slots with random symbols
        SetVisibleSlots(true);
        ShowRandomInitialSymbols();
    }

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────

    public SymbolData ResultSymbol => _resultSymbol;
    public bool IsSpinning => _isSpinning;

    public void StartSpin(float delay = 0f, SymbolData forcedResult = null)
    {
        if (_spinCoroutine != null) StopCoroutine(_spinCoroutine);
        _spinCoroutine = StartCoroutine(SpinRoutine(delay, forcedResult));
    }

    public void ForceStop()
    {
        if (_spinCoroutine != null) StopCoroutine(_spinCoroutine);
        _isSpinning = false;
        SnapToResult(_resultSymbol ?? GetRandomSymbol());
    }

    /// <summary>Called by SlotMachine before each spin — shows strip, hides static slots.</summary>
    public void PrepareForSpin()
    {
        if (symbolStrip != null)
            symbolStrip.gameObject.SetActive(true);

        RandomiseStrip();
        SetVisibleSlots(false);
    }

    // ──────────────────────────────────────────────
    //  Startup Display
    // ──────────────────────────────────────────────

    /// <summary>Fill all 3 visible slots with random symbols at game start.</summary>
    private void ShowRandomInitialSymbols()
    {
        if (visibleSymbolImages == null) return;

        for (int i = 0; i < visibleSymbolImages.Length; i++)
        {
            Image img = visibleSymbolImages[i];
            if (img == null)
            {
                Debug.LogWarning($"[Reel] {gameObject.name}: visibleSymbolImages[{i}] is NULL — check Inspector assignment!");
                continue;
            }

            img.gameObject.SetActive(true);
            img.color = Color.white;
            img.sprite = GetRandomSymbol().sprite;
        }

        // Pre-assign result so it is never null before first spin
        _resultSymbol = GetRandomSymbol();
    }

    // ──────────────────────────────────────────────
    //  Private Helpers
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
    }

    private void BuildStripImages()
    {
        if (symbolStrip == null) return;

        _stripImages = new Image[STRIP_ROWS];
        _stripTotalHeight = STRIP_ROWS * symbolHeight;

        foreach (Transform child in symbolStrip)
            Destroy(child.gameObject);

        for (int i = 0; i < STRIP_ROWS; i++)
        {
            GameObject cell = new GameObject($"StripCell_{i}", typeof(RectTransform), typeof(Image));
            cell.transform.SetParent(symbolStrip, false);

            RectTransform rt = cell.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(symbolStrip.sizeDelta.x, symbolHeight);
            rt.anchoredPosition = new Vector2(0f, -i * symbolHeight);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);

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

    private SymbolData GetRandomSymbol()
    {
        if (_weightedPool != null && _weightedPool.Count > 0)
            return _weightedPool[Random.Range(0, _weightedPool.Count)];
        if (symbolPool != null && symbolPool.Count > 0)
            return symbolPool[0];
        return null;
    }

    // ──────────────────────────────────────────────
    //  Spin Coroutine
    // ──────────────────────────────────────────────

    private IEnumerator SpinRoutine(float delay, SymbolData forcedResult)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        _isSpinning = true;
        _resultSymbol = forcedResult ?? GetRandomSymbol();

        // Phase 1: Full speed
        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            ScrollStrip(spinSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 2: Ease-out
        elapsed = 0f;
        while (elapsed < decelerateDuration)
        {
            float t = elapsed / decelerateDuration;
            float easedSpeed = Mathf.Lerp(spinSpeed, 0f, t * t);
            ScrollStrip(easedSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 3: Snap
        SnapToResult(_resultSymbol);
        _isSpinning = false;
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
            float rawY = -i * symbolHeight + _currentOffset;
            float wrappedY = ((rawY % _stripTotalHeight) + _stripTotalHeight) % _stripTotalHeight;
            _stripImages[i].rectTransform.anchoredPosition = new Vector2(0f, wrappedY - _stripTotalHeight);
        }
    }

    private void SnapToResult(SymbolData result)
    {
        if (result == null) return;

        // Hide scrolling strip
        if (symbolStrip != null)
            symbolStrip.gameObject.SetActive(false);

        SymbolData above = GetRandomSymbol();
        SymbolData below = GetRandomSymbol();

        if (visibleSymbolImages != null)
        {
            for (int i = 0; i < visibleSymbolImages.Length; i++)
            {
                Image img = visibleSymbolImages[i];
                if (img == null) continue;

                img.gameObject.SetActive(true);
                img.color = Color.white;

                switch (i)
                {
                    case 0: img.sprite = above.sprite;  break; // top
                    case 1: img.sprite = result.sprite; break; // MIDDLE = result
                    case 2: img.sprite = below.sprite;  break; // bottom
                }
            }
        }

        _currentOffset = 0f;
    }

    private void SetVisibleSlots(bool active)
    {
        if (visibleSymbolImages == null) return;
        foreach (var img in visibleSymbolImages)
            if (img != null) img.gameObject.SetActive(active);
    }
}