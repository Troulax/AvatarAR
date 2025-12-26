using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    private enum HumanPhase
    {
        WaitingRoll,
        ChoosingPawn,
        Moving
    }

    [Header("References")]
    [SerializeField] private DiceRollerUI dice;
    [SerializeField] private PawnMover mover;

    [Header("Board Reference (for Bot AI)")]
    [Tooltip("Bot AI'nin hamle simülasyonu yapması için TilePath referansı.")]
    [SerializeField] private TilePath tilePath;

    [Header("Safe Star Tiles (Ring) - Bot AI")]
    [Tooltip("Ring üzerindeki güvenli yıldız tile objelerini buraya sürükle (toplam 8 adet). Bu tile'lara inişte bot capture saymaz.")]
    [SerializeField] private List<Transform> safeStarRingTiles = new();

    [Header("Turn UI")]
    [SerializeField] private Image turnImage;
    [SerializeField] private Sprite redTurnSprite;
    [SerializeField] private Sprite greenTurnSprite;
    [SerializeField] private Sprite blueTurnSprite;
    [SerializeField] private Sprite yellowTurnSprite;
    [SerializeField] private float turnDelaySeconds = 2f;

    [Header("Teams Order")]
    [SerializeField] private TeamColor[] turnOrder = new[]
    {
        TeamColor.Red, TeamColor.Green, TeamColor.Blue, TeamColor.Yellow
    };

    [Header("All Pawns (assign in Inspector)")]
    [SerializeField] private List<Pawn> redPawns = new();
    [SerializeField] private List<Pawn> greenPawns = new();
    [SerializeField] private List<Pawn> bluePawns = new();
    [SerializeField] private List<Pawn> yellowPawns = new();

    [Header("Config")]
    [SerializeField] private bool redIsHuman = true;

    private int turnIndex = 0;
    private bool turnInProgress = false;

    // Human selection
    private Pawn humanSelectedPawn;
    private bool humanExtraRollPending = false;
    private HumanPhase humanPhase = HumanPhase.WaitingRoll;
    private int lastHumanRoll = 0;
    private readonly HashSet<Pawn> humanSelectablePawns = new();

    // Bot AI safe star cache (ring indices)
    private readonly HashSet<int> safeStarRingIndices = new();

    public TeamColor CurrentTeam => turnOrder[turnIndex];
    public bool IsHumanTurn => redIsHuman && CurrentTeam == TeamColor.Red;

    private void Start()
    {
        int redIdx = System.Array.IndexOf(turnOrder, TeamColor.Red);
        turnIndex = redIdx >= 0 ? redIdx : 0;

        UpdateTurnUI();
        ClearAllSelectableFlags();
        BuildSafeStarIndexCache();

        StartCoroutine(TurnLoop());
    }

    private void BuildSafeStarIndexCache()
    {
        safeStarRingIndices.Clear();

        if (tilePath == null || tilePath.RingTiles == null || tilePath.RingTiles.Count == 0)
            return;

        foreach (var tf in safeStarRingTiles)
        {
            if (tf == null) continue;
            int idx = tilePath.RingTiles.IndexOf(tf);
            if (idx >= 0) safeStarRingIndices.Add(idx);
        }
    }

    private bool IsSafeStarIndex(int ringIndex) => safeStarRingIndices.Contains(ringIndex);

    public void SetHumanSelectedPawn(Pawn pawn)
    {
        if (!IsHumanTurn) return;
        if (pawn == null || pawn.team != TeamColor.Red) return;
        if (humanPhase != HumanPhase.ChoosingPawn) return;
        if (!humanSelectablePawns.Contains(pawn)) return;

        humanSelectedPawn = pawn;
        Debug.Log("Human selected pawn: " + pawn.name);
    }

    public void OnHumanPressedRoll()
    {
        if (!IsHumanTurn) return;
        if (turnInProgress) return;
        if (dice != null && dice.IsRolling) return;
        if (humanPhase != HumanPhase.WaitingRoll) return;

        StartCoroutine(PlaySingleTurnForHuman());
    }

    private IEnumerator TurnLoop()
    {
        while (true)
        {
            if (IsHumanTurn)
            {
                dice.SetRollButtonInteractable(humanPhase == HumanPhase.WaitingRoll);

                while (!turnInProgress) yield return null;
                while (turnInProgress) yield return null;

                if (humanExtraRollPending)
                {
                    humanExtraRollPending = false;
                    humanPhase = HumanPhase.WaitingRoll;
                    dice.SetRollButtonInteractable(true);
                    continue;
                }
            }
            else
            {
                dice.SetRollButtonInteractable(false);
                yield return PlaySingleTurnForBot(CurrentTeam);
            }

            yield return SwitchToNextTurnWithDelay();
        }
    }

    private IEnumerator SwitchToNextTurnWithDelay()
    {
        yield return new WaitForSeconds(turnDelaySeconds);

        turnIndex = (turnIndex + 1) % turnOrder.Length;
        UpdateTurnUI();

        ClearAllSelectableFlags();
        humanSelectablePawns.Clear();
        humanSelectedPawn = null;
        lastHumanRoll = 0;
        humanPhase = HumanPhase.WaitingRoll;
        humanExtraRollPending = false;

        // safe star cache runtime’da boş kaldıysa tekrar dene
        if (safeStarRingIndices.Count == 0 && safeStarRingTiles.Count > 0)
            BuildSafeStarIndexCache();
    }

    private void UpdateTurnUI()
    {
        if (turnImage == null) return;

        switch (CurrentTeam)
        {
            case TeamColor.Red: turnImage.sprite = redTurnSprite; break;
            case TeamColor.Green: turnImage.sprite = greenTurnSprite; break;
            case TeamColor.Blue: turnImage.sprite = blueTurnSprite; break;
            case TeamColor.Yellow: turnImage.sprite = yellowTurnSprite; break;
        }
    }

    // -------------------------
    // HUMAN TURN
    // -------------------------
    private IEnumerator PlaySingleTurnForHuman()
    {
        turnInProgress = true;
        humanPhase = HumanPhase.Moving;
        dice.SetRollButtonInteractable(false);

        lastHumanRoll = 0;
        yield return dice.RollDiceCoroutine(v => lastHumanRoll = v);

        BuildHumanSelectablePawns(lastHumanRoll);

        if (humanSelectablePawns.Count == 0)
        {
            Debug.Log($"Human rolled {lastHumanRoll} but no playable pawn. Passing.");
            humanExtraRollPending = false;

            ClearAllSelectableFlags();
            humanPhase = HumanPhase.WaitingRoll;
            turnInProgress = false;
            yield break;
        }

        // 6 değilse ve tek oynanabilir pawn varsa otomatik seç
        if (lastHumanRoll != 6 && humanSelectablePawns.Count == 1)
        {
            humanSelectedPawn = humanSelectablePawns.First();
        }
        else
        {
            humanSelectedPawn = null;
            humanPhase = HumanPhase.ChoosingPawn;

            while (humanSelectedPawn == null)
                yield return null;
        }

        humanPhase = HumanPhase.Moving;
        ClearAllSelectableFlags();

        yield return mover.MoveSelectedPawnCoroutine(humanSelectedPawn, lastHumanRoll);

        humanExtraRollPending = (lastHumanRoll == 6);
        turnInProgress = false;
    }

    private void BuildHumanSelectablePawns(int rolled)
    {
        humanSelectablePawns.Clear();

        foreach (var p in redPawns)
            if (p != null) p.SetSelectable(false);

        foreach (var pawn in redPawns)
        {
            if (pawn == null) continue;
            if (pawn.HasFinished) continue;

            bool playable =
                (pawn.IsInStart && rolled == 6) ||
                pawn.IsOnRing ||
                pawn.IsInSafeZone;

            if (!playable) continue;

            humanSelectablePawns.Add(pawn);
            pawn.SetSelectable(true);
        }
    }

    private void ClearAllSelectableFlags()
    {
        foreach (var p in redPawns) if (p != null) p.SetSelectable(false);
        foreach (var p in greenPawns) if (p != null) p.SetSelectable(false);
        foreach (var p in bluePawns) if (p != null) p.SetSelectable(false);
        foreach (var p in yellowPawns) if (p != null) p.SetSelectable(false);
    }

    // -------------------------
    // BOT TURN (AI)
    // -------------------------
    private struct SimResult
    {
        public bool canMove;
        public bool endsOnRing;
        public int ringIndex;
    }

    private IEnumerator PlaySingleTurnForBot(TeamColor team)
    {
        turnInProgress = true;

        // Bot: 6 geldikçe otomatik devam
        bool extraTurn;
        do
        {
            int rolled = 0;
            yield return dice.RollDiceCoroutine(v => rolled = v);

            Pawn chosen = ChooseBotPawnSmart(team, rolled);

            if (chosen == null)
            {
                Debug.Log($"{team} rolled {rolled} but no playable pawn. Passing.");
                turnInProgress = false;
                yield break;
            }

            yield return mover.MoveSelectedPawnCoroutine(chosen, rolled);

            extraTurn = (rolled == 6);
            if (extraTurn)
            {
                // ✅ 6 sonrası da “tur arası” hissi
                yield return new WaitForSeconds(turnDelaySeconds);
            }

        } while (extraTurn);

        turnInProgress = false;
    }

    private Pawn ChooseBotPawnSmart(TeamColor team, int rolled)
    {
        // TilePath yoksa fallback
        if (tilePath == null || tilePath.RingTiles == null || tilePath.RingTiles.Count == 0)
            return ChooseBotPawnFallback(team, rolled);

        TeamPath teamPath = tilePath.GetTeamPath(team);
        int ringCount = tilePath.RingTiles.Count;

        List<Pawn> myPawns = GetList(team);
        List<Pawn> enemyPawns = GetAllEnemyPawns(team);

        // 1) Oynanabilir adaylar + simülasyon
        var candidates = new List<(Pawn pawn, SimResult sim)>();

        foreach (var p in myPawns)
        {
            if (p == null) continue;
            if (p.HasFinished) continue;

            SimResult sim = SimulateMove(p, teamPath, ringCount, rolled);
            if (!sim.canMove) continue;

            candidates.Add((p, sim));
        }

        if (candidates.Count == 0)
            return null;

        // 2) CAPTURE önceliği (garanti)
        var captureCandidates = new List<Pawn>();
        foreach (var c in candidates)
        {
            if (!c.sim.endsOnRing) continue;

            // güvenli yıldız tile ise capture sayma
            if (IsSafeStarIndex(c.sim.ringIndex)) continue;

            bool hasEnemy = enemyPawns.Any(e => e != null && e.IsOnRing && !e.HasFinished && e.ringIndex == c.sim.ringIndex);
            if (hasEnemy)
                captureCandidates.Add(c.pawn);
        }

        if (captureCandidates.Count > 0)
        {
            // Birden çok varsa: en ileride olanı seçelim (genelde daha akıllı)
            return captureCandidates
                .OrderByDescending(p => GetProgressScore(p, teamPath, ringCount))
                .First();
        }

        // 3) 6 ise: mümkünse yeni pawn indir (start'tan)
        if (rolled == 6)
        {
            var startPawns = candidates
                .Where(c => c.pawn.IsInStart)
                .Select(c => c.pawn)
                .ToList();

            if (startPawns.Count > 0)
            {
                // "hali hazırda oyunda pawn varken" yeni pawn indirsin; ama zaten startPawn seçmek genel olarak doğru.
                // İstersen "board'da en az 1 pawn varsa" diye ekstra şart eklenebilir.
                return startPawns[0];
            }
        }

        // 4) Diğer durum: en ilerideki pawn (oyunu zorlaştırır)
        return candidates
            .OrderByDescending(c => GetProgressScore(c.pawn, teamPath, ringCount))
            .First().pawn;
    }

    private Pawn ChooseBotPawnFallback(TeamColor team, int rolled)
    {
        // TilePath yoksa: en azından oynanabilir mantıkla seç
        var list = GetList(team);

        // capture hesaplayamayız, sadece 6 ise start pawn önceliği, yoksa ilk oynanabilir
        if (rolled == 6)
        {
            var sp = list.FirstOrDefault(p => p != null && !p.HasFinished && p.IsInStart);
            if (sp != null) return sp;
        }

        return list.FirstOrDefault(p => p != null && !p.HasFinished && (!p.IsInStart || rolled == 6));
    }

    private SimResult SimulateMove(Pawn pawn, TeamPath teamPath, int ringCount, int steps)
    {
        // PawnMover ile aynı kural setini baz alır (basitleştirilmiş):
        // - Start: sadece 6 ile çıkar, ringIndex = startIndexOnRing
        // - Ring: entryIndex'e gelirse safe zone'a girer, overshoot yasak
        // - SafeZone: overshoot yasak
        // Bu simülasyon sadece "endsOnRing + ringIndex" bilgisini capture hesabı için döndürür.

        SimResult r = new SimResult { canMove = false, endsOnRing = false, ringIndex = -1 };

        if (pawn == null || pawn.HasFinished) return r;

        // Start
        if (pawn.IsInStart)
        {
            if (steps != 6) return r;
            r.canMove = true;
            r.endsOnRing = true;
            r.ringIndex = teamPath.startIndexOnRing;
            return r;
        }

        // SafeZone
        if (pawn.IsInSafeZone)
        {
            int finishStepIndex = teamPath.safeZoneTiles.Length;
            int remainingToFinish = finishStepIndex - pawn.safeIndex;
            if (steps > remainingToFinish) return r;

            // Safe zone sonu capture değil
            r.canMove = true;
            r.endsOnRing = false;
            return r;
        }

        // Ring
        if (pawn.IsOnRing)
        {
            int entryIndex = (teamPath.startIndexOnRing - 2 + ringCount) % ringCount;

            int ringStepsToEntry;
            if (pawn.ringIndex <= entryIndex) ringStepsToEntry = entryIndex - pawn.ringIndex;
            else ringStepsToEntry = ringCount - pawn.ringIndex + entryIndex;

            int safeAndFinishLength = teamPath.safeZoneTiles.Length + 1;
            int totalRemainingPath = ringStepsToEntry + safeAndFinishLength;

            if (steps > totalRemainingPath) return r;

            // Eğer steps <= ringStepsToEntry: ring üzerinde kalır
            if (steps <= ringStepsToEntry)
            {
                int newIndex = (pawn.ringIndex + steps) % ringCount;
                r.canMove = true;
                r.endsOnRing = true;
                r.ringIndex = newIndex;
                return r;
            }
            else
            {
                // Safe zone'a giriyor (capture yok)
                r.canMove = true;
                r.endsOnRing = false;
                return r;
            }
        }

        return r;
    }

    private float GetProgressScore(Pawn pawn, TeamPath teamPath, int ringCount)
    {
        // Basit “en ileride” metriği:
        // Start: -1
        // Ring: team start'a göre clockwise mesafe
        // Safe: ringCount + safeIndex
        if (pawn == null) return -999f;
        if (pawn.HasFinished) return 99999f;

        if (pawn.IsInStart) return -1f;

        if (pawn.IsInSafeZone)
            return ringCount + pawn.safeIndex;

        if (pawn.IsOnRing)
        {
            int dist = (pawn.ringIndex - teamPath.startIndexOnRing + ringCount) % ringCount;
            return dist;
        }

        return 0f;
    }

    private List<Pawn> GetList(TeamColor team)
    {
        return team switch
        {
            TeamColor.Red => redPawns,
            TeamColor.Green => greenPawns,
            TeamColor.Blue => bluePawns,
            TeamColor.Yellow => yellowPawns,
            _ => redPawns
        };
    }

    private List<Pawn> GetAllEnemyPawns(TeamColor team)
    {
        var all = new List<Pawn>();
        if (team != TeamColor.Red) all.AddRange(redPawns);
        if (team != TeamColor.Green) all.AddRange(greenPawns);
        if (team != TeamColor.Blue) all.AddRange(bluePawns);
        if (team != TeamColor.Yellow) all.AddRange(yellowPawns);
        return all;
    }
}
