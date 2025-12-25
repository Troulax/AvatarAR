using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public TeamColor CurrentTurn { get; private set; } = TeamColor.Red;

    public bool CanPlay(TeamColor team) => team == CurrentTurn;

    // Şimdilik test: tur değiştirme yok
    public void EndTurn_NoChange() { }
}
