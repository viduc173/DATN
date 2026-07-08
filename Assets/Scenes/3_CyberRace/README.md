# Scene `CyberRace.unity` — Báo cáo & Hướng dẫn

> 📐 **Kiến trúc tổng:** xem [Assets/Scenes/Design/RaceStats_Architecture.md](../Design/RaceStats_Architecture.md) — design doc đầy đủ cho hệ thống parts → stats → eligibility → race scene, kèm bảng map abstract 0-100 → physics VehicleController và bảng "build chiến thuật" theo loại track.

---

## 🆕 Hệ thống `LevelSettings` (master config per scene)

Thay vì rải config ra nhiều component, từ giờ mỗi màn có **1 asset `LevelSettings`** chứa toàn bộ rule + xe player + linh kiện. 1 MonoBehaviour `LevelController` trong scene đọc asset đó và apply.

### File mới

| File | Loại | Vai trò |
|---|---|---|
| [CarStats.cs](../../Script/CarStats.cs) | `[Serializable]` struct | Bundle 8 stat chính của xe (speed, drive force, brake force, steer angle, tire friction, rolling resistance, mass). Có toán tử `+` để cộng dồn. |
| [CarPart.cs](../../Script/CarPart.cs) | `ScriptableObject` | 1 linh kiện (Engine/Tires/Brakes/Suspension/Body/Aero/Other) với `statBonus: CarStats`. |
| [PlayerCarLoadout.cs](../../Script/PlayerCarLoadout.cs) | `ScriptableObject` | "Xe + linh kiện đang lắp" của player. `GetEffectiveStats()` = `baseStats` + Σ `equippedParts[].statBonus`. |
| [LevelSettings.cs](../../Script/LevelSettings.cs) | `ScriptableObject` | **Master** cho 1 màn: race rules (laps, end scene), countdown, reference tới PlayerCarLoadout. |
| [LevelController.cs](../../Script/LevelController.cs) | `MonoBehaviour` (executionOrder -100) | Awake() đọc LevelSettings và apply xuống RacePositionTracker, MatchWaitTime, player VehicleController. |

### Luồng dữ liệu

```
LevelSettings_CyberRace.asset
  ├── totalLaps = 5
  ├── countdownTime = 3s
  ├── endSceneName = "GarageLobby_pc"
  └── playerLoadout = PlayerLoadout_Default.asset
                       ├── baseStats: { maxDriveForce=2000, maxSpeed=27.78, ... }
                       └── equippedParts:
                            ├── TurboV2.asset       (Engine, +500 driveForce, +5 maxSpeed)
                            └── SportTires.asset    (Tires, +0.2 tireFriction, +5° steerAngle)
                            
              ↓ LevelController.Awake() in scene ↓

  RacePositionTracker.totalLaps = 5
  MatchWaitTime.waitTime = 3
  Player VehicleController.maxDriveForce = 2500   (2000 + 500)
  Player VehicleController.tireFriction = 1.2     (1.0 + 0.2)
  Player VehicleController.maxSteerAngle = 40     (35 + 5)
```

### Cách dùng (workflow garage → race)

1. **Tạo CarPart assets cho mỗi linh kiện shop bán:**
   Right-click Project → Create → Race → Car Part. Đặt `partName`, `slot`, fill `statBonus` (vd `maxDriveForce = 500` cho turbo).

2. **Tạo PlayerCarLoadout asset cho player:**
   Create → Race → Player Car Loadout. Fill `baseStats` (giá trị xe gốc). Bắt đầu với `equippedParts` rỗng — sẽ add khi player mua linh kiện trong garage.

3. **Khi player mua/lắp linh kiện trong garage UI**, gọi:
   ```csharp
   playerLoadout.EquipPart(turboV2Asset);
   // hoặc: playerLoadout.UnequipPart(...)
   ```
   ScriptableObject persist tự động trong Editor (Play Mode thì reset).

4. **Tạo 1 LevelSettings asset cho mỗi màn đua:**
   Create → Race → Level Settings. Set `totalLaps`, `endSceneName`, kéo `playerLoadout` vào field.

5. **Trong scene của màn đua**, tạo GameObject "LevelController" với component `LevelController`:
   - Kéo asset LevelSettings vào field `settings`
   - Gán `raceTracker` / `matchWaitTime` (hoặc để null để auto-find)
   - Gán `playerVehicle` (hoặc để null, sẽ tìm theo tag `Player`)

6. **Bấm Play** — LevelController.Awake() chạy trước, override:
   - `RacePositionTracker.totalLaps`
   - `MatchWaitTime.waitTime`
   - Player VehicleController stats (8 field)
   - Rigidbody mass

### Tương thích ngược với RaceSettings

`RaceSettings.cs` cũ vẫn còn — `RacePositionTracker` vẫn đọc field `raceSettings` nếu được gán. Scene CyberRace hiện đang dùng `RaceSettings_CyberRace.asset`. Khi sẵn sàng migrate sang LevelSettings:
- Tạo `LevelSettings_CyberRace.asset` (copy giá trị từ RaceSettings cũ).
- Add `LevelController` vào scene, wire vào LevelSettings mới.
- Có thể clear `RacePositionTracker.raceSettings = null` hoặc để 2 hệ thống chạy song song (LevelController.Awake() chạy TRƯỚC, override RacePositionTracker.Start() → giá trị cuối là từ LevelSettings).

### Quy ước "stat = 0 nghĩa là không override"

Trong `CarStats`, field nào = 0 thì `LevelController.ApplyStatsTo()` BỎ QUA, giữ giá trị có sẵn trên VehicleController. Vd nếu PlayerCarLoadout.baseStats chỉ set `maxDriveForce = 2500` và để các field khác = 0, chỉ `maxDriveForce` được override.

CarPart.statBonus cũng dùng quy ước này: parts thường chỉ tune 1-2 field, các field khác = 0 và sẽ không gây bug vì cộng 0 vào tổng cũng = 0 → không override.

---



**Đường dẫn:** [Assets/Scenes/3_CyberRace/CyberRace.unity](CyberRace.unity)
**Cập nhật:** 2026-05-25

---

## 1. Tổng quan scene

Scene đua xe `CyberRace` dùng hệ thống xếp hạng + đếm vòng tự viết (không phụ thuộc asset đua xe ngoài). Toàn bộ logic chia thành các script sau:

| Script | Vai trò |
|---|---|
| [Assets/Script/RaceSettings.cs](../../Script/RaceSettings.cs) | **ScriptableObject cấu hình:** số vòng đua + scene về sau khi kết thúc + delay |
| [Assets/Script/RacePositionTracker.cs](../../Script/RacePositionTracker.cs) | Theo dõi vị trí từng xe, tính xếp hạng, đếm vòng, đẩy UI, load scene khi xong |
| [Assets/Script/CheckpointTrigger.cs](../../Script/CheckpointTrigger.cs) | (Tùy chọn) gắn lên collider checkpoint để detect lap bằng trigger thay vì distance |
| [Assets/Script/UIReceiver.cs](../../Script/UIReceiver.cs) | Đẩy dữ liệu từ tracker ra một số TextMeshPro phụ |

Scene KHÔNG có "score" cộng điểm — UI hiển thị **vị trí xếp hạng (1st/2nd/…)** + **số checkpoint** + **số vòng (lap)**.

---

## 2. ⚙️ File cấu hình race — `RaceSettings`

### Cách sử dụng

**Asset mẫu có sẵn:** [RaceSettings_CyberRace.asset](RaceSettings_CyberRace.asset) (ngay cạnh scene). Mở Unity, click vào file này để chỉnh các thông số trong Inspector:

| Field | Ý nghĩa | Mặc định |
|---|---|---|
| `totalLaps` | Số vòng cần hoàn thành để về đích | `3` |
| `autoLoadSceneOnFinish` | Tự động load scene khác khi PLAYER về đích | `true` |
| `endSceneName` | Tên scene sẽ load (phải có trong Build Settings) | `GarageLobby_pc` |
| `loadSceneDelay` | Số giây chờ sau khi về đích rồi mới load (cho xem kết quả) | `3` |
| `lapWrapThreshold` | Ngưỡng detect 1 vòng. `0` = tự tính `checkpoints.Count / 4` | `0` |

### Gán vào scene

1. Trong scene `CyberRace`, chọn GameObject có component `RacePositionTracker`.
2. Trong Inspector, kéo `RaceSettings_CyberRace.asset` vào field **`Race Settings`** (mục "Race Settings (file cấu hình)").
3. Khi tracker chạy `Start()`, nó sẽ tự đọc `totalLaps` từ file (override field cũ).
4. Khi player hoàn thành đủ số vòng → tự load `endSceneName` sau `loadSceneDelay` giây.

> ⚠️ **Quan trọng:** Scene `GarageLobby_pc` (hoặc bất kỳ scene nào bạn đặt trong `endSceneName`) **phải có trong File → Build Settings → Scenes In Build** thì `SceneManager.LoadScene` mới chạy được.

### Tạo thêm file settings cho race khác

Trong Unity: **Right-click ở Project window → Create → Race → Race Settings**. Đặt tên tùy ý, chỉnh trong Inspector, kéo vào tracker của scene tương ứng.

---

## 3. ✅ Các bug đã sửa

| Mã | Bug | Trạng thái | File / dòng |
|---|---|---|---|
| B1 | `lapCount` không bao giờ tăng — `OnCheckpointPassed` không có ai gọi | ✅ Sửa | [RacePositionTracker.cs:336-414](../../Script/RacePositionTracker.cs#L336-L414) — thêm lap detection bằng "wrap" trong `UpdateAllPositions` |
| B2 | Điều kiện mâu thuẫn `checkpointIndex == 0 && currentCheckpointIndex == count-1` | ✅ Sửa | [RacePositionTracker.cs:495-525](../../Script/RacePositionTracker.cs#L495-L525) — `OnCheckpointPassed` giờ lưu `previousIndex` trước khi gán |
| B3 | `hasFinished` không kiểm tra đủ vòng, set ngay khi chạm CP cuối | ✅ Sửa | [RacePositionTracker.cs:374-379](../../Script/RacePositionTracker.cs#L374-L379) — chỉ finish khi `lapCount >= totalLaps` |
| B5 | Detect checkpoint bằng distance dễ ăn gian | ✅ Có giải pháp (opt-in) | [CheckpointTrigger.cs](../../Script/CheckpointTrigger.cs) — gắn lên collider isTrigger của từng CP |
| B6 | Hằng số `/100f` chuẩn hóa khoảng cách sai | ✅ Sửa | [RacePositionTracker.cs:402-409](../../Script/RacePositionTracker.cs#L402-L409) — dùng `Vector3.Distance(cp[i], cp[i+1])` làm mẫu số động |
| B8 | UI hiển thị `lap + 1` không clamp khi finish (có thể ra `Lap 4/3`) | ✅ Sửa | [RacePositionTracker.cs:GetDisplayLap](../../Script/RacePositionTracker.cs) + [UIReceiver.cs:82,134](../../Script/UIReceiver.cs#L82) — clamp tại `totalLaps` |
| — | **Mới:** chống ăn gian khi chạy ngược qua finish line | ✅ Thêm | [RacePositionTracker.cs:381-387](../../Script/RacePositionTracker.cs#L381-L387) — `backwardWrap` giảm `lapCount` |
| — | **Mới:** tự load scene khi player về đích | ✅ Thêm | [RacePositionTracker.cs:419-453](../../Script/RacePositionTracker.cs#L419-L453) — coroutine `LoadEndSceneAfterDelay` |
| — | **Mới:** events `onLapCompleted` & `onPlayerRaceFinished` | ✅ Thêm | [RacePositionTracker.cs:95-99](../../Script/RacePositionTracker.cs#L95-L99) — hook UnityEvent vào hệ thống khác (sound, popup, …) |

### Bug runtime đã sửa (driving + countdown)

| Bug | Nguyên nhân | Cách sửa |
|---|---|---|
| Player không lái được xe + không hiện countdown + spam `NullReferenceException EVP.VehicleAudio.DoWindAudio` | Player vehicle `CarType_3` có `VehicleController` **disabled mặc định** trong scene (`m_Enabled: 0` dòng 17027 YAML). MatchWaitTime đáng lẽ enable lại sau 5s countdown, nhưng `waitObject` + `WaitText` chưa gán → `Start()` crash ở `waitObject.SetActive(true)` → code enable VehicleController không bao giờ chạy → `VehicleController.OnEnable` không cache `m_rigidbody` → `VehicleAudio` lấy `cachedRigidbody = null` mỗi frame. | 1️⃣ Sửa [MatchWaitTime.cs](../../Script/MatchWaitTime.cs) null-safe: nếu `waitObject`/`WaitText` null thì log warning + bỏ qua phần UI, **vẫn chạy logic disable/enable xe**. 2️⃣ Sửa [VehicleAudio.cs:288-294](../../EVP5/Scripts/VehicleAudio.cs#L288-L294) defensive: nếu `cachedRigidbody == null` thì `StopAllAudioSources()` + return (không spam exception). |

> Sau fix: scene start → MatchWaitTime disable cả AI + Player xe → countdown 5s hiển thị trên UI Canvas → tự enable xe → có thể lái.

### 🐛 Bug B12: `SetVehiclesEnabled` early-out — AI không bao giờ bị disable

**Triệu chứng:** Bấm Play → AI lao đi luôn không chờ countdown.

**Root cause:** Field `vehiclesEnabled = false` (giá trị mặc định). Khi `Start()` gọi `SetVehiclesEnabled(false)`, dòng đầu trong method check `if (vehiclesEnabled == enabled) return;` → `false == false` → **return ngay**, vòng lặp disable AI không bao giờ chạy.

**Fix:** Xóa bỏ check early-out đó. [MatchWaitTime.cs:SetVehiclesEnabled](../../Script/MatchWaitTime.cs) giờ luôn loop qua list và set `enabled` trên từng `CarAIController` / `VehicleController`. Cũng log chi tiết:
```
[MatchWaitTime] SetVehiclesEnabled(False) → AI:6/6 (missing 0), Player:1/1 (missing 0)
```
Nếu có xe không tìm thấy component, log warning kèm tên GameObject.

### 📝 User đã re-wire countdown UI

Trong lần Unity re-serialize trước, các CountdownText fileID `8888888*` tôi tạo bị Unity drop. User đã wire lại bằng UI của họ:
- `MatchWaitTime.waitObject` → fileID `1733650506` (GameObject `RaceCountDown` ở world pos -211, -87, -26)
- `MatchWaitTime.WaitText` → fileID `990472761` (TMP `Text (TMP)` ở hierarchy `RaceCountDown > Canvas > Image > Text (TMP)`)
- Hierarchy đúng: `SetActive(true/false)` trên RaceCountDown sẽ hiện/ẩn toàn bộ canvas con + TMP.

**Sau B12 fix:** AI sẽ bị disable suốt 5s countdown. TMP text được update mỗi frame qua `WaitText.text = "Race starts in: X"`. Bấm Play → kiểm tra Console:
- `[MatchWaitTime] SetVehiclesEnabled(False) → AI:6/6, Player:1/1` ngay khi scene start
- Sau 5s: `[MatchWaitTime] SetVehiclesEnabled(True) → AI:6/6, Player:1/1`

Nếu vẫn không thấy text trong VR, kiểm tra vị trí RaceCountDown (-211, -87, -26) — có thể không nằm trong tầm nhìn của player. Di chuyển trong Editor: chọn `=============== UI ================= > RaceCountDown` và set transform position phù hợp.

---

**Countdown UI (legacy section — đã bị Unity drop khi re-save scene):**

> ⚠️ Project này dùng **VR** (XR Interaction Toolkit). ScreenSpaceOverlay **không render trong VR headset** — chỉ hiển thị trên monitor. Toàn bộ UI phải World Space.

`CountdownText` được đặt là child của Canvas `UI` có sẵn (fileID `2090954917`, World Space) — chung cha với leaderboard:

| fileID | Vai trò |
|---|---|
| `8888888881` | GameObject `CountdownText` (`waitObject` của MatchWaitTime) |
| `8888888882` | RectTransform — parent = Canvas "UI" (`2090954918`), stretch full panel, top-align |
| `8888888883` | CanvasRenderer |
| `8888888884` | TextMeshProUGUI — fontSize `0.03` (world units), bold, màu vàng, căn top-center (`WaitText` của MatchWaitTime) |

CountdownText và Leaderboard chung Canvas nhưng khác `m_VerticalAlignment`:
- Countdown: `128` (top)
- Leaderboard (`969080097`): `256` (middle)
→ không chồng nhau visually.

**Lap UI (không động vào):** đã ở World Space sẵn từ thiết kế gốc của scene. Cụ thể:
- Canvas `UIController` (`680478255`, World Space) là child của Player `CarType_3` → di chuyển theo xe.
- Canvas `UI` (`2090954917`, World Space) là child của prefab HUD.
Tôi KHÔNG tạo thêm canvas nào theo camera. Nếu bạn muốn lap UI tách rời khỏi xe (ví dụ billboard cố định cạnh track), cần unparent Canvas `UIController` khỏi `334949159` trong Editor.

---

**Bug B11 (race-start false lap):** trước đây khi xe spawn gần start/finish line, `FindClosestCheckpoint` có thể trả về CP86 frame đầu và CP0 frame sau → forwardWrap → lap+1 ngay. Fix bằng cách thêm `maxCheckpointInLap`: chỉ count lap khi xe đã chạm CP ở giữa track (≥ n/2 = 43) trước khi qua finish line. Reset `maxCheckpointInLap = 0` sau mỗi lap.

Cũng đã bật `showDebugInfo = 1` trên Score Manager để Console log mỗi sự kiện lap (`[Race] Player hoàn thành lap X/Y`, `... chưa chạm midway`, `... đi ngược`, `... VỀ ĐÍCH!`) — tắt sau khi xong debug.

Muốn đổi font/cỡ/màu/vị trí countdown: chọn `UI > CountdownText` trong Hierarchy của Unity Editor.

### Bug đã sửa trực tiếp trong scene YAML

| Mã | Bug | Trạng thái | Chi tiết |
|---|---|---|---|
| B4 | UI text trên `Score Manager > RacePositionTracker` đều null | ✅ Đã wire trong scene | Xem [Mục 4](#4-wiring-ui-trong-scene-đã-làm-sẵn) |
| B10 | UIReceiver `autoUpdate` đè lên RacePositionTracker | ✅ Đã tắt | `autoUpdate = 0` trên UIReceiver (dòng ~41764 YAML). Tracker giờ là người duy nhất ghi UI. |
| — | UIReceiver `racePositionTracker` ref null (đang auto-find) | ✅ Đã gán | Trỏ tới `Score Manager` (fileID 659421443) — không cần auto-find nữa. |
| — | `RacePositionTracker.raceSettings` chưa gán | ✅ Đã gán | Trỏ tới `RaceSettings_CyberRace.asset` (3 vòng, về `GarageLobby_pc`). |

---

## 4. Wiring UI trong scene (đã làm sẵn)

Scene `CyberRace.unity` chỉ có **5 TextMeshProUGUI** đang tồn tại. Đã wire vào `Score Manager > RacePositionTracker` như sau:

| Field của RacePositionTracker | fileID TMP | Ghi chú |
|---|---|---|
| `leaderboardText` | `969080097` | Text leaderboard nhiều dòng (Top-N) |
| `position1Text` | `2588598530752189336` | Vị trí top 1 |
| `position2Text` | `988013395` | Vị trí top 2 |
| `position3Text` | `275595328` | Vị trí top 3 |
| `playerPositionText` | `1100583316` | Dòng riêng cho Player |
| `positionText`, `positionFullText`, `checkpointText`, `lapText`, `position4-6Text` | `null` | Scene chưa có TMP cho các slot này. Add thêm TextMeshPro vào Canvas rồi kéo vào Inspector nếu cần. |

Đồng thời:
- `Score Manager > RacePositionTracker.raceSettings` đã trỏ tới [RaceSettings_CyberRace.asset](RaceSettings_CyberRace.asset).
- `UIReceiver` đã tắt `autoUpdate` để không đè text mà tracker đang ghi. Nếu muốn dùng lại UIReceiver, bật `autoUpdate = 1` và disable các UI field tương ứng trên tracker để tránh ghi đôi.

> ⚠️ **Lưu ý về `totalLaps`:** Field `totalLaps` trong scene đang là `1`, nhưng vì đã gán `raceSettings` nên giá trị thực dùng = `raceSettings.totalLaps` (mặc định `3`). Chỉnh số vòng trong [RaceSettings_CyberRace.asset](RaceSettings_CyberRace.asset).

---

## 5. 🔄 Cách logic lap mới hoạt động

`UpdateAllPositions()` chạy mỗi 0.1s. Với mỗi racer:

1. Tìm checkpoint gần nhất (`closestCheckpointIndex`).
2. So sánh với `lastCheckpointIndex` (vòng trước):
   - **Forward wrap** = trước đó gần CP cuối (`>= n - wrapThreshold`), giờ gần CP đầu (`< wrapThreshold`) → `lapCount++`, raise `onLapCompleted`.
   - **Backward wrap** (chạy ngược) → `lapCount--` (chống ăn gian).
   - Khác → chỉ update `lastCheckpointIndex`.
3. Nếu `lapCount >= totalLaps` → `MarkRacerFinished()`:
   - Set `hasFinished = true`, `finishPosition = nextFinishPosition++`.
   - Nếu là Player + `raceSettings.autoLoadSceneOnFinish` → khởi động coroutine load scene sau `loadSceneDelay` giây.
4. Cập nhật `totalProgress` cho ranking (dùng `segmentLength` thật, không phải `100f`).

`wrapThreshold` mặc định = `checkpoints.Count / 4` = 87/4 ≈ 21. Tức nếu xe đang gần CP67-86 và frame sau lại gần CP0-20 → tính là 1 vòng. Đủ tolerance để không miss lap nhưng cũng đủ chặt để không count nhầm.

### Race flow với `totalLaps = 3`

| Thời điểm | `lapCount` | Hiển thị UI | Trạng thái |
|---|---|---|---|
| Start | 0 | `Lap 1/3` | đang chạy lap 1 |
| Cross finish lần 1 | 1 | `Lap 2/3` | đang chạy lap 2 |
| Cross finish lần 2 | 2 | `Lap 3/3` | đang chạy lap 3 (last lap) |
| Cross finish lần 3 | 3 → `hasFinished = true` | `Lap 3/3` (clamp) | 🏁 về đích, load scene sau `loadSceneDelay`s |

---

## 6. 🎯 Khi nào dùng `CheckpointTrigger` thay vì detect distance?

Dùng [CheckpointTrigger.cs](../../Script/CheckpointTrigger.cs) nếu:
- Track có shortcut tiềm năng, người chơi có thể skip 1 đoạn track.
- Cần detect lap chính xác đến từng frame (không phải tick 0.1s).
- 87 checkpoint quá dày, distance detection bị noise.

Cách dùng:
1. Trên mỗi GameObject checkpoint, add component `Box Collider` + tick `Is Trigger`.
2. Add `CheckpointTrigger`. Set `checkpointIndex` = thứ tự (hoặc `-1` để auto từ `sibling index`).
3. Set `racerTag` = tag của xe (mặc định `"Player"`). Hoặc đảm bảo collider con của xe có `Rigidbody` ở root để `attachedRigidbody.transform` resolve đúng.
4. Để nguyên `tracker = null` để tự `FindObjectOfType`.

Hệ thống distance-based vẫn chạy song song và sync với trigger qua field `lastCheckpointIndex`, không conflict.

---

## 7. 📋 Tóm tắt file mới/đã sửa

```
Assets/Script/
├── RaceSettings.cs              (MỚI) ScriptableObject cấu hình
├── RaceSettings.cs.meta         (MỚI)
├── CheckpointTrigger.cs         (MỚI) Trigger opt-in cho lap detection
├── CheckpointTrigger.cs.meta    (MỚI)
├── RacePositionTracker.cs       (SỬA) Lap counting, finish, scene load, totalProgress fix
└── UIReceiver.cs                (SỬA) Clamp lap display

Assets/Scenes/3_CyberRace/
├── README.md                          (CẬP NHẬT)
├── RaceSettings_CyberRace.asset       (MỚI) Asset cấu hình mẫu cho scene này
└── RaceSettings_CyberRace.asset.meta  (MỚI)
```

---

## 8. 🚦 Việc bạn cần làm trong Unity Editor

Hầu hết đã được wire sẵn trong scene YAML. Chỉ còn:

1. **Mở Unity** → để Unity import 4 file mới (`RaceSettings.cs`, `CheckpointTrigger.cs` + 2 `.asset`).
2. **File → Build Settings** → đảm bảo scene `GarageLobby_pc` (hoặc scene `endSceneName` bạn đặt) đã được add vào "Scenes In Build". Đây là điều kiện bắt buộc để `SceneManager.LoadScene` chạy được.
3. (Tùy chọn) Mở [RaceSettings_CyberRace.asset](RaceSettings_CyberRace.asset) và chỉnh:
   - `totalLaps` = số vòng bạn muốn (mặc định `3`).
   - `endSceneName` = tên scene về (mặc định `GarageLobby_pc`).
   - `loadSceneDelay` = giây chờ (mặc định `3`).
4. (Tùy chọn) Thêm các TextMeshPro mới vào Canvas + kéo vào các field `positionText`, `lapText`, `checkpointText`, `position4-6Text` của `Score Manager` nếu muốn hiển thị thêm các label đó.
5. Bấm Play → chạy đua → kiểm tra Console có log `[Race] Player hoàn thành lap X/Y` và `[Race] Player đã VỀ ĐÍCH!` không. UI 5 label sẽ tự update mỗi 0.1s.
