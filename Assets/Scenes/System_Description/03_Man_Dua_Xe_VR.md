# 03 — Các màn đua VR (3 màn cùng hệ thống)

> Ba màn đua VR **dùng CHUNG một hệ thống**, chỉ khác model/bối cảnh:
> - `6_Stadium_Sunny` → `Stadium_Sunny_vr.unity` — **màn CHUẨN tham chiếu** (đầy đủ pipeline nhất).
> - `3_CyberRace` → `CyberRace_vr.unity`.
> - `2_Racing_Circuit` → `Racing_Circuit_vr.unity`.
>
> **Nguồn (đọc khi đào sâu):** bộ pipeline Stadium — `6_Stadium_Sunny/Stadium_CarLoad_Pipeline.md`,
> `Stadium_Results_Pipeline.md`, `Stadium_SpeedBoost_Pipeline.md`, `Stadium_BonusSpawn_Pipeline.md`,
> `Stadium_Nitro_Pipeline.md`; hệ xếp hạng/lap — `3_CyberRace/README.md`. Bối cảnh/độ khó/thưởng từng màn +
> AI: [05_LoiDuLieu_Stats_KinhTe_AI.md](05_LoiDuLieu_Stats_KinhTe_AI.md). Hạ tầng VR: [04](04_HaTang_VR_Chung.md).

---

## 1. Vòng đời một chặng đua

```
Vào scene đua (từ Garage)
   │
   ▼
[A] LOAD XE  (LoadSceneController -110)  → activate đúng xe player + áp stats + màu sơn
   ▼
[B] ĐẾM NGƯỢC (MatchWaitTime)            → khóa xe player + AI; đếm 3–5s; mở khóa
   ▼
[C] ĐUA       (RacePositionTracker)      → đếm checkpoint/lap, xếp hạng HUD; nitro + bonus
   ▼
[D] VỀ ĐÍCH   (lap cuối → onPlayerRaceFinished)
   ▼
[E] KẾT QUẢ   (RaceResultsController)    → dừng mọi xe, cộng Gold theo hạng, mở bảng kết quả
   ▼
[F] VỀ GARAGE (Exit modal / auto-load)   → GarageLobby_vr
```

Hai hệ xe khác nhau trong scene đua:
- **Xe Player = hệ EVP** (`VehicleController`, namespace `EVP`, tag "Player").
- **Xe AI = hệ Off_Road_Racing** (`EasyCarController` + `CarAIController`, namespace `ALIyerEdon`), spawn bởi `Race_Manager`.

---

## 2. [A] Load đúng xe player từ Garage  (`Stadium_CarLoad_Pipeline.md`)

Garage ghi xe đang chọn vào **`ActiveLoadout`** (`SavedCarIndex` = PlayerPrefs `ActiveCarIndex`; `Current` = static).

```
ActiveLoadout.SavedCarIndex
        ▼
LoadSceneController  [DefaultExecutionOrder(-110)]  (chạy sớm nhất)
   ResolveSelectedCarIndex():  1) ActiveLoadout (chính)  2) SystemController (legacy)
                               3) xe đang active sẵn      4) defaultCarIndex
   ActivateCar(index): tắt cả 3 CarType_*, bật đúng 1 xe → currentActiveCar
        ├── RacePositionTracker.SetPlayer(car.transform)   (xếp hạng bám đúng xe)
        ├── MatchWaitTime.SetPlayerVehicle(car)            (đếm ngược khóa đúng xe)
        └── ApplyLoadoutPaint() → LoadoutPaintApplier      (áp màu sơn từ loadout)
```
- 3 xe `CarType_0/1/2` bắt đầu **inactive**; chỉ 1 được bật theo index.
- **Stats:** `LevelController` (execOrder -100) đọc `ActiveLoadout.Current.GetEffectiveStats()` → quy đổi sang
  `VehicleController` (mapping 6 stat → physics: xem [05](05_LoiDuLieu_Stats_KinhTe_AI.md) §2).
- **Màu sơn:** `LoadoutPaintApplier` áp `loadout.paint` lên body renderer (cần `CarPaintTarget` / tag "PaintPart").
- ⚠️ **Ràng buộc index:** thứ tự slot xe trong garage phải khớp tên `CarType_0/1/2` ở scene đua (convention thuần, không có data-link).

---

## 3. [B] Đếm ngược  (`MatchWaitTime`)
- Khi start: disable `VehicleController` (player) + `CarAIController`/`VehicleController` (AI) → **không ai chạy**.
- Hiển thị đếm ngược (vd "Race starts in: X") trên UI **world-space** (`waitObject`/`WaitText`). VR bắt buộc
  world-space (Screen-Space Overlay không render trong kính).
- Hết giờ → enable lại tất cả xe → bắt đầu đua.
- Bug lịch sử đã sửa (ghi trong `3_CyberRace/README.md`): early-out `SetVehiclesEnabled` khiến AI không bị
  khóa; null-safe khi thiếu `waitObject`; `VehicleAudio` defensive khi `cachedRigidbody == null`.

---

## 4. [C] Xếp hạng, checkpoint & lap  (`RacePositionTracker` — chi tiết `3_CyberRace/README.md`)
- `UpdateAllPositions()` chạy mỗi ~0.1s. Mỗi xe: tìm checkpoint gần nhất, so với checkpoint vòng trước:
  - **Forward wrap** (gần CP cuối → gần CP đầu) → `lapCount++`, raise `onLapCompleted`.
  - **Backward wrap** (chạy ngược) → `lapCount--` (chống ăn gian).
- `wrapThreshold` mặc định = `checkpoints.Count / 4`. Có `maxCheckpointInLap` (phải qua giữa track) chống
  đếm lap giả lúc spawn gần vạch.
- `lapCount >= totalLaps` → `MarkRacerFinished()` (set `finishPosition` tăng dần). Player về đích → bắn
  `onPlayerRaceFinished`.
- Tùy chọn `CheckpointTrigger` (collider isTrigger) thay cho detect-by-distance khi cần chính xác.
- HUD: leaderboard + vị trí top + dòng Player (`UIReceiver` đẩy ra TMP); UI world-space.
- `RaceSettings` (asset cạnh scene) cấu hình: `totalLaps`, `autoLoadSceneOnFinish`, `endSceneName`,
  `loadSceneDelay`, `lapWrapThreshold`, `prizeByPosition`, `bonusesPerLap`.

---

## 5. [Đua] Nitro & Bonus tăng tốc

### 5.1 Nitro  (`Stadium_Nitro_Pipeline.md`)
- PC giữ **Shift** → phun nitro (VR: input cò/nút tương ứng đã wire). Nhiên liệu có hạn, phun là tụt, nhả là hồi.
- `NitroController` (trên `PlayerController`, execOrder -70): quản nhiên liệu + bật VFX `Nitro` gắn vào
  `CarType_* > NitroFx_Anchor` của xe active.
- **Mọi tham số quy đổi từ stat xe** (`RecomputeFromStats`): stat `nitro` (CAR+ECU) → bình + tốc độ hồi;
  `acceleration` (ENGINE) → lực đẩy chính; `grip` (TIRES) → góp nhỏ. Tái dùng cơ chế `SpeedBoost`.
- Cạn → khóa tới khi hồi đạt `refireChargeThreshold` (vd 50%). Chi tiết bảng tham số: xem nguồn + [05](05_LoiDuLieu_Stats_KinhTe_AI.md) §3.

### 5.2 Bonus tăng tốc nhặt trên đường  (`Stadium_SpeedBoost_Pipeline.md`)
- Xe Player chạm bonus pickup (`PlayerTriggerEvent`, tag "Player") → bật VFX "Effect", ẩn mesh, đẩy bonus vào
  `BonusEvent.bonusQueue` (singleton, DontDestroyOnLoad).
- `BonusReceiver` (trên `BonusController`, luôn active) poll queue mỗi 0.1s → `HandleSpeedBonus` →
  `SpeedBoost.GetForActivePlayer().ActivateBoost(value, duration)`.
- `SpeedBoost` (tự AddComponent vào xe active): **nâng trần `maxSpeedForward` × hệ số** (mặc định 1.6) +
  **lực đẩy** mỗi FixedUpdate cho tới khi đạt trần → cảm giác bốc; hết duration (mặc định 3s) khôi phục.
  Nhặt liên tiếp = **gia hạn**, không nhân chồng tốc độ.
- Chỉnh độ mạnh ở GameObject `BonusEvent`: `defaultBonusValue` / `defaultBonusDuration`.

### 5.3 Spawn bonus theo vòng  (`Stadium_BonusSpawn_Pipeline.md`)
- `LapBonusSpawner` (trên `BonusPlace`, execOrder -80): nhân bản prefab `Assets/Prefabs/Bonus/Object.prefab`
  gần các checkpoint, Z hướng về checkpoint kế tiếp.
- Nghe `onLapCompleted` → spawn batch mỗi lap; số/lap = `RaceSettings.bonusesPerLap` (mặc định 3). Chỉ Player
  kích spawn. `clearPreviousLapBonuses` xoá bonus chưa ăn của lap trước.
- ⚠️ Clone từ prefab không giữ tham chiếu singleton `BonusEvent` → `WireBonusEvent(clone)` tự AddListener runtime.

---

## 6. [E] Kết thúc chặng: bảng kết quả + thưởng  (`Stadium_Results_Pipeline.md`)

```
onPlayerRaceFinished
   ▼
RaceResultsController.HandlePlayerFinished()
   1) SortedStandings()  ← snapshot xếp hạng (theo finishPosition/totalProgress)
   2) StopAllVehicles()  ← phanh cứng player (SetValue 0,0,1,1,0 + disable input) + đứng yên AI
   3) GrantReward()      ← prize = RaceSettings.GetPrize(player.finishPosition);
                            inventory.AddGold + SaveToPlayerPrefs   (CHỈ Player có ví)
   4) PopulateBoard()    ← điền Rank_1..Rank_12 (tên + tiền thưởng theo hạng)
   5) achievementsWindow.OpenWindow()  ← animation fade+pop (ModalWindowManager)
```
- Bảng **Achievements** tái dùng làm bảng kết quả; tối đa 12 dòng; dòng thừa ẩn (`hideEmptyRows`).
- Dùng `finishPosition` (set ngay lúc về đích) thay vì `GetPlayerPosition()` để chính xác về timing.
- **Bắt buộc 1 lần:** chạy menu `Tools/Stadium/Setup Results Board` (tạo asset + nhân Rank_6..12 + wire
  `RaceResultsController` + gán `ModalWindow.controller`).
- `RaceSettings.GetPrize(position)` (1-based): trả `prizeByPosition[position-1]`, ngoài mảng = 0 (không thưởng).
  Bảng thưởng từng màn: xem [05](05_LoiDuLieu_Stats_KinhTe_AI.md) §4.
- ⚠️ Xe Player phải đặt **sau vạch đích** (grid xuất phát) để không finish sớm 1 vòng.

---

## 7. [F + UI] Menu VR khi đua: `VRRaceMenu`  (`Stadium_Results_Pipeline.md §6.2`)

Bản VR khác bản PC: **không có chuột/phím Tab**. `UI_Main Menu` (chứa cả **Achievements** = bảng kết quả lẫn
**Modal Windows** = modal Exit/Pause) được chuyển sang **world-space**.

- `UI_Main Menu` **GIỮ ACTIVE** (không tắt cả canvas, vì tắt là mất luôn modal kết quả + exit).
- `PCHotkeyManager` (Tab → exit) **inactive** trong scene VR → thay bằng **`VRRaceMenu`** (trên EventSystem):
  - Bấm **A/X tay trái** → mở/đóng modal Exit (`ModalWindowManager.OpenWindow/CloseWindow`).
  - **Tự hiện tia** khi có modal mở (kết quả tự mở khi về đích, hoặc exit); ẩn tia khi đang lái.
- Nút **Exit/Back** trong modal bấm bằng **tia** (modal nằm trong canvas world-space):
  `SceneChanger.LoadScene` hoặc `RaceResultsController.ReturnToGarage()` → quay lại `GarageLobby_vr`.
- Gắn tự động: `Tools/VR/Convert Main Menu to VR` (tự nhận scene đua → dùng `VRRaceMenu`).

---

## 8. Ba màn đua khác nhau ở đâu (tóm tắt — chi tiết [05](05_LoiDuLieu_Stats_KinhTe_AI.md))

| Màn | Bối cảnh | Mặt đường | Laps | Thưởng 1/2/3 | Stat ưu tiên |
|---|---|---|---|---|---|
| **6_Stadium_Sunny** (chuẩn) | Sân vận động, nắng | Khô, grip tốt | 1 | 1000/600/300 | maxSpeed, acceleration |
| **3_CyberRace** | Đô thị cyberpunk, neon | Nhựa tốc độ cao | 3 | 1000/600/300 | engine + tires + brakes |
| **2_Racing_Circuit** | Đường đua circuit nhiều cua | Nhựa | (TBD) | (TBD) | tổng hợp |

> **Hệ thống giống hệt nhau** — cùng bộ script (LoadSceneController, MatchWaitTime, RacePositionTracker,
> LevelController/RaceSettings, NitroController, SpeedBoost, LapBonusSpawner, RaceResultsController, VRRaceMenu).
> Khác biệt chỉ là **model 3D, mặt đường (grip), số vòng, bảng thưởng** trong asset RaceSettings của mỗi màn.
> `6_Stadium_Sunny` là khuôn — 2 màn kia phỏng theo (hiện chưa có doc riêng, dùng bộ `Stadium_*.md`).

---

## 9. Hạn chế / hướng phát triển (cho đồ án)
- AI hiện **đồng nhất** (`Car_AI` + `Race_Manager`); thiết kế phân Tier/cá tính (Rookie/Drifter/Blaze/Shadow/
  Mudcrawler) trong `Design/Enemy_AI_Design.md` **chưa code**.
- `2_Racing_Circuit` chưa có `RaceSettings` riêng / chưa cân bằng thưởng đầy đủ (theo `Race_Levels_Design.md`).
- Soft-gate stat vào màn (yêu cầu grip/speed...) **chưa khóa cứng** (`statRequirements = 0`).
- Bonus đặc biệt (clean race, personal best...) là ý tưởng, chưa implement.

## 10. Cần biết thêm thì xem đâu
- Load xe + countdown: `6_Stadium_Sunny/Stadium_CarLoad_Pipeline.md`.
- Kết quả + thưởng + dừng xe + menu VR: `6_Stadium_Sunny/Stadium_Results_Pipeline.md`.
- Nitro: `Stadium_Nitro_Pipeline.md`. Bonus tăng tốc: `Stadium_SpeedBoost_Pipeline.md`. Spawn bonus: `Stadium_BonusSpawn_Pipeline.md`.
- Xếp hạng/lap/checkpoint + bug đã sửa: `3_CyberRace/README.md`.
- Quy trình dựng scene đua mới: `Design/OffRoadRacing_Scene_Refactor_Guide.md`.
