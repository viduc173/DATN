using UnityEngine;
using TMPro;

/// <summary>
/// Marker gắn vào mỗi TextMeshProUGUI để đánh dấu vai trò trong HUD đua xe.
/// RacePositionTracker và UIReceiver tự FindObjectsOfType để auto-assign — không cần drag-drop tay.
/// </summary>
public enum RaceUIRole
{
    Position1,
    Position2,
    Position3,
    Position4,
    Position5,
    Position6,
    PlayerPosition,
    WaitCountdown,
}

[RequireComponent(typeof(TextMeshProUGUI))]
public class RaceUILabel : MonoBehaviour
{
    public RaceUIRole role;

    private TextMeshProUGUI _tmp;
    public TextMeshProUGUI TMP
    {
        get
        {
            if (_tmp == null) _tmp = GetComponent<TextMeshProUGUI>();
            return _tmp;
        }
    }
}
