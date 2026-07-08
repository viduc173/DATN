# Race Stats System — Architecture Design

**Tài liệu:** thiết kế kiến trúc tổng cho hệ thống chỉ số xe + linh kiện + gate vào màn.
**Phạm vi:** garage (lắp/tháo linh kiện), gate eligibility (xe có đủ chỉ số để chạy màn không), race scene (apply chỉ số xe người chơi vào VehicleController vật lý).
**Ngày tạo:** 2026-05-26

---

## 1. Gameplay loop (tổng quan)

```
┌─────────┐    tháo/lắp     ┌──────────────┐   eligibility    ┌──────────┐
│ GARAGE  │ ───────────────▶│ PLAYER       │ ─────────────────▶│  LEVEL   │
│ scene   │  CarPart asset  │ CARLOADOUT   │   gate check     │ SETTINGS │
│         │ ◀───────────────│ asset        │ ◀─────────────────│ asset    │
└─────────┘   currentStats  └──────┬───────┘  requirements    └────┬─────┘
                                   │                               │
                                   │  GetEffectiveStats()          │
                                   │  → CarStats(0-100)            │
                                   ▼                               ▼
                            ┌──────────────────────────────────────────┐
                            │ LevelController (in race scene)          │
                            │ 1. Validate stats ≥ requirements         │
                            │ 2. Map abstract 0-100 → VehicleController│
                            │ 3. Apply lên xe player                   │
                            └──────────────────────────────────────────┘
```

**Bước:**
1. **Garage:** player thay đổi `PlayerCarLoadout.equippedParts` (add/remove `CarPart`).
2. **Eligibility check:** trước khi load race scene, gọi `LevelEligibility.Validate(loadout, levelSettings)`. Nếu fail → block (UI báo "Cần `maxSpeed ≥ 60` để chạy màn này"). Nếu pass → load scene.
3. **Race scene:** `LevelController.Awake()` đọc `LevelSettings + PlayerCarLoadout`, map stats abstract sang physics, apply vào `VehicleController` của xe player.

---

## 2. Data model

### 2.1. `CarStats` — 5 stat trung tính 0-100

Là **chỉ số game-design** mà player nhìn thấy trên UI (slider 0-100). Không phải giá trị physics thật.

```csharp
[Serializable]
public class CarStats {
    [Range(0,100)] float maxSpeed;      // Top speed
    [Range(0,100)] float acceleration;  // 0-100 km/h time
    [Range(0,100)] float grip;          // Tire friction (vào cua)
    [Range(0,100)] float braking;       // Khoảng cách phanh
    [Range(0,100)] float handling;      // Góc bẻ lái (cua gắt)

    public static CarStats operator +(CarStats a, CarStats b) → clamp 0-100
    public static CarStats operator -(CarStats a, CarStats b) → clamp 0-100
}
```

**Quy ước:** 50 = baseline (xe gốc của VehicleController). 0 = tệ nhất chơi được. 100 = max tier max upgrade.

### 2.2. `CarPart` — 1 linh kiện

```csharp
[CreateAssetMenu("Race/Car Part")]
public class CarPart : ScriptableObject {
    string partName;
    PartSlot slot;       // Engine/Tires/Brakes/Suspension/Body/Aero/Other
    string description;
    Sprite icon;
    CarStats statBonus;  // Delta cộng vào base. Có thể âm/dương.
    int tier;            // 1, 2, 3 — UI sort
    int costGold;        // Giá shop
}
```

**Ví dụ assets:**
- `TurboV2.asset`: slot=Engine, statBonus.acceleration=+15, statBonus.maxSpeed=+5
- `SportTires.asset`: slot=Tires, statBonus.grip=+15, statBonus.handling=+5, statBonus.maxSpeed=-3 (trade-off)
- `HeavyArmor.asset`: slot=Body, statBonus.braking=+10, statBonus.acceleration=-8 (xe nặng = chậm)

### 2.3. `PlayerCarLoadout` — bộ xe + linh kiện player đang dùng

```csharp
[CreateAssetMenu("Race/Player Car Loadout")]
public class PlayerCarLoadout : ScriptableObject {
    string loadoutName;
    GameObject carPrefab;        // (tùy chọn) spawn từ prefab
    CarStats baseStats;          // Stats xe gốc (mặc định 50/50/50/50/50)
    List<CarPart> equippedParts; // Parts đang lắp

    CarStats GetEffectiveStats() {
        var s = baseStats;
        foreach (p in equippedParts) s = s + p.statBonus;  // clamp 0-100
        return s;
    }

    bool EquipPart(CarPart);
    bool UnequipPart(CarPart);
}
```

**Tại sao là ScriptableObject:** 1 asset đại diện cho "save data" của 1 player. Trong Editor, modify asset → persist. Build runtime cần thêm save-to-disk layer (xem mục 6).

### 2.4. `LevelSettings` — config 1 màn

```csharp
[CreateAssetMenu("Race/Level Settings")]
public class LevelSettings : ScriptableObject {
    [Header("Race Rules")]
    int totalLaps;
    int lapWrapThreshold;

    [Header("Match Setup")]
    float countdownTime;

    [Header("Stat Requirements (gate)")]
    CarStats statRequirements;   // ✨ MỚI: xe phải có stats ≥ giá trị này
                                 //   stat = 0 trong requirements = không yêu cầu
    string requirementsDescription; // UI hint vd "Yêu cầu: Top Speed 60+, Grip 50+"

    [Header("Player Car & Stats")]
    PlayerCarLoadout playerLoadout; // null = dùng global current loadout
    bool applyPlayerStatOverrides;
    bool spawnPlayerCarFromPrefab;

    [Header("On Race Finish")]
    bool autoLoadSceneOnFinish;
    string endSceneName;
    float loadSceneDelay;
}
```

---

## 3. Chỉ số tác động lên xe (mapping abstract → physics)

### 3.0. Triết lý — Option C (Relative to prefab baseline) + clamp cứng

**Quy tắc tổng:** stat=50 → giá trị physics **không đổi so với prefab gốc**. Mỗi prefab có cá tính riêng (sport car vs truck) — CarStats chỉ MODIFY tương đối, không ép về 1 chuẩn industry.

```
final = clamp( base × Lerp(LO, HI, stat/100) , absolute_min, absolute_max )
       ↑           ↑                              ↑
       giá trị     scale factor                  giới hạn vật lý cứng
       prefab gốc  (range tùy stat)              (chống prefab quái gở)
```

**Tại sao có 2 lớp limit (LO/HI tương đối + absolute_min/max):**
- LO/HI: control "mức độ tác động" của stat. Vd acceleration LO=0.5/HI=2.0 → max ×4 lần (rộng). Grip LO=0.7/HI=1.3 → max ×1.86 (hẹp, vì friction breakage).
- Absolute clamp: bảo vệ physics ngay cả khi designer set prefab base sai bét. Vd maxSteerAngle dù prefab set 80° + handling=100 cũng bị cắt về 48°.

### 3.1. `maxSpeed` (0-100) → `VehicleController.maxSpeedForward` (m/s)

```csharp
final = Clamp(base.maxSpeedForward * Lerp(0.5f, 2.0f, stat/100), 8f, 60f)
```

| Stat | Multiplier | Vd prefab base=27.78 (default) | km/h | Ghi chú |
|---|---|---|---|---|
| 0   | ×0.5  | 13.89 | 50  | Xe rất yếu |
| 50  | ×1.0  | 27.78 | 100 | Y hệt prefab gốc |
| 100 | ×2.0  | 55.56 | 200 | Top tier |

**Quyết định:** Top speed cuối track thẳng. Range rộng (×0.5..×2.0) vì top speed scale tốt tuyến tính. Clamp tuyệt đối [8, 60] m/s (~30..216 km/h).

### 3.2. `acceleration` (0-100) → `VehicleController.maxDriveForce` (N)

```csharp
final = Clamp(base.maxDriveForce * Lerp(0.5f, 2.0f, stat/100), 300f, 6000f)
```

| Stat | Multiplier | Vd prefab base=2000 |
|---|---|---|
| 0   | ×0.5  | 1000 |
| 50  | ×1.0  | 2000 |
| 100 | ×2.0  | 4000 |

**Quyết định:** Lực kéo từ engine. Quan trọng ở start race + thoát cua. Range rộng vì người chơi cảm nhận rõ "ga khỏe" hay "ga yếu". Clamp [300, 6000] N.

### 3.3. `grip` (0-100) → **COMPOSITE** (2 fields: tireFriction + maxDriveSlip)

`grip` cao = bám hơn ở 2 mặt: ma sát tăng + ngưỡng drift giảm. Để feel rõ rệt, modify 2 fields **cùng lúc**:

```csharp
float t = stat / 100f;
final.tireFriction = Clamp(base.tireFriction * Lerp(0.7f, 1.3f, t), 0.5f, 1.6f);
final.maxDriveSlip = Clamp(base.maxDriveSlip * Lerp(1.4f, 0.7f, t), 2f,   8f);
//                                              ↑     ↑
//                                              t=0 → ×1.4 (drift sớm)
//                                              t=1 → ×0.7 (drift muộn)
```

| Stat | tireFriction (base 1.0) | maxDriveSlip (base 4.0) | Cảm giác |
|---|---|---|---|
| 0   | 0.70 | 5.6  | Lốp trơn + drift dễ — xe trượt liên tục |
| 50  | 1.00 | 4.0  | Y hệt prefab gốc |
| 100 | 1.30 | 2.8  | Lốp bám + khó drift — vào cua sắc không trượt |

**Range hẹp ([0.7, 1.3] cho friction)** vì vượt ra ngoài physics breakage (tires "keo" hoặc "băng"). Clamp tuyệt đối [0.5, 1.6].

**Quyết định:** Bám đường tổng hợp. Composite cho feel "bám" rõ ràng — player thấy ngay là drift khó hơn khi grip cao.

### 3.4. `braking` (0-100) → `VehicleController.maxBrakeForce` (N)

```csharp
final = Clamp(base.maxBrakeForce * Lerp(0.5f, 2.0f, stat/100), 800f, 8000f)
```

| Stat | Multiplier | Vd prefab base=3000 |
|---|---|---|
| 0   | ×0.5  | 1500 |
| 50  | ×1.0  | 3000 |
| 100 | ×2.0  | 6000 |

**Quyết định:** Khoảng cách phanh. Quan trọng trước cua gấp — phanh khỏe → vào cua chậm hơn rồi tăng tốc ra. Clamp [800, 8000] N.

### 3.5. `handling` (0-100) → **COMPOSITE** (2 fields: maxSteerAngle + antiRoll)

`handling` cao = cua gắt hơn + body ít lắc/lật khi vào cua tốc độ cao:

```csharp
float t = stat / 100f;
final.maxSteerAngle = Clamp(base.maxSteerAngle * Lerp(0.75f, 1.25f, t), 22f,   48f);
final.antiRoll      = Clamp(base.antiRoll      * Lerp(0.6f,  1.5f,  t), 0.05f, 0.5f);
```

| Stat | maxSteerAngle (base 35°) | antiRoll (base 0.2) | Cảm giác |
|---|---|---|---|
| 0   | 26.25° | 0.12 | Bẻ lái cứng + body lắc — cua chậm và xe đẩy hông |
| 50  | 35.00° | 0.20 | Y hệt prefab gốc |
| 100 | 43.75° | 0.30 | Bẻ lái sắc + body cứng — cua gắt không lật |

**Range hẹp ([0.75, 1.25] cho steerAngle)** vì wheel angle có giới hạn vật lý cứng (> 50° = wheel clip vào body).

**Quyết định:** Khả năng vào cua tổng hợp. Composite cho feel "linh hoạt" rõ ràng — player thấy ngay là xe cua gắt hơn + không bị xô khi vào cua tốc độ cao.

### 3.6. Bảng tổng mapping (cheat sheet)

| Stat | VehicleController fields | Multiplier range | Absolute clamp |
|---|---|---|---|
| `maxSpeed` | `maxSpeedForward` | ×0.5 .. ×2.0 | [8, 60] m/s |
| `acceleration` | `maxDriveForce` | ×0.5 .. ×2.0 | [300, 6000] N |
| `grip` (composite) | `tireFriction` + `maxDriveSlip` | ×0.7..×1.3 + ×1.4..×0.7 | [0.5, 1.6] + [2, 8] |
| `braking` | `maxBrakeForce` | ×0.5 .. ×2.0 | [800, 8000] N |
| `handling` (composite) | `maxSteerAngle` + `antiRoll` | ×0.75..×1.25 + ×0.6..×1.5 | [22°, 48°] + [0.05, 0.5] |

**Tổng cộng: 5 stats → 7 fields VehicleController.** 3 stat simple (1-to-1) + 2 stat composite (1-to-2).

### 3.7. Bảng "build chiến thuật"

| Loại track | Cần stat | Loại part khuyên |
|---|---|---|
| Drag/Highway thẳng | maxSpeed + acceleration | Turbo, Aero (giảm drag) |
| Mountain hairpin | handling + braking + grip | Sport tires, big brakes, light body |
| Mixed | Cân bằng tất cả | Tier 2 mỗi slot |
| Rally/Drift | grip thấp, handling cao | Drift tires (giảm grip), quick steering |

### 3.8. Edge cases & lưu ý implementation

**1. "Đọc base" timing — phải đọc TRƯỚC khi override:**
```csharp
void Awake() {
    // BƯỚC 1: cache base values từ prefab/scene
    cachedBase.maxSpeedForward = vc.maxSpeedForward;
    cachedBase.maxDriveForce   = vc.maxDriveForce;
    // ... etc

    // BƯỚC 2: apply override
    ApplyStatsTo(vc, loadout.GetEffectiveStats());
}
```
Nếu apply trước khi cache, lần thứ 2 ApplyStatsTo() sẽ scale base đã bị scale → compound. **Phải cache 1 lần ở Awake và dùng cache trong các lần re-apply.**

**2. Re-apply khi loadout thay đổi runtime:**
Nếu player thay linh kiện mid-race (vd qua pitstop), gọi `LevelController.ReapplyLoadout()` — đọc effective stats mới, áp dụng từ `cachedBase` (không từ giá trị hiện tại).

**3. Composite stats có "ngược chiều":**
- `grip` cao → `tireFriction` ×↑ NHƯNG `maxDriveSlip` ×↓ (giảm) — đây là **intentional**: bám cao = drift threshold thấp.
- Nếu Lerp dùng nhầm thứ tự (vd `Lerp(0.7, 1.4)` thay vì `Lerp(1.4, 0.7)`) — feel sẽ ngược (grip cao = drift dễ). Test kỹ.

**4. Clamp lifecycle:**
- Multiplier clamp (LO/HI): tunable design-time qua hằng số trong LevelController.
- Absolute clamp (min/max): hardcoded — bảo vệ physics. Designer KHÔNG được vượt qua kể cả khi prefab quái gở.

---

## 4. Eligibility gate

### 4.1. Logic

```csharp
public static class LevelEligibility {
    public struct Result {
        public bool isEligible;
        public List<string> missingStats;  // "maxSpeed: 45 / 60", ...
    }

    public static Result Validate(PlayerCarLoadout loadout, LevelSettings settings) {
        var effective = loadout.GetEffectiveStats();
        var req = settings.statRequirements;
        var missing = new List<string>();

        if (req.maxSpeed     > 0 && effective.maxSpeed     < req.maxSpeed)     missing.Add($"maxSpeed: {effective.maxSpeed:0}/{req.maxSpeed:0}");
        if (req.acceleration > 0 && effective.acceleration < req.acceleration) missing.Add($"acceleration: {effective.acceleration:0}/{req.acceleration:0}");
        if (req.grip         > 0 && effective.grip         < req.grip)         missing.Add($"grip: {effective.grip:0}/{req.grip:0}");
        if (req.braking      > 0 && effective.braking      < req.braking)      missing.Add($"braking: {effective.braking:0}/{req.braking:0}");
        if (req.handling     > 0 && effective.handling     < req.handling)     missing.Add($"handling: {effective.handling:0}/{req.handling:0}");

        return new Result { isEligible = missing.Count == 0, missingStats = missing };
    }
}
```

**Quy ước:** `statRequirements.X = 0` nghĩa là không yêu cầu stat X. Vd màn "Drag" chỉ cần maxSpeed=70, các stat khác = 0 (không check).

### 4.2. UI flow (đề xuất)

```
[Level Select scene]
  Player chọn màn → gọi LevelEligibility.Validate(currentLoadout, level.settings)
    ├── pass → SceneManager.LoadScene(level.sceneName)
    └── fail → popup "Xe chưa đủ chỉ số":
                  • Top Speed: 45 / 60 ❌
                  • Grip: 52 / 50 ✅
                  [Quay lại Garage]
```

### 4.3. Ở race scene

`LevelController.Awake()` cũng gọi `Validate()` defensively — nếu fail (vd player cheat skip menu), log warning + cho chạy (không block) HOẶC SceneManager.LoadScene("GarageLobby_pc"). Quyết định tùy game design.

---

## 5. Flow chi tiết: Garage → Race

### 5.1. Garage scene
- Hiển thị inventory part assets player sở hữu.
- Player drag part vào slot → gọi `currentLoadout.EquipPart(part)`.
- UI hiển thị real-time `currentLoadout.GetEffectiveStats()` (xem `CarStatsUIManager`).
- Save: nếu là Editor → ScriptableObject tự persist. Build → cần save layer (mục 6).

### 5.2. Level select
- Render danh sách levels. Mỗi level có `LevelSettings` asset.
- Cho mỗi level, hiển thị `LevelEligibility.Validate(currentLoadout, level)` → tick xanh / X đỏ.
- Click level → nếu eligible → SceneManager.LoadScene(level.sceneName).
- Truyền `currentLoadout` xuống race scene qua 1 trong các cách (mục 6).

### 5.3. Race scene
- `LevelController.Awake()` chạy execOrder -100, TRƯỚC `RacePositionTracker.Start()`.
- Đọc `settings: LevelSettings`. Lấy loadout từ:
  - Nếu `settings.playerLoadout != null` → dùng nó.
  - Else → fallback `GameSession.currentLoadout` (singleton).
- `ApplyStatsTo(playerVehicle, loadout.GetEffectiveStats())` map abstract → physics.
- Cập nhật `RacePositionTracker.totalLaps`, `MatchWaitTime.waitTime`.

### 5.4. Race finish
- `RacePositionTracker.MarkRacerFinished(player)` → `LoadEndSceneAfterDelay(endSceneName)`.
- Quay lại garage. Loadout persist nguyên (không bị tháo linh kiện sau race).

---

## 6. Persistence: làm sao loadout sống qua scene + save game

### 6.1. Editor only (đủ cho dev/test)
- ScriptableObject .asset file modify trực tiếp = persist tự động.
- Hạn chế: Play Mode modifications **reset** khi Stop Play (Unity Editor behavior). Để fix:
  - Modify trong Edit Mode (ngoài Play).
  - HOẶC dùng `[SerializeField]` runtime override + manually call `EditorUtility.SetDirty(loadout)` + `AssetDatabase.SaveAssets()`.

### 6.2. Runtime / build
Asset modifications KHÔNG persist trong build. Cần save layer:

**Option A — JSON file:**
```csharp
public static class LoadoutSaveSystem {
    public static void Save(PlayerCarLoadout loadout) {
        var data = new SaveData {
            loadoutName = loadout.loadoutName,
            equippedPartGuids = loadout.equippedParts.Select(p => p.name).ToList(),
            baseStats = loadout.baseStats,
        };
        File.WriteAllText(Application.persistentDataPath + "/loadout.json", JsonUtility.ToJson(data));
    }
    public static void Load(PlayerCarLoadout target) { ... }
}
```

**Option B — PlayerPrefs (simpler nhưng giới hạn):**
```csharp
PlayerPrefs.SetString("loadout", JsonUtility.ToJson(loadout));
```

### 6.3. Chia sẻ loadout qua scene transition

**Option A — Static field (đơn giản nhất):**
```csharp
public static class GameSession {
    public static PlayerCarLoadout currentLoadout;
}
```
Garage set `GameSession.currentLoadout = mySaved`. Race scene đọc.

**Option B — DontDestroyOnLoad GameObject:**
```csharp
public class GameSessionManager : MonoBehaviour {
    public static GameSessionManager Instance;
    public PlayerCarLoadout currentLoadout;
    void Awake() {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

**Option C — LevelSettings.playerLoadout override (hiện tại):**
Mỗi LevelSettings asset reference 1 PlayerCarLoadout cụ thể. Phù hợp khi mỗi màn force xe khác nhau (vd "tutorial level chỉ dùng StarterCar").

Hiện code dùng option C. Migrate sang A+C hybrid khi build production.

---

## 7. Extension points (mở rộng tương lai)

### 7.1. Thêm stat mới
1. Add field vào `CarStats` (vd `weight`, `aero`).
2. Update `operator +/-` để clamp.
3. Update `CarStatsUIManager.StatRow` nếu cần UI.
4. Add mapping trong `LevelController.ApplyStatsTo()`.

### 7.2. Slot uniqueness
Hiện code không enforce 1 slot = 1 part (player có thể lắp 2 Engine cùng lúc). Để enforce:
```csharp
public bool EquipPart(CarPart newPart) {
    var existing = equippedParts.FirstOrDefault(p => p.slot == newPart.slot);
    if (existing != null) UnequipPart(existing);
    equippedParts.Add(newPart);
    return true;
}
```

### 7.3. Conditional / non-linear effects
Vd "Nitrous": +30 acceleration TEMPORARY khi player nhấn Boost. Không nằm trong CarStats — implement riêng dạng MonoBehaviour trên xe player, đọc input, modify `vc.maxDriveForce` temporarily.

### 7.4. Per-level stat modifiers (handicap/buff)
Vd "Rain stage: -20% grip cho tất cả xe". Add field `LevelSettings.statModifier: CarStats` (multiplicative), apply sau khi tính `GetEffectiveStats()`:
```csharp
var stats = loadout.GetEffectiveStats();
stats.grip *= (1f + levelSettings.statModifier.grip / 100f); // -20 → *0.8
```

### 7.5. AI car stats
Mỗi AI car có thể có riêng `PlayerCarLoadout`-style config. Currently AI dùng VehicleController defaults. Để buff/nerf AI:
- Tạo `AICarConfig: ScriptableObject` tương tự `PlayerCarLoadout` nhưng cho AI.
- `LevelSettings.aiConfigs: List<AICarConfig>`.
- `LevelController.Awake()` áp dụng tương tự.

---

## 8. File tree (sau khi implement đầy đủ)

```
Assets/
├── Scenes/
│   ├── Design/
│   │   └── RaceStats_Architecture.md       ← bạn đang đọc
│   ├── 5_GarageLobby_pc/
│   │   └── GarageLobby_pc.unity             ← garage hub thật
│   ├── 3_CyberRace/
│   │   ├── CyberRace.unity
│   │   ├── LevelSettings_CyberRace.asset
│   │   └── README.md
│   ├── 6_Stadium_Sunny/  7_Stadium_Rain/  8_Beach_Sunny/   ← scene đua đang dùng
│   └── 4_Racing_Circuit/                    ← chưa vào game
└── Script/
    ├── Interact Tool/
    │   ├── CarStats.cs                     ← 5 stats 0-100 (đã có)
    │   ├── CarStatsUIManager.cs            ← UI slider animate (đã có)
    │   └── ...
    ├── CarPart.cs                          ← SO 1 part
    ├── PlayerCarLoadout.cs                 ← SO bộ xe player
    ├── LevelSettings.cs                    ← SO config màn
    ├── LevelEligibility.cs                 ← ⚠️ chưa tạo — gate check
    ├── LevelController.cs                  ← MonoBehaviour applier
    ├── RacePositionTracker.cs              ← xếp hạng + lap (đã có)
    ├── MatchWaitTime.cs                    ← countdown (đã có)
    └── RaceSettings.cs                     ← legacy, sẽ deprecate
```

---

## 9. Trạng thái implementation hiện tại

| Component | Tình trạng | Ghi chú |
|---|---|---|
| `CarStats` | ✅ Có sẵn | `Interact Tool/CarStats.cs`, 5 stats 0-100 với operator +/-, tự clamp |
| `CarPart` | ✅ | SO, slot enum + statBonus delta |
| `PlayerCarLoadout` | ✅ | `GetEffectiveStats()` cộng dồn parts |
| `LevelSettings` | ✅ | Có sẵn `statRequirements` + `requirementsDescription` |
| `LevelEligibility.Validate` | ✅ | Static helper, trả `Result { isEligible, missingStats[] }` |
| `LevelController` (Apply Option A — absolute) | ⚠️ Cần refactor | Hiện đang dùng absolute override với SDK defaults — cần đổi sang Option C + clamped + composite |
| `LevelController.ReapplyLoadout()` runtime | ❌ Chưa có | Để gọi sau pitstop / thay linh kiện mid-race |
| `GameSession` singleton | ❌ Chưa có | Chia sẻ loadout qua scene transitions |
| Save/Load JSON | ❌ Chưa có | Cho build runtime persistence |

### 9.1. Refactor plan cho `LevelController` (Phase 1)

Theo design ở mục 3, cần thay `ApplyStatsTo()` và `MapStat()` hiện tại bằng implementation Option C + clamped + composite:

```csharp
// Cache base values lúc Awake() — chỉ 1 lần
private struct PhysicsBaseline {
    public float maxSpeedForward, maxDriveForce, tireFriction,
                 maxDriveSlip, maxBrakeForce, maxSteerAngle, antiRoll;
}
private PhysicsBaseline cachedBase;

void Awake() {
    // ... resolve refs, spawn car ...
    CachePhysicsBaseline();
    ApplyToRaceTracker();
    ApplyToMatchWaitTime();
    ApplyPlayerLoadoutStats();
}

private void CachePhysicsBaseline() {
    if (playerVehicle == null) return;
    cachedBase.maxSpeedForward = playerVehicle.maxSpeedForward;
    cachedBase.maxDriveForce   = playerVehicle.maxDriveForce;
    cachedBase.tireFriction    = playerVehicle.tireFriction;
    cachedBase.maxDriveSlip    = playerVehicle.maxDriveSlip;
    cachedBase.maxBrakeForce   = playerVehicle.maxBrakeForce;
    cachedBase.maxSteerAngle   = playerVehicle.maxSteerAngle;
    cachedBase.antiRoll        = playerVehicle.antiRoll;
}

private void ApplyStatsTo(VehicleController vc, CarStats stats) {
    // Simple stats (3 fields)
    vc.maxSpeedForward = MapMul(cachedBase.maxSpeedForward, stats.maxSpeed,     0.5f, 2.0f, 8f, 60f);
    vc.maxDriveForce   = MapMul(cachedBase.maxDriveForce,   stats.acceleration, 0.5f, 2.0f, 300f, 6000f);
    vc.maxBrakeForce   = MapMul(cachedBase.maxBrakeForce,   stats.braking,      0.5f, 2.0f, 800f, 8000f);

    // Composite: grip → tireFriction + maxDriveSlip (ngược chiều)
    float gripT = stats.grip / 100f;
    vc.tireFriction = Mathf.Clamp(cachedBase.tireFriction * Mathf.Lerp(0.7f, 1.3f, gripT), 0.5f, 1.6f);
    vc.maxDriveSlip = Mathf.Clamp(cachedBase.maxDriveSlip * Mathf.Lerp(1.4f, 0.7f, gripT), 2f,   8f);

    // Composite: handling → maxSteerAngle + antiRoll
    float handT = stats.handling / 100f;
    vc.maxSteerAngle = Mathf.Clamp(cachedBase.maxSteerAngle * Mathf.Lerp(0.75f, 1.25f, handT), 22f,   48f);
    vc.antiRoll      = Mathf.Clamp(cachedBase.antiRoll      * Mathf.Lerp(0.6f,  1.5f,  handT), 0.05f, 0.5f);
}

private static float MapMul(float baseValue, float stat, float lo, float hi, float absMin, float absMax) {
    float t = Mathf.Clamp01(stat / 100f);
    return Mathf.Clamp(baseValue * Mathf.Lerp(lo, hi, t), absMin, absMax);
}
```

**Test cases sau refactor:**
1. Loadout 50/50/50/50/50 + xe prefab default → physics fields y hệt prefab base (không đổi). Verify với log.
2. Loadout 100/50/50/50/50 → `maxSpeedForward = base × 2.0` (clamp 60).
3. Loadout với prefab quái gở (maxSteerAngle prefab = 80°) + handling=100 → final ≤ 48° (absolute clamp hoạt động).
4. Re-apply scenario: gọi `ReapplyLoadout()` 3 lần → final value KHÔNG compound (cache hoạt động).

### 9.2. Phase 2 (sau khi Phase 1 playtested)

- AnimationCurve per-stat thay cho Lerp tuyến tính.
- Per-level handicap (vd "Rain stage: grip ×0.8 toàn bộ xe").
- AI car stats config.
- `GameSession` singleton + save/load JSON.

Sau khi bạn ✅ design này, tôi sẽ refactor `LevelController.ApplyStatsTo()` theo plan ở mục 9.1.
