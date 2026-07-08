# Stadium Sunny — Pipeline spawn Bonus theo vòng đua

> Tài liệu mô tả cách scene `6_Stadium_Sunny` **tự sinh bonus mỗi vòng đua**: nhân bản 1 BonusObject
> mẫu (inactive) trong `BonusPlace`, đặt gần các checkpoint, số lượng / vòng cấu hình trong
> `RaceSettings_Stadium.asset`.
> Unity 6. Triển khai 2026-06-03. Bổ trợ cho `Stadium_SpeedBoost_Pipeline.md` (bonus = tăng tốc).

---

## 0. Bối cảnh — yêu cầu

- Mỗi **lap** sinh ra một vài bonus.
- Bonus nhân bản từ **`BonusPlace > Object`** (GameObject đang **inactive**, đóng vai trò template).
- Bonus đặt **gần các object là con của object có tag `CheckPoint`** (= các checkpoint).
- **Số bonus / lap** cấu hình trong `RaceSettings_Stadium.asset` (field `bonusesPerLap`).

---

## 1. Cấu trúc liên quan trong scene

```
BonusPlace (GameObject, luôn active)
├── Object  (m_IsActive = 0)  ← TEMPLATE, không bao giờ tự bật
│   ├── Bonus  (BoxCollider trigger 3x3x3 + Rigidbody + floating + PlayerTriggerEvent)
│   │   └── item  (mesh hiển thị)
│   └── Effect (m_IsActive = 0)  ← VFX bật khi ăn bonus
└── LapBonusSpawner   ← THÊM MỚI (component trên chính BonusPlace)

Checkpoint_Manager (tag "CheckPoint")
└── Checkpoint_0 .. Checkpoint_N   ← điểm neo để spawn bonus gần đó
```

`Object` đã **tự wire sẵn** trong `PlayerTriggerEvent.onPlayerEnter`:
1. `Effect.SetActive(true)` — bật VFX.
2. `item.SetActive(false)` — ẩn mesh đã ăn.
3. `BonusEvent.TriggerBonusUnityEvent()` — đẩy bonus vào queue → tăng tốc (xem `Stadium_SpeedBoost_Pipeline.md`).

> **Wiring khi nhân bản:** call 1 & 2 (`Effect.SetActive` / `item.SetActive`) trỏ object **bên trong**
> `Object` → Unity **remap** sang clone khi Instantiate, hoạt động tự động.
>
> ⚠️ **Call 3 (`BonusEvent.TriggerBonusUnityEvent`) khi nguồn là PREFAB:** prefab asset **không thể**
> giữ tham chiếu tới singleton `BonusEvent` nằm trong scene → trong prefab `m_Target = null` → ăn bonus
> sẽ **không tăng tốc**. `LapBonusSpawner.WireBonusEvent(clone)` tự `AddListener` runtime trỏ
> `BonusEvent.Instance.TriggerBonusUnityEvent` cho mỗi clone để vá lại. (Template trong scene thì call 3
> đã wire sẵn nên không cần — nhưng spawner vẫn nối an toàn, không gây double-fire vì call null bị bỏ qua.)

---

## 2. Luồng — `LapBonusSpawner.cs` (MỚI)

`Assets/Scripts/LapBonusSpawner.cs`, gắn trên **BonusPlace**. `[DefaultExecutionOrder(-80)]`.

```
Start()
  ├─ ResolveRefs()       : tracker = FindObjectOfType<RacePositionTracker>();
  │                        raceSettings = (đã wire asset, hoặc lấy từ tracker);
  │                        bonusTemplate = BonusPlace.GetChild(0) nếu để trống
  ├─ DiscoverCheckpoints(): dùng tracker.checkpoints; nếu trống → object tag "CheckPoint" → con
  ├─ SubscribeTracker()  : tracker.onLapCompleted += HandleLapCompleted
  └─ SpawnBatch(1)       : batch cho Lap 1 (ngay khi vào race)

onLapCompleted("Player", lapCount)   [tracker bắn khi Player hoàn thành 1 vòng]
  └─ nextLap = lapCount + 1; nếu nextLap ≤ totalLaps → SpawnBatch(nextLap)
       (lap cuối = về đích → không spawn thừa)

SpawnBatch(lap)
  ├─ source = bonusPrefab (ưu tiên) hoặc bonusTemplate (fallback)
  ├─ count = raceSettings.bonusesPerLap   (fallback fallbackBonusesPerLap nếu thiếu asset)
  ├─ (tuỳ chọn) ClearSpawned()            : xoá bonus chưa ăn của lap trước
  ├─ PickCheckpointIndices(count, nCP)    : chọn checkpoint ngẫu nhiên không trùng (lặp nếu count>nCP)
  └─ mỗi checkpoint idx: Instantiate(source) tại cp.position + up*spawnHeight + lệch ngẫu nhiên(spawnRadius)
       với rotation = GetForwardToNextCheckpoint(idx) → SetActive(true)
```

> **Hướng Z của bonus** (`GetForwardToNextCheckpoint`): trục Z (forward) hướng về **checkpoint kế tiếp**
> theo thứ tự vòng đua — `next = checkpointAnchors[(idx+1) % nCP]` (checkpoint cuối vòng lại về 0).
> Vd spawn ở `Checkpoint_4` → Z hướng tới `Checkpoint_5`. Chỉ xoay quanh trục Y (`dir.y = 0`) để bonus
> đứng thẳng. Hai checkpoint trùng vị trí → giữ rotation của checkpoint hiện tại.

> **Chỉ Player** kích spawn (AI hoàn thành lap không tạo bonus). Với race `totalLaps = 1`, chỉ có
> batch Lap 1 lúc vào race (đủ đúng yêu cầu "mỗi lap 1 batch").

---

## 3. Cấu hình

### 3.1. Số bonus / lap — `RaceSettings_Stadium.asset`
| Field | Mặc định | Ý nghĩa |
|---|---|---|
| `bonusesPerLap` | 3 | Số bonus spawn cho **mỗi vòng**. 0 = tắt spawn. |

(Field mới thêm vào `RaceSettings.cs` — dùng chung kiểu asset, các scene khác cũng có.)

### 3.2. Tuỳ chỉnh đặt vị trí — component `LapBonusSpawner` trên BonusPlace
| Field | Mặc định | Ý nghĩa |
|---|---|---|
| `bonusPrefab` | `Assets/Prefabs/Bonus/Object.prefab` (đã wire bằng GUID) | **Ưu tiên** — prefab nhân bản. Trống → dùng `bonusTemplate`. |
| `bonusTemplate` | `Object` (đã wire) | Fallback: template trong scene khi không có prefab. |
| `spawnParent` | (trống = BonusPlace) | Cha chứa clone. |
| `tracker` / `raceSettings` | auto / asset | Để trống vẫn tự tìm. |
| `spawnHeight` | 1.5 | Nâng bonus khỏi checkpoint (m). |
| `spawnRadius` | 3 | Lệch ngẫu nhiên quanh checkpoint (m). 0 = ngay checkpoint. |
| `clearPreviousLapBonuses` | true | Xoá bonus chưa ăn của lap trước khi sang lap mới. |
| `fallbackBonusesPerLap` | 3 | Dùng khi không có `raceSettings`. |

---

## 4. Code đã thêm / sửa

| File | Thay đổi |
|---|---|
| `Assets/Scripts/LapBonusSpawner.cs` | **MỚI** — spawn bonus mỗi lap gần checkpoint, nhân bản **prefab** (`bonusPrefab`, fallback template), Z hướng về checkpoint kế tiếp (`GetForwardToNextCheckpoint`), nghe `onLapCompleted`. |
| `Assets/Prefabs/Bonus/Object.prefab` | Prefab bonus được nhân bản (root "Object" active, tự wire `PlayerTriggerEvent` → Effect/item/BonusEvent). |
| `Assets/Script/RaceSettings.cs` | Thêm `int bonusesPerLap` (Min 0, default 3). |
| `Assets/Scenes/6_Stadium_Sunny/RaceSettings_Stadium.asset` | `bonusesPerLap: 3`. |
| `Assets/Scenes/6_Stadium_Sunny/Stadium_Sunny.unity` | Thêm `LapBonusSpawner` lên `BonusPlace`; `bonusPrefab` = `Object.prefab` (GUID), `bonusTemplate` = `Object` (fallback), `raceSettings` = asset. |

---

## 5. Verify

1. Compile sạch. Mở scene Stadium.
2. Chỉnh `RaceSettings_Stadium.asset > bonusesPerLap` (vd 5).
3. Play:
   - Console: `[LapBonusSpawner] Tìm thấy N checkpoint ...` → `Lap 1: spawn 5 bonus ...`.
   - Thấy bonus xuất hiện gần các checkpoint (nâng ~1.5m). Lái xe qua → VFX bật + xe tăng tốc.
   - (Race nhiều lap) Hoàn thành 1 vòng → Console `Lap 2: spawn ...`, bonus lap trước (chưa ăn) bị xoá.
4. Đặt `bonusesPerLap = 0` → không bonus nào spawn.

⚠️ **Lưu ý dọn dẹp:** nếu scene còn **bonus đặt tay** (PlayerTriggerEvent active ngoài template) từ
trước, chúng vẫn hoạt động song song với bonus spawn. Muốn chỉ dùng hệ spawn → xoá các bonus đặt tay,
giữ lại `BonusPlace > Object` (template inactive).

---

## 6. Liên quan
- `Stadium_SpeedBoost_Pipeline.md` — bonus ăn vào làm gì (tăng tốc thật).
- `Stadium_Results_Pipeline.md` / `Stadium_CarLoad_Pipeline.md` — phần còn lại của scene.
- `RacePositionTracker.onLapCompleted` — nguồn sự kiện "hoàn thành 1 vòng".
- `Checkpoint_Manager` (tag "CheckPoint") — danh sách checkpoint dùng làm điểm spawn.
