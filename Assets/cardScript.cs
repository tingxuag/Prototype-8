using System;
using System.Collections;
using UnityEngine;

public class cardScript : MonoBehaviour
{
    private Coroutine _moveCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
