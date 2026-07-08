using UnityEngine;

/// <summary>
/// Bridge tĩnh truyền PlayerCarLoadout từ garage sang racing scene.
/// Static field tồn tại suốt session (kể cả khi chuyển scene).
/// GarageCarManager set Current khi đổi xe. LevelController đọc Current.
/// </summary>
public static class ActiveLoadout
{
    private const string PREF_KEY = "ActiveCarIndex";

    /// <summary>Loadout của xe đang được chọn. Racing scene đọc từ đây.</summary>
    public static PlayerCarLoadout Current { get; set; }

    /// <summary>Index xe đã chọn — persist qua PlayerPrefs để sống qua session.</summary>
    public static int SavedCarIndex
    {
        get => PlayerPrefs.GetInt(PREF_KEY, 0);
        set { PlayerPrefs.SetInt(PREF_KEY, value); PlayerPrefs.Save(); }
    }

    /// <summary>True nếu garage đã từng ghi lựa chọn xe (phân biệt "chưa chọn" với "chọn index 0").</summary>
    public static bool HasSavedCar => PlayerPrefs.HasKey(PREF_KEY);
}
