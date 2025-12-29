using System;
using System.Collections.Generic;
using UnityEngine;

public enum ElementType { Earth, Fire, Air, Water }
public enum AvatarType { Kyoshi, Roku, Aang, Korra }

[Serializable]
public class AvatarOfferConfig
{
    public AvatarType avatar;
    public ElementType element;
    public Transform tile; // Pawn'ın FINALDE durduğu tile transform'u
}

public class OfferManager : MonoBehaviour
{
    [Header("Offer Tiles (4 adet)")]
    [SerializeField] private List<AvatarOfferConfig> offers = new();

    private Dictionary<Transform, AvatarOfferConfig> offerByTile = new();

    private void Awake() => RebuildCache();

#if UNITY_EDITOR
    private void OnValidate() => RebuildCache();
#endif

    private void RebuildCache()
    {
        offerByTile.Clear();
        if (offers == null) return;

        foreach (var o in offers)
        {
            if (o == null || o.tile == null) continue;
            offerByTile[o.tile] = o; // aynı tile tekrar girilirse en sonuncu geçerli
        }
    }

    public bool TryGetOffer(Transform tile, out AvatarOfferConfig offer)
    {
        offer = null;
        if (tile == null) return false;
        return offerByTile.TryGetValue(tile, out offer);
    }
}
