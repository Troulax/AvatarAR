using UnityEngine;

public class Pawn : MonoBehaviour
{
    public TeamColor team = TeamColor.Red; 
    public bool HasFinished => safeIndex == -2;
    public int ringIndex = -1;
    public int safeIndex = -1;

    public bool IsInStart => ringIndex == -1 && safeIndex == -1;
    public bool IsOnRing => ringIndex >= 0;
    public bool IsInSafeZone => safeIndex >= 0;

    public bool IsSelectable { get; private set; } = false;

    public void SetSelectable(bool selectable)
    {
        IsSelectable = selectable;             
    }
}
