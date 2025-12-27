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

    [Header("End Game UI")]
    [Tooltip("İnsan oyuncu kazanırsa aktif olacak ekran.")]
    [SerializeField] private GameObject winScreen;
    [Tooltip("Botlardan biri kazanırsa aktif olacak ekran.")]
    [SerializeField] private GameObject loseScreen;

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

    [Header("Human Config")]
    [Tooltip("İnsan oyuncu açık mı? (1 insan 3 bot senaryosunda true)")]
    [SerializeField] private bool enableHuman = true;

    [Tooltip("İnsan oyuncunun takımı. (Şimdilik Inspector'dan; ileride UI'dan değiştirilebilir)")]
    [SerializeField] private TeamColor humanTeam = TeamColor.Red;

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

    // Game end
    private bool gameEnded = false;

    public TeamColor CurrentTeam => turnOrder[turnIndex];
    public bool IsHumanTurn => enableHuman && CurrentTeam == humanTeam;

    private void Start()
    {
        // End screens default kapalı
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);

        // Eğer insan takımı turnOrder içinde varsa, oradan başlatmak daha tutarlı
        int humanIdx = System.Array.IndexOf(turnOrder, humanTeam);
        turnIndex = humanIdx >= 0 ? humanIdx : 0;

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

    // UI / PawnSelection burayı çağırır
    public void SetHumanSelectedPawn(Pawn pawn)
    {
        if (gameEnded) return;

        if (!IsHumanTurn) return;
        if (pawn == null || pawn.team != humanTeam) return;
        if (humanPhase != HumanPhase.ChoosingPawn) return;
        if (!humanSelectablePawns.Contains(pawn)) return;

        humanSelectedPawn = pawn;
        Debug.Log("Human selected pawn: " + pawn.name);
    }

    // RollDiceButton OnClick -> burası
    public void OnHumanPressedRoll()
    {
        if (gameEnded) return;

        if (!IsHumanTurn) return;
        if (turnInProgress) return;
        if (dice != null && dice.IsRolling) return;
        if (humanPhase != HumanPhase.WaitingRoll) return;

        StartCoroutine(PlaySingleTurnForHuman());
    }

    private IEnumerator TurnLoop()
    {
        while (!gameEnded)
        {
            if (IsHumanTurn)
            {
                dice.SetRollButtonInteractable(humanPhase == HumanPhase.WaitingRoll);

                while (!turnInProgress && !gameEnded) yield return null;
                while (turnInProgress && !gameEnded) yield return null;

                if (gameEnded) yield break;

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

                if (gameEnded) yield break;
            }

            yield return SwitchToNextTurnWithDelay();
        }
    }

    private IEnumerator SwitchToNextTurnWithDelay()
    {
        if (gameEnded) yield break;

        yield return new WaitForSeconds(turnDelaySeconds);

        if (gameEnded) yield break;

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
        if (gameEnded) yield break;

        turnInProgress = true;
        humanPhase = HumanPhase.Moving;
        dice.SetRollButtonInteractable(false);

        lastHumanRoll = 0;
        yield return dice.RollDiceCoroutine(v => lastHumanRoll = v);

        if (gameEnded)
        {
            turnInProgress = false;
            yield break;
        }

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

            while (humanSelectedPawn == null && !gameEnded)
                yield return null;

            if (gameEnded)
            {
                turnInProgress = false;
                yield break;
            }
        }

        humanPhase = HumanPhase.Moving;
        ClearAllSelectableFlags();

        yield return mover.MoveSelectedPawnCoroutine(humanSelectedPawn, lastHumanRoll);

        // ✅ Oyun bitti mi kontrol
        CheckEndGameAndHandle();

        if (!gameEnded)
            humanExtraRollPending = (lastHumanRoll == 6);

        turnInProgress = false;
    }

    private void BuildHumanSelectablePawns(int rolled)
    {
        humanSelectablePawns.Clear();

        var humanList = GetList(humanTeam);

        foreach (var p in humanList)
            if (p != null) p.SetSelectable(false);

        foreach (var pawn in humanList)
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
        if (gameEnded) yield break;

        turnInProgress = true;

        // Bot: 6 geldikçe otomatik devam
        bool extraTurn;
        do
        {
            if (gameEnded) break;

            int rolled = 0;
            yield return dice.RollDiceCoroutine(v => rolled = v);

            if (gameEnded) break;

            Pawn chosen = ChooseBotPawnSmart(team, rolled);

            if (chosen == null)
            {
                Debug.Log($"{team} rolled {rolled} but no playable pawn. Passing.");
                turnInProgress = false;
                yield break;
            }

            yield return mover.MoveSelectedPawnCoroutine(chosen, rolled);

            // ✅ Oyun bitti mi kontrol
            CheckEndGameAndHandle();
            if (gameEnded) break;

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
        // Eğer bu takım insanın takımıysa bot gibi davranmasın
        if (enableHuman && team == humanTeam)
            return null;

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
            // Birden çok varsa: en ileride olanı seç
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
                return startPawns[0];
        }

        // 4) Diğer durum: en ilerideki pawn
        return candidates
            .OrderByDescending(c => GetProgressScore(c.pawn, teamPath, ringCount))
            .First().pawn;
    }

    private Pawn ChooseBotPawnFallback(TeamColor team, int rolled)
    {
        var list = GetList(team);

        if (rolled == 6)
        {
            var sp = list.FirstOrDefault(p => p != null && !p.HasFinished && p.IsInStart);
            if (sp != null) return sp;
        }

        return list.FirstOrDefault(p => p != null && !p.HasFinished && (!p.IsInStart || rolled == 6));
    }

    private SimResult SimulateMove(Pawn pawn, TeamPath teamPath, int ringCount, int steps)
    {
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
                r.canMove = true;
                r.endsOnRing = false;
                return r;
            }
        }

        return r;
    }

    private float GetProgressScore(Pawn pawn, TeamPath teamPath, int ringCount)
    {
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

    // -------------------------
    // END GAME
    // -------------------------
    private void CheckEndGameAndHandle()
    {
        if (gameEnded) return;

        // Her takım için "4 pawn finished" kontrolü
        if (IsTeamFinished(redPawns)) { EndGame(TeamColor.Red); return; }
        if (IsTeamFinished(greenPawns)) { EndGame(TeamColor.Green); return; }
        if (IsTeamFinished(bluePawns)) { EndGame(TeamColor.Blue); return; }
        if (IsTeamFinished(yellowPawns)) { EndGame(TeamColor.Yellow); return; }
    }

    private bool IsTeamFinished(List<Pawn> pawns)
    {
        if (pawns == null || pawns.Count == 0) return false;
        return pawns.All(p => p != null && p.HasFinished);
    }

    private void EndGame(TeamColor winnerTeam)
    {
        gameEnded = true;

        // Inputları kapat
        ClearAllSelectableFlags();
        humanSelectablePawns.Clear();
        humanSelectedPawn = null;
        humanPhase = HumanPhase.Moving;
        humanExtraRollPending = false;
        turnInProgress = false;

        if (dice != null)
            dice.SetRollButtonInteractable(false);

        bool humanWon = enableHuman && (winnerTeam == humanTeam);

        if (winScreen != null) winScreen.SetActive(humanWon);
        if (loseScreen != null) loseScreen.SetActive(!humanWon);

        Debug.Log($"GAME ENDED. Winner: {winnerTeam}. HumanWon={humanWon}");
    }
}
