using UnityEngine;

/// <summary>
/// Gắn lên collider (isTrigger = true) của từng checkpoint nếu muốn detect lap
/// theo trigger thay vì theo distance. Tự gọi RacePositionTracker.OnCheckpointPassed
/// khi xe chạm vào.
///
/// KHÔNG bắt buộc: RacePositionTracker đã tự đếm vòng dựa trên distance.
/// Component này chỉ là tùy chọn nếu bạn cần độ chính xác cao hơn.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [Tooltip("Index của checkpoint này trong list checkpoints của RacePositionTracker. -1 = auto từ thứ tự sibling.")]
    public int checkpointIndex = -1;

    [Tooltip("Tag của các xe (player + AI). Trigger chỉ phản hồi nếu collider có tag này hoặc parent có RacerTransform.")]
    public string racerTag = "Player";

    [Tooltip("Tham chiếu tới RacePositionTracker. Nếu null sẽ tự FindObjectOfType.")]
    public RacePositionTracker tracker;

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[CheckpointTrigger] Collider trên '{name}' chưa bật isTrigger. Đã tự bật.");
            col.isTrigger = true;
        }

        if (tracker == null)
            tracker = FindObjectOfType<RacePositionTracker>();

        if (checkpointIndex < 0 && transform.parent != null)
            checkpointIndex = transform.GetSiblingIndex();
    }

    void OnTriggerEnter(Collider other)
    {
        if (tracker == null) return;

        // Tìm root transform — racer có thể có nhiều child collider
        Transform racerRoot = other.attachedRigidbody != null
            ? other.attachedRigidbody.transform
            : other.transform.root;

        tracker.OnCheckpointPassed(racerRoot, checkpointIndex);
    }
}
