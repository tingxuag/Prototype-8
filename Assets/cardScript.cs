using System;
using System.Collections;
using UnityEngine;

public class cardScript : MonoBehaviour
{
    private Coroutine _moveCoroutine;

    private SpriteRenderer _spriteRenderer;
    private Color _originalColor;

    private bool _isClickable = false;

    private static readonly Color GrayColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    public event Action<cardScript> OnCardClicked;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalColor = _spriteRenderer.color;
    }

    void Start() { }
    void Update() { }

    /// <summary>
    /// Enables or disables click detection on this card.
    /// </summary>
    public void SetClickable(bool clickable)
    {
        _isClickable = clickable;
    }

    /// <summary>
    /// Returns whether this card is currently clickable.
    /// </summary>
    public bool IsClickable() => _isClickable;

    /// <summary>
    /// Sets the sprite color to gray.
    /// </summary>
    public void SetColorGray()
    {
        _spriteRenderer.color = GrayColor;
    }

    /// <summary>
    /// Restores the sprite color to the value it had when the object first awoke.
    /// </summary>
    public void RestoreOriginalColor()
    {
        _spriteRenderer.color = _originalColor;
    }

    /// <summary>
    /// Immediately teleports this GameObject to the target position, cancelling any move in progress.
    /// </summary>
    /// <param name="position">World-space position to snap to.</param>
    public void SnapTo(Vector3 position)
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        transform.position = position;
    }

    /// <summary>
    /// Moves this GameObject to the target position over the specified duration.
    /// If a move is already in progress, it is interrupted and the new one begins from the current position.
    /// </summary>
    /// <param name="targetPosition">World-space position to move to.</param>
    /// <param name="duration">Time in seconds to complete the move.</param>
    /// <param name="onComplete">Optional callback invoked when the move finishes.</param>
    public void MoveTo(Vector3 targetPosition, float duration, Action onComplete = null)
    {
        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(MoveCoroutine(targetPosition, duration, onComplete));
    }

    private IEnumerator MoveCoroutine(Vector3 targetPosition, float duration, Action onComplete)
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        _moveCoroutine = null;
        onComplete?.Invoke();
    }
}
