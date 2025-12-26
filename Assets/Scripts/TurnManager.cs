using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DiceRollerUI dice;
    [SerializeField] private PawnMover mover;

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

    private Pawn humanSelectedPawn;

    // Human 6 atınca otomatik yeniden atma yerine "pending" bekle
    private bool humanExtraRollPending = false;

    public TeamColor CurrentTeam => turnOrder[turnIndex];
    public bool IsHumanTurn => redIsHuman && CurrentTeam == TeamColor.Red;

    private void Start()
    {
        // oyunu kırmızı ile başlat
        int redIdx = System.Array.IndexOf(turnOrder, TeamColor.Red);
        turnIndex = redIdx >= 0 ? redIdx : 0;

        UpdateTurnUI();

        StartCoroutine(TurnLoop());
    }

    // PawnSelection burayı çağırır
    public void SetHumanSelectedPawn(Pawn pawn)
    {
        if (!IsHumanTurn) return;
        if (pawn == null || pawn.team != TeamColor.Red) return;

        humanSelectedPawn = pawn;
        Debug.Log("Human selected: " + pawn.name);
    }

    // RollDiceButton OnClick -> burası
    public void OnHumanPressedRoll()
    {
        if (!IsHumanTurn) return;
        if (turnInProgress) return;
        if (dice != null && dice.IsRolling) return;

        StartCoroutine(PlaySingleTurnForHuman());
    }

    private IEnumerator TurnLoop()
    {
        while (true)
        {
            if (IsHumanTurn)
            {
                // Human: buton açık, kullanıcı basacak
                dice.SetRollButtonInteractable(true);

                // Human bir kez oynayana kadar bekle
                while (!turnInProgress)
                    yield return null;

                // Oynama bitene kadar bekle
                while (turnInProgress)
                    yield return null;

                // Eğer 6 geldiyse tur değişmesin, kullanıcı tekrar butona bassın
                if (humanExtraRollPending)
                {
                    humanExtraRollPending = false;
                    // aynı turda kal
                    continue;
                }
            }
            else
            {
                // Bot: buton kapalı, otomatik oynar
                dice.SetRollButtonInteractable(false);
                yield return PlaySingleTurnForBot(CurrentTeam);
            }

            // Tur gerçekten bitiyorsa: gecikme + sonra tur değişimi + UI güncelleme
            yield return SwitchToNextTurnWithDelay();
        }
    }

    // İstenen davranış: TurnImage delay bitince değişsin
    private IEnumerator SwitchToNextTurnWithDelay()
    {
        // Eski tur görseli kalsın
        yield return new WaitForSeconds(turnDelaySeconds);

        // Bekleme bitince sıradaki takıma geç ve UI'yi güncelle
        NextTeamImmediate();
        UpdateTurnUI();
    }

    private void NextTeamImmediate()
    {
        turnIndex = (turnIndex + 1) % turnOrder.Length;
        humanSelectedPawn = null;
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

    // Human: tek zar atar. 6 gelirse extra hak var ama otomatik atmaz.
    private IEnumerator PlaySingleTurnForHuman()
    {
        turnInProgress = true;

        // pawn seçilmediyse, ilk kırmızıyı kullan (test güvenliği)
        if (humanSelectedPawn == null)
            humanSelectedPawn = redPawns.FirstOrDefault(p => p != null && !p.HasFinished);

        if (humanSelectedPawn == null)
        {
            turnInProgress = false;
            yield break;
        }

        int rolled = 0;

        // 1) Zar
        yield return dice.RollDiceCoroutine(v => rolled = v);

        // 2) Hareket
        yield return mover.MoveSelectedPawnCoroutine(humanSelectedPawn, rolled);

        // 3) 6 geldiyse: extra roll hakkı var ama otomatik atma!
        humanExtraRollPending = (rolled == 6);

        if (humanExtraRollPending)
            Debug.Log("Human rolled 6 => extra roll (press button again)");

        turnInProgress = false;
    }

    private IEnumerator PlaySingleTurnForBot(TeamColor team)
    {
        turnInProgress = true;

        Pawn pawn = ChooseBotPawn(team);
        if (pawn == null)
        {
            Debug.Log($"{team} has no pawn to play. Passing turn.");
            turnInProgress = false;
            yield break;
        }

        // Bot: 6 geldikçe otomatik devam
        yield return PlayTurnForPawn_Bot(pawn);

        turnInProgress = false;
    }

    private Pawn ChooseBotPawn(TeamColor team)
    {
        // Şimdilik basit: bitmemiş ilk pawn.
        // (İstersen sonra akıllı seçim ekleriz.)
        var list = GetList(team);
        return list.FirstOrDefault(p => p != null && !p.HasFinished);
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

    // Botlar için: 6 geldikçe otomatik yeniden at
    private IEnumerator PlayTurnForPawn_Bot(Pawn pawn)
    {
        if (pawn == null) yield break;

        bool extraTurn;
        do
        {
            int rolled = 0;

            yield return dice.RollDiceCoroutine(v => rolled = v);
            yield return mover.MoveSelectedPawnCoroutine(pawn, rolled);

            extraTurn = (rolled == 6);

            if (extraTurn)
                Debug.Log($"{pawn.team} rolled 6 => extra turn (bot auto)");

        } while (extraTurn);
    }
}
