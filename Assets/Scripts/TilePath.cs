using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TilePath : MonoBehaviour
{
    [Tooltip("Tiles parent object. Children should be Tile_0, Tile_1, ... in order.")]
    [SerializeField] private Transform tilesParent;

    public List<Transform> Tiles { get; private set; }

    private void Awake()
    {
        // Child'ları isim sırasına göre al (Tile_0, Tile_1...)
        Tiles = tilesParent
            .Cast<Transform>()
            .OrderBy(t => ExtractIndex(t.name))
            .ToList();
    }

    private int ExtractIndex(string name)
    {
        // "Tile_12" -> 12
        var parts = name.Split('_');
        if (parts.Length >= 2 && int.TryParse(parts[^1], out int idx))
            return idx;

        // isim formatı farklıysa sona atmasın diye
        return int.MaxValue;
    }
}
