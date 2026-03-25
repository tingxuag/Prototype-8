using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class gameManagerScript : MonoBehaviour
{
    [Header("Cards")]
    public GameObject[] cards;          // Drag your 3 card GameObjects here

    [Header("Positions")]
    public Transform[] slots;           // Drag your 3 fixed position Transforms here
    public string[] slotNames;          // Display names for each slot, e.g. "Left", "Center", "Right"

    [Header("Shuffle Settings")]
    public int shuffleCount = 5;        // How many swaps to perform
    public float moveDuration = 0.4f;   // Seconds each card takes to reach its slot
    public float delayBetweenSwaps = 0.1f; // Pause between consecutive swaps

    [Header("Round Settings")]
    public float colorRevealDuration = 2f;  // Seconds cards stay visible before turning gray

    [Header("UI")]
    public TextMeshProUGUI questionText; // Drag UI/QuestionText here

    // Tracks which slot each card currently occupies (index into slots[])
    private int[] _cardSlotAssignment;
    private cardScript[] _cardScripts;

    private bool _isRoundActive = false;

    private void Start()
    {
        CacheCardScripts();
    }

    private void Update()
    {
        if (!_isRoundActive && Keyboard.current[Key.F].wasPressedThisFrame)
            StartRound();
    }

    private void CacheCardScripts()
    {
        _cardScripts = new cardScript[cards.Length];
        for (int i = 0; i < cards.Length; i++)
            _cardScripts[i] = cards[i].GetComponent<cardScript>();
    }

    /// <summary>
    /// Records the initial slot assignment, randomizes it, then snaps each card to its slot.
    /// </summary>
    public void InitialiseSlotAssignments()
    {
        _cardSlotAssignment = new int[cards.Length];
        for (int i = 0; i < cards.Length; i++)
            _cardSlotAssignment[i] = i;

        // Fisher-Yates shuffle on slot assignments
        for (int i = _cardSlotAssignment.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = _cardSlotAssignment[i];
            _cardSlotAssignment[i] = _cardSlotAssignment[randomIndex];
            _cardSlotAssignment[randomIndex] = temp;
        }

        for (int i = 0; i < cards.Length; i++)
            _cardScripts[i].SnapTo(slots[_cardSlotAssignment[i]].position);
    }

    /// <summary>
    /// Starts a new round: reveals card colors, grays them out, shuffles, then shows the question.
    /// </summary>
    public void StartRound()
    {
        if (_isRoundActive) return;
        StartCoroutine(RoundCoroutine());
    }

    private IEnumerator RoundCoroutine()
    {
        _isRoundActive = true;

        questionText.gameObject.SetActive(false);

        InitialiseSlotAssignments();

        foreach (cardScript card in _cardScripts)
            card.RestoreOriginalColor();

        yield return new WaitForSeconds(colorRevealDuration);

        foreach (cardScript card in _cardScripts)
            card.SetColorGray();

        yield return ShuffleCoroutine();

        ShowQuestion();

        _isRoundActive = false;
    }

    private IEnumerator ShuffleCoroutine()
    {
        for (int round = 0; round < shuffleCount; round++)
        {
            // Pick two distinct random card indices to swap
            int indexA = Random.Range(0, cards.Length);
            int indexB;
            do { indexB = Random.Range(0, cards.Length); }
            while (indexB == indexA);

            // Swap their slot assignments
            int slotA = _cardSlotAssignment[indexA];
            int slotB = _cardSlotAssignment[indexB];
            _cardSlotAssignment[indexA] = slotB;
            _cardSlotAssignment[indexB] = slotA;

            // Move both cards simultaneously; wait for both to arrive
            bool cardADone = false;
            bool cardBDone = false;

            _cardScripts[indexA].MoveTo(slots[slotB].position, moveDuration, () => cardADone = true);
            _cardScripts[indexB].MoveTo(slots[slotA].position, moveDuration, () => cardBDone = true);

            yield return new WaitUntil(() => cardADone && cardBDone);

            if (delayBetweenSwaps > 0f)
                yield return new WaitForSeconds(delayBetweenSwaps);
        }
    }

    private void ShowQuestion()
    {
        int askedSlotIndex = Random.Range(0, slots.Length);
        string slotLabel = (slotNames != null && askedSlotIndex < slotNames.Length)
            ? slotNames[askedSlotIndex]
            : $"position {askedSlotIndex + 1}";

        questionText.text = $"Which card was in the {slotLabel} position?";
        questionText.gameObject.SetActive(true);
    }
}
