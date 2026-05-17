using UnityEngine;

/// <summary>
/// Handles all win/loss evaluation and payout calculation.
/// Checks whether all reels show the same symbol (jackpot condition),
/// calculates the payout, and raises events for the UI to react to.
/// </summary>
public class PayoutManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Events (subscribed by SlotMachine & UIManager)
    // ──────────────────────────────────────────────

    /// <summary>Fired when the player wins. Passes the amount won.</summary>
    public System.Action<int, SymbolData> OnWin;

    /// <summary>Fired when the player loses.</summary>
    public System.Action OnLose;

    /// <summary>Fired when a bonus / near-miss is detected (2 out of 3 match).</summary>
    public System.Action<SymbolData> OnNearMiss;

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Evaluate the three reel results against the current bet.
    /// Fires OnWin, OnLose, or OnNearMiss accordingly.
    /// Returns the payout amount (0 if no win).
    /// </summary>
    public int Evaluate(SymbolData r1, SymbolData r2, SymbolData r3, int betAmount)
    {
        // ── Jackpot: all three match ──
        if (r1.symbolName == r2.symbolName && r2.symbolName == r3.symbolName)
        {
            int payout = betAmount * r1.payoutMultiplier;
            Debug.Log($"[PayoutManager] WIN! Symbol={r1.symbolName} | Bet={betAmount} | Payout={payout}");
            OnWin?.Invoke(payout, r1);
            return payout;
        }

        // ── Near-miss: two of three match ──
        if (r1.symbolName == r2.symbolName || r2.symbolName == r3.symbolName || r1.symbolName == r3.symbolName)
        {
            SymbolData nearSymbol = (r1.symbolName == r2.symbolName) ? r1
                                  : (r2.symbolName == r3.symbolName) ? r2 : r1;
            Debug.Log($"[PayoutManager] Near-miss! Symbol={nearSymbol.symbolName}");
            OnNearMiss?.Invoke(nearSymbol);
            OnLose?.Invoke();
            return 0;
        }

        // ── Total miss ──
        Debug.Log($"[PayoutManager] LOSE | {r1.symbolName} | {r2.symbolName} | {r3.symbolName}");
        OnLose?.Invoke();
        return 0;
    }

    /// <summary>
    /// Returns a human-readable paytable string for display in the UI.
    /// </summary>
    public static string GetPaytableText()
    {
        return  "PAYTABLE\n" +
                "🎰 Seven  × Seven  × Seven  = 10× Bet\n" +
                "🔔 Bell   × Bell   × Bell   = 5×  Bet\n" +
                "▬  Bar    × Bar    × Bar    = 3×  Bet\n" +
                "🍒 Cherry × Cherry × Cherry = 2×  Bet";
    }
}