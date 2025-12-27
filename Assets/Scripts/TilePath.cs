using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TeamColor { Red, Green, Blue, Yellow }

[Serializable]
public class TeamPath
{
    public TeamColor team;
    public int startIndexOnRing;      // Red 1, Green 12, Blue 23, Yellow 34
    public Transform[] safeZoneTiles; // 4 adet
    public Transform finishTile;      // 1 adet
}

public class TilePath : MonoBehaviour
{
    [Header("Ring Path")]
    [SerializeField] private Transform ringTilesParent;
    public List<Transform> RingTiles { get; private set; }

    [Header("Team Paths")]
    [SerializeField] private TeamPath[] teamPaths;

    public TeamPath GetTeamPath(TeamColor team)
    {
        return teamPaths.First(tp => tp.team == team);
    }

    private void Awake()
    {
        RingTiles = ringTilesParent
            .Cast<Transform>()
            .Select(t => (t, idx: ExtractIndex(t.name)))
            .Where(x => x.idx >= 0)
            .OrderBy(x => x.idx)
            .Select(x => x.t)
            .ToList();

        if (RingTiles.Count == 0)
            Debug.LogError("RingTiles is empty. Check ringTilesParent and tile names (Tile_0..).");
    }

    private int ExtractIndex(string name)
    {
        // "Tile_12" => 12
        if (!name.StartsWith("Tile_")) return -1;
        var parts = name.Split('_');
        if (parts.Length < 2) return -1;
        if (int.TryParse(parts[1], out int idx)) return idx;
        return -1;
    }
}
