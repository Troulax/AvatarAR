using UnityEngine;

public class HumanPawnSelector : MonoBehaviour
{
    [SerializeField] private TurnManager turnManager;

    public void OnPawnSelected(Pawn pawn)
    {
        if (pawn == null) return;
        if (pawn.team != TeamColor.Red) return;

        turnManager.SetHumanSelectedPawn(pawn);
    }
}
