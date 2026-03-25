using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class gameManagerScript : MonoBehaviour
{
    [Header("Cards")]
    public GameObject[] cards;          // Drag your 3 card GameObjects here

    [Header("Positions")]
    public Transform[] slots;           // Drag your 3 fixed position Transforms here

    [Header("Shuffle Settings")]
    public int shuffleCount = 5;        // How many swaps to perform
    public float moveDuration = 0.4f;   // Seconds each card takes to reach its slot
    public float delayBetweenSwaps = 0.1f; // Pause between consecutive swaps

    // Tracks which slot each card currently occupies (index into slots[])
    private int[] _cardSlotAssignment;

    private bool _isShuffling = false;

    private void Start()
    {
        InitialiseSlotAssignments();
    }

    private void Update()
    {
        if (!_isShuffling && Keyboard.current[Key.F].wasPressedThisFrame)
            StartShuffle();
    }

    /// <summary>
    /// Records the initial slot assignment and snaps each card to its slot.
    /// Call this after assigning cards and slots in the Inspector.
    /// </summary>
    public void InitialiseSlotAssignments()
    {
        _cardSlotAssignment = new int[cards.Length];
        for (int i = 0; i < cards.Length; i++)
        {
            _cardSlotAssignment[i] = i;
            cards[i].transform.position = slots[i].position;
        }
    }

    /// <summary>
    /// Starts the shuffle sequence. Each round picks two cards at random and swaps their slots.
    /// All cards in a swap move simultaneously; the next swap begins after both arrive.
    /// </summary>
    public void StartShuffle()
    {
        if (_isShuffling) return;
        StartCoroutine(ShuffleCoroutine());
    }

    private IEnumerator ShuffleCoroutine()
    {
        _isShuffling = true;

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

            cards[indexA].GetComponent<cardScript>().MoveTo(slots[slotB].position, moveDuration, () => cardADone = true);
            cards[indexB].GetComponent<cardScript>().MoveTo(slots[slotA].position, moveDuration, () => cardBDone = true);

            yield return new WaitUntil(() => cardADone && cardBDone);

            if (delayBetweenSwaps > 0f)
                yield return new WaitForSeconds(delayBetweenSwaps);
        }

        _isShuffling = false;
    }
}
