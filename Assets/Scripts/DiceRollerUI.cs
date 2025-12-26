using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DiceRollerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button rollButton;
    [SerializeField] private Image diceFaceImage;

    [Header("Dice Sprites (Index 0=1 .. 5=6)")]
    [SerializeField] private Sprite[] diceFaceSprites = new Sprite[6];

    [Header("Fake Roll Animation")]
    [SerializeField] private int shuffleFrames = 12;
    [SerializeField] private float frameDelay = 0.06f;

    public bool IsRolling { get; private set; }
    public int CurrentValue { get; private set; }

    public void SetRollButtonInteractable(bool interactable)
    {
        if (rollButton != null) rollButton.interactable = interactable;
    }

    public IEnumerator RollDiceCoroutine(System.Action<int> onResult)
    {
        if (IsRolling) yield break;

        if (diceFaceSprites == null || diceFaceSprites.Length < 6)
        {
            Debug.LogError("DiceFaceSprites must contain 6 sprites.");
            yield break;
        }

        IsRolling = true;
        SetRollButtonInteractable(false);

        for (int i = 0; i < shuffleFrames; i++)
        {
            int fake = Random.Range(1, 7);
            SetDiceFace(fake);
            yield return new WaitForSeconds(frameDelay);
        }

        CurrentValue = Random.Range(1, 7);
        SetDiceFace(CurrentValue);

        onResult?.Invoke(CurrentValue);

        IsRolling = false;
        // Butonun tekrar açılıp açılmayacağına TurnManager karar verecek
    }

    private void SetDiceFace(int value)
    {
        if (diceFaceImage == null) return;
        diceFaceImage.sprite = diceFaceSprites[value - 1];
        diceFaceImage.enabled = true;
    }
}
