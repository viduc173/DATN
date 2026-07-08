# Inventory UI — Context triển khai

> Phần đã code cho `UI_Main Menu > Part > Inventory`, theo brief [UI_Design_Inventory.md](UI_Design_Inventory.md)
> + caveat [UI_Design_Inventory_Note.md](UI_Design_Inventory_Note.md). Đồng bộ với [Shop_UI_Implementation.md](Shop_UI_Implementation.md). Index: [README.md](README.md).

---

## 1. Script mới

### `Assets/Script/Garage/InventoryUIController.cs` (guid `a9b8c7d6e5f4430c1b2a3948576e5f40`)
- Gắn lên GameObject **"Inventory"** (self = search root → không đụng button menu khác).
- Mỗi panel discover template bằng component `ShopButtonManager` (tên-agnostic → khớp cả "Item Show Button" lẫn "Item Button"), ẩn template + cache, duplicate 1 clone / part từ `context.GetOwnedParts(slot)`.
- Gán icon/title/description; `enablePrice = false`; với **wheel/brake** title hiện `"{tên} (N)"` với N = `GetAvailableQuantity`.
- Map panel→slot: wheel→Wheels, brake→Brakes, engine→Engine, suspension→Suspension, ecu→ECU.
- **Wire 2 nút** (ShopButtonManager chỉ tự nối nút Default → `onPurchaseClick`; nút Purchased nối tay qua `purchasedButton.onClick`):
  - **Wheel/Brake**: Default "Send To Garage" → `staging.Spawn(part, N)` + `SetState(Purchased)`; Purchased "Return To Inventory" → `staging.Clear(part)` + `SetState(Default)`. **KHÔNG đụng loadout xe.** State khởi tạo theo `staging.IsSpawned(part)`.
  - **Engine/Susp/ECU**: Default "Equip" → `context.EquipOwnedPart` + `SetState(Purchased)`; Purchased "Unequip" → `context.UnequipOwnedPart` + `SetState(Default)`. State khởi tạo theo `DisplayedLoadout.HasPart(part)`.
- Refresh (Rebuild) khi: enable, `onPartPurchased`, `onPartEquipped`, `onPartUnequipped`, `onDisplayedCarChanged`.

### `Assets/Script/Garage/GaragePartStaging.cs` (guid `e1f2a3b4c5d6470e8f9a0b1c2d3e4f50`)
- Gắn lên **"CartPartPlace"**. Quản việc **spawn/clear mô hình thật** (đáp ứng "Send/Return") tại anchor:
  - `wheelAnchor` = `CartPartPlace > Wheels` (Transform `1036742926`)
  - `brakeAnchor` = `CartPartPlace > Brake` (Transform `1976490975`)
- `Spawn(part, count)`: wheel → `Instantiate(part.wheelPrefab)` × count; brake → `brakePrefabLeft` + `brakePrefabRight` × count (cặp L/R). Xếp lưới theo `spacing`/`perRow`. `freezeSpawned` set Rigidbody kinematic để mô hình đứng yên ở khu staging.
- `Clear(part)` destroy đúng các mô hình đã spawn của part đó. Track qua `Dictionary<CarPart, List<GameObject>>`.
- **Không** thay đổi inventory quantity, **không** equip lên xe — lắp thật là bước kéo-thả riêng (`WheelItem`/`WheelSocket`).

## 2. Sửa data layer
`Assets/Script/Garage/GarageDisplayedCarContext.cs`:
- Thêm event `onPartUnequipped : UnityEvent<CarPart>` (fire từ `UnequipOwnedWheel/Brake` + method mới).
- Thêm `UnequipOwnedPart(CarPart)` cho engine/susp/ecu (lấp gap §5 của note): kiểm tra `HasPart` → `CarLoadoutSlot.UnequipPart` → sync + fire `onPartUnequipped`.

## 3. Đã wire vào scene (.unity)
- `InventoryUIController` (comp `101378296`) trên Inventory GO `1714841121`; refs auto (context/staging fileID 0), panelSlots 5 entry.
- `GaragePartStaging` (comp `101378297`) trên CartPartPlace GO `1829321517`; anchor đã trỏ Wheels/Brake.

## 4. Còn lại / cần chốt
- **Layout mô hình staging**: hiện xếp lưới đơn giản + Rigidbody kinematic. Tinh chỉnh `spacing`/`perRow`/`spawnWithGravity` trong Inspector cho hợp khu vực `CartPartPlace`.
- **Lắp lên xe** (kéo-thả `WheelItem` → `WheelSocket`) là hệ có sẵn riêng — Inventory UI không tự lắp.
- **Persistence** (gold + đồ qua phiên) vẫn chưa nối — xem [Shop_UI_Implementation.md](Shop_UI_Implementation.md) §7.

---

## 5. ĐÃ TRIỂN KHAI: spray (spawn) + car (select) trong Inventory

Inventory panel hiện đã có đủ: `wheel, brake, engine, suspension, ecu, spray, car` (spray template "Item Show Button", car template "Item Button"). Anchor `CartPartPlace > Spray` (Transform `1744478876`) đã có. Phân 3 nhóm hành vi:

| Nhóm | Slot/panel | Nút Default | Nút Purchased | Hành vi |
|---|---|---|---|---|
| **Staging (spawn)** | wheel, brake, **spray** | Send To Garage | Return To Inventory | spawn/clear mô hình ở CartPartPlace (KHÔNG đụng loadout) |
| **Equip** | engine, suspension, ecu | Equip | Unequip | gắn/tháo vào loadout xe đang hiển thị |
| **Select** | **car** | Select | Selected | đổi xe đang hiển thị của màn |

### 5a. Spray (Send/Return spawn — y như wheel)
- Spray = Paint CarPart (permanent, đã own sau khi mua ở shop). Inventory `GetOwnedParts(Paint)` → các spray đã sở hữu.
- **Send To Garage** → `GaragePartStaging.Spawn(paint, 1)` spawn **spray model** tại `CartPartPlace > Spray`; **Return** → `Clear`. Không tiêu hao kho (permanent), không gắn lên xe (việc sơn là hệ paint riêng — người chơi cầm spray can để sơn).
- Code cần:
  - **`CarPart`**: thêm `public GameObject sprayPrefab;`.
  - **6 Paint asset**: gán `sprayPrefab` → `sprayCan_{color}` tương ứng.
  - **`GaragePartStaging`**: thêm `sprayAnchor` (auto-find "Spray"); `Spawn` xử lý slot `Paint` → spawn `sprayPrefab`; `AnchorFor(Paint)` → sprayAnchor.
  - **`InventoryUIController`**: thêm map `spray → Paint`; coi Paint là nhóm "staging" (Send/Return) chứ không phải equip. Title spray = partName (không hiện "(N)" vì permanent).

### 5b. Car (Select — đổi xe đang hiển thị)
- Car panel liệt kê **xe đã sở hữu** (`PlayerInventory.ownedLoadouts`). Bấm 1 xe → đặt nó làm **xe đang hiển thị của màn** (active car), để khi gắn ecu/part sau đó áp vào xe đó.
- Đổi xe = `GarageCarManager.SetActiveCar(index)` (set `ActiveLoadout.Current` + activate slot). Map loadout→slot qua `CarLoadoutSlot.loadout`.
- State nút: xe đang active → Purchased ("Selected"); xe khác → Default ("Select"). Bấm Default → select. Sau khi đổi → `onDisplayedCarChanged` → rebuild để cập nhật xe nào đang "Selected".
- Code cần:
  - **`GarageCarManager`**: thêm `public bool SelectByLoadout(PlayerCarLoadout)` (tìm slot có loadout đó → `SetActiveCar`).
  - **`GarageDisplayedCarContext`**: thêm `GetOwnedCars()` (→ `ownedLoadouts`) + `SelectCar(loadout)` (→ `carManager.SelectByLoadout`).
  - **`InventoryUIController`**: xử lý panel `car` riêng (giống ShopUIController: `_carTemplate` + `carPanelName="car"`): duplicate theo `GetOwnedCars()`, title=loadoutName/icon, **ẩn price** (không bán ở inventory); bấm → `SelectCar`; state theo `DisplayedLoadout == loadout`. Refresh khi `onDisplayedCarChanged`.

### 5c. Lưu ý
- Spray "send" hiện model spray can để người chơi sơn (hệ paint riêng), KHÔNG trừ kho và KHÔNG gắn loadout.
- Car select chỉ đổi xe đang hiển thị; E/Q cũng đổi xe (đã gate theo sở hữu) → cùng dùng `SetActiveCar`, nhất quán.
- Inventory car panel chỉ hiện xe đã mua; mua thêm ở shop → xuất hiện ở đây.
