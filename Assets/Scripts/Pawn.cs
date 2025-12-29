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

    [SerializeField, Tooltip("Debug için: Bu pawn en son hangi turda avatar buff aldı. -1 ise hiç almadı.")]
    private int lastAvatarBuffTurnId = -1;

    // -------------------------
    // AVATAR BUFF STATE (runtime)
    // -------------------------
    [Header("Avatar Buff State (runtime)")]
    [Tooltip("Kyoshi koruması: >0 iken bu pawn capture edilemez.")]
    public int KyoshiProtectionTurnsLeft = 0;

    [Tooltip("Korra (native olmayan): oyun boyunca capture yaparsa tekrar zar hakkı.")]
    public bool KorraExtraRollOnCapture = false;

    [Tooltip("Korra (native): capture yaparsa 1 kere 6 sayılacak ve tur bitecek (extra roll yok).")]
    public bool KorraSixOnCapture = false;

    [Tooltip("Roku (native olmayan): bir sonraki zarda kontrol edilecek (>=4 ise +1 pawn indir).")]
    public bool RokuPendingCheckNextRoll = false;

    public bool TryConsumeAvatarBuff(int currentTurnId)
    {
        if (currentTurnId < 0) return false;

        if (lastAvatarBuffTurnId == currentTurnId)
            return false;

        lastAvatarBuffTurnId = currentTurnId;
        return true;
    }

    public void ResetAvatarBuffLock()
    {
        lastAvatarBuffTurnId = -1;
    }

    public void SetSelectable(bool selectable)
    {
        IsSelectable = selectable;
    }
}
