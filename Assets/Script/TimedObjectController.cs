using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TimedObjectController : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("Thời gian duy trì GameObject khi bật (giây)")]
    public float activeDuration = 5f;

    [Header("Runtime Info")]
    [Tooltip("Thời gian còn lại (để xem trong Inspector)")]
    [SerializeField] private float timeRemaining = 0f;

    [Header("Events")]
    [Tooltip("Event được gọi trước khi GameObject tắt")]
    public UnityEvent onObjectDeactivated;

    private Coroutine timerCoroutine;

    void OnEnable()
    {
        // Tự động bắt đầu đếm ngược khi object được enable
        timeRemaining = activeDuration;
        
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        
        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    void OnDisable()
    {
        // Dừng timer khi object bị disable
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    private IEnumerator TimerRoutine()
    {
        timeRemaining = activeDuration;

        while (timeRemaining > 0)
        {
            yield return new WaitForSeconds(0.1f);
            timeRemaining -= 0.1f;
        }

        // Hết thời gian
        timeRemaining = 0f;

        // Gọi event trước khi tắt
        onObjectDeactivated?.Invoke();

        // Tắt chính GameObject này
        gameObject.SetActive(false);

        timerCoroutine = null;
    }
}
