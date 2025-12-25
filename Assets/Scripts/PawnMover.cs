using System.Collections;
using UnityEngine;

public class PawnMover : MonoBehaviour
{
    [SerializeField] private TilePath tilePath;
    [SerializeField] private float stepDuration = 0.15f;

    public IEnumerator MoveSelectedPawnCoroutine(Pawn pawn, int steps)
    {
        if (pawn == null || tilePath == null || tilePath.Tiles == null)
            yield break;

        // START ALANINDA MI?
        if (pawn.CurrentTileIndex == -1)
        {
            if (steps != 6)
                yield break;

            // 6 gelince path'e çık
            steps = 1; // sadece Tile_0'a çıkış
        }

        for (int i = 0; i < steps; i++)
        {
            int nextIndex = pawn.CurrentTileIndex + 1;
            if (nextIndex < 0 || nextIndex >= tilePath.Tiles.Count)
                yield break;

            Transform nextTile = tilePath.Tiles[nextIndex];

            Vector3 startPos = pawn.transform.position;
            Vector3 endPos = nextTile.position;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, stepDuration);
                pawn.transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            pawn.transform.position = endPos;
            pawn.CurrentTileIndex = nextIndex;
            yield return null;
        }
    }
}
