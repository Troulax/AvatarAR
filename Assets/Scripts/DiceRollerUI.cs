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
    [SerializeField] private int shuffleFrames = 12;
    [SerializeField] private float frameDelay = 0.06f;

    [Header("Gameplay References")]
    [SerializeField] private PawnMover pawnMover;
    [Tooltip("Şimdilik test için Inspector'dan bir pawn ver. Sonra seçim sistemiyle dinamik olacak.")]
    [SerializeField] private Pawn selectedPawn;

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

        if (diceFaceImage == null)
        {
            Debug.LogError("DiceFaceImage is not assigned.");
            return;
        }

        if (pawnMover == null)
        {
            Debug.LogError("PawnMover is not assigned.");
            return;
        }

        if (selectedPawn == null)
        {
            Debug.LogWarning("SelectedPawn is not assigned. Assign a pawn for testing.");
            return;
        }

        StartCoroutine(RollAndMoveCoroutine());
    }

    private IEnumerator RollAndMoveCoroutine()
    {
        CanRoll = false;
        SetRollButtonInteractable(false);

        // Fake rolling: hızlı sprite değişimi
        for (int i = 0; i < shuffleFrames; i++)
        {
            int fakeValue = Random.Range(1, 7);
            SetDiceFace(fakeValue);
            yield return new WaitForSeconds(frameDelay);
        }

        // Final sonuç
        CurrentValue = Random.Range(1, 7);
        SetDiceFace(CurrentValue);

        Debug.Log($"Final Dice Value: {CurrentValue}");

        // Pawn'ı zar kadar ilerlet (tile tile)
        yield return pawnMover.MoveSelectedPawnCoroutine(selectedPawn, CurrentValue);

        // Hareket bitince tekrar zar atılabilir
        CanRoll = true;
        SetRollButtonInteractable(true);
    }

    private void SetDiceFace(int value)
    {
        diceFaceImage.sprite = diceFaceSprites[value - 1];
        diceFaceImage.enabled = true;
    }

    private void SetRollButtonInteractable(bool interactable)
    {
        if (rollButton != null)
            rollButton.interactable = interactable;
    }

    // Seçim sistemi ekleyince burayı çağırarak pawn'u değiştireceksin
    public void SetSelectedPawn(Pawn pawn)
    {
        selectedPawn = pawn;
    }
}
