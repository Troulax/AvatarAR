using System.Collections.Generic;
using UnityEngine;

public class TileAnchor : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Minimum offset (tile boyutuna göre). Yetersiz kalırsa pawn boyutundan otomatik büyütülür.")]
    [SerializeField] private float baseOffset = 0.10f;

    [Tooltip("Zemin üstü yükseklik.")]
    [SerializeField] private float height = 0.02f;

    [Header("Auto Scale")]
    [Tooltip("Pawn'ın collider/renderer yarıçapına göre offset büyütme çarpanı. 1.2–2.0 arası iyi.")]
    [SerializeField] private float pawnRadiusMultiplier = 1.8f;

    [Tooltip("Pawn sayısı arttıkça biraz daha aç (1.0 = kapalı, 1.15 = biraz daha aç).")]
    [SerializeField] private float crowdExpand = 1.12f;

    private readonly List<Transform> occupants = new();

    public void SetDefaults(float newOffset, float newHeight)
    {
        baseOffset = newOffset;
        height = newHeight;
    }

    public void Add(Transform pawnTf)
    {
        if (pawnTf == null) return;
        if (!occupants.Contains(pawnTf))
            occupants.Add(pawnTf);

        RepositionAll();
    }

    public void Remove(Transform pawnTf)
    {
        if (pawnTf == null) return;
        occupants.Remove(pawnTf);

        RepositionAll();
    }

    public void RepositionAll()
    {
        int count = occupants.Count;
        for (int i = 0; i < count; i++)
        {
            occupants[i].position = GetSlotWorldPos(i, count, occupants[i]);
        }
    }

    private Vector3 GetSlotWorldPos(int index, int count, Transform pawnTf)
    {
        Vector3 center = transform.position + Vector3.up * height;

        float off = Mathf.Max(baseOffset, EstimatePawnBasedOffset(pawnTf));
        // kalabalık oldukça biraz daha aç
        if (count >= 3)
            off *= Mathf.Pow(crowdExpand, count - 2);

        // 1–4 için sabit slotlar (çapraz/köşeler)
        if (count == 1)
            return center;

        if (count == 2)
        {
            // çaprazı biraz daha “geniş” yapıyoruz
            Vector3 a = new Vector3(-off, 0, +off);
            Vector3 b = new Vector3(+off, 0, -off);
            return center + (index == 0 ? a : b);
        }

        if (count == 3)
        {
            Vector3[] s =
            {
                new Vector3(-off, 0, +off),
                new Vector3(+off, 0, +off),
                new Vector3(0,    0, -off * 1.15f),
            };
            return center + s[index];
        }

        if (count == 4)
        {
            Vector3[] s =
            {
                new Vector3(-off, 0, +off),
                new Vector3(+off, 0, +off),
                new Vector3(-off, 0, -off),
                new Vector3(+off, 0, -off),
            };
            return center + s[index];
        }

        // 5+ olursa daireye diz
        float radius = off * 1.35f;
        float angle = (Mathf.PI * 2f) * (index / (float)count);
        return center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
    }

    private float EstimatePawnBasedOffset(Transform pawnTf)
    {
        if (pawnTf == null) return 0f;

        // 1) Collider varsa onu kullan
        var col = pawnTf.GetComponentInChildren<Collider>();
        if (col != null)
        {
            // bounds extents: yarı boyut
            float r = Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
            if (r > 0.0001f)
                return r * pawnRadiusMultiplier;
        }

        // 2) Renderer varsa onu kullan
        var rend = pawnTf.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            float r = Mathf.Max(rend.bounds.extents.x, rend.bounds.extents.z);
            if (r > 0.0001f)
                return r * pawnRadiusMultiplier;
        }

        return 0f;
    }
}
