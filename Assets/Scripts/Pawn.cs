using UnityEngine;

public class Pawn : MonoBehaviour
{
    public TeamColor team = TeamColor.Red;
    public bool HasFinished => safeIndex == -2;

    // -1 = start area (board'a girmedi)
    public int ringIndex = -1;

    // safe zone içindeyse 0..3, değilse -1
    public int safeIndex = -1;

    public bool IsInStart => ringIndex == -1 && safeIndex == -1;
    public bool IsOnRing => ringIndex >= 0;
    public bool IsInSafeZone => safeIndex >= 0;
}
