using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PawnMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TilePath tilePath;
    [SerializeField] private float stepDuration = 0.15f;

    [Header("Managers (Offer System)")]
    [Tooltip("GameManager üstündeki TurnManager referansı (TurnId için).")]
    [SerializeField] private TurnManager turnManager;
    [Tooltip("GameManager üstündeki OfferManager referansı (offer tile konfigleri için).")]
    [SerializeField] private OfferManager offerManager;

    [Header("Safe Star Tiles (Ring)")]
    [Tooltip("Ring üzerindeki güvenli yıldız tile objelerini buraya sürükle (toplam 8 adet). Bu tile'lara inince capture olmaz.")]
    [SerializeField] private List<Transform> safeStarRingTiles = new();

    [Header("Capture Animation")]
    [SerializeField] private float capturePopHeight = 0.12f;
    [SerializeField] private float capturePopTime = 0.12f;
    [SerializeField] private float captureMoveToStartTime = 0.35f;
    [SerializeField] private float captureShrinkScale = 0.65f;

    [Header("Capture Delay")]
    [Tooltip("Capture animasyonu bittikten sonra tur devam etmeden önce beklenecek süre (sn).")]
    [SerializeField] private float delayAfterCaptureSeconds = 1f;

    // ✅ Capture event (TurnManager Korra/Roku için dinleyecek)
    public System.Action<Pawn, Pawn> OnCaptureHappened; // (moverPawn, capturedPawn)

    // Pawn.cs'e dokunmadan "hangi tile anchor'da duruyor?" bilgisini tutuyoruz
    private readonly Dictionary<Pawn, TileAnchor> currentAnchorByPawn = new();

    // Start pozisyon cache
    private readonly Dictionary<Pawn, Vector3> startPosByPawn = new();
    private readonly Dictionary<Pawn, Quaternion> startRotByPawn = new();

    // Safe star ringIndex cache
    private readonly HashSet<int> safeStarRingIndices = new();

    private void Awake()
    {
        CacheAllPawnStartPositions();
        RebuildSafeStarIndexCache();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildSafeStarIndexCache();
    }
#endif

    private void CacheAllPawnStartPositions()
    {
        var pawns = FindObjectsOfType<Pawn>(true);
        foreach (var p in pawns)
        {
            if (p == null) continue;
            if (startPosByPawn.ContainsKey(p)) continue;

            startPosByPawn[p] = p.transform.position;
            startRotByPawn[p] = p.transform.rotation;
        }
    }

    private void RebuildSafeStarIndexCache()
    {
        safeStarRingIndices.Clear();

        if (tilePath == null) return;

        if (tilePath.RingTiles == null || tilePath.RingTiles.Count == 0)
            return;

        for (int i = 0; i < safeStarRingTiles.Count; i++)
        {
            Transform starTf = safeStarRingTiles[i];
            if (starTf == null) continue;

            int idx = tilePath.RingTiles.IndexOf(starTf);

            if (idx < 0)
            {
                string starName = starTf.name;

                for (int r = 0; r < tilePath.RingTiles.Count; r++)
                {
                    Transform ringTf = tilePath.RingTiles[r];
                    if (ringTf == null) continue;

                    if (ringTf.name == starName) { idx = r; break; }

                    Transform child = ringTf.Find(starName);
                    if (child != null) { idx = r; break; }

                    if (starTf.parent != null && ringTf.name == starTf.parent.name) { idx = r; break; }
                }
            }

            if (idx >= 0) safeStarRingIndices.Add(idx);
            else Debug.LogWarning($"[PawnMover] safeStarRingTiles içindeki '{starTf.name}', tilePath.RingTiles ile eşleşmedi. RingTiles listendeki tile objeleri ile Inspector'da seçtiklerin aynı değil.");
        }
    }

    private bool IsSafeStarIndex(int ringIndex) => safeStarRingIndices.Contains(ringIndex);

    public IEnumerator MoveSelectedPawnCoroutine(Pawn pawn, int steps)
    {
        if (pawn == null || tilePath == null) yield break;
        if (tilePath.RingTiles == null || tilePath.RingTiles.Count == 0) yield break;
        if (pawn.HasFinished) yield break;

        if (!startPosByPawn.ContainsKey(pawn))
        {
            startPosByPawn[pawn] = pawn.transform.position;
            startRotByPawn[pawn] = pawn.transform.rotation;
        }

        if (safeStarRingIndices.Count == 0 && safeStarRingTiles.Count > 0)
            RebuildSafeStarIndexCache();

        TeamPath teamPath = tilePath.GetTeamPath(pawn.team);
        int ringCount = tilePath.RingTiles.Count;

        UnregisterPawnFromAnchor(pawn);

        int remainingSteps = steps;
        Transform finalTile = null;

        // START -> RING (6)
        if (pawn.IsInStart)
        {
            if (steps != 6) yield break;

            pawn.ringIndex = teamPath.startIndexOnRing;
            finalTile = tilePath.RingTiles[pawn.ringIndex];

            yield return MoveTo(finalTile, pawn);

            yield return TryCaptureOnFinalTileAnimated(pawn);

            // ✅ Offer check (hamle sonunda)
            TryTriggerOfferOnFinalTile(pawn, finalTile);

            RegisterPawnOnFinalTile(pawn, finalTile);
            yield break;
        }

        // RING
        if (pawn.IsOnRing)
        {
            int entryIndex = (teamPath.startIndexOnRing - 2 + ringCount) % ringCount;

            int ringStepsToEntry;
            if (pawn.ringIndex <= entryIndex) ringStepsToEntry = entryIndex - pawn.ringIndex;
            else ringStepsToEntry = ringCount - pawn.ringIndex + entryIndex;

            int safeAndFinishLength = teamPath.safeZoneTiles.Length + 1;
            int totalRemainingPath = ringStepsToEntry + safeAndFinishLength;

            if (steps > totalRemainingPath)
                yield break;

            while (remainingSteps > 0 && pawn.IsOnRing)
            {
                if (pawn.ringIndex == entryIndex)
                {
                    pawn.ringIndex = -1;
                    pawn.safeIndex = 0;

                    finalTile = teamPath.safeZoneTiles[0];
                    yield return MoveTo(finalTile, pawn);

                    remainingSteps--;
                    break;
                }

                pawn.ringIndex = (pawn.ringIndex + 1) % ringCount;
                finalTile = tilePath.RingTiles[pawn.ringIndex];

                yield return MoveTo(finalTile, pawn);
                remainingSteps--;
            }
        }

        // SAFE ZONE (capture yok / offer yok varsayımı)
        if (pawn.IsInSafeZone)
        {
            int finishStepIndex = teamPath.safeZoneTiles.Length;
            int remainingToFinish = finishStepIndex - pawn.safeIndex;

            if (remainingSteps > remainingToFinish)
                yield break;

            while (remainingSteps > 0)
            {
                pawn.safeIndex++;

                if (pawn.safeIndex < teamPath.safeZoneTiles.Length)
                {
                    finalTile = teamPath.safeZoneTiles[pawn.safeIndex];
                    yield return MoveTo(finalTile, pawn);
                }
                else if (pawn.safeIndex == finishStepIndex)
                {
                    finalTile = teamPath.finishTile;
                    yield return MoveTo(finalTile, pawn);

                    pawn.safeIndex = -2;
                    Debug.Log($"{pawn.name} FINISHED!");

                    RegisterPawnOnFinalTile(pawn, finalTile);
                    yield break;
                }

                remainingSteps--;
            }
        }

        // Ring'de bitti ise capture dene
        yield return TryCaptureOnFinalTileAnimated(pawn);

        // ✅ Offer check (hamle sonunda)
        TryTriggerOfferOnFinalTile(pawn, finalTile);

        RegisterPawnOnFinalTile(pawn, finalTile);
    }

    private IEnumerator MoveTo(Transform target, Pawn pawn)
    {
        if (target == null || pawn == null) yield break;

        Vector3 start = pawn.transform.position;
        Vector3 end = target.position;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, stepDuration);
            pawn.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        pawn.transform.position = end;
    }

    private IEnumerator TryCaptureOnFinalTileAnimated(Pawn moverPawn)
    {
        if (moverPawn == null) yield break;
        if (!moverPawn.IsOnRing) yield break;

        int targetRingIndex = moverPawn.ringIndex;

        if (IsSafeStarIndex(targetRingIndex))
            yield break;

        var allPawns = FindObjectsOfType<Pawn>(true);
        for (int i = 0; i < allPawns.Length; i++)
        {
            Pawn other = allPawns[i];
            if (other == null) continue;
            if (other == moverPawn) continue;

            if (other.team == moverPawn.team) continue;
            if (!other.IsOnRing) continue;
            if (other.HasFinished) continue;
            if (other.ringIndex != targetRingIndex) continue;

            // ✅ Kyoshi koruması varsa capture iptal
            if (other.KyoshiProtectionTurnsLeft > 0)
            {
                Debug.Log($"[Kyoshi] {other.name} is protected. Capture prevented.");
                yield break;
            }

            yield return CapturePawnToStartAnimated(other);

            // ✅ Capture event
            OnCaptureHappened?.Invoke(moverPawn, other);

            if (delayAfterCaptureSeconds > 0f)
                yield return new WaitForSeconds(delayAfterCaptureSeconds);

            yield break;
        }
    }

    private IEnumerator CapturePawnToStartAnimated(Pawn capturedPawn)
    {
        if (capturedPawn == null) yield break;

        UnregisterPawnFromAnchor(capturedPawn);

        // ✅ Capture reset: start'a dönünce avatar buff hakkı sıfırlansın
        capturedPawn.ResetAvatarBuffLock();

        capturedPawn.ringIndex = -1;
        capturedPawn.safeIndex = -1;

        if (!startPosByPawn.TryGetValue(capturedPawn, out var startPos))
        {
            startPos = capturedPawn.transform.position;
            startPosByPawn[capturedPawn] = startPos;
        }

        Vector3 startWorldPos = capturedPawn.transform.position;
        Vector3 popPos = startWorldPos + Vector3.up * capturePopHeight;

        Vector3 originalScale = capturedPawn.transform.localScale;
        Vector3 shrinkScale = originalScale * Mathf.Max(0.05f, captureShrinkScale);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, capturePopTime);
            capturedPawn.transform.position = Vector3.Lerp(startWorldPos, popPos, t);
            capturedPawn.transform.localScale = Vector3.Lerp(originalScale, shrinkScale, t);
            yield return null;
        }

        Vector3 endPos = startPos + Vector3.up * capturePopHeight;

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, captureMoveToStartTime);
            capturedPawn.transform.position = Vector3.Lerp(popPos, endPos, t);
            yield return null;
        }

        capturedPawn.transform.position = startPos;

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, capturePopTime);
            capturedPawn.transform.localScale = Vector3.Lerp(shrinkScale, originalScale, t);
            yield return null;
        }

        capturedPawn.transform.localScale = originalScale;
        Debug.Log($"CAPTURED (animated): {capturedPawn.name} sent back to start.");
    }

    private static ElementType TeamToElementType(TeamColor team)
    {
        return team switch
        {
            TeamColor.Red => ElementType.Fire,
            TeamColor.Green => ElementType.Earth,
            TeamColor.Blue => ElementType.Water,
            TeamColor.Yellow => ElementType.Air,
            _ => ElementType.Fire
        };
    }

    private static bool IsPawnNativeToOffer(Pawn pawn, AvatarOfferConfig offer)
    {
        if (pawn == null || offer == null) return false;
        return TeamToElementType(pawn.team) == offer.element;
    }

    // ✅ Offer trigger (hamle sonunda çalışır)
    private void TryTriggerOfferOnFinalTile(Pawn pawn, Transform finalTile)
    {
        if (pawn == null || finalTile == null) return;
        if (offerManager == null || turnManager == null) return;

        if (!offerManager.TryGetOffer(finalTile, out var offer))
            return;

        // Bu pawn bu takım turunda zaten offer aldı mı?
        bool consumed = pawn.TryConsumeAvatarBuff(turnManager.TurnId);
        if (!consumed)
        {
            Debug.Log($"[Offer] {pawn.name} already took an avatar buff this turn (TurnId={turnManager.TurnId}).");
            return;
        }

        bool isNative = IsPawnNativeToOffer(pawn, offer);

        Debug.Log($"[Offer] {pawn.name} triggered {offer.avatar} ({offer.element}) | native={isNative} | tile='{finalTile.name}' | TurnId={turnManager.TurnId}");

        switch (offer.avatar)
        {
            case AvatarType.Kyoshi:
            {
                int turns = isNative ? Random.Range(2, 6) : Random.Range(1, 4); // 2-5 / 1-3
                pawn.KyoshiProtectionTurnsLeft = Mathf.Max(pawn.KyoshiProtectionTurnsLeft, turns);
                Debug.Log($"[Kyoshi] {pawn.name} protection set to {pawn.KyoshiProtectionTurnsLeft} turns.");
                break;
            }

            case AvatarType.Roku:
            {
                if (isNative)
                {
                    // Native: 1 pawn indir + tur geçecek
                    turnManager.QueueRokuNativeDeploy(pawn.team);
                }
                else
                {
                    // Non-native: next roll check
                    pawn.RokuPendingCheckNextRoll = true;
                    Debug.Log($"[Roku] {pawn.name} pending next roll check (>=4 => extra deploy).");
                }
                break;
            }

            case AvatarType.Aang:
            {
                // Glider: ekstra ilerleme (capture final tile’da zaten çalışıyor)
                int extraSteps = isNative ? Random.Range(5, 9) : Random.Range(3, 7); // 5-8 / 3-6
                turnManager.QueueAangExtraMove(pawn, extraSteps);
                Debug.Log($"[Aang] queued extra move: {pawn.name} +{extraSteps} steps.");
                break;
            }

            case AvatarType.Korra:
            {
                if (isNative)
                {
                    pawn.KorraSixOnCapture = true;
                    Debug.Log($"[Korra] {pawn.name} will count as 6 on capture (then turn ends).");
                }
                else
                {
                    pawn.KorraExtraRollOnCapture = true;
                    Debug.Log($"[Korra] {pawn.name} will grant extra roll on capture (game-long).");
                }
                break;
            }
        }
    }

    private void RegisterPawnOnFinalTile(Pawn pawn, Transform finalTileTransform)
    {
        if (pawn == null) return;
        if (finalTileTransform == null) return;

        TileAnchor newAnchor = finalTileTransform.GetComponent<TileAnchor>();
        if (newAnchor == null) return;

        if (currentAnchorByPawn.TryGetValue(pawn, out TileAnchor oldAnchor))
        {
            if (oldAnchor != null && oldAnchor != newAnchor)
                oldAnchor.Remove(pawn.transform);
        }

        newAnchor.Add(pawn.transform);
        currentAnchorByPawn[pawn] = newAnchor;
    }

    public void UnregisterPawnFromAnchor(Pawn pawn)
    {
        if (pawn == null) return;

        if (currentAnchorByPawn.TryGetValue(pawn, out TileAnchor oldAnchor))
        {
            if (oldAnchor != null)
                oldAnchor.Remove(pawn.transform);
        }

        currentAnchorByPawn.Remove(pawn);
    }
}
