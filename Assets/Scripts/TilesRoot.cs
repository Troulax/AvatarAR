using UnityEngine;

public class TilesRoot : MonoBehaviour
{
    [Header("Parents")]
    [SerializeField] private Transform ringTiles;
    [SerializeField] private Transform safeZoneTiles;
    [SerializeField] private Transform finishTile;

    [Header("TileAnchor defaults")]
    [SerializeField] private float offset = 0.08f;
    [SerializeField] private float height = 0.02f;

    [ContextMenu("Ensure TileAnchors On All Tiles")]
    public void EnsureAnchors()
    {
        AddAnchorsUnder(ringTiles);
        AddAnchorsUnder(safeZoneTiles);

        if (finishTile != null)
            AddAnchorIfMissing(finishTile);

        Debug.Log("TileAnchors ensured.");
    }

    private void AddAnchorsUnder(Transform parent)
    {
        if (parent == null) return;

        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            AddAnchorIfMissing(child);
        }
    }

    private void AddAnchorIfMissing(Transform tile)
    {
        if (tile == null) return;

        var anchor = tile.GetComponent<TileAnchor>();
        if (anchor == null)
            anchor = tile.gameObject.AddComponent<TileAnchor>();

        anchor.SetDefaults(offset, height);
    }
}
