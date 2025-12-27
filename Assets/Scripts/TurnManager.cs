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

    [Header("Team Selecting UI")]
    [Tooltip("TeamSelecting panel objesi (Earth/Fire/Air/Water butonları burada).")]
    [SerializeField] private GameObject teamSelectingPanel;

    [Header("Game UI Objects (disabled until team selected)")]
    [Tooltip("GameScreen başlarken kapalı olacak; human team seçince açılacak objeler.")]
    [SerializeField] private GameObject rollButtonObject;
    [SerializeField] private GameObject turnImageObject;
    [SerializeField] private GameObject settingsButtonObject;
    [SerializeField] private GameObject playerImageObject;
    [SerializeField] private GameObject boardBaseObject;

    [Tooltip("GameScreen başlarken kapalı olacak; human team seçince açılacak dekor objesi.")]
    [SerializeField] private GameObject decorationsObject;

    [Header("Player Image")]
    [Tooltip("PlayerImage UI objesinin Image component'i. (playerImageObject üstünde veya child'ında olabilir; buraya doğru Image'ı sürükle)")]
    [SerializeField] private Image playerImage;

    [Tooltip("Fire seçilince kullanılacak sprite (fire_player).")]
    [SerializeField] private Sprite firePlayerSprite;

    [Tooltip("Earth seçilince kullanılacak sprite (earth_player).")]
    [SerializeField] private Sprite earthPlayerSprite;

    [Tooltip("Air seçilince kullanılacak sprite (air_player).")]
    [SerializeField] private Sprite airPlayerSprite;

    [Tooltip("Water seçilince kullanılacak sprite (water_player).")]
    [SerializeField] private Sprite waterPlayerSprite;

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

    [Tooltip("İnsan oyuncunun takımı. TeamSelecting seçiminden sonra set edilir.")]
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

    // Game state
    private bool gameEnded = false;
    private bool gameStarted = false; // team seçilmeden oyun başlamaz

    public TeamColor CurrentTeam => turnOrder[turnIndex];
    public bool IsHumanTurn => gameStarted && enableHuman && CurrentTeam == humanTeam;

    private void Start()
    {
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);

        gameEnded = false;
        gameStarted = false;

        if (teamSelectingPanel != null) teamSelectingPanel.SetActive(true);
        SetGameUIActive(false);

        if (dice != null) dice.SetRollButtonInteractable(false);

        ClearAllSelectableFlags();
        BuildSafeStarIndexCache();
    }

    private void SetGameUIActive(bool active)
    {
        if (rollButtonObject != null) rollButtonObject.SetActive(active);
        if (turnImageObject != null) turnImageObject.SetActive(active);
        if (settingsButtonObject != null) settingsButtonObject.SetActive(active);
        if (playerImageObject != null) playerImageObject.SetActive(active);
        if (boardBaseObject != null) boardBaseObject.SetActive(active);
        if (decorationsObject != null) decorationsObject.SetActive(active);
    }

    // -------------------------
    // TEAM SELECT BUTTON HOOKS
    // -------------------------
    // Mapping:
    // Fire  = Red
    // Earth = Green
    // Water = Blue
    // Air   = Yellow
    public void SelectEarth() => StartGameWithHumanElement(Element.Earth);
    public void SelectFire()  => StartGameWithHumanElement(Element.Fire);
    public void SelectWater() => StartGameWithHumanElement(Element.Water);
    public void SelectAir()   => StartGameWithHumanElement(Element.Air);

    private enum Element { Fire, Earth, Air, Water }

    private void StartGameWithHumanElement(Element element)
    {
        // Element -> Team
        TeamColor team = element switch
        {
            Element.Fire => TeamColor.Red,
            Element.Earth => TeamColor.Green,
            Element.Water => TeamColor.Blue,
            Element.Air => TeamColor.Yellow,
            _ => TeamColor.Red
        };

        // Element -> Player sprite
        Sprite sprite = element switch
        {
            Element.Fire => firePlayerSprite,
            Element.Earth => earthPlayerSprite,
            Element.Water => waterPlayerSprite,
            Element.Air => airPlayerSprite,
            _ => null
        };

        StartGameWithHumanTeam(team, sprite);
    }

    /// <summary>
    /// TeamSelecting sonrası çağrılır. Oyunu başlatır.
    /// </summary>
    private void StartGameWithHumanTeam(TeamColor selectedTeam, Sprite selectedPlayerSprite)
    {
        if (gameEnded) return;
        if (gameStarted) return;

        enableHuman = true;
        humanTeam = selectedTeam;

        if (playerImage != null && selectedPlayerSprite != null)
            playerImage.sprite = selectedPlayerSprite;

        if (teamSelectingPanel != null) teamSelectingPanel.SetActive(false);
        SetGameUIActive(true);

        int idx = System.Array.IndexOf(turnOrder, humanTeam);
        turnIndex = idx >= 0 ? idx : 0;

        turnInProgress = false;
        humanSelectablePawns.Clear();
        humanSelectedPawn = null;
        lastHumanRoll = 0;
        humanPhase = HumanPhase.WaitingRoll;
        humanExtraRollPending = false;

        UpdateTurnUI();
        ClearAllSelectableFlags();

        gameStarted = true;

        if (dice != null)
            dice.SetRollButtonInteractable(IsHumanTurn && humanPhase == HumanPhase.WaitingRoll);

        StartCoroutine(TurnLoop());
    }

    // -------------------------
    // SAFE STAR CACHE
    // -------------------------
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

    // -------------------------
    // HUMAN INPUT
    // -------------------------
    public void SetHumanSelectedPawn(Pawn pawn)
    {
        if (!gameStarted || gameEnded) return;

        if (!IsHumanTurn) return;
        if (pawn == null || pawn.team != humanTeam) return;
        if (humanPhase != HumanPhase.ChoosingPawn) return;
        if (!humanSelectablePawns.Contains(pawn)) return;

        humanSelectedPawn = pawn;
        Debug.Log("Human selected pawn: " + pawn.name);
    }

    public void OnHumanPressedRoll()
    {
        if (!gameStarted || gameEnded) return;

        if (!IsHumanTurn) return;
        if (turnInProgress) return;
        if (dice != null && dice.IsRolling) return;
        if (humanPhase != HumanPhase.WaitingRoll) return;

        StartCoroutine(PlaySingleTurnForHuman());
    }

    // -------------------------
    // MAIN TURN LOOP
    // -------------------------
    private IEnumerator TurnLoop()
    {
        while (!gameEnded)
        {
            if (!gameStarted)
            {
                yield return null;
                continue;
            }

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

        if (enableHuman && team == humanTeam)
            yield break;

        turnInProgress = true;

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

            CheckEndGameAndHandle();
            if (gameEnded) break;

            extraTurn = (rolled == 6);
            if (extraTurn)
                yield return new WaitForSeconds(turnDelaySeconds);

        } while (extraTurn);

        turnInProgress = false;
    }

    private Pawn ChooseBotPawnSmart(TeamColor team, int rolled)
    {
        if (tilePath == null || tilePath.RingTiles == null || tilePath.RingTiles.Count == 0)
            return ChooseBotPawnFallback(team, rolled);

        TeamPath teamPath = tilePath.GetTeamPath(team);
        int ringCount = tilePath.RingTiles.Count;

        List<Pawn> myPawns = GetList(team);
        List<Pawn> enemyPawns = GetAllEnemyPawns(team);

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

        // 1) CAPTURE önceliği
        var captureCandidates = new List<Pawn>();
        foreach (var c in candidates)
        {
            if (!c.sim.endsOnRing) continue;
            if (IsSafeStarIndex(c.sim.ringIndex)) continue;

            bool hasEnemy = enemyPawns.Any(e => e != null && e.IsOnRing && !e.HasFinished && e.ringIndex == c.sim.ringIndex);
            if (hasEnemy)
                captureCandidates.Add(c.pawn);
        }

        if (captureCandidates.Count > 0)
        {
            return captureCandidates
                .OrderByDescending(p => GetProgressScore(p, teamPath, ringCount))
                .First();
        }

        // 2) 6 ise: start pawn
        if (rolled == 6)
        {
            var startPawns = candidates.Where(c => c.pawn.IsInStart).Select(c => c.pawn).ToList();
            if (startPawns.Count > 0)
                return startPawns[0];
        }

        // 3) en ileride
        return candidates.OrderByDescending(c => GetProgressScore(c.pawn, teamPath, ringCount)).First().pawn;
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

        if (pawn.IsInStart)
        {
            if (steps != 6) return r;
            r.canMove = true;
            r.endsOnRing = true;
            r.ringIndex = teamPath.startIndexOnRing;
            return r;
        }

        if (pawn.IsInSafeZone)
        {
            int finishStepIndex = teamPath.safeZoneTiles.Length;
            int remainingToFinish = finishStepIndex - pawn.safeIndex;
            if (steps > remainingToFinish) return r;

            r.canMove = true;
            r.endsOnRing = false;
            return r;
        }

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

            r.canMove = true;
            r.endsOnRing = false;
            return r;
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
