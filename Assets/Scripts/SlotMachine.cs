using System.Collections;
using UnityEngine;

/// <summary>
/// Central game controller for the slot machine.
/// Coordinates reel spins, bet management, balance tracking,
/// bonus features, and communicates results to UIManager via PayoutManager.
/// 
/// Attach this to a persistent "GameController" GameObject in the scene.
/// </summary>
public class SlotMachine : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector References
    // ──────────────────────────────────────────────

    [Header("Reels (assign left → right)")]
    public Reel reelLeft;
    public Reel reelCenter;
    public Reel reelRight;

    [Header("Managers")]
    public PayoutManager payoutManager;
    public UIManager uiManager;

    [Header("Game Settings")]
    [Tooltip("Starting balance for the player.")]
    public int startingBalance = 500;

    [Tooltip("Available bet amounts the player can cycle through.")]
    public int[] betOptions = { 10, 50, 100 };

    [Header("Reel Stagger")]
    [Tooltip("Delay (seconds) between each reel starting to spin.")]
    public float reelStaggerDelay = 0.15f;

    [Tooltip("Delay (seconds) between each reel stopping (left → center → right).")]
    public float reelStopDelay = 0.4f;

    [Header("Bonus Feature")]
    [Tooltip("Symbol that triggers a free-spin bonus when it appears on all 3 reels.")]
    public SymbolData bonusSymbol;

    [Tooltip("Number of free spins awarded by the bonus.")]
    public int freeSpinCount = 3;

    // ──────────────────────────────────────────────
    //  State
    // ──────────────────────────────────────────────

    private int _balance;
    private int _currentBetIndex = 0;
    private bool _isSpinning = false;
    private int _freeSpinsRemaining = 0;

    // ──────────────────────────────────────────────
    //  Properties
    // ──────────────────────────────────────────────

    public int Balance => _balance;
    public int CurrentBet => betOptions[_currentBetIndex];
    public bool HasFreeSpins => _freeSpinsRemaining > 0;

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────

    private void Start()
    {
        _balance = startingBalance;

        // Subscribe to payout events
        payoutManager.OnWin    += HandleWin;
        payoutManager.OnLose   += HandleLose;
        payoutManager.OnNearMiss += HandleNearMiss;

        // Initialise UI
        uiManager.UpdateBalance(_balance);
        uiManager.UpdateBet(CurrentBet);
        uiManager.UpdateFreeSpins(_freeSpinsRemaining);
        uiManager.SetSpinButtonInteractable(true);
    }

    private void OnDestroy()
    {
        // Always unsubscribe events to avoid memory leaks
        if (payoutManager != null)
        {
            payoutManager.OnWin    -= HandleWin;
            payoutManager.OnLose   -= HandleLose;
            payoutManager.OnNearMiss -= HandleNearMiss;
        }
    }

    // ──────────────────────────────────────────────
    //  Public Button Handlers (called from UI buttons)
    // ──────────────────────────────────────────────

    /// <summary>Called when the player presses the SPIN button (or pulls the lever).</summary>
    public void OnSpinPressed()
    {
        if (_isSpinning) return;

        // Free spin: no balance deduction
        if (_freeSpinsRemaining > 0)
        {
            _freeSpinsRemaining--;
            uiManager.UpdateFreeSpins(_freeSpinsRemaining);
            StartCoroutine(SpinSequence());
            return;
        }

        // Check sufficient balance
        if (_balance < CurrentBet)
        {
            uiManager.ShowMessage("Not enough coins!");
            return;
        }

        // Deduct bet
        _balance -= CurrentBet;
        uiManager.UpdateBalance(_balance);
        StartCoroutine(SpinSequence());
    }

    /// <summary>Increase bet to the next option.</summary>
    public void OnBetIncrease()
    {
        if (_isSpinning) return;
        _currentBetIndex = (_currentBetIndex + 1) % betOptions.Length;
        uiManager.UpdateBet(CurrentBet);
    }

    /// <summary>Decrease bet to the previous option.</summary>
    public void OnBetDecrease()
    {
        if (_isSpinning) return;
        _currentBetIndex = (_currentBetIndex - 1 + betOptions.Length) % betOptions.Length;
        uiManager.UpdateBet(CurrentBet);
    }

    // ──────────────────────────────────────────────
    //  Core Spin Sequence
    // ──────────────────────────────────────────────

    /// <summary>
    /// Full spin sequence:
    /// 1. Prepare reels (show strip, hide static slots)
    /// 2. Start each reel with a stagger
    /// 3. Stop reels one-by-one (staggered) 
    /// 4. Evaluate result via PayoutManager
    /// </summary>
    private IEnumerator SpinSequence()
    {
        _isSpinning = true;
        uiManager.SetSpinButtonInteractable(false);
        uiManager.HideMessage();
        uiManager.HideWinEffect();

        // ── Prepare reels (show scrolling strip) ──
        reelLeft.PrepareForSpin();
        reelCenter.PrepareForSpin();
        reelRight.PrepareForSpin();

        // ── Start spinning (staggered) ──
        reelLeft.StartSpin(0f);
        reelCenter.StartSpin(reelStaggerDelay);
        reelRight.StartSpin(reelStaggerDelay * 2f);

        // ── Wait for left reel's full spin duration ──
        float totalSpinTime = reelLeft.spinDuration + reelLeft.decelerateDuration + 0.1f;
        yield return new WaitForSeconds(totalSpinTime);

        // ── Stop reels one-by-one (staggered) ──
        // Left reel has already stopped naturally; center & right get a nudge via ForceStop
        yield return new WaitForSeconds(reelStopDelay);
        // Center reel should stop shortly after (already decelerating); just wait
        yield return new WaitForSeconds(reelStopDelay);
        // Right reel stops last
        yield return new WaitForSeconds(reelStopDelay);

        // Small safety wait to ensure all coroutines finish
        yield return new WaitForSeconds(0.1f);

        // ── Evaluate results ──
        SymbolData r1 = reelLeft.ResultSymbol;
        SymbolData r2 = reelCenter.ResultSymbol;
        SymbolData r3 = reelRight.ResultSymbol;

        if (r1 == null || r2 == null || r3 == null)
        {
            Debug.LogError("[SlotMachine] One or more reel results are null!");
            FinishSpin();
            yield break;
        }

        payoutManager.Evaluate(r1, r2, r3, CurrentBet);

        FinishSpin();
    }

    private void FinishSpin()
    {
        _isSpinning = false;
        uiManager.SetSpinButtonInteractable(true);

        // If out of money, offer to reset
        if (_balance <= 0 && _freeSpinsRemaining == 0)
            uiManager.ShowGameOverPrompt();
    }

    // ──────────────────────────────────────────────
    //  Payout Event Handlers
    // ──────────────────────────────────────────────

    private void HandleWin(int amount, SymbolData winSymbol)
    {
        _balance += amount;
        uiManager.UpdateBalance(_balance);
        uiManager.ShowWinEffect(amount);

        // ── Bonus Feature: Cherry bonus triggers free spins ──
        if (bonusSymbol != null && winSymbol.symbolName == bonusSymbol.symbolName)
        {
            _freeSpinsRemaining += freeSpinCount;
            uiManager.UpdateFreeSpins(_freeSpinsRemaining);
            uiManager.ShowMessage($"BONUS! {freeSpinCount} Free Spins Awarded!");
        }
        else
        {
            uiManager.ShowMessage($"YOU WIN! +{amount} coins!");
        }
    }

    private void HandleLose()
    {
        // Near-miss is handled separately; this covers a full miss
        // UIManager already shows near-miss message if needed
    }

    private void HandleNearMiss(SymbolData nearSymbol)
    {
        uiManager.ShowMessage("So close! Try again!");
    }

    // ──────────────────────────────────────────────
    //  Game Reset
    // ──────────────────────────────────────────────

    /// <summary>Reset the game to starting state (called from Game Over → YES button).</summary>
    public void ResetGame()
    {
        _balance = startingBalance;
        _freeSpinsRemaining = 0;
        _currentBetIndex = 0;

        uiManager.UpdateBalance(_balance);
        uiManager.UpdateBet(CurrentBet);
        uiManager.UpdateFreeSpins(_freeSpinsRemaining);
        uiManager.HideMessage();
        uiManager.HideWinEffect();
        uiManager.SetSpinButtonInteractable(true);

        Debug.Log("[SlotMachine] Game Reset.");
    }
}