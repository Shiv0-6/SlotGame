using UnityEngine;

/// <summary>
/// ScriptableObject that holds data for a single slot symbol.
/// Create instances via Assets > Create > SlotGame > SymbolData
/// </summary>
[CreateAssetMenu(fileName = "NewSymbol", menuName = "SlotGame/SymbolData")]
public class SymbolData : ScriptableObject
{
    [Header("Symbol Identity")]
    public string symbolName;          // e.g. "Seven", "Cherry", "Bell", "Bar"
    public Sprite sprite;              // The symbol's sprite

    [Header("Payout Settings")]
    [Tooltip("Multiplier applied to the current bet when all 3 reels show this symbol")]
    public int payoutMultiplier = 2;   // e.g. 7 = 10x, Cherry = 2x, Bell = 5x, Bar = 3x

    [Header("Rarity")]
    [Range(1, 10)]
    [Tooltip("Higher weight = appears more often. Lower = rarer.")]
    public int weight = 5;             // Used for weighted RNG
}