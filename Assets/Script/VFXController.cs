using System.Collections;
using UnityEngine;

public class VFXController : MonoBehaviour
{
    [Header("Effect Settings")]
    [Tooltip("GameObject chứa effect (VFX/Particle System)")]
    public GameObject effectObject;
    
    [Tooltip("Tự động tắt effect sau khoảng thời gian này (0 = không tự động tắt)")]
    public float autoDisableAfter = 0f;
    
    [Header("Particle System Settings")]
    [Tooltip("Nếu dùng Particle System, có restart khi play lại không")]
    public bool restartParticleOnPlay = true;

    private ParticleSystem[] particleSystems;
    private Coroutine autoDisableCoroutine;

    void Awake()
    {
        // Tìm tất cả Particle Systems trong effect object
        if (effectObject != null)
        {
            particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>();
        }
    }

    /// <summary>
    /// Kích hoạt effect
    /// </summary>
    public void PlayEffect()
    {
        if (effectObject == null)
        {
            Debug.LogWarning("Effect Object chưa được gán!");
            return;
        }

        // Bật GameObject
        effectObject.SetActive(true);

        // Nếu có Particle System, play nó
        if (particleSystems != null && particleSystems.Length > 0)
        {
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    if (restartParticleOnPlay)
                    {
                        ps.Stop();
                        ps.Clear();
                    }
                    ps.Play();
                }
            }
        }

        // Tự động tắt nếu cần
        if (autoDisableAfter > 0)
        {
            if (autoDisableCoroutine != null)
            {
                StopCoroutine(autoDisableCoroutine);
            }
            autoDisableCoroutine = StartCoroutine(AutoDisableRoutine());
        }
    }

    /// <summary>
    /// Tắt effect
    /// </summary>
    public void StopEffect()
    {
        if (effectObject == null) return;

        // Stop các Particle Systems
        if (particleSystems != null && particleSystems.Length > 0)
        {
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    ps.Stop();
                }
            }
        }

        // Tắt GameObject
        effectObject.SetActive(false);

        // Hủy coroutine auto disable nếu đang chạy
        if (autoDisableCoroutine != null)
        {
            StopCoroutine(autoDisableCoroutine);
            autoDisableCoroutine = null;
        }
    }

    /// <summary>
    /// Toggle effect on/off
    /// </summary>
    public void ToggleEffect()
    {
        if (effectObject == null) return;

        if (effectObject.activeSelf)
        {
            StopEffect();
        }
        else
        {
            PlayEffect();
        }
    }

    /// <summary>
    /// Kích hoạt effect trong khoảng thời gian nhất định
    /// </summary>
    public void PlayEffectForDuration(float duration)
    {
        autoDisableAfter = duration;
        PlayEffect();
    }

    private IEnumerator AutoDisableRoutine()
    {
        yield return new WaitForSeconds(autoDisableAfter);
        StopEffect();
    }

    void OnDestroy()
    {
        if (autoDisableCoroutine != null)
        {
            StopCoroutine(autoDisableCoroutine);
        }
    }
}
