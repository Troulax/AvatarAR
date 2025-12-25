using System.Collections;
using UnityEngine;

public class PawnMover : MonoBehaviour
{
    [SerializeField] private TilePath tilePath;
    [SerializeField] private float stepDuration = 0.15f;

    public IEnumerator MoveSelectedPawnCoroutine(Pawn pawn, int steps)
    {
        if (pawn == null || tilePath == null) yield break;
        if (tilePath.RingTiles == null || tilePath.RingTiles.Count == 0) yield break;
        if (pawn.HasFinished) yield break;

        TeamPath teamPath = tilePath.GetTeamPath(pawn.team);
        int ringCount = tilePath.RingTiles.Count;

        // 1) START ALANI: sadece 6 ile ring'e çıkar
        if (pawn.IsInStart)
        {
            if (steps != 6) yield break;

            pawn.ringIndex = teamPath.startIndexOnRing;
            yield return MoveTo(tilePath.RingTiles[pawn.ringIndex], pawn);
            yield break;
        }

        // 2) RING: steps kadar ilerle, gerekirse safe zone'a gir
        if (pawn.IsOnRing)
        {
            int entryIndex = (teamPath.startIndexOnRing - 2 + ringCount) % ringCount;

            // Ring'de safe zone girişine kaç adım var?
            int ringStepsToEntry;
            if (pawn.ringIndex <= entryIndex)
                ringStepsToEntry = entryIndex - pawn.ringIndex;
            else
                ringStepsToEntry = ringCount - pawn.ringIndex + entryIndex;

            // Safe zone + finish toplam uzunluk
            int safeAndFinishLength = teamPath.safeZoneTiles.Length + 1;

            // Toplam kalan yol
            int totalRemainingPath = ringStepsToEntry + safeAndFinishLength;

            // Zar toplam yolu aşıyorsa → hiç hareket etme
            if (steps > totalRemainingPath)
                yield break;

            // Adım adım ilerle
            for (int i = 0; i < steps; i++)
            {
                // Ring'de ve entry'e gelindiyse → safe zone'a gir
                if (pawn.ringIndex == entryIndex)
                {
                    pawn.ringIndex = -1;
                    pawn.safeIndex = 0;
                    yield return MoveTo(teamPath.safeZoneTiles[0], pawn);
                    continue;
                }

                // Ring içinde normal ilerleme
                pawn.ringIndex = (pawn.ringIndex + 1) % ringCount;
                yield return MoveTo(tilePath.RingTiles[pawn.ringIndex], pawn);
            }
        }

        // 3) SAFE ZONE: fazla atarsa ilerlemez, finish de bir adım sayılır (exact olmalı)
        if (pawn.IsInSafeZone)
        {
            int finishStepIndex = teamPath.safeZoneTiles.Length; // safe 0..3 ise finishStepIndex=4
            int remainingToFinish = finishStepIndex - pawn.safeIndex;

            // Fazla atarsa hiç hareket etmez
            if (steps > remainingToFinish)
                yield break;

            for (int i = 0; i < steps; i++)
            {
                pawn.safeIndex++;

                // Hala safe tile içindeyse
                if (pawn.safeIndex < teamPath.safeZoneTiles.Length)
                {
                    yield return MoveTo(teamPath.safeZoneTiles[pawn.safeIndex], pawn);
                }
                // Finish adımı
                else if (pawn.safeIndex == finishStepIndex)
                {
                    yield return MoveTo(teamPath.finishTile, pawn);
                    pawn.safeIndex = -2; // finished
                    Debug.Log($"{pawn.name} FINISHED!");
                    yield break;
                }
            }
        }
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
}
