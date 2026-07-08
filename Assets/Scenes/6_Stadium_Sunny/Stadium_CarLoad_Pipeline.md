# Stadium Sunny — Pipeline load xe từ Garage

> Tài liệu mô tả cách scene `6_Stadium_Sunny` load **đúng xe player đang display ở garage**,
> và cách `MatchWaitTime` + `RacePositionTracker` bám theo đúng xe đó.
> Unity 6. Triển khai 2026-06-03.

---

## 0. Bối cảnh — tại sao phải sửa

Garage (`5_GarageLobby_pc`) lưu xe đang chọn vào **`ActiveLoadout`**:
- `ActiveLoadout.SavedCarIndex` — index xe (PlayerPrefs key `"ActiveCarIndex"`, persist qua session).
- `ActiveLoadout.Current` — reference `PlayerCarLoadout` (static, sống qua chuyển scene trong cùng phiên).

`GarageCarManager.SetActiveCar()` ghi cả 2 mỗi lần đổi xe (phím E/Q hoặc chọn từ inventory).

**BUG cũ:** `LoadSceneController` ở Stadium đọc index từ `SystemController.GetSelectedCarIndex()`.
Nhưng live garage **không có** `SystemController` (chỉ scene backup `0_BackUpScene/Garage Menu.unity` mới có,
và cũng không ai gọi `SetSelectedCar` từ garage). Hậu quả:
`SystemController.Instance == null` → fallback `defaultCarIndex = 1` → **luôn load CarType_1**, bất kể chọn xe gì.

Ngoài ra `MatchWaitTime.playerVehicles` liệt kê cả 3 xe, và `RacePositionTracker.playerTransform`
resolve theo tag "Player" với thứ tự `Start()` không đảm bảo → dễ trỏ sai xe.

---

## 1. Cấu trúc scene liên quan

```
MatchController (GameObject, fileID 830455956)
├── LoadSceneController   ← chọn + activate đúng xe player, điều phối 2 hệ kia
└── MatchWaitTime         ← đếm ngược, khoá/mở VehicleController của xe player

PlayerCarManager (carContainer)
├── CarType_0   (tag "Player", m_IsActive: 0)
├── CarType_1   (tag "Player", m_IsActive: 0)
└── CarType_2   (tag "Player", m_IsActive: 0)

RacePositionTracker (GameObject riêng) ← bảng xếp hạng HUD, cần biết transform player
```

- 3 xe đều bắt đầu **inactive**; `LoadSceneController` SetActive đúng 1 xe theo index.
- `LoadSceneController.cars` và `MatchWaitTime.playerVehicles` đều list `[CarType_0, CarType_1, CarType_2]` theo đúng thứ tự index 0/1/2.

---

## 2. Luồng đã triển khai

```
ActiveLoadout.SavedCarIndex (garage ghi)
        │
        ▼
LoadSceneController  [DefaultExecutionOrder(-110)]  ← chạy SỚM
   ResolveSelectedCarIndex():
     1) ActiveLoadout.HasSavedCar → SavedCarIndex   (nguồn chính — garage)
     2) SystemController.GetSelectedCarIndex()       (legacy/backup)
     3) xe đang active sẵn trong scene
     4) defaultCarIndex (Inspector)
        │
   ActivateCar(index): tắt hết 3 xe, bật đúng 1 xe → currentActiveCar
        │
        ├── UpdateRacePositionTracker() → RacePositionTracker.SetPlayer(car.transform)
        └── UpdateMatchWaitTime()        → MatchWaitTime.SetPlayerVehicle(car)
```

Vì `LoadSceneController` chạy ở execution order **-110** (trước RacePositionTracker / MatchWaitTime /
LevelController), xe đúng đã active + được push sang 2 hệ kia trước khi chúng `Start()`.

---

## 3. Code đã thay đổi (4 file)

| File | Thay đổi |
|---|---|
| `Assets/Script/ActiveLoadout.cs` | Thêm `HasSavedCar` (`PlayerPrefs.HasKey`) — phân biệt "chưa chọn" với "chọn index 0". |
| `Assets/Script/LoadSceneController.cs` | `[DefaultExecutionOrder(-110)]`; `ResolveSelectedCarIndex()` ưu tiên `ActiveLoadout`; thêm field `matchWaitTime` + `autoFindMatchWaitTime`; sau activate push sang `RacePositionTracker.SetPlayer()` và `MatchWaitTime.SetPlayerVehicle()`; `SwitchCar()` cũng ghi `ActiveLoadout.SavedCarIndex`. |
| `Assets/Script/RacePositionTracker.cs` | Thêm `SetPlayer(Transform)` — set `playerTransform` **và** cập nhật/đăng ký entry "Player" trong `allRacers` rồi `ResetRace()`. Idempotent, an toàn gọi trước/sau `Start()`. |
| `Assets/Script/MatchWaitTime.cs` | Thêm `SetPlayerVehicle(GameObject)` — clear `playerVehicles`, chỉ giữ xe đang active; khoá VC ngay nếu còn countdown. |
| `Assets/Script/LoadoutPaintApplier.cs` | **MỚI** — áp màu sơn từ `CarLoadoutSlot.loadout.paint` (fallback `ActiveLoadout.Current.paint`) lên body renderer (CarPaintTarget + tag "PaintPart"). Thay phần "apply paint" mà scene đua thiếu (garage dùng `GarageSaveManager.RestorePaint`). |
| `Assets/Script/LoadSceneController.cs` | Sau khi activate xe gọi `ApplyLoadoutPaint()` → tự `AddComponent<LoadoutPaintApplier>` nếu xe chưa có rồi `.Apply()`. Không cần wire tay trong scene. |

### Paint (màu sơn)
Scene đua KHÔNG có `GarageSaveManager` nên CarType_* không tự lên màu từ loadout. `CarLoadoutSlot` trên mỗi
CarType_* trỏ tới `Loadout_CarType{N}` (cùng asset garage sơn vào) — data có sẵn, chỉ thiếu bước apply.
`LoadoutPaintApplier` (tự thêm bởi LoadSceneController) đọc `loadout.paint` và set lên body renderer.
⚠️ Cần xe có `CarPaintTarget` (bodyRenderer + materialSlotIndex) hoặc renderer tag "PaintPart" thì mới ăn màu.
⚠️ Trong build, `loadout.paint` set lúc runtime ở garage KHÔNG persist qua lần mở app mới (chỉ trong cùng session).

---

## 4. Wiring trong scene

- **Không cần sửa `.unity`.** `LoadSceneController.matchWaitTime` để trống → tự `GetComponent<MatchWaitTime>()`
  (cùng GameObject MatchController) hoặc `FindObjectOfType`. `racePositionTracker` đã được gán sẵn trong Inspector
  (có `autoFindTracker` làm backup).
- `MatchWaitTime.playerVehicles` trong scene vẫn list 3 xe cũng không sao — runtime bị `SetPlayerVehicle()` ghi đè
  thành đúng 1 xe.

---

## 5. ⚠️ Ràng buộc index (phải đảm bảo)

Index là **convention thuần** — KHÔNG có data-link giữa GameObject `CarType_N` ở Stadium và `PlayerCarLoadout` ở garage.
Nó đúng **chỉ khi** thứ tự slot con của `CarPlace` trong garage (slot 0/1/2) khớp với tên `CarType_0/1/2` ở scene đua.

**Cách verify trong Editor:**
1. Ở garage bấm E/Q chọn từng xe → xem Console `[GarageCarManager] Active → [index] '...'`.
2. Vào Stadium → xem `[LoadSceneController] Loading car index N (source: ActiveLoadout (garage))` có ra đúng xe đã chọn không.

Nếu lệch: cần map theo **identity loadout** thay vì index (vd gán `PlayerCarLoadout`/id lên từng `CarType_N`).

---

## 6. Liên quan
- `Assets/Scenes/Design/Shop_Inventory UI_Design/Garage_UI_Design.md` — mục 4 "Current Displayed Car".
- `ActiveLoadout` cũng được `LevelController.ApplyPlayerLoadoutStats()` đọc (`ActiveLoadout.Current`) để apply stats xe.
  Nếu scene có `LevelController`, nó cần xe player active trước (đã đảm bảo nhờ execution order -110).
