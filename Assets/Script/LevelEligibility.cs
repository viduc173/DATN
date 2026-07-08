using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kiểm tra xe player có đủ chỉ số để chạy 1 màn cụ thể không.
///
/// Quy tắc: với từng stat, nếu `LevelSettings.statRequirements.X > 0` thì
/// `playerLoadout.GetEffectiveStats().X` phải ≥ giá trị đó. Stat = 0 trong
/// requirements = không yêu cầu (skip check).
///
/// Cách dùng (UI level select):
/// <code>
///   var result = LevelEligibility.Validate(currentLoadout, levelSettings);
///   if (result.isEligible) {
///       SceneManager.LoadScene(levelSettings.sceneName);
///   } else {
///       ShowPopup("Xe chưa đủ chỉ số:\n" + result.GetUserMessage());
///   }
/// </code>
/// </summary>
public static class LevelEligibility
{
    public struct StatGap
    {
        public string statName;
        public float current;
        public float required;
        public float gap;        // = required - current (luôn > 0 khi fail)

        public override string ToString() => $"{statName}: {current:0}/{required:0} (thiếu {gap:0})";
    }

    public struct Result
    {
        public bool isEligible;
        public List<StatGap> missingStats;

        /// <summary>Message gợi ý hiển thị UI khi fail.</summary>
        public string GetUserMessage()
        {
            if (isEligible) return "Đủ điều kiện ✓";
            if (missingStats == null || missingStats.Count == 0) return "Đủ điều kiện ✓";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Xe chưa đủ chỉ số:");
            foreach (var gap in missingStats)
                sb.AppendLine($"  • {gap}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Validate effective stats của loadout vs requirements của level.
    /// Trả Result với danh sách stats thiếu (nếu có).
    /// </summary>
    public static Result Validate(PlayerCarLoadout loadout, LevelSettings settings)
    {
        var result = new Result { missingStats = new List<StatGap>() };

        if (settings == null)
        {
            Debug.LogWarning("[LevelEligibility] LevelSettings null — coi như eligible.");
            result.isEligible = true;
            return result;
        }

        if (loadout == null)
        {
            Debug.LogWarning("[LevelEligibility] PlayerCarLoadout null — coi như eligible (sẽ dùng VehicleController default).");
            result.isEligible = true;
            return result;
        }

        var effective = loadout.GetEffectiveStats();
        var req = settings.statRequirements;

        AddGapIfMissing(result.missingStats, "Top Speed",    effective.maxSpeed,     req.maxSpeed);
        AddGapIfMissing(result.missingStats, "Acceleration", effective.acceleration, req.acceleration);
        AddGapIfMissing(result.missingStats, "Grip",         effective.grip,         req.grip);
        AddGapIfMissing(result.missingStats, "Braking",      effective.braking,      req.braking);
        AddGapIfMissing(result.missingStats, "Handling",     effective.handling,     req.handling);

        result.isEligible = result.missingStats.Count == 0;
        return result;
    }

    private static void AddGapIfMissing(List<StatGap> gaps, string name, float current, float required)
    {
        if (required <= 0f) return; // 0 = không yêu cầu
        if (current >= required) return; // pass

        gaps.Add(new StatGap
        {
            statName = name,
            current = current,
            required = required,
            gap = required - current,
        });
    }

    /// <summary>
    /// Convenience: check pass/fail không cần Result object.
    /// </summary>
    public static bool IsEligible(PlayerCarLoadout loadout, LevelSettings settings)
        => Validate(loadout, settings).isEligible;
}
