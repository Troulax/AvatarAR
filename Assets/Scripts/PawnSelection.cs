using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PawnSelection : MonoBehaviour
{
    [SerializeField] private Camera rayCamera;          // AR Camera / Main Camera
    [SerializeField] private DiceRollerUI diceRollerUI; // Dice script

    private Pawn selected;

    void Update()
    {
        if (rayCamera == null || diceRollerUI == null) return;

        // Dokunma veya mouse tıklaması yakala (Input System)
        Vector2? screenPos = GetPointerDownPosition();
        if (screenPos == null) return;

        // UI üstüne tıklanıyorsa pawn seçme (çakışma olmasın)
        if (IsPointerOverUI(screenPos.Value))
            return;

        Ray ray = rayCamera.ScreenPointToRay(screenPos.Value);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Pawn pawn = hit.collider.GetComponentInParent<Pawn>();
            if (pawn != null)
            {
                selected = pawn;
                diceRollerUI.SetSelectedPawn(selected);
                Debug.Log("Selected pawn: " + pawn.name);
            }
        }
    }

    private Vector2? GetPointerDownPosition()
    {
        // Mobil dokunma
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        // Editor / PC mouse
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return Mouse.current.position.ReadValue();

        return null;
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        var ped = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        return results.Count > 0;
    }
}
