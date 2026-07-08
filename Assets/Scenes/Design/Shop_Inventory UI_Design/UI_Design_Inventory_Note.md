# UI_Design_Inventory – Note kiểm tra & lưu ý

> Đối chiếu brief [UI_Design_Inventory.md](UI_Design_Inventory.md) với scene
> `Assets/Scenes/5_GarageLobby_pc/GarageLobby_pc.unity` + code (`GarageDisplayedCarContext`,
> `CarLoadoutSlot`, `PlayerCarLoadout`, `PlayerInventory`, `ShopButtonManager`).
> Liên quan: [Garage_UI_Design.md](Garage_UI_Design.md) · [Shop_UI_Implementation.md](Shop_UI_Implementation.md) · [README.md](README.md).

## 0. Lỗi nhỏ đầu file brief
3 dòng header đầu `UI_Design_Inventory.md` bị **copy nhầm từ UI_Design.md** ("Đây là brief gốc cho Shop Button"). Đây là brief **Inventory**, không phải Shop Button → nên sửa/bỏ 3 dòng đó.

## 1. Kết quả đối chiếu

| Mục trong brief | Thực tế trong scene/code | Trạng thái |
|---|---|---|
| `UI_Main Menu > Part > Inventory` | Có | ✅ |
| `Inventory > Content > Panel Content > Panels > {wheel\|brake\|engine\|suspension\|ecu}` | Đủ 5 panel | ✅ |
| Chuỗi `wheel > Content > List > Layout Group > "Item Show Button"` | Đúng cho **wheel & brake** | ✅ |
| Template tên **"Item Show Button"** | wheel/brake = "Item Show Button"; **engine/ecu/suspension = "Item Button"** | ⚠️ Khác tên — xem §2 |
| Button có 2 nút SendToGarage(default)/ReturnToInventory(purchased) | `ShopButtonManager` có `purchaseButton`+`purchasedButton` (đã wire) ↔ State.Default/Purchased | ✅ cơ chế có, nhưng phải tự wire — xem §3 |
| Hiển thị số lượng "(12)" | `PlayerInventory.GetAvailableQuantity(part)` | ✅ có API — xem §6 |
| Wheel/Brake: Send/Return = **spawn/clear mô hình thật** ở `CartPartPlace`, KHÔNG gán vào loadout xe | Có prefab trong data (`wheelPrefab`/`brakePrefab*`) + anchor `CartPartPlace>Wheels/Brake`; cần script quản object spawn | ⚠️ Cần code — xem §4/§4b |
| Engine/Susp/ECU: equip + unequip, theo xe đang hiển thị, 1 part/category | Equip ✅, **unequip CHƯA expose** | ⚠️ Gap — xem §5 |

## 2. Tên template không đồng nhất
- `wheel`, `brake` → **"Item Show Button"** (đúng brief).
- `engine`, `ecu`, `suspension` → vẫn là **"Item Button"** (tên cũ).
- ⇒ Script populate inventory nên **discover template bằng component `ShopButtonManager`** trong từng panel (như `ShopUIController` đã làm cho shop) để khỏi phụ thuộc tên. Nếu muốn nhất quán thì đổi 3 cái kia thành "Item Show Button".
- Tất cả 5 template đều có `ShopButtonManager` với `purchaseButton` **và** `purchasedButton` được wire hợp lệ.

## 3. Cơ chế 2 nút = tái dụng state của ShopButtonManager
- `ShopButtonManager`: `State.Default` → hiện `purchaseButton`; `State.Purchased` → hiện `purchasedButton` (+ `purchasedIndicator`). Đổi qua `SetState(...)`.
- Map theo brief:
  - **Wheel/Brake**: Default = "Send To Garage", Purchased = "Return To Inventory". **Đã spawn mô hình loại này ra garage → Purchased; chưa spawn → Default** (state phản ánh "đã hiện ra garage chưa", KHÔNG phải "đã lắp lên xe chưa").
  - **Engine/Susp/ECU**: Default = "Chưa gắn" (click → equip), Purchased = "Đã gắn" (click → unequip). Trạng thái đọc từ `DisplayedLoadout.HasPart(part)`.
- ⚠️ **QUAN TRỌNG**: `ShopButtonManager.InitializePurchaseEvents()` **chỉ** wire `purchaseButton.onClick → onPurchaseClick`. Nút thứ 2 (`purchasedButton` = Return/Unequip) **KHÔNG được wire sẵn** → UI phải tự `purchasedButton.onClick.AddListener(...)`.
- ⚠️ Đừng dùng `Purchase()` built-in (nó tự khoá sang Purchased). Tự gọi `SetState()` sau khi equip/unequip thành công.

## 4. Wheel/Brake — Send/Return = SPAWN/CLEAR mô hình thật (KHÔNG gán vào xe)

> **Đính chính (điểm dễ hiểu sai):** Send/Return **chỉ spawn ra và xoá đi mô hình vật lý** lấy từ
> data của CarPart, tại anchor `CartPartPlace > Wheels`/`Brake`. **KHÔNG** gán/equip thẳng loại bánh-phanh
> đang chọn vào loadout của xe hiện tại. Việc lắp thật lên xe là **một bước riêng** (người chơi thao tác
> vật lý — xem cuối mục này).

- **Send To Garage (ShowInGarage)**: với loại bánh/phanh đang chọn, `Instantiate` **prefab mô hình thật từ data**
  của CarPart (`CarPart.wheelPrefab`; brake dùng `brakePrefabLeft` + `brakePrefabRight`) **dưới anchor
  `CartPartPlace > Wheels` / `Brake`**. Số lượng spawn = số đang có của loại đó (`GetAvailableQuantity`).
  → đổi nút sang `State.Purchased`.
- **Return To Inventory**: **`Destroy`/clear** các mô hình đã spawn của loại đó ở anchor → đổi nút về `State.Default`.
- ⇒ Send/Return **không** đụng tới `loadout.wheels/brakes`, **không** gọi `EquipOwnedWheel/Brake`,
  **không** trừ/cộng số khi spawn-clear theo nghĩa "lắp vào xe". Nó thuần tuý là hiện/ẩn mô hình trong garage.
- "Hàm lưu trữ trong scene đếm bao nhiêu bánh/brake đã lấy ra garage" mà brief nói = **một bộ đếm/registry
  của riêng phần spawn này** (loại nào đang được spawn ra, bao nhiêu cái) — KHÔNG phải `loadout`. Cần 1 script
  quản lý các object đã spawn ở `CartPartPlace` để Return biết xoá cái nào.

**Lắp thật lên xe (bước riêng, không thuộc Send/Return):** sau khi mô hình đã spawn ra garage, người chơi
**kéo/thả** nó vào `WheelSocket` trên xe (hệ `WheelItem` + `XRGrabInteractable`/`PCInteractorObject` đã có).
Lúc attach/detach mới là lúc `WheelStats`/`CarLoadoutSlot` ghi vào loadout và inventory được cộng/trừ — xem
[[session_garage_tire_save_restore]]. (Context có sẵn `EquipOwnedWheel(part, socketName)`/`UnequipOwnedWheel`
nếu sau này muốn lắp bằng UI thay vì kéo-thả.)

## 4b. Anchor spawn: `CartPartPlace > Wheels` / `Brake`
- Trong scene có root **`CartPartPlace`** (GO `1829321517`) với 3 con: `Spray`, **`Wheels`** (GO `1036742925`), **`Brake`** (GO `1976490974`).
- `Wheels` và `Brake` hiện là **Transform RỖNG** (chỉ có Transform, 0 child) → đúng là **anchor/parent để spawn** mô hình bánh/phanh khi bấm Send.
- ⚠️ **Phân biệt 2 chỗ (đừng nhầm):**
  - **`CartPartPlace > Wheels/Brake`** = nơi **Send spawn mô hình ra** (root, KHÔNG nằm trên xe). Đây là đối tượng của Send/Return.
  - **`WheelSocket`** (nằm TRÊN xe, vd "Wheel_FrontLeft") = nơi **lắp thật** vào xe (ghi loadout). Không liên quan trực tiếp tới Send/Return.

## 5. Engine/Suspension/ECU — equip có, unequip thiếu
- Xe đang hiển thị: `context.DisplayedLoadout` / `DisplayedLoadoutSlot` (đọc `GarageCarManager.ActiveSlot`). Đổi xe → event **`onDisplayedCarChanged`** → UI re-evaluate trạng thái.
- Equip: `context.EquipOwnedPart(part)` → `CarLoadoutSlot.EquipPart` → `loadout.EquipPart` (mỗi slot 1 field, tự thay part cũ). ✅ **1 part/category** đã được enforce ở `PlayerCarLoadout`.
- Trạng thái "đã gắn": `DisplayedLoadout.HasPart(part)`. ✅
- ⚠️ **UNEQUIP CHƯA CÓ ở context**: `GarageDisplayedCarContext` chỉ có `UnequipOwnedWheel/Brake`, **không** có `UnequipOwnedPart` cho engine/susp/ecu. `CarLoadoutSlot.UnequipPart(part)` + `PlayerCarLoadout.UnequipPart` có sẵn nhưng **chưa expose** → cần thêm `context.UnequipOwnedPart(part)`.
- "Đổi xe khác mà chưa gắn thì ghi nhận chưa gắn": đúng tự nhiên — mỗi xe 1 loadout riêng, state đọc từ loadout xe đang hiển thị. Chỉ cần refresh `HasPart` khi `onDisplayedCarChanged`.
- Permanent: equip **không tiêu hao** (vẫn own), nhiều xe dùng chung 1 part. Khớp [Garage_UI_Design.md](Garage_UI_Design.md) §2.

## 6. Hiển thị số lượng "(N)"
- `context.Inventory.GetAvailableQuantity(part)` → int. Dùng cho **wheel/brake** (quantity slot). Engine/susp/ecu là unlock (0/1) → thường không cần hiện số.
- Cần cập nhật lại số sau mỗi equip/unequip/mua: poll hoặc nghe `onPartEquipped` / `onPartPurchased` (lưu ý: `UnequipOwnedWheel/Brake` hiện **không fire event** → tự gọi refresh sau khi unequip).

## 7. Tổng kết phần cần code — ✅ ĐÃ TRIỂN KHAI

Đã code đầy đủ — chi tiết ở [Inventory_UI_Implementation.md](Inventory_UI_Implementation.md):

1. ✅ **`InventoryUIController`** populate mỗi panel từ `GetOwnedParts(slot)`, discover template bằng component (khớp cả "Item Show Button"/"Item Button").
2. ✅ Hiển thị `"{tên} (N)"` cho wheel/brake.
3. ✅ Wheel/Brake: Send → `GaragePartStaging.Spawn` (spawn mô hình ở `CartPartPlace`), Return → `Clear`; nút Purchased được wire tay. Registry = `GaragePartStaging` (Dictionary part→objects).
4. ✅ Engine/Susp/ECU: equip/unequip; đã **thêm `context.UnequipOwnedPart`** + event `onPartUnequipped`.
5. ✅ Refresh theo `onDisplayedCarChanged` / `onPartEquipped` / `onPartUnequipped` / `onPartPurchased`.
