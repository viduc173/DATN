using System.Collections;
using UnityEngine;

public class ContinuousVFXSpawner : MonoBehaviour
{
    [Header("VFX Settings")]
    [Tooltip("VFX prefab to spawn")]
    public GameObject vfxPrefab;
    
    [Tooltip("Spawn interval in seconds")]
    public float spawnInterval = 0.2f;
    
    [Tooltip("Position offset from vehicle")]
    public Vector3 spawnOffset = Vector3.zero;
    
    [Tooltip("Auto destroy VFX after this time (0 = no auto destroy)")]
    public float vfxLifetime = 2f;
    
    [Header("Optional Settings")]
    [Tooltip("Spawn point transform (if null, uses this object's position)")]
    public Transform spawnPoint;
    
    [Tooltip("Enable spawning on start")]
    public bool spawnOnStart = true;

    [Header("Trigger Settings")]
    [Tooltip("Enable/Disable spawning (can be toggled at runtime)")]
    public bool isSpawningEnabled = true;
    
    [Tooltip("Use velocity trigger (spawn only when vehicle is moving)")]
    public bool useVelocityTrigger = false;
    
    [Tooltip("Minimum velocity to spawn VFX (only if useVelocityTrigger is true)")]
    public float minVelocityToSpawn = 0.5f;
    
    [Tooltip("Rigidbody to check velocity from (auto-detected if null)")]
    public Rigidbody vehicleRigidbody;

    private Coroutine spawnCoroutine;
    private bool wasSpawning = false;

    void Start()
    {
        // Auto-detect rigidbody if not assigned
        if (useVelocityTrigger && vehicleRigidbody == null)
        {
            vehicleRigidbody = GetComponent<Rigidbody>();
            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = GetComponentInParent<Rigidbody>();
            }
        }

        if (spawnOnStart)
        {
            StartSpawning();
        }
    }

    void Update()
    {
        // Handle velocity-based triggering
        if (useVelocityTrigger && vehicleRigidbody != null)
        {
            float currentVelocity = vehicleRigidbody.linearVelocity.magnitude;
            bool shouldSpawn = currentVelocity >= minVelocityToSpawn;

            if (shouldSpawn && !wasSpawning && isSpawningEnabled)
            {
                StartSpawning();
                wasSpawning = true;
            }
            else if ((!shouldSpawn || !isSpawningEnabled) && wasSpawning)
            {
                StopSpawning();
                wasSpawning = false;
            }
        }
        // Handle manual trigger
        else
        {
            if (isSpawningEnabled && !wasSpawning)
            {
                StartSpawning();
                wasSpawning = true;
            }
            else if (!isSpawningEnabled && wasSpawning)
            {
                StopSpawning();
                wasSpawning = false;
            }
        }
    }

    /// <summary>
    /// Start spawning VFX continuously
    /// </summary>
    public void StartSpawning()
    {
        if (spawnCoroutine == null && vfxPrefab != null)
        {
            spawnCoroutine = StartCoroutine(SpawnVFXRoutine());
        }
    }

    /// <summary>
    /// Stop spawning VFX
    /// </summary>
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    /// <summary>
    /// Toggle spawning on/off
    /// </summary>
    public void ToggleSpawning()
    {
        isSpawningEnabled = !isSpawningEnabled;
    }

    /// <summary>
    /// Enable spawning
    /// </summary>
    public void EnableSpawning()
    {
        isSpawningEnabled = true;
    }

    /// <summary>
    /// Disable spawning
    /// </summary>
    public void DisableSpawning()
    {
        isSpawningEnabled = false;
    }

    private IEnumerator SpawnVFXRoutine()
    {
        while (true)
        {
            SpawnVFX();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnVFX()
    {
        if (vfxPrefab == null)
        {
            Debug.LogWarning("VFX Prefab is not assigned!");
            return;
        }

        // Determine spawn position
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        if (spawnPoint != null)
        {
            spawnPosition = spawnPoint.position + spawnPoint.TransformDirection(spawnOffset);
            spawnRotation = spawnPoint.rotation;
        }
        else
        {
            spawnPosition = transform.position + transform.TransformDirection(spawnOffset);
            spawnRotation = transform.rotation;
        }

        // Instantiate VFX
        GameObject vfx = Instantiate(vfxPrefab, spawnPosition, spawnRotation);

        // Auto destroy if lifetime is set
        if (vfxLifetime > 0)
        {
            Destroy(vfx, vfxLifetime);
        }
    }

    void OnDestroy()
    {
        StopSpawning();
    }

    // Visualize spawn point in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 offset = spawnPoint != null ? spawnPoint.TransformDirection(spawnOffset) : transform.TransformDirection(spawnOffset);
        Gizmos.DrawWireSphere(position + offset, 0.1f);
    }
}
