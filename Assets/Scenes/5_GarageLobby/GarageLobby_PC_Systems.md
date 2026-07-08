# GarageLobby PC — Hướng dẫn hệ thống Bánh Xe & Sơn Xe

Tài liệu này mô tả cách hoạt động và cách thêm mới hệ thống **tháo/lắp bánh xe** và **sơn xe** cho xe mới trong scene `GarageLobby_pc`.

---

## 1. Hệ thống Bánh Xe (Wheel Attach/Detach)

### 1.1 Cấu trúc GameObject

Mỗi bánh xe cần 2 lớp GameObject theo cấu trúc cha–con:

```
[CarRoot]
  └── WheelTrans
        ├── [SocketGO]          ← Cha — chứa WheelSocket
        │     └── [WheelMeshGO] ← Con — chứa WheelItem + các component vật lý
        ├── [SocketGO]
        │     └── [WheelMeshGO]
        └── ...
```

**Ví dụ thực tế (CarType_2):**

```
CarType_2
  └── WheelTrans (775776635)
        ├── RMCar05_WheelFrontLeft  (socket GO)  → layer 7
        │     └── RMCar05WheelFrontLeft (mesh GO) → layer 11 "Wheels"
        ├── RMCar05_WheelFrontRight (socket GO)  → layer 7
        │     └── RMCar05WheelFrontRight (mesh GO)→ layer 11 "Wheels"
        ├── RMCar05_WheelRearLeft   (socket GO)  → layer 7
        │     └── RMCar05WheelRearLeft  (mesh GO) → layer 11 "Wheels"
        └── RMCar05_WheelRearRight  (socket GO)  → layer 7
              └── RMCar05WheelRearRight (mesh GO) → layer 11 "Wheels"
```

---

### 1.2 Component trên Socket GO (cha)

| Component     | Layer | Cấu hình quan trọng                                   |
| ------------- | ----- | ----------------------------------------------------- |
| `WheelSocket` | 7     | `initialWheel` = WheelItem component trên mesh GO con |

**WheelSocket Inspector:**

- `Side` — tự detect từ tên GO: tên chứa "left" → Left, "right" → Right
- `Left Side Rotation Y` — rotation khi attach bánh trái (thường = `0`)
- `Right Side Rotation Y` — rotation khi attach bánh phải (thường = `180`)
- `Initial Wheel` — **kéo WheelItem script (không phải GO)** của mesh GO con vào đây
- `Snap Radius` — bán kính OverlapSphere để detect XR grab (mặc định `0.2`)

> **Lưu ý rotation:** Bánh trái thường `Y=0`, bánh phải `Y=180` vì mặt bánh quay ngược chiều. Kiểm tra `LocalEulerAnglesHint.Y` của mesh GO gốc để xác định đúng giá trị.

---

### 1.3 Component trên Wheel Mesh GO (con)

Thêm đúng thứ tự này (thứ tự ảnh hưởng đến fileID trong YAML):

| #   | Component            | Layer | Cấu hình quan trọng                                                              |
| --- | -------------------- | ----- | -------------------------------------------------------------------------------- |
| 1   | `XRGrabInteractable` | 11    | `m_Enabled: 0` (tắt cho PC-only mode)                                            |
| 2   | `Rigidbody`          | 11    | `UseGravity: true`, `IsKinematic: false` (WheelItem sẽ tự điều chỉnh khi attach) |
| 3   | `BoxCollider`        | 11    | Chỉnh size fit mesh (~0.29×0.77×0.77 cho bánh GR86)                              |
| 4   | `WheelItem`          | 11    | `state: Attached`, `snapDistance: 0.3`                                           |
| 5   | `PCInteractorObject` | 11    | Xem bảng dưới                                                                    |

**PCInteractorObject cho bánh xe:**

- `Interaction Type` = `AutoDetect` (tự nhận WheelItem)
- `Allow Direct Input` = `false` (không dùng F-key trực tiếp trên object, để PCHotkeyManager gọi)
- `Interact Key` = `F` (keycode 102)
- `Player Camera` = Camera của PC player (fileID: 625429101 trong scene này)
- `Max Interact Distance` = `3`
- `Raycast Mask` = `4091` (loại bỏ layer 2)
- `Hold Distance` = `0.8`
- `Hold Rotation Offset Euler` = `(0, -90, -18.88)`
- `Ignore Wheel Snap Distance For PC` = `true`
- `Allow Socket Toggle When Empty` = `true`
- `Ghost Material` = `WheelGhostMaterial` (`Assets/Prefabs/CarPart/Wheel/WheelGhostMaterial.mat`)
- `Ghost Preview Distance` = `1.2` (nên lớn hơn `snapDistance` của WheelItem)

---

### 1.4 Luồng hoạt động

```
Scene Load
  └── WheelSocket.Start()
  │     └── AttachWheel(initialWheel)
  │           └── WheelItem.RegisterToSocket(socket)
  │                 ├── SetParent(socket.transform)
  │                 ├── localPosition = Vector3.zero
  │                 ├── localRotation = GetAttachRotation()
  │                 └── Rigidbody: isKinematic=true, useGravity=false
  └── WheelItem.Start() — nếu không có socket cha (bánh rời)
        └── state = Detached  ← sửa mismatch serialized state

Nhấn F để THÁO bánh (đang attached):
  └── PCInteractorObject.TryInteract()
        └── WheelItem.ToggleAttachState(ignoreSnapDistance=true)
              └── WheelItem.Detach()
                    ├── _lastSocket = currentSocket  ← nhớ socket gốc
                    ├── socket.NotifyWheelDetached()
                    ├── SetParent(null)
                    └── Rigidbody: isKinematic=false, useGravity=true → bánh rơi

Nhìn vào bánh rơi + nhấn F để LẮP LẠI:
  └── PCInteractorObject.TryInteract()
        └── WheelItem.ToggleAttachState(true)
              └── WheelItem.TryAttachNearestSocket(ignoreSnapDistance=true)
                    ├── Nếu _lastSocket còn trống & active → AttachWheel(_lastSocket)
                    └── Nếu không → FindNearestSocket(∞, requireEmpty=true)
                                    [chỉ xét socket activeInHierarchy + thuộc ActiveCar]

Cầm bánh (chuột trái):
  └── PCInteractorObject.PickupObject()
        ├── WheelItem.Detach() nếu IsAttached
        ├── Rigidbody: kinematic=true, useGravity=false
        ├── Tắt tất cả Collider  ← tránh depenetration fling khi gần xe
        └── CreateGhost()  ← tạo bản sao mesh với ghostMaterial

Đang cầm — mỗi LateUpdate:
  └── UpdateHeldObject()
        ├── Lerp vị trí/rotation về phía trước camera
        └── UpdateGhostPreview()
              ├── FindNearestAvailableSocket(ghostPreviewDistance=1.2m)
              ├── Tìm thấy → ghost hiện tại đúng position/rotation của socket đó
              │             _currentPreviewSocket = socket
              └── Không tìm thấy → ghost ẩn, _currentPreviewSocket = null

Thả bánh (chuột trái):
  └── PCInteractorObject.ReleaseHeldObject()
        ├── DestroyGhost()
        ├── Bật lại tất cả Collider  ← TRƯỚC khi restore physics (fresh contact state)
        ├── Rigidbody: khôi phục useGravity/kinematic gốc
        ├── Nếu _currentPreviewSocket != null  [ghost đang hiện]
        │     → AttachWheel trực tiếp vào socket đó
        └── Nếu không → TryAttachNearestSocket(false, snapDistance=0.3m)
```

---

### 1.5 Lưu ý quan trọng (bug đã gặp)

| Vấn đề                                              | Nguyên nhân                                                                                                              | Fix                                                                                                                |
| --------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------ |
| Bánh biến mất sau khi lắp lại                       | `FindNearestSocket` chọn socket của xe khác đang inactive (`HasWheel=false` lúc Start)                                   | Thêm `activeInHierarchy` check trong `FindNearestSocket`                                                           |
| Bánh lắp vào sai vị trí                             | `FindNearestSocket(∞)` quét toàn scene, bánh rơi gần socket khác hơn socket gốc                                          | Lưu `_lastSocket` khi Detach, ưu tiên trả về socket gốc                                                            |
| Bánh rời (không gắn vào socket) bị floating lơ lửng | YAML serialized `state: Attached` nhưng không có socket cha → `Start()` gọi `SetRigidbodyFree(false)` sai                | `WheelItem.Start()`: nếu không có socket cha thì reset `state = Detached`, để Rigidbody giữ nguyên gravity từ YAML |
| Bánh bị văng mạnh khi thả gần xe                    | Kinematic Rigidbody bị overlap vào collider xe trong lúc cầm → khi `isKinematic=false` physics bắn depenetration impulse | `PickupObject`: tắt hết Collider lúc cầm; `ReleaseHeldObject`: bật lại Collider trước khi restore physics          |
| Ghost hiện nhưng thả ra không snap                  | `ghostPreviewDistance` (1.2m) > `snapDistance` (0.3m) → `TryAttachNearestSocket(false)` không đủ gần                     | Lưu `_currentPreviewSocket` khi ghost hiện, khi thả gọi `AttachWheel` thẳng vào socket đó                          |

---

### 1.6 Thêm xe mới — Checklist

- [ ] Đặt tên socket GO chứa "left"/"right" để `WheelSocket.AutoDetectSide()` hoạt động đúng
- [ ] Kiểm tra `m_LocalEulerAnglesHint.Y` của mesh GO gốc: nếu Y=180 thì `rightSideRotationY=180`, nếu Y=0 thì `leftSideRotationY=0`
- [ ] Set layer socket GO = `7`, layer mesh GO = `11` (layer "Wheels" phải tồn tại trong Project Settings)
- [ ] Gán đúng `initialWheel` = **component WheelItem** (không phải GO) trên mesh con
- [ ] `XRGrabInteractable` phải disabled (`m_Enabled: 0`) nếu chỉ dùng PC
- [ ] `BoxCollider` phải fit mesh thực tế (xem trong Scene view)
- [ ] Thêm `PCInteractorManager` singleton vào scene nếu chưa có
- [ ] Đảm bảo `GarageCarManager` trong scene có `ActiveCarTransform` trỏ đúng xe đang active

---

## 2. Hệ thống Sơn Xe (Car Paint)

> CODEx NOTE 2026-05-27: Ghi chu nhanh ve design hien tai, dung khi can sua scene sau nay.

### Design notes - Wheel socket vs Wheel item

Mot banh xe trong garage phai co 2 cap GameObject, khong gan logic thao/lap truc tiep vao mesh render bat ky:

```text
CarType_X
  WheelTrans
    CarXX_Wheel_FrontLeft          # socket GO, layer 7, co WheelSocket
      CarXX_Wheel_FrontLeft_LOD0   # wheel item GO, layer 11, co WheelItem + PCInteractorObject
```

Quy uoc:

- `CarXX_Wheel_*` la socket, giu vi tri lap banh tren xe.
- `CarXX_Wheel_*_LOD0` la banh that player thao/cam/lap.
- `WheelSocket.initialWheel` phai tro vao component `WheelItem` tren child `*_LOD0`, khong tro vao GameObject.
- Socket GO can co `WheelSocket`, layer `7`, ten chua `Left`/`Right`.
- Wheel item GO can co `XRGrabInteractable` disabled, `Rigidbody`, `BoxCollider`, `WheelItem`, `PCInteractorObject`, `WheelStats`, layer `11`.
- `PCInteractorObject.requireCenterRaycast=true` nghia la tam man hinh phai hit collider cua wheel item hoac child cua no.

`CarType_2` da duoc chuan hoa theo pattern:

```text
CarType_2
  WheelTrans
    Car02_Wheel_FrontLeft      -> socket, initialWheel = Car02_Wheel_FrontLeft_LOD0
      Car02_Wheel_FrontLeft_LOD0
    Car02_Wheel_FrontRight     -> socket, initialWheel = Car02_Wheel_FrontRight_LOD0
      Car02_Wheel_FrontRight_LOD0
    Car02_Wheel_BackLeft       -> socket, initialWheel = Car02_Wheel_BackLeft_LOD0
      Car02_Wheel_BackLeft_LOD0
    Car02_Wheel_BackRight      -> socket, initialWheel = Car02_Wheel_BackRight_LOD0
      Car02_Wheel_BackRight_LOD0
```

Neu mot `CarType` khong thao/cam duoc banh trong khi xe khac van duoc, hay kiem tra scene truoc:

- Socket co `WheelSocket` chua.
- Wheel mesh co `WheelItem`, `PCInteractorObject`, `Rigidbody`, `Collider` chua.
- `initialWheel` co tro dung `WheelItem` cua child khong.
- Layer socket/wheel co dung `7`/`11` khong.
- Collider cua wheel item co enabled va fit mesh khong.

### Design notes - Paint active car and PaintPart

Paint khong hardcode renderer cua mot xe trong spray can. Cac `sprayCan_*` nen de `carBodyRenderer = null`, sau do `CarPaintCan` resolve target qua `GarageCarManager`.

Luong paint:

```text
Player cam spray can + bam F
  PCCameraController.HandleInteractionInput()
    -> heldObject.TryInteract()
      -> CarPaintCan.ApplyPaint()
        -> GarageCarManager.TryGetActivePaintTargets()
          -> lay active car hien tai theo Q/E
          -> lay CarPaintTarget explicit
          -> lay them Renderer co tag PaintPart trong active car
        -> ResolvePaintMaterial(carTypeName)
        -> swap material cho tat ca target
```

Quy uoc:

- `GarageCarManager.ActiveCarName` / `CarPaintTarget.carTypeName` quyet dinh dang son cho `CarType_0`, `CarType_1`, hay `CarType_2`.
- `CarPaintCan.paintMaterial` la fallback, hien dung cho `CarType_0`.
- `CarPaintCan.carPaintMaterials[]` map material override theo car name, vi cung mau `blue/black/grey/...` nhung moi car type co material rieng.
- `CarPaintTarget` dung cho renderer chinh/explicit, co `bodyRenderer` va `materialSlotIndex`.
- Mesh phu can son cung luc thi gan tag `PaintPart`.
- Renderer co tag `PaintPart` nhung khong co `CarPaintTarget` se duoc son vao material slot cuoi (`sharedMaterials.Length - 1`). Ly do: cac door part cua `CarType_2` dat material paint o slot cuoi.
- Renderer co `CarPaintTarget` duoc uu tien slot explicit, vi body cua `CarType_2` dung slot `0`.

Bug da gap:

- `CarType_1` bi son ra material `CarType2_blue`: `CarPaintTarget.carTypeName` bi gan sai.
- `CarType_2` co nhieu `PaintPart` nhung chi mot so part doi mau: tag-only renderer dang bi son slot `0`, trong khi door paint nam slot cuoi.
- Spray can chi son 1 xe: `carBodyRenderer` hardcode, phai de null va dung active car manager.

### 2.1 Cấu trúc thực tế trong scene

```
Spray  (root GO, layer 0)          fileID: 1744478875
  ├── sprayCan_blue                 fileID: 1931476517  layer 11
  ├── sprayCan_red                  fileID: 513116518   layer 11
  ├── sprayCan_grey                 fileID: 987423437   layer 11
  ├── sprayCan_gray_orange          fileID: 1043946721  layer 11
  ├── sprayCan_white                fileID: 1734136397  layer 11
  └── sprayCan_black                fileID: 1806325659  layer 11
```

Mỗi spray can GO có cấu trúc con như sau:

```
sprayCan_blue  (root)
  ├── [mesh child 1]    ← model 3D, tự có MeshCollider/MeshRenderer
  ├── [mesh child 2]    ← model 3D
  ├── [mesh child 3]    ← model 3D
  └── VFXSpawnPoint     fileID: 192534071 — localPosition (0, 0.484, -0.042)
                        ← Transform con trống, dùng làm điểm spawn hiệu ứng phun sơn
```

> **Collider:** Không có BoxCollider riêng trên root GO. Collider đến từ các child GO của model 3D (MeshCollider). `PCInteractorObject` dùng `GetComponentsInChildren<Collider>()` nên vẫn detect được.

---

### 2.2 Component trên mỗi Spray Can GO

| #   | Component            | Ghi chú                                        |
| --- | -------------------- | ---------------------------------------------- |
| 1   | `Transform`          | localPosition đặt tại vị trí bình trong garage |
| 2   | `Rigidbody`          | Cho phép pick-up bằng chuột trái (cầm bình)    |
| 3   | `XRGrabInteractable` | **m_Enabled: 0** — tắt cho PC-only mode        |
| 4   | `CarPaintCan`        | Script chính — xem cấu hình bên dưới           |
| 5   | `PCInteractorObject` | Bắt F-key — xem cấu hình bên dưới              |

---

### 2.3 CarPaintCan — Cấu hình chi tiết

| Field               | Giá trị trong scene                                | Mô tả                                                   |
| ------------------- | -------------------------------------------------- | ------------------------------------------------------- |
| `carBodyRenderer`   | MeshRenderer của `Car01_Body` (fileID: 1649262071) | MeshRenderer thân xe cần đổi màu — **đây là CarType_1** |
| `materialSlotIndex` | `0`                                                | Slot material thứ 0 trên Renderer                       |
| `paintMaterial`     | Xem bảng màu bên dưới                              | Material màu mới                                        |
| `vfxPrefab`         | guid: `509d82fbb38fd6e469da2bed9eaec8d2`           | Prefab hiệu ứng phun sơn                                |
| `vfxSpawnPoint`     | Transform con `VFXSpawnPoint` (fileID: 192534072)  | Vị trí spawn VFX                                        |
| `vfxScale`          | `(0.35, 0.35, 0.35)`                               | Scale VFX                                               |
| `vfxLifetime`       | `1` giây                                           | Thời gian tồn tại VFX                                   |

**Bảng màu — Material GUID của từng bình:**

| Tên bình             | Material GUID                      |
| -------------------- | ---------------------------------- |
| sprayCan_blue        | `82237c04e69f860498197d1949c76c66` |
| sprayCan_red         | `d59397374d3f0374b829ea2e1e893732` |
| sprayCan_grey        | `9e086fdc6fb18014cbc2394a41996941` |
| sprayCan_gray_orange | `bdd7cb420b85b3a4cada09a46981f6a3` |
| sprayCan_white       | `6d42eab3bd9264641a5a7fdbba9f88fc` |
| sprayCan_black       | `281a6eddc43f7514d87e1119d046b288` |

> **Lưu ý:** `carBodyRenderer` hiện trỏ vào **Car01_Body của CarType_1**. Khi thêm xe mới, phải đổi tham chiếu này sang MeshRenderer thân xe tương ứng.

---

### 2.4 PCInteractorObject cho Spray Can

```yaml
interactionType: 0 # AutoDetect — tự nhận CarPaintCan
allowDirectInput: 0
interactKey: 102 # F
playerCamera: 625429101 # PC Camera trong scene này
maxInteractDistance: 3
raycastMask: 4091
requireCenterRaycast: 1
allowPickupWithLeftClick: 1
holdDistance: 0.8
holdPositionOffset: { x: 0, y: -0.5, z: 0 } # Khác bánh xe (y: -0.4)
holdRotationOffsetEuler: { x: 0, y: 0, z: 0 } # Không xoay (khác bánh xe)
holdMoveSpeed: 180
holdRotateSpeed: 180
```

---

### 2.5 Public API của CarPaintCan

```csharp
paintCan.PreviewPaint();          // Swap material tạm + spawn VFX (dùng cho hover UI)
paintCan.ApplyPaint();            // Xác nhận — lưu material mới làm gốc  ← F-key gọi cái này
paintCan.CancelPreview();         // Trả lại material gốc (dùng cho hover UI)
paintCan.SetPaintMaterial(mat);   // Đổi material bình lúc runtime
paintCan.TriggerVFX();            // Kích hoạt VFX thủ công từ UnityEvent / AnimationEvent
```

---

### 2.6 Luồng hoạt động

```
Scene Load:
  └── CarPaintCan.Awake() → cache originalMaterial = carBodyRenderer.sharedMaterials[slot]

Nhìn vào bình sơn + nhấn F:
  └── PCInteractorObject.TryInteract()
        └── CarPaintCan.ApplyPaint()
              ├── sharedMaterials[slot] = paintMaterial
              ├── originalMaterial = paintMaterial  ← lưu gốc mới
              └── SpawnVFX() → Instantiate(vfxPrefab) tại vfxSpawnPoint → Destroy(vfxLifetime)

Cầm bình (chuột trái giữ):
  └── PCInteractorObject.PickupObject()
        └── Rigidbody: kinematic=true, useGravity=false
        └── UpdateHeldObject() mỗi LateUpdate → lerp về holdDistance phía trước camera

Thả bình (chuột trái nhả):
  └── PCInteractorObject.ReleaseHeldObject()
        └── Rigidbody: khôi phục useGravity/kinematic gốc → bình rơi xuống
```

---

### 2.7 Thêm bình sơn cho xe mới — Checklist

- [ ] Tìm MeshRenderer thân xe mới → lấy fileID của component đó (dùng trong scene YAML)
- [ ] Kiểm tra `materialSlotIndex` trên Renderer của xe mới (mở Inspector → Materials, đếm từ 0)
- [ ] Tạo Materials màu mới (hoặc dùng lại material cũ nếu cùng màu)
- [ ] Duplicate một trong 6 spray can hiện có trong Hierarchy
- [ ] Trên component `CarPaintCan` của bình mới: đổi `carBodyRenderer` → Renderer xe mới, `paintMaterial` → material màu mới
- [ ] Tạo child GO `VFXSpawnPoint` (empty Transform) ở đầu bình (khoảng `y: +0.48`)
- [ ] Gán `vfxSpawnPoint` = Transform vừa tạo
- [ ] Đặt GO vào dưới parent `Spray` trong Hierarchy
- [ ] **Không** cần thêm BoxCollider riêng — collider đến từ mesh model của bình

---

### 2.8 Thêm bộ bình sơn cho xe thứ 2 song song với xe thứ 1

Vì `carBodyRenderer` là field **trực tiếp** trên CarPaintCan (không dynamic), mỗi bình chỉ sơn được **1 xe cố định**. Để hỗ trợ nhiều xe:

**Cách 1 — Duplicate cả nhóm Spray:**

```
Spray_CarType1   ← active khi CarType1 đang được chọn
  └── sprayCan_blue (trỏ Car01_Body)
  └── ...

Spray_CarType2   ← active khi CarType2 đang được chọn
  └── sprayCan_blue (trỏ RMCar05_Body)
  └── ...
```

Dùng `GarageCarManager` để bật/tắt nhóm tương ứng khi đổi xe.

**Cách 2 — Cập nhật `carBodyRenderer` lúc runtime:**

```csharp
// Trong GarageCarManager.OnCarChanged:
foreach (CarPaintCan can in FindObjectsByType<CarPaintCan>(...))
    can.SetCarBodyRenderer(newCar.GetBodyRenderer());
```

Cần thêm method `SetCarBodyRenderer(Renderer r)` vào `CarPaintCan.cs`.

---

## 2.9 Design notes - Engine, Suspension, BrakeCaliper

> CODEx NOTE 2026-05-28: BrakeCaliper mirrors the WheelItem/WheelSocket flow. Engine and Suspension are data-only loadout parts.

### Engine and Suspension

- `CarPart.PartSlot.Engine` and `CarPart.PartSlot.Suspension` are loadout data only.
- They do not spawn or replace scene models in `GarageLobby_pc`.
- Each `PlayerCarLoadout.equippedParts` keeps the selected engine/suspension part so race scenes or UI can read stats later through `PlayerCarLoadout.GetEffectiveStats()`.
- Current stock defaults are `Assets/Data/CarParts/Engine_Stock.asset` and `Assets/Data/CarParts/Suspension_Stock.asset`.

### UI_CarStats live stats

> CODEx NOTE 2026-05-28: `UI_CarStats` no longer shows random stat changes. It now displays the active car's current effective stats after loadout parts are equipped or removed.

- Runtime source of truth is `GarageCarManager.ActiveSlot -> CarLoadoutSlot.GetEffectiveStats()`.
- `CarStatsUIManager` auto-tracks the active car, subscribes to `CarLoadoutSlot.StatsChanged`, and refreshes rows when wheels/brakes/parts call `Equip*` or `Unequip*`.
- `TriggerRandomStatBoost()` is intentionally kept as a compatibility wrapper for existing UnityEvent wiring in `GarageLobby_pc.unity`; it now calls `RefreshCurrentStats()` and shows real current stats.
- Initial loadout restore can happen one frame after scene start, so `CarStatsUIManager` refreshes once on `Start()` and once again after one frame without popping the panel.
- `showPanelOnStatsChanged` controls whether the stats panel opens when a component change updates the loadout.
- `showPanelOnPartRemoved` controls whether detach/unequip also opens the panel. Keep it off to show stats only when a part is attached/equipped.
- `UI_CarStats` must stay active in the scene so `CarStatsUIManager` can subscribe to loadout events. The script hides/shows the Canvas component instead of disabling the whole GameObject.
- `WheelStats` and `BrakeStats` call `CarStatsUIManager.ReportPartChangeAnchor(anchor, isAttach)` before updating the loadout, so the stats panel knows where the changed part/socket is and whether the change was attach or detach.
- `suppressPanelDuringStartup` blocks panel popups during scene boot, because initial sockets may attach/restored parts and fire stats events before the player does anything.
- When stats change, `showDelayAfterStatsChanged` waits briefly before showing the panel so the attach action can finish first.
- When shown, `placeInFrontOfPlayer` places the world-space panel once between the active camera and that part anchor (`anchorBlendFromPlayer`, plus `worldOffset`) and rotates the panel toward the player on Y axis only. It does not keep following the camera while visible.

### BrakeCaliper structure

```text
CarType_X
  BrakeCaliper
    FL        # socket GO, tag BrakePart, runtime BrakeSocket
      ...     # visual brake item, BrakeItem/BrakeStats/PCInteractorObject
    FR
    RL
    RR
```

- The socket GO is the transform tagged `BrakePart`.
- `BrakeRuntimeBootstrap` adds `BrakeSocket` to every object with tag `BrakePart` at scene boot.
- The first renderer child under the socket is treated as the initial brake item if it does not already have `BrakeItem`.
- `BrakeSocket` auto-detects side from socket name: `FL`/`RL`/`Left` use left prefab, `FR`/`RR`/`Right` use right prefab.
- Brake prefabs live in `Assets/Prefabs/CarPart/BrakeCaliper`.
- Brake data assets live in `Assets/Data/CarParts/Brakes_*.asset` and reference left/right prefab variants through `CarPart.brakePrefabLeft` and `CarPart.brakePrefabRight`.

### Brake collider / PC pickup notes

> CODEx NOTE 2026-05-28: Scene brake items were missing `BoxCollider`, so center-screen PC raycast could not hit the `BrakeItem` and left-click pickup/detach did nothing.

- Every visual brake item GO must have `BrakeItem`, `BrakeStats`, `PCInteractorObject`, `Rigidbody`, and a non-trigger `BoxCollider`.
- The socket GO (`FL`/`FR`/`RL`/`RR`, tag `BrakePart`) should keep `BrakeSocket`; the collider belongs on the visual brake item, not on the socket, so `hit.collider.GetComponentInParent<PCInteractorObject>()` resolves to the item being picked up.
- Current scene instances in `GarageLobby_pc.unity` were fixed by adding `BoxCollider` to the 12 brake item objects:
  `Car01_BrakeCaliper_*`, `RMCar05_Brake*LOD0`, and `Car02_BrakeCaliper_*LOD0`.
- Brake prefabs under `Assets/Prefabs/CarPart/BrakeCaliper` already include `BoxCollider`, so restored/replaced loadout brakes should remain pickable.
- Ghost preview is created by `PCInteractorObject.CreateGhost()` when the held brake is near an empty `BrakeSocket`.
  If `ghostMaterial` is assigned, it uses that material; if the scene item has `ghostMaterial: {fileID: 0}`, `PCInteractorObject` now creates a runtime cyan transparent fallback material so brake ghost still appears.
- Symptom checklist: if a brake cannot be picked up, verify the visual item has an enabled Collider, its layer is included by `PCInteractorObject.raycastMask`, and the center raycast is hitting the item instead of only the car body/socket.
  If pickup works but no ghost appears, verify the item is within `ghostPreviewDistance` of an empty `BrakeSocket` and `FindNearestAvailableSocket()` is not filtering out the active car.

### Brake interaction flow

```text
Scene boot
  GarageSaveManager.Awake()
    -> BuildPartCatalog()
    -> BrakeRuntimeBootstrap.EnsureSceneSetup(default Normal Brakes)
    -> CacheSavedState()

After one frame
  GarageSaveManager.Start()
    -> RestorePaint()
    -> RestoreTires()
    -> RestoreBrakes()

Player interaction
  Look at BrakeItem + left click -> detach/hold
  Hold near BrakeSocket -> ghost preview
  Release while ghost is visible -> attach to preview socket
  Press F on attached BrakeItem/BrakeSocket -> detach
  Press F on detached BrakeItem near socket -> attach nearest socket
```

### Brake save/load contract

- Brake save key is per car and per socket: `GB_{carName}_{brakeSocketName}`.
- Example: `GB_CarType_0_FL`, `GB_CarType_0_FR`, `GB_CarType_0_RL`, `GB_CarType_0_RR`.
- `CarLoadoutSlot` also keeps the active brake parts in `PlayerCarLoadout.equippedParts`.
- Restore priority: PlayerPrefs brake key first; if missing, one brake in loadout applies to all sockets; otherwise multiple brake parts are applied by socket order.

### Files

- `Assets/Script/Brakes/BrakeSocket.cs`
- `Assets/Script/Brakes/BrakeItem.cs`
- `Assets/Script/Brakes/BrakeStats.cs`
- `Assets/Script/Brakes/BrakeRuntimeBootstrap.cs`
- `Assets/Script/GarageSaveManager.cs`
- `Assets/Script/CarLoadoutSlot.cs`
- `Assets/Script/CarPart.cs`

---

## 3. Hệ thống PC Interaction (tổng quan)

```
PCInteractorObject    ← gắn trên từng object tương tác
PCInteractorManager   ← singleton, đảm bảo chỉ cầm 1 vật/lúc
PCHotkeyManager       ← quản lý F-key, duyệt qua tất cả PCInteractorObject trong view
PCCameraController    ← camera first-person, raycast từ center screen
```

### Cơ chế raycast khi nhấn F

1. `PCHotkeyManager` (hoặc PCInteractorObject với `allowDirectInput=true`) lắng nghe `Input.GetKeyDown(F)`
2. Bắn ray từ center màn hình (`ViewportPointToRay(0.5, 0.5, 0)`)
3. Nếu ray hit collider thuộc `PCInteractorObject` nào đó → gọi `TryInteract()`
4. `TryInteract()` kiểm tra distance, raycast xác nhận → gọi `ExecuteInteraction()`
5. `ExecuteInteraction()` dispatch theo `_resolvedInteractionType`:
   - `CarPaintCan` → `ApplyPaint()`
   - `WheelItem` → `ToggleAttachState(ignoreSnapDistance=true)`
   - `WheelSocket` → `ToggleAttachState(attachNearestWhenEmpty=true)`

### Pick-up bằng chuột trái

- Object có `Rigidbody` + `allowPickupWithLeftClick=true` → click chuột trái để cầm / thả
- Object di chuyển về phía trước camera ở `holdDistance`, Collider bị tắt trong lúc cầm (tránh văng)
- Khi cầm WheelItem: ghost preview hiện tại socket gần nhất trong `ghostPreviewDistance`
- Thả → nếu ghost đang hiện (`_currentPreviewSocket != null`) → snap ngay vào socket đó; nếu không → fallback `TryAttachNearestSocket(false)`

---

## 4. Layer Reference

| Layer        | Số  | Dùng cho                                                          |
| ------------ | --- | ----------------------------------------------------------------- |
| Default      | 0   | Mặc định                                                          |
| _(Car body)_ | 7   | Socket GOs, car structure                                         |
| **Wheels**   | 11  | WheelItem mesh GOs — bắt buộc để WheelSocket.OverlapSphere detect |

> Layer "Wheels" phải được tạo trong **Project Settings → Tags and Layers** trước khi dùng.
