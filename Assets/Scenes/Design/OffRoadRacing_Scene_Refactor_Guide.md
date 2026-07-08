# Hướng Dẫn Refactor Scene Off_Road_Racing → Custom Scene

> Tài liệu ghi lại **toàn bộ thay đổi** khi chuyển một scene demo gốc của asset Off_Road_Racing sang scene custom đầy đủ của game. Dựa trên case study thực tế: `Off_Road_Racing/Scene/1_Stadium_Sunny.unity` → `Assets/Scenes/6_Stadium_Sunny/Stadium_Sunny.unity`.

---

## 1. Tổng Quan Sự Khác Biệt

| Chỉ số | Scene gốc (asset) | Scene custom | Thay đổi |
|--------|-------------------|--------------|----------|
| Tổng YAML docs | ~2,450 | ~2,649 | +199 |
| GameObjects | 572 | 617 | +45 |
| Camera | 0 | 4 | +4 |
| MonoBehaviour | 87 | 145 | +58 |
| Scripts unique | 17 | 50 | +33 |
| Root objects | ~19 | 7 (gom nhóm) | Đơn giản hơn |
| Sun Light (m_Sun) | Không có | Có | +1 |

**Tính chất:** Scene gốc là **môi trường tĩnh** (terrain + props, không có player/UI/logic). Scene custom là **scene đua đầy đủ** có player car, HUD, match countdown, AI tracking.

---

## 2. Cấu Trúc Root Objects (Sau Refactor)

Scene custom gom toàn bộ object vào **7 root group** thay vì ~19 root rời rạc:

```
[Background]                  ← terrain, scene props, road network (giữ nguyên từ gốc)
========= Scene Design ======  ← spawn points, checkpoints, waypoints, limiters
=========== Light ==========   ← tất cả light objects
=========== System ========    ← game managers (Score Manager, MatchController, RaceCountDown)
============ Main =========    ← để trống (placeholder / label tổ chức)
PlayerCarManager               ← xe player (CarType_3) + toàn bộ components
Bonus                          ← BonusEvent, Bonus objects
```

> **Quy tắc tổ chức:** Mọi object trong scene phải thuộc 1 trong 7 nhóm. Không có root object rời rạc.

---

## 3. Những Thứ Bị Xoá Khỏi Scene Gốc

| Loại | Số lượng | Lý do |
|------|----------|-------|
| `Person_*` character models | ~24 instances | Demo crowd, không dùng trong game |
| `Crowd` objects | 2 | Demo asset |
| `Mesh1` | 4 | Debug mesh |
| `mixamorig:*` skeleton bones | ~80+ | Skeleton của người đi bộ demo |

**Cách xoá:** Tìm theo tên trong Hierarchy, Delete. Không ảnh hưởng đến track/terrain.

---

## 4. Những Thứ Được Thêm Vào

### 4.1 Nhóm `=========== System ========`

Ba GameObject manager cần thiết cho hệ thống đua:

```
=========== System ========
├── Score Manager              ← RacePositionTracker.cs
├── MatchController            ← MatchWaitTime.cs
└── RaceCountDown              ← canvas đếm ngược + text
```

#### Score Manager (RacePositionTracker)
Script: `Assets/Script/RacePositionTracker.cs`

**Serialized fields cần gán:**
```
playerTransform      → Transform gốc của CarType_3
autoDetectCheckpoints = true   (tự tìm checkpoint trong scene)
totalLaps            → thường = 1 hoặc 3
updateInterval       = 0.1
raceSettings         → ScriptableObject chứa totalLaps, endSceneName
position1Text        → Text component "Top_1/Label"
position2Text        → Text component "Top_2/Label"  
position3Text        → Text component "Top_3/Label"
playerPositionText   → Text component "You/Label"
```

**AI tự đăng ký lúc runtime:** `Race_Manager.Start()` gọi `posTracker.RegisterAIRacer(transform, "AI i")` — không cần gán tay trong Inspector.

#### MatchController (MatchWaitTime)
Script: `Assets/Script/MatchWaitTime.cs`

**Serialized fields:**
```
waitTime    = 5         (giây đếm ngược trước khi race bắt đầu)
WaitText    → Text component trong RaceCountDown canvas
waitObject  → GameObject RaceCountDown (bật/tắt canvas)
vehicles    = []        (để trống — MatchWaitTime tự tìm AI qua Car_AI/VehicleController)
onRaceStarted (UnityEvent) → Race_Manager subscribe runtime
```

#### RaceCountDown
Canvas con của MatchController chứa Text đếm ngược. Không cần script riêng — được điều khiển bởi MatchWaitTime.

---

### 4.2 Nhóm `PlayerCarManager`

Cây hierarchy đầy đủ của xe player:

```
PlayerCarManager
└── CarType_3  [tag: Player, Layer: Vehicle]
    ├── Audio (engine, tires, wind...)
    ├── Steer → Car02_Interior_SteeringWheel
    ├── BonusController
    ├── WheelColliders (FL, FR, RL, RR)
    ├── Trigger (6x Trail effects)
    ├── Backward Camera
    │   ├── Backward Camera Normal
    │   └── Backward Camera Zoom
    ├── Car02_Lights_Emissive
    ├── WheelTrans (4x wheel mesh + brake disc)
    ├── RealisticCar02_HD_Complete (car body model)
    ├── CorrectCamera
    ├── Camera_Controller_Assest
    │   ├── Camera          [depth=-1, near=0.4, far=1500]
    │   └── Main Camera     [tag: MainCamera, depth=-1, near=0.1, far=1000]
    ├── XR Inside Car & Input
    │   ├── Camera_Root
    │   ├── Camera Offset
    │   │   └── Main Camera [tag: MainCamera, depth=0, near=0.05, far=1000]
    │   │       └── Sphere  [IsActive: false — tắt để tránh che camera]
    │   ├── RightHand Controller (XR hand skeleton)
    │   └── LeftHand Controller (XR hand skeleton)
    ├── Colliders
    │   └── ColliderBody [tag: Player]
    ├── PlayerCar_FillLight  [Point Light, Realtime, Range=7]
    └── Player_InteriorLight [Point Light, Realtime, Range=3.5]
```

**Lưu ý quan trọng:**
- `CarType_3` và `ColliderBody` đều cần **tag = "Player"**
- `Sphere` (con của Main Camera cockpit) phải `IsActive = false` — nếu bật sẽ che tối màn hình ở near clip nhỏ
- `PlayerCar_FillLight` là fill light ngoại thất, `Player_InteriorLight` chiếu nội thất góc FPS

---

### 4.3 UIController (Canvas HUD)

```
UIController  [con của CarType_3]
└── Canvas
    └── Top List
        ├── Top_1 → Label  [Text vị trí 1]
        ├── Top_2 → Label  [Text vị trí 2]
        ├── Top_3 → Label  [Text vị trí 3]
        └── You   → Label  [Text vị trí player]
```

Script `UIReceiver.cs` (trên UIController):
```
autoFindTracker  = true   (tự tìm RacePositionTracker)
position1Text    → Top_1/Label
position2Text    → Top_2/Label
position3Text    → Top_3/Label
playerPositionText → You/Label
```

---

### 4.4 Nhóm `[Background]` (giữ nguyên + dọn dẹp)

Giữ nguyên từ scene gốc:
- Terrain, Scene, Scene Objects, Road Network, Side_Objects, Side_Blocks
- Airplane_Path_1, Airplane_Mover

Xoá khỏi scene gốc:
- Tất cả Person_*, Crowd, Mesh1, skeleton bones demo

---

### 4.5 Nhóm `========= Scene Design ======`

Giữ nguyên từ scene gốc, chỉ gom vào group:
- `Spawn_Points` — vị trí xuất phát xe
- `Checkpoint_Manager` — quản lý toàn bộ checkpoint
- `Waypoints` — path cho AI
- `Speed_Limiters` — hạn chế tốc độ
- `Road_Blockers` — chặn đường

---

### 4.6 Lighting

| Thay đổi | Chi tiết |
|----------|----------|
| **Sun Light** | Thêm `m_Sun` trỏ tới Directional Light → shadows đúng, bake lightmap chính xác |
| **Player_FillLight** | Point Light trên xe (Realtime, range=7, intensity=1.6) — bù sáng ngoại thất xe player |
| **Player_InteriorLight** | Point Light trong cabin (Realtime, range=3.5, intensity=2.0) — chiếu nội thất góc FPS |
| **Reflection Probe** | Chuyển sang **Realtime + OnAwake** nếu scene chưa bake → tránh nháy |

**Ambient, Lightmap data**: giữ nguyên từ scene gốc.

---

## 5. Scripts Mới Thêm (33 Scripts)

Phân nhóm theo vai trò:

| Nhóm | Script | Vai trò |
|------|--------|---------|
| **Race System** | `MatchWaitTime.cs` | Đếm ngược, bật AI chạy |
| | `RacePositionTracker.cs` | Theo dõi & hiển thị thứ hạng |
| | `UIReceiver.cs` | Nhận dữ liệu từ tracker → cập nhật HUD |
| **Player Input** | `PlayerDriveInputManager.cs` | Quản lý input (keyboard/gamepad/VR) |
| | `PlayerDriverInputFromKeyboard.cs` | Input keyboard |
| | `PlayerDriveInputFromLogitech.cs` | Input vô-lăng Logitech |
| | `PlayerInputFromVRController.cs` | Input VR controller |
| **EVP Vehicle** | `VehicleController.cs` | Physics controller xe player |
| | `VehicleAudio.cs` | Âm thanh xe player |
| | `VehicleCameraController.cs` | Camera theo xe |
| **VR/XR** | `HandData.cs`, `HandInputValue.cs`, `HandAnimated.cs` | Animation tay VR |
| | `XRKnob.cs` | Knob UI cho XR |
| **Bonus** | `BonusReceiver.cs`, `BonusEvent.cs` | Hệ thống nhặt bonus |
| **Utility** | `LoadSceneController.cs` | Chuyển scene sau khi race xong |
| | `CameraPositionCorrector.cs` | Chỉnh vị trí camera |
| | `ItemFloatingAnimation.cs` | Animation item nổi |
| | `PlayerTriggerEvent.cs` | Trigger khi xe chạm |
| | `TimedObjectController.cs` | Object bật/tắt theo thời gian |
| | `BorderSpace.cs` | Giới hạn vùng đua |

---

## 6. Quy Trình Áp Dụng Cho Scene Mới

Khi refactor một scene Off_Road_Racing mới, làm theo thứ tự:

### Bước 1 — Chuẩn bị scene gốc
- Mở scene gốc (vd `Off_Road_Racing/Scene/2_Desert_Sunny.unity`)
- Lưu vào `Assets/Scenes/<N>_<TenScene>/`

### Bước 2 — Xoá objects thừa
- Xoá tất cả `Person_*`, `Crowd`, `Mesh1`, skeleton bones demo
- Kiểm tra: chỉ giữ terrain, road, props, spawn points, checkpoints, waypoints

### Bước 3 — Gom vào groups
Tạo 7 empty GameObject làm root:
```
'[Background]'            ← kéo terrain, scene props vào
'========= Scene Design ======'  ← kéo spawn, checkpoint, waypoints vào
'=========== Light =========='   ← kéo lights vào
'=========== System ========'    ← thêm mới (xem bước 4)
'============ Main ========='    ← để trống
PlayerCarManager                  ← thêm mới (xem bước 5)
Bonus                             ← thêm mới hoặc copy từ scene cũ
```

### Bước 4 — Copy nhóm System từ scene reference
1. Mở `Assets/Scenes/6_Stadium_Sunny/Stadium_Sunny.unity` (scene reference)
2. Copy `=========== System ========` → Paste vào scene mới
3. Re-link: `MatchWaitTime.WaitText` → text object của scene này; `MatchWaitTime.waitObject` → RaceCountDown

### Bước 5 — Copy PlayerCarManager
1. Từ scene reference, copy `PlayerCarManager` → Paste vào scene mới
2. Đặt xe đúng vị trí vạch xuất phát (cạnh Spawn_Position gốc)
3. Đảm bảo tag = "Player" trên `CarType_3` và `ColliderBody`
4. Kiểm tra `Sphere` trong cockpit camera: **IsActive = false**

### Bước 6 — Gán references trong Inspector
| Component | Field | Gán vào |
|-----------|-------|---------|
| `RacePositionTracker` | `playerTransform` | Transform của `CarType_3` |
| `RacePositionTracker` | `raceSettings` | ScriptableObject settings |
| `UIReceiver` | auto-find = true | (không cần gán tay) |
| `MatchWaitTime` | `WaitText` | Text trong RaceCountDown |
| `MatchWaitTime` | `waitObject` | GameObject RaceCountDown |

### Bước 7 — Gán Sun Light
- Chọn Directional Light trong scene
- `Window → Rendering → Lighting → Sun Source` → kéo Directional Light vào

### Bước 8 — Thêm Reflection Probe nếu chưa có
- Nếu scene chưa bake: đặt Mode = Realtime, Refresh = On Awake, Time Slicing = No Time Slicing
- Nếu đã bake: giữ Mode = Baked

### Bước 9 — Kiểm tra theo recipe
Đối chiếu với [OffRoad_Scene_Conversion_Recipe.md](OffRoad_Scene_Conversion_Recipe.md):
- `Race_Manager` chỉ spawn AI (player đặt sẵn)
- `MatchWaitTime.onRaceStarted` đã có UnityEvent
- `RacePositionTracker.RegisterAIRacer()` sẵn sàng nhận AI
- Null-guard các script demo (SmoothFollow2, Minimap_Camera, Load_Settings, Checkpoint_Trigger)

### Bước 10 — Play test
Chạy thử và kiểm tra:
- [ ] AI spawn ngay khi Play
- [ ] Countdown đếm ngược, hết giờ AI chạy
- [ ] HUD hiện đúng thứ hạng
- [ ] Góc FPS không bị tối (Sphere tắt)
- [ ] Không có NRE trong Console

---

## 7. Các Lỗi Thường Gặp

| Lỗi | Nguyên nhân | Sửa |
|-----|-------------|-----|
| Camera cockpit tối dù đặt near nhỏ | `Sphere` object (con của Main Camera) đang `IsActive=true` | Set `Sphere.IsActive = false` |
| Reflection probe nháy nháy | Probe mode Baked nhưng scene chưa bake | Đổi sang Realtime + OnAwake |
| `Tag IgnoreWheelCollision not defined` | Tag chưa có trong TagManager | Thêm tag vào `ProjectSettings/TagManager.asset` |
| `InputSystem.cs NRE` | Player không có `Car_AI` component | Null-guard: `if (_playerAI != null) _playerAI.enabled = false;` |
| AI không chạy sau countdown | `MatchWaitTime.onRaceStarted` chưa được subscribe | Race_Manager phải gọi `matchWait.onRaceStarted.AddListener(OnRaceStarted)` |
| HUD thứ hạng không cập nhật | `RacePositionTracker` không tìm thấy checkpoint | Đảm bảo `autoDetectCheckpoints=true` hoặc gán checkpoint thủ công |
| Bảng thứ hạng không hiện AI | AI chưa được register | `Race_Manager.Start()` phải gọi `posTracker.RegisterAIRacer()` |

---

## 8. File Liên Quan

| File | Vai trò |
|------|---------|
| [OffRoad_Scene_Conversion_Recipe.md](OffRoad_Scene_Conversion_Recipe.md) | Quy trình 6 bước chi tiết về Race_Manager + AI system |
| [Race_Levels_Design.md](Race_Levels_Design.md) | Thiết kế 5 scene đua thật (bối cảnh, mặt đường, AI, laps + thưởng) |
| [Enemy_AI_Design.md](Enemy_AI_Design.md) | Cấp độ và cá tính AI |
| `Assets/Script/RacePositionTracker.cs` | Script tracker thứ hạng |
| `Assets/Script/MatchWaitTime.cs` | Script countdown + event bắt đầu đua |
| `Assets/Script/UIReceiver.cs` | Script nhận data → cập nhật HUD |
| `Assets/Off_Road_Racing/Scripts/AI/Race_Manager.cs` | Script quản lý AI spawn + ranking asset |

---

*Cập nhật: 2026-05-31 | Case study: `1_Stadium_Sunny.unity` → `Stadium_Sunny.unity`*
