using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI elements:
/// - Balance and Bet displays
/// - Spin button interactability
/// - Win effect (flashing panel + coin count)
/// - Game Over popup (YES/NO buttons)
/// - Free spin counter
/// - Message display
/// 
/// All Text fields use TextMeshPro (TMP_Text).
/// </summary>
public class UIManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector References
    // ──────────────────────────────────────────────

    [Header("HUD Text")]
    public TMP_Text balanceText;         // Shows current balance
    public TMP_Text betText;             // Shows current bet
    public TMP_Text messageText;         // Shows win/lose/near-miss messages
    public TMP_Text freeSpinsText;       // Shows free spin counter

    [Header("Spin Button")]
    public Button spinButton;            // The main SPIN / lever button

    [Header("Bet Buttons")]
    public Button betIncreaseButton;     // Right arrow — increase bet
    public Button betDecreaseButton;     // Left arrow — decrease bet

    [Header("Win Effect")]
    public GameObject winEffectPanel;    // Panel shown on win (assign the GIF/animation panel)
    public TMP_Text winAmountText;       // Displays "WIN: +X coins"

    [Header("Game Over Popup")]
    public GameObject gameOverPopup;     // The popup.png panel GameObject
    public Button yesButton;             // YES — restart game
    public Button noButton;              // NO — quit / do nothing

    [Header("References")]
    public SlotMachine slotMachine;      // Assigned in Inspector

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────

    private void Start()
    {
        // Wire up buttons
        spinButton.onClick.AddListener(slotMachine.OnSpinPressed);
        betIncreaseButton.onClick.AddListener(slotMachine.OnBetIncrease);
        betDecreaseButton.onClick.AddListener(slotMachine.OnBetDecrease);

        if (yesButton != null)
            yesButton.onClick.AddListener(OnYesPressed);
        if (noButton != null)
            noButton.onClick.AddListener(OnNoPressed);

        // Initial state
        HideWinEffect();
        HideMessage();
        if (gameOverPopup != null) gameOverPopup.SetActive(false);
        if (freeSpinsText != null) freeSpinsText.gameObject.SetActive(false);
    }

    // ──────────────────────────────────────────────
    //  Public Update Methods
    // ──────────────────────────────────────────────

    /// <summary>Update the balance display.</summary>
    public void UpdateBalance(int balance)
    {
        if (balanceText != null)
            balanceText.text = $"COINS: {balance}";
    }

    /// <summary>Update the current bet display.</summary>
    public void UpdateBet(int bet)
    {
        if (betText != null)
            betText.text = $"BET: {bet}";
    }

    /// <summary>Update the free spins counter display.</summary>
    public void UpdateFreeSpins(int count)
    {
        if (freeSpinsText == null) return;
        if (count > 0)
        {
            freeSpinsText.gameObject.SetActive(true);
            freeSpinsText.text = $"FREE SPINS: {count}";
        }
        else
        {
            freeSpinsText.gameObject.SetActive(false);
        }
    }

    /// <summary>Enable or disable the Spin button.</summary>
    public void SetSpinButtonInteractable(bool interactable)
    {
        if (spinButton != null)
            spinButton.interactable = interactable;
    }

    // ──────────────────────────────────────────────
    //  Message Display
    // ──────────────────────────────────────────────

    public void ShowMessage(string msg)
    {
        if (messageText == null) return;
        messageText.gameObject.SetActive(true);
        messageText.text = msg;
    }

    public void HideMessage()
    {
        if (messageText == null) return;
        messageText.gameObject.SetActive(false);
        messageText.text = "";
    }

    // ──────────────────────────────────────────────
    //  Win Effect
    // ──────────────────────────────────────────────

    public void ShowWinEffect(int amount)
    {
        if (winEffectPanel != null)
            winEffectPanel.SetActive(true);
        if (winAmountText != null)
            winAmountText.text = $"+{amount} COINS!";

        // Auto-hide win effect after 2.5 seconds
        StartCoroutine(HideWinEffectAfterDelay(2.5f));
    }

    public void HideWinEffect()
    {
        if (winEffectPanel != null)
            winEffectPanel.SetActive(false);
        if (winAmountText != null)
            winAmountText.text = "";
    }

    private IEnumerator HideWinEffectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideWinEffect();
    }

    // ──────────────────────────────────────────────
    //  Game Over Popup
    // ──────────────────────────────────────────────

    /// <summary>Show the "Out of coins — play again?" popup.</summary>
    public void ShowGameOverPrompt()
    {
        if (gameOverPopup != null)
            gameOverPopup.SetActive(true);
    }

    private void OnYesPressed()
    {
        if (gameOverPopup != null)
            gameOverPopup.SetActive(false);
        slotMachine.ResetGame();
    }

    private void OnNoPressed()
    {
        if (gameOverPopup != null)
            gameOverPopup.SetActive(false);
        // In a real game you might return to a main menu
        // For the assignment, just hide the popup
        ShowMessage("Thanks for playing!");
    }

    // ──────────────────────────────────────────────
    //  Lever Animation Helper
    // ──────────────────────────────────────────────

    /// <summary>
    /// Animate the lever: swap sprites from idle → pulled → idle.
    /// Call this from a separate LeverButton script on the lever image.
    /// </summary>
    public IEnumerator AnimateLever(Image leverImage, Sprite idleSprite, Sprite pulledSprite)
    {
        leverImage.sprite = pulledSprite;
        yield return new WaitForSeconds(0.25f);
        leverImage.sprite = idleSprite;
    }
}