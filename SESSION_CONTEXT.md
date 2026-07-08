# Session Context — VHD_DATN_V2

> Cập nhật: 2026-05-28
> Dùng file này để resume chat mới: paste toàn bộ nội dung vào đầu conversation.

---

## 1. Dự án là gì?

Unity game (Unity 6), PC, garage + racing. Scene chính đang làm: `Assets/Scenes/5_GarageLobby_pc/GarageLobby_pc.unity`.

---

## 2. Hệ thống đã hoàn thiện trong các session trước

### 2.1 WheelItem / Bánh xe vật lý

- **`Assets/Script/Wheels/WheelItem.cs`** — fix state mismatch khi Start(): nếu không có parent socket mà state=Attached → chỉ sửa `state = Detached`, không đụng Rigidbody (tránh float).
- **`snapDistance = 1.2f`** (đồng bộ với ghostPreviewDistance).
- Có `FindNearestAvailableSocket(float maxDistance)` public wrapper.
- **Fix mới (2026-05-28):** Thêm guard `if (IsAttached && currentSocket != null) return;` vào đầu `Start()` để tránh double-registration khi prefab được spawn + attach trước khi `Start()` chạy.

### 2.2 PCInteractorObject — anti-fling + ghost preview

- **`Assets/Script/PC_Player_Input/PCInteractorObject.cs`**
- Anti-fling: disable colliders khi pickup, re-enable trước khi restore physics → tránh depenetration impulse.
- Ghost preview: khi cầm bánh, tạo ghost GO với `ghostMaterial` tại socket gần nhất trong `ghostPreviewDistance`.
- `_currentPreviewSocket` tracking: ghost hiện = sẽ snap ngay khi thả.
- Khi thả: nếu ghost đang hiện → `AttachWheel` thẳng vào socket đó; không thì `TryAttachNearestSocket(false)`.

### 2.3 CarPart Shop Architecture

File thiết kế: `Assets/Scenes/Design/CarPart_Shop_Architecture.md`

**Scripts (tất cả đã tạo):**
| Script | Mô tả |
|--------|--------|
| `Assets/Script/CarPart.cs` | ScriptableObject định nghĩa 1 linh kiện (slot, stats, giá, wheelPrefab) |
| `Assets/Script/PlayerCarLoadout.cs` | ScriptableObject lưu trang bị của 1 xe |
| `Assets/Script/PlayerInventory.cs` | ScriptableObject lưu linh kiện player đã mua |
| `Assets/Script/CarLoadoutSlot.cs` | MonoBehaviour trên car GO, bridge sang PlayerCarLoadout |
| `Assets/Script/WheelStats.cs` | MonoBehaviour trên WheelItem GO, bridge sang CarLoadoutSlot |

**Data assets đã tạo:**

```
Assets/Data/CarParts/
  Tires_Stock.asset    (GUID: 6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c)
  Tires_Sport.asset    (GUID: 7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c)
  Tires_Racing.asset   (GUID: 8f9a0b1c2d3e4f5a6b7c8d9e0f1a2b3c)
  Tires_Blue.asset     → wheelPrefab: CarWheel_blue
  Tires_Grey.asset     → wheelPrefab: CarWheel_Grey
  Tires_GreyNormal.asset → wheelPrefab: CarWheel_Grey_Normal
  Tires_Normal.asset   → wheelPrefab: CarWheel_Normal
  Tires_BlackFast.asset → wheelPrefab: CarWheel_BlackFast
  Tires_Offroad_Normal.asset → wheelPrefab: CarWheel_Offroad_Normal
  Tires_Offroad_Good.asset   → wheelPrefab: CarWheel_OffRoad_Good
  Tires_Offroad_Great.asset  → wheelPrefab: CarWheel_Offroad_Great

Assets/Data/Loadouts/
  Loadout_CarType1.asset  (GUID: 9f0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c)
  Loadout_CarType2.asset  (GUID: 0f1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c)
  Loadout_CarType3.asset  (GUID: 1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c)

Assets/Data/PlayerInventory.asset  (GUID: 2f3a4b5c6d7e8f9a0b1c2d3e4f5a6b7c)
  → ownedParts: [Tires_Stock], gold: 1000
```

**Đã wire vào scene `GarageLobby_pc.unity`:**

- 8 `WheelStats` component (fileID 5000000001–5000000008) gắn trên 8 WheelItem GO, tất cả trỏ Tires_Stock, `isOwnedByDefault: 1`
- 3 `CarLoadoutSlot` component (fileID 7000000001–7000000003) gắn trên CarType_1/2/3, mỗi cái trỏ đúng Loadout asset

### 2.4 CarPaintCan — sơn xe theo xe đang active

- **`Assets/Script/Interact Tool/CarPaintCan.cs`** — đã rewrite: auto-detect xe active qua `GarageCarManager.Instance.ActiveSlot` + `CarPaintTarget`.
- **`Assets/Script/Interact Tool/CarPaintTarget.cs`** — component mới, đặt trên car body GO, có `bodyRenderer` + `materialSlotIndex`.
- Khi active car đổi (Q/E), preview auto cancel.

**Đã wire vào scene:**

- 6 spray can: `carBodyRenderer` đã xoá (= null) → dùng auto-detect
- 3 `CarPaintTarget` mới (fileID 8000000001–8000000003):
  - Car01_Body → MeshRenderer 1649262071, slot 0
  - Car02_Body_LOD0 → MeshRenderer 1855538283, slot 0
  - RMCar05_Paint_LOD0 → MeshRenderer 1878331802, slot 0

### 2.5 Design notes — Wheel & Paint

Ghi chu ASCII de tranh loi encoding:

**Wheel design**

- Moi vi tri banh phai co 2 cap GO:
  `CarXX_Wheel_*` = socket GO, layer 7, co `WheelSocket`.
  `CarXX_Wheel_*_LOD0` = wheel item GO, layer 11, co `XRGrabInteractable` disabled, `Rigidbody`, `BoxCollider`, `WheelItem`, `PCInteractorObject`, `WheelStats`.
- `WheelSocket.initialWheel` phai tro vao component `WheelItem` cua child `*_LOD0`.
- Khi Start, socket attach initial wheel, wheel item set parent ve socket, localPosition zero, localRotation theo side, Rigidbody kinematic.
- Neu chi mot `CarType` khong thao/cam duoc banh, kiem tra scene truoc: socket co `WheelSocket` chua, wheel item co `WheelItem/PCInteractorObject/Rigidbody/Collider` chua, layer co dung 7/11 khong, `initialWheel` co dung khong.
- `CarType_2` da duoc chuan hoa: `Car02_Wheel_FrontLeft/FrontRight/BackLeft/BackRight` la socket, cac `*_LOD0` la wheel item.

**Paint design**

- Spray can khong hardcode renderer. `carBodyRenderer` nen null de `CarPaintCan` resolve qua `GarageCarManager`.
- `GarageCarManager.TryGetActivePaintTargets()` lay xe active theo Q/E, gom `CarPaintTarget` explicit va renderer tag `PaintPart`.
- Material theo car type: `paintMaterial` fallback cho `CarType_0`, `carPaintMaterials[]` override cho `CarType_1`, `CarType_2`.
- `CarPaintTarget.carTypeName` phai dung voi car root (`CarType_0`, `CarType_1`, `CarType_2`) de khong bi lay nham material.
- Renderer tag `PaintPart` khong co `CarPaintTarget` se son slot cuoi (`sharedMaterials.Length - 1`), vi door parts cua `CarType_2` dat paint material o slot cuoi.
- Renderer co `CarPaintTarget` dung `materialSlotIndex` explicit; body cua `CarType_2` dung slot 0.

### 2.6 Garage Save/Restore System *(hoàn thành 2026-05-28)*

**Vấn đề gốc:** `WheelSocket.Start()` auto-attach bánh mặc định → gọi `RecordTires()` → ghi đè PlayerPrefs trước khi `GarageSaveManager.Start()` có thể restore.

**Fix:** Cache PlayerPrefs trong `Awake()` (trước mọi `Start()`), dùng `IEnumerator Start()` với `yield return null` để đợi WheelSocket.Start() xong, rồi restore từ cache.

**PlayerPrefs Keys:**
- `GP_{carSlotName}` = material.name (màu sơn)
- `GW_{carSlotName}` = CarPart.partName (loại bánh)
- `ActiveCarIndex` = index xe đang chọn (qua `ActiveLoadout.SavedCarIndex`)

**Files đã sửa:**

**`Assets/Script/ActiveLoadout.cs`** *(tạo mới)*
```csharp
public static class ActiveLoadout
{
    private const string PREF_KEY = "ActiveCarIndex";
    public static PlayerCarLoadout Current { get; set; }
    public static int SavedCarIndex
    {
        get => PlayerPrefs.GetInt(PREF_KEY, 0);
        set { PlayerPrefs.SetInt(PREF_KEY, value); PlayerPrefs.Save(); }
    }
}
```

**`Assets/Script/GarageCarManager.cs`** `[DefaultExecutionOrder(-50)]`
- `Awake()`: khôi phục `_activeIndex` từ `ActiveLoadout.SavedCarIndex`
- `SetActiveCar()`: lưu `ActiveLoadout.SavedCarIndex` + set `ActiveLoadout.Current = loadoutSlot.loadout`

**`Assets/Script/GarageSaveManager.cs`** `[DefaultExecutionOrder(-40)]`
- `Awake()`: `CacheSavedState()` — đọc PlayerPrefs vào Dictionary trước khi bất kỳ Start() nào chạy
- `IEnumerator Start()`: `yield return null` → `RestorePlayerPrefsFromCache()` → `RestorePaint()` → `RestoreTires()`
- `RestoreTires()` làm 2 việc:
  1. `loadoutSlot.RestoreTires(part)` — cập nhật CarStats SO, silent, không fire events
  2. Visual: foreach WheelSocket → nếu sai loại thì `DetachCurrentWheel()` + disable cũ + `Instantiate(part.wheelPrefab)` + `socket.AttachWheel(newItem)` → `RegisterToSocket()` tự re-parent + position + rotation

**`Assets/Script/LevelController.cs`** `[DefaultExecutionOrder(-100)]`
- `ApplyPlayerLoadoutStats()` dùng `ActiveLoadout.Current ?? settings.playerLoadout`
- Map CarStats abstract 0-100 → VehicleController physics fields qua piecewise lerp (50 = default value)

**`Assets/Script/CarPart.cs`** — thêm field:
```csharp
[Header("Visual (Garage)")]
public GameObject wheelPrefab;
```

**Wheel Prefabs** (`Assets/Prefabs/CarPart/Wheel/`):

| Prefab | GUID | Root GO fileID |
|--------|------|----------------|
| CarWheel_blue | `d9c7183530f8b314e8f51d0c27a3d8f1` | `1573107979128479402` |
| CarWheel_Grey | `2adebbb17e9fac84f95ad876f4cf5363` | `4553176099797289384` |
| CarWheel_Grey_Normal | `266425a174b8c6c40ad9f18f2895994b` | `2771704192400638498` |
| CarWheel_Normal | `8fe86a910667d224d8e89b8bf66280c0` | `8070956857943865358` |
| CarWheel_BlackFast | `74dabec23139cfb49b5c2fc03828bca2` | `6031644104618473191` |
| CarWheel_Offroad_Normal | `45a1212436432004c9d82eb649a656e7` | `1575805836955711915` |
| CarWheel_OffRoad_Good | `536dd36b0c2fc2d4e89c00c13ed10c11` | `4433352475528090614` |
| CarWheel_Offroad_Great | `1ed559af5ec70904491763edb348d16e` | `5201561421110888248` |

**Flow load lại game:**
```
GarageCarManager.Awake() [Order -50]
  → DiscoverSlots(), SetActiveCar(ActiveLoadout.SavedCarIndex)

GarageSaveManager.Awake() [Order -40]
  → CacheSavedState()  ← đọc GP_/GW_ vào cache TRƯỚC mọi Start()

WheelSocket.Start()  ← ghi đè GW_ về "Stock Tires"

GarageSaveManager.Start() [coroutine]
  → yield return null
  → RestorePlayerPrefsFromCache()  ← ghi lại giá trị gốc từ cache
  → RestorePaint()
  → RestoreTires()
      → loadoutSlot.RestoreTires(part)  ← stats silent
      → foreach WheelSocket: Detach cũ → Instantiate prefab → AttachWheel
          → RegisterToSocket() → SetParent(socket), đúng localRot
          → onAttached → WheelStats → EquipTires() → RecordTires()
```

**TODO sau khi test:**
- Gán `GarageSaveManager.partCatalog[]` trong Inspector của CarPlace (kéo tất cả CarPart asset vào)

---

## 3. Luồng tương tác chính (F key)

```
PCCameraController.HandleInteractionInput()
  └── Raycast từ giữa màn hình
  └── Tìm PCInteractorObject trên vật bị hit
  └── interactor.TryInteract(camera)
        └── CanInteractFromPlayerView() — phải nhìn thẳng vào vật
        └── ExecuteInteraction()
              ├── CarPaintCan  → ApplyPaint()
              ├── WheelItem    → ToggleAttachState() hoặc Release
              └── WheelSocket  → ToggleAttachState()
```

---

## 4. Cấu trúc scene hierarchy (các GO quan trọng)

```
GarageLobby_pc (scene)
├── Player
│   └── [PCCameraController, PCPlayerMovement]
│   └── Camera (child)
├── CarPlace  ← [GarageCarManager -50] [GarageSaveManager -40]
│   ├── CarType_1  [GarageCarSlot] [CarLoadoutSlot → Loadout_CarType1]
│   │   └── Car01_Body  [CarPaintTarget → MeshRenderer 1649262071, slot 0]
│   ├── CarType_2  [GarageCarSlot] [CarLoadoutSlot → Loadout_CarType2]
│   │   └── Car02_Body > Car02_Body_LOD0  [CarPaintTarget → MeshRenderer 1855538283, slot 0]
│   └── CarType_3  [GarageCarSlot] [CarLoadoutSlot → Loadout_CarType3]
│       └── RMCar05_Paint_LOD0  [CarPaintTarget → MeshRenderer 1878331802, slot 0]
└── CartPartPlace
    ├── Spray
    │   ├── sprayCan_blue   [CarPaintCan: paintMaterial=blue, carBodyRenderer=null→auto]
    │   ├── sprayCan_red    [CarPaintCan: paintMaterial=red,  carBodyRenderer=null→auto]
    │   ├── sprayCan_white  [CarPaintCan: paintMaterial=white]
    │   ├── sprayCan_black  [CarPaintCan: paintMaterial=black]
    │   ├── sprayCan_grey   [CarPaintCan: paintMaterial=grey]
    │   └── sprayCan_gray_orange [CarPaintCan: paintMaterial=gray_orange]
    └── Wheels  (CartPartPlace/Wheels — chứa WheelSet GOs)
```

---

## 5. Các file scripts quan trọng

| File                                            | Ghi chú |
| ----------------------------------------------- | ------- |
| `Assets/Script/Interact Tool/CarPaintCan.cs`    | auto-detect active car |
| `Assets/Script/Interact Tool/CarPaintTarget.cs` | GUID: `6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b` |
| `Assets/Script/WheelStats.cs`                   | bridge WheelItem ↔ CarLoadoutSlot |
| `Assets/Script/CarLoadoutSlot.cs`               | bridge xe ↔ PlayerCarLoadout SO |
| `Assets/Script/PlayerInventory.cs`              | SO lưu parts đã mua |
| `Assets/Script/CarPart.cs`                      | SO linh kiện, có `wheelPrefab` field |
| `Assets/Script/PlayerCarLoadout.cs`             | SO loadout xe |
| `Assets/Script/ActiveLoadout.cs`                | static bridge garage → racing scene |
| `Assets/Script/GarageCarManager.cs`             | Order -50, singleton, quản lý xe active |
| `Assets/Script/GarageSaveManager.cs`            | Order -40, singleton, save/restore paint+tires |
| `Assets/Script/LevelController.cs`             | Order -100, apply loadout stats vào VehicleController |
| `Assets/Script/Wheels/WheelItem.cs`             | quản lý trạng thái bánh, register vào socket |
| `Assets/Script/Wheels/WheelSocket.cs`           | detect + attach WheelItem |

---

## 6. Việc chưa làm / có thể làm tiếp

- [ ] **Test save/restore** trong Unity Editor: lắp bánh → tắt Play → Play lại → kiểm tra wheel đúng loại spawn tại đúng socket.
- [ ] **Gán partCatalog[]** trên GarageSaveManager trong Inspector (kéo tất cả CarPart asset vào).
- [ ] **CartPartPlace/Wheels hierarchy**: Tạo WheelSet GOs cho Tires_Sport và Tires_Racing trong scene (chưa có bánh vật lý cho 2 loại này, `wheelPrefab = null`).
- [ ] **ShopUI**: UI để player mua linh kiện (gọi `PlayerInventory.TryBuyPart()`), khi mua thành công thì `SetActive(true)` WheelSet tương ứng.
- [ ] **WheelPartSpawner**: Script `Start()` kiểm tra `PlayerInventory.OwnsPart()` để `SetActive` đúng WheelSet.
- [ ] **Persistence**: `PlayerInventory.SaveToPlayerPrefs()` / `LoadFromPlayerPrefs()` trong Build.

---

## 7. Key bugs đã fix (để không fix lại)

| Bug | Nguyên nhân | Fix |
| --- | ----------- | --- |
| Bánh float không rơi | `Start()` gọi `SetRigidbodyFree(false)` → kinematic | Chỉ sửa `state`, không đụng Rigidbody |
| Bánh tự gắn lại khi click thả | `ignoreWheelSnapDistanceForPC` trong fallback snap | Dùng `TryAttachNearestSocket(false)` làm fallback |
| Bánh văng khi thả gần xe | Depenetration impulse overlap | Disable colliders khi hold, re-enable trước restore physics |
| Spray can chỉ sơn CarType_1 | `carBodyRenderer` hardcode 1 renderer | Xoá hardcode, dùng `CarPaintTarget` + `GarageCarManager` |
| Save bị ghi đè về Stock khi load | `WheelSocket.Start()` auto-attach → gọi `RecordTires()` | Cache PlayerPrefs trong Awake, restore sau `yield return null` |
| Double-registration khi spawn prefab | `WheelItem.Start()` gọi `RegisterToSocket()` lần 2 | Guard `if (IsAttached && currentSocket != null) return;` |

---

## 8. Scene file stats

- Path: `Assets/Scenes/5_GarageLobby_pc/GarageLobby_pc.unity`
- Branch: `main`
---

## CODEx NOTE 2026-05-28 - Brake/Engine/Suspension

- `CarPart` now supports `brakePrefabLeft` and `brakePrefabRight` for `PartSlot.Brakes`.
- Engine and Suspension are data-only `CarPart` entries stored in each `PlayerCarLoadout.equippedParts`; they do not replace scene models.
- Brake sockets are scene transforms tagged `BrakePart`, normally `FL`, `FR`, `RL`, `RR` under each car's `BrakeCaliper` root.
- `BrakeRuntimeBootstrap` turns those tagged transforms into `BrakeSocket`s at boot and ensures their visual child has `BrakeItem`, `BrakeStats`, and `PCInteractorObject`.
- Brake prefabs under `Assets/Prefabs/CarPart/BrakeCaliper` also carry `BrakeItem`, `BrakeStats`, `PCInteractorObject`, `Rigidbody`, and `BoxCollider` so loadout restore can spawn them directly.
- Brake save key format: `GB_{carName}_{brakeSocketName}`. This allows four different brake calipers per car, similar to the per-wheel tire keys.
- `GarageSaveManager` restores in this order: paint, tires, brakes. Brake restore uses PlayerPrefs first, then falls back to brake entries in the loadout.
