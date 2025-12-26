using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PawnSelection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera rayCamera;          // AR Camera / Main Camera
    [SerializeField] private TurnManager turnManager;   // GameManager üzerindeki TurnManager

    private Pawn selectedPawn;

    void Update()
    {
        if (rayCamera == null || turnManager == null) return;

        Vector2? screenPos = GetPointerDownPosition();
        if (screenPos == null) return;

        // UI üstüne tıklanıyorsa pawn seçme (butonla çakışmasın)
        if (IsPointerOverUI(screenPos.Value))
            return;

        Ray ray = rayCamera.ScreenPointToRay(screenPos.Value);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Pawn pawn = hit.collider.GetComponentInParent<Pawn>();
            if (pawn == null) return;

            // Sadece RED human seçebilsin
            if (!turnManager.IsHumanTurn) return;
            if (pawn.team != TeamColor.Red) return;

            selectedPawn = pawn;
            turnManager.SetHumanSelectedPawn(selectedPawn);

            Debug.Log("Selected pawn: " + selectedPawn.name);
        }
    }

    private Vector2? GetPointerDownPosition()
    {
        // Mobil dokunma
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        // Editor / PC mouse
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            return Mouse.current.position.ReadValue();
        }

        return null;
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        var ped = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        return results.Count > 0;
    }
}
