# Design — Hệ thống Linh Kiện & Mua Sắm (CarPart Shop)

Tài liệu thiết kế cơ chế mua linh kiện, lắp ráp, và ghi nhớ trang bị của từng xe.

---

## 1. Tổng quan kiến trúc

```
┌──────────────────────────────────────────────────────────────────┐
│  DATA LAYER (ScriptableObjects — .asset trong Project)           │
│                                                                  │
│  PlayerInventory          PlayerCarLoadout × N xe                │
│  ─────────────────         ────────────────────────              │
│  ownedParts[]              baseStats (CarStats)                  │
│  TryBuyPart()              equippedParts[]  ← CarPart refs       │
│                            GetEffectiveStats()                   │
└────────────────────────────────┬─────────────────────────────────┘
                                 │ sync
┌────────────────────────────────▼─────────────────────────────────┐
│  SCENE LAYER (MonoBehaviours)                                    │
│                                                                  │
│  CarLoadoutSlot            WheelStats                            │
│  ─────────────────         ────────────────                      │
│  loadout → Loadout_Car1    partData → CarPart_SportTires         │
│  EquipTires(part)          listens: WheelItem.onAttached         │
│  UnequipTires(part)        finds CarLoadoutSlot in car parent    │
│                            calls EquipTires / UnequipTires       │
└──────────────────────────────────────────────────────────────────┘
```

**Luồng dữ liệu:**
```
Player mua bánh → PlayerInventory.ownedParts[] (thêm CarPart_SportTires)
Player lắp bánh → WheelStats.HandleAttached()
                   → CarLoadoutSlot.EquipTires(CarPart_SportTires)
                   → PlayerCarLoadout.equippedParts[] (thay Tires cũ)
Race bắt đầu   → LevelController reads PlayerCarLoadout
                   → GetEffectiveStats() → CarStats (0-100)
                   → Map sang physics (VehicleController fields)
```

---

## 2. ScriptableObject Data Model

### 2.1 CarPart (đã có — `Assets/Script/CarPart.cs`)

Mỗi loại bánh xe = 1 `CarPart` asset với `slot = Tires`:

```
Assets/Data/CarParts/
  ├── Tires_Stock.asset       costGold: 0,  grip:+0,  maxSpeed:+0  (mặc định)
  ├── Tires_Sport.asset       costGold: 500, grip:+10, handling:+8
  ├── Tires_Racing.asset      costGold: 1200, grip:+20, maxSpeed:+5, handling:+15
  └── Tires_Offroad.asset     costGold: 800,  grip:+5,  braking:+12
```

> Tạo asset: Right-click Project → **Race/Car Part** → chọn `slot = Tires`

### 2.2 PlayerCarLoadout (đã có — `Assets/Script/PlayerCarLoadout.cs`)

Mỗi xe có **1 asset riêng**, đặt ở:
```
Assets/Data/Loadouts/
  ├── Loadout_CarType1.asset   baseStats: {speed:50, accel:55, grip:45, ...}
  ├── Loadout_CarType2.asset   baseStats: {speed:60, accel:45, grip:50, ...}
  └── Loadout_CarType3.asset   ...
```

Khi lắp bánh: `equippedParts` sẽ chứa đúng 1 `CarPart` có `slot = Tires` (Tires cũ bị replace).

### 2.3 PlayerInventory (mới — `Assets/Script/PlayerInventory.cs`)

**1 asset duy nhất toàn game** — lưu danh sách linh kiện đã sở hữu:
```
Assets/Data/PlayerInventory.asset
```

- `ownedParts[]` — thêm khi mua, không xoá (owned vĩnh viễn)
- `TryBuyPart(part, ref gold)` — kiểm tra tiền → deduct → add to list
- Trong Editor: asset là "save". Trong Build: cần serialize ra JSON/PlayerPrefs (xem mục 8)

---

## 3. Scene Hierarchy — CartPartPlace

```
CartPartPlace
  ├── Spray  (existing, unchanged)
  │     ├── sprayCan_blue
  │     └── ...
  └── Wheels                          ← parent chứa bánh xe sở hữu
        ├── WheelSet_Stock            ← mặc định, luôn active
        │     ├── Wheel_FL  (WheelItem + WheelStats → Tires_Stock)
        │     ├── Wheel_FR
        │     ├── Wheel_RL
        │     └── Wheel_RR
        ├── WheelSet_Sport            ← chỉ active khi đã mua
        │     ├── Wheel_FL  (WheelItem + WheelStats → Tires_Sport)
        │     └── ...
        └── WheelSet_Racing           ← chỉ active khi đã mua
              └── ...
```

**Trạng thái active của WheelSet:**
- Chưa mua: `SetActive(false)` — ẩn khỏi scene
- Đã mua nhưng chưa lắp xe: active, nằm dưới `CartPartPlace/Wheels`
- Đã lắp lên xe: WheelItem GO được reparent vào `WheelSocket` trên xe (hệ thống WheelItem hiện tại tự xử lý)
- Tháo ra khỏi xe: WheelItem GO quay về world root → player nhặt lên → để lại `CartPartPlace/Wheels` hoặc lắp sang xe khác

---

## 4. Per-Car Loadout Memory

Mỗi car GO trong `CarPlace` cần 2 component:

| Component | Mô tả |
|---|---|
| `GarageCarSlot` | Đã có — quản lý activate/deactivate xe |
| `CarLoadoutSlot` | Mới — trỏ đến `PlayerCarLoadout` asset của xe đó |

```
CarPlace
  ├── CarType_1   [GarageCarSlot] [CarLoadoutSlot → Loadout_CarType1.asset]
  ├── CarType_2   [GarageCarSlot] [CarLoadoutSlot → Loadout_CarType2.asset]
  └── CarType_3   [GarageCarSlot] [CarLoadoutSlot → Loadout_CarType3.asset]
```

**Khi bánh xe được lắp vào CarType_2:**
```
WheelStats.HandleAttached()
  → socket.GetComponentInParent<CarLoadoutSlot>()  → tìm thấy slot CarType_2
  → slot.EquipTires(Tires_Sport)
  → Loadout_CarType2.equippedParts = [..., Tires_Sport]  ← lưu ngay vào asset
```

Vì `PlayerCarLoadout` là ScriptableObject, thay đổi tồn tại **giữa các lần Play** trong Editor.

---

## 5. Component trên mỗi WheelItem GO trong CartPartPlace/Wheels

| Component | Cấu hình |
|---|---|
| `Transform` | Vị trí trên kệ/sàn trong garage |
| `Rigidbody` | `UseGravity: true`, `IsKinematic: false` — bánh rơi/nằm tự nhiên |
| `MeshRenderer` + `MeshFilter` | Visual của bánh |
| `BoxCollider` | Fit mesh, không trigger |
| `WheelItem` | `state: Detached`, `snapDistance: 1.2` |
| `WheelStats` | `partData` → đúng `CarPart` asset (vd: Tires_Sport) |
| `PCInteractorObject` | Giống setup trên xe — `ghostMaterial`, `ghostPreviewDistance: 1.2` |

---

## 6. Luồng Mua Linh Kiện (dự kiến)

```
UI Shop hiện danh sách CarPart chưa owned (PlayerInventory.OwnsPart() == false)
  │
  ▼
Player click "Mua" → ShopUI.gọi PlayerInventory.TryBuyPart(part, ref playerGold)
  │  success: true
  ▼
Tìm WheelSet GO tương ứng trong CartPartPlace/Wheels
  → WheelSet.SetActive(true)  ← bánh xuất hiện trong garage
  │
  ▼ (lần sau load scene)
PlayerInventory.ownedParts → rebuild: SetActive(true) cho các WheelSet đã owned
```

**Ánh xạ CarPart → WheelSet GO:**
Thêm field vào `WheelStats`: `isOwnedByDefault = false`. Script khởi động:
```csharp
// WheelPartSpawner.cs (optional helper):
void Start()
{
    bool owned = inventory.OwnsPart(wheelStats.partData) || wheelStats.isOwnedByDefault;
    gameObject.SetActive(owned);
}
```

Hoặc đơn giản hơn: ShopUI trực tiếp gọi `wheelSetGO.SetActive(true)` khi mua.

---

## 7. Stats Flow — Bánh xe → Race

```
Garage: WheelStats.HandleAttached()
          └── CarLoadoutSlot.EquipTires(Tires_Sport)
                └── PlayerCarLoadout.equippedParts = [Tires_Sport, ...]

Race scene load: LevelController.Start()
  └── PlayerCarLoadout.GetEffectiveStats()
        └── baseStats + Tires_Sport.statBonus + ... = CarStats {speed:60, grip:65, ...}
              └── Map sang VehicleController:
                    motorForce  ← acceleration (0-100 → physics value)
                    maxSpeed    ← maxSpeed
                    gripFactor  ← grip
                    brakeForce  ← braking
                    steerFactor ← handling
```

---

## 8. Persistence (Save/Load)

| Môi trường | Cách lưu |
|---|---|
| Editor (hiện tại) | ScriptableObject asset tự lưu khi thay đổi — `EditorUtility.SetDirty()` |
| Build (tương lai) | Serialize `PlayerInventory.ownedParts[]` (GUIDs hoặc tên) ra `PlayerPrefs` / JSON. Load lại trong `Awake()` |

**Ghi chú:** `PlayerCarLoadout.equippedParts` cũng cần serialize riêng nếu muốn nhớ giữa các session build. Phương án đơn giản: lưu `partName` (string) vào PlayerPrefs, restore bằng `Resources.Load<CarPart>`.

---

## 9. Checklist — Thêm loại bánh xe mới

- [ ] Tạo `CarPart` asset (`Race/Car Part`, `slot = Tires`) tại `Assets/Data/CarParts/`
  - Điền `partName`, `costGold`, `statBonus` (vd: `grip: +15`, `handling: +10`)
  - Gán `icon` (sprite để hiện trong Shop UI)
- [ ] Duplicate 1 WheelSet đã có trong `CartPartPlace/Wheels`
  - Rename thành `WheelSet_[TenBanh]`
  - Đổi mesh (MeshFilter) trên từng Wheel_FL/FR/RL/RR nếu cần visual khác
- [ ] Trên mỗi WheelItem GO trong WheelSet mới:
  - `WheelStats.partData` → trỏ vào CarPart asset vừa tạo
- [ ] `SetActive(false)` trên WheelSet GO (chưa owned)
- [ ] Thêm entry trong Shop UI (nếu có) trỏ vào CarPart asset + WheelSet GO
- [ ] Test: mua → WheelSet active → cầm bánh → lắp lên xe → vào race → kiểm tra stats

---

## 10. Checklist — Thêm xe mới hỗ trợ loadout

- [ ] Tạo `PlayerCarLoadout` asset (`Race/Player Car Loadout`) tại `Assets/Data/Loadouts/`
  - Điền `baseStats` theo cân bằng xe mới
  - `equippedParts` để trống (sẽ tự cập nhật khi lắp bánh)
- [ ] Gắn `CarLoadoutSlot` component lên GO xe mới trong scene
  - `loadout` → trỏ vào asset vừa tạo
- [ ] Đảm bảo 4 WheelSocket nằm trong hierarchy của xe đó (để `GetComponentInParent<CarLoadoutSlot>()` tìm được)
- [ ] Trong `LevelController`, đọc đúng `PlayerCarLoadout` của xe player chọn
