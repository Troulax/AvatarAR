using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DiceRollerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button rollButton;
    [SerializeField] private Image diceFaceImage;

    [Header("Dice Sprites")]
    [Tooltip("Index 0 = face 1, index 5 = face 6")]
    [SerializeField] private Sprite[] diceFaceSprites = new Sprite[6];

    [Header("Fake Roll Animation")]
    [SerializeField] private int shuffleFrames = 22;
    [SerializeField] private float frameDelay = 0.08f;

    public int CurrentValue { get; private set; }
    public bool CanRoll { get; private set; } = true;

    public void Roll()
    {
        if (!CanRoll) return;

        if (diceFaceSprites == null || diceFaceSprites.Length < 6)
        {
            Debug.LogError("DiceFaceSprites array must contain 6 sprites (1–6).");
            return;
        }

        StartCoroutine(FakeRollCoroutine());
    }

    private IEnumerator FakeRollCoroutine()
    {
        CanRoll = false;

        if (rollButton != null)
            rollButton.interactable = false;

        for (int i = 0; i < shuffleFrames; i++)
        {
            int fakeValue = Random.Range(1, 7);
            SetDiceFace(fakeValue);
            yield return new WaitForSeconds(frameDelay);
        }

        CurrentValue = Random.Range(1, 7);
        SetDiceFace(CurrentValue);

        Debug.Log($"Final Dice Value: {CurrentValue}");

        CanRoll = true;

        if (rollButton != null)
            rollButton.interactable = true;
    }

    private void SetDiceFace(int value)
    {
        if (diceFaceImage == null) return;

        diceFaceImage.sprite = diceFaceSprites[value - 1];
        diceFaceImage.enabled = true;
    }
}
