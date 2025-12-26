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

        int remainingSteps = steps;

        if (pawn.IsInStart)
        {
            if (steps != 6) yield break;

            pawn.ringIndex = teamPath.startIndexOnRing;
            yield return MoveTo(tilePath.RingTiles[pawn.ringIndex], pawn);
            yield break;
        }

        if (pawn.IsOnRing)
        {
            int entryIndex = (teamPath.startIndexOnRing - 2 + ringCount) % ringCount;

            int ringStepsToEntry;
            if (pawn.ringIndex <= entryIndex)
                ringStepsToEntry = entryIndex - pawn.ringIndex;
            else
                ringStepsToEntry = ringCount - pawn.ringIndex + entryIndex;

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
                    yield return MoveTo(teamPath.safeZoneTiles[0], pawn);
                    remainingSteps--;
                    break;
                }

                pawn.ringIndex = (pawn.ringIndex + 1) % ringCount;
                yield return MoveTo(tilePath.RingTiles[pawn.ringIndex], pawn);
                remainingSteps--;
            }
        }

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
                    yield return MoveTo(teamPath.safeZoneTiles[pawn.safeIndex], pawn);
                }

                else if (pawn.safeIndex == finishStepIndex)
                {
                    yield return MoveTo(teamPath.finishTile, pawn);
                    pawn.safeIndex = -2;
                    Debug.Log($"{pawn.name} FINISHED!");
                    yield break;
                }

                remainingSteps--;
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
