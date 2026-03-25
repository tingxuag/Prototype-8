using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class gameManagerScript : MonoBehaviour
{
    [Header("Cards")]
    public GameObject[] cards;              // Drag your 3 card GameObjects here

    [Header("Positions")]
    public Transform[] slots;              // Drag your 3 fixed position Transforms here
    public string[] slotNames;             // Display names for each slot, e.g. "Left", "Center", "Right"

    [Header("Shuffle Settings")]
    public int shuffleCount = 5;           // How many swaps to perform
    public float moveDuration = 0.4f;      // Seconds each card takes to reach its slot
    public float delayBetweenSwaps = 0.1f; // Pause between consecutive swaps

    [Header("Round Settings")]
    public float colorRevealDuration = 2f; // Seconds cards stay visible before turning gray
    public float roundEndDelay = 2f;       // Seconds to show the result before starting the next round
    public float accelerationFactor = 0.8f;// Speed multiplier applied to durations on each correct answer
    public float minMoveDuration = 0.05f;  // Floor for moveDuration so it never reaches zero

    [Header("UI")]
    public TextMeshProUGUI questionText;   // Drag UI/QuestionText here

    // Tracks which slot each card currently occupies (index into slots[])
    private int[] _cardSlotAssignment;
    private int[] _initialSlotAssignment;  // Snapshot before the shuffle begins
    private cardScript[] _cardScripts;

    private bool _isRoundActive = false;
    private int _correctCardIndex = -1;

    private float _currentMoveDuration;
    private float _currentDelayBetweenSwaps;

    private static readonly Color CorrectColor = Color.green;
    private static readonly Color WrongColor = Color.red;
    private static readonly Color DefaultTextColor = Color.white;

    private void Start()
    {
        CacheCardScripts();
        ResetSpeed();
    }

    private void Update()
    {
        if (!_isRoundActive && Keyboard.current[Key.F].wasPressedThisFrame)
            StartRound();

        if (Mouse.current.leftButton.wasPressedThisFrame)
            HandleMouseClick();
    }

    private void HandleMouseClick()
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        if (hit == null) return;

        for (int i = 0; i < _cardScripts.Length; i++)
        {
            if (hit.gameObject == cards[i] && _cardScripts[i].IsClickable())
            {
                HandleCardClicked(i);
                return;
            }
        }
    }

    private void CacheCardScripts()
    {
        _cardScripts = new cardScript[cards.Length];
        for (int i = 0; i < cards.Length; i++)
            _cardScripts[i] = cards[i].GetComponent<cardScript>();
    }

    /// <summary>
    /// Resets shuffle speed back to the base Inspector values.
    /// </summary>
    private void ResetSpeed()
    {
        _currentMoveDuration = moveDuration;
        _currentDelayBetweenSwaps = delayBetweenSwaps;
    }

    /// <summary>
    /// Applies the acceleration factor to speed up the next round's shuffle.
    /// </summary>
    private void AccelerateSpeed()
    {
        _currentMoveDuration = Mathf.Max(_currentMoveDuration * accelerationFactor, minMoveDuration);
        _currentDelayBetweenSwaps = Mathf.Max(_currentDelayBetweenSwaps * accelerationFactor, 0f);
    }

    /// <summary>
    /// Records the initial slot assignment, randomizes it, snaps each card, then takes a snapshot before shuffling.
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

        // Snapshot the layout before the shuffle so we can check the answer later
        _initialSlotAssignment = (int[])_cardSlotAssignment.Clone();
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

            _cardScripts[indexA].MoveTo(slots[slotB].position, _currentMoveDuration, () => cardADone = true);
            _cardScripts[indexB].MoveTo(slots[slotA].position, _currentMoveDuration, () => cardBDone = true);

            yield return new WaitUntil(() => cardADone && cardBDone);

            if (_currentDelayBetweenSwaps > 0f)
                yield return new WaitForSeconds(_currentDelayBetweenSwaps);
        }
    }

    private void ShowQuestion()
    {
        int askedSlotIndex = Random.Range(0, slots.Length);
        string slotLabel = (slotNames != null && askedSlotIndex < slotNames.Length)
            ? slotNames[askedSlotIndex]
            : $"position {askedSlotIndex + 1}";

        // Find which card was in that slot at the start of the round
        _correctCardIndex = -1;
        for (int i = 0; i < _initialSlotAssignment.Length; i++)
        {
            if (_initialSlotAssignment[i] == askedSlotIndex)
            {
                _correctCardIndex = i;
                break;
            }
        }

        questionText.color = DefaultTextColor;
        questionText.text = $"Which card was in the {slotLabel} position?";
        questionText.gameObject.SetActive(true);

        foreach (cardScript card in _cardScripts)
            card.SetClickable(true);
    }

    /// <summary>
    /// Handles a card click during the answer phase.
    /// </summary>
    private void HandleCardClicked(int clickedIndex)
    {
        foreach (cardScript card in _cardScripts)
        {
            card.SetClickable(false);
            card.RestoreOriginalColor();
        }

        bool correct = clickedIndex == _correctCardIndex;

        if (correct)
        {
            questionText.color = CorrectColor;
            questionText.text = "Correct!";
            AccelerateSpeed();
            StartCoroutine(StartNextRoundAfterDelay());
        }
        else
        {
            questionText.color = WrongColor;
            questionText.text = $"Wrong! It was the {cards[_correctCardIndex].name}.";
            ResetSpeed();
        }
    }

    private IEnumerator StartNextRoundAfterDelay()
    {
        yield return new WaitForSeconds(roundEndDelay);
        StartRound();
    }
}