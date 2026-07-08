# Furia Rush — Garage Shop / Inventory System (FULL CONTEXT)

> File tổng hợp self-contained để dán sang chat khác. Tóm tắt toàn bộ thiết kế + code đã làm cho
> hệ Shop/Inventory/Car trong garage. Unity 6, scene: `Assets/Scenes/5_GarageLobby_pc/GarageLobby_pc.unity`.

---

## 0. Tổng quan

UI garage ở `UI_Main Menu > Part` gồm các panel `Shop` và `Inventory`. Mỗi panel có cây:
`... > Content > Panel Content > Panels > {category} > Content > List > Layout Group > <button template>`.

- **Shop panels**: `wheel, brake, engine, suspension, ecu, car, spray` (template tên **"Shop Button"**).
- **Inventory panels**: `wheel, brake, engine, suspension, ecu` (template tên **"Item Show Button"** cho wheel/brake, **"Item Button"** cho engine/ecu/suspension). CHƯA có panel `car`/`spray`.
- Button dùng **`Michsky.UI.Heat.ShopButtonManager`** (guid `85d9d6a0868c13243ba8f3abbae7ed13`): 2 state `Default`/`Purchased`, fields `purchaseButton`/`purchasedButton`/`purchasedIndicator` (ButtonManager/GameObject), event `onPurchaseClick`. ⚠️ `UpdateState()` bail nếu `purchasedIndicator == null` → các controller TỰ toggle state (hàm `ApplyState`, null-safe) thay vì `SetState`. Nút thứ 2 (`purchasedButton`) KHÔNG được Heat tự wire → controller tự `AddListener`.
- Tab điều hướng panel là Heat `PanelManager` (riêng, không liên quan controller).

---

## 1. Data layer (ScriptableObjects + assets)

**`CarPart`** (`Assets/Script/CarPart.cs`, assets ở `Assets/Data/CarParts/{Tires,Brakes,Engine,Suspension,ECU,Paint}`):
`partName, slot (PartSlot), description, icon, statBonus, wheelPrefab, brakePrefabLeft, brakePrefabRight, tier, costGold`.
PartSlot: Engine=0, Wheels=1, Brakes=2, Suspension=3, Body=4, Aero=5, Other=6, ECU=7, **Paint=8**.
- **Spray = Paint CarPart** (`Paint_{Black,Blue,Grey,Orange,Red,White}`, slot=Paint, có costGold/icon).

**`PlayerCarLoadout`** (`Assets/Script/PlayerCarLoadout.cs`, assets `Assets/Data/Loadouts/Loadout_CarType{0,1,2}`):
`loadoutName, carPrefab, icon, costGold, description, baseStats, paint, engine, suspension, ecu, wheels[], brakes[]`.
API: `GetEffectiveStats()` (baseStats + parts), `EquipPart/UnequipPart/HasPart`, **`ResetParts()`** (xoá part lắp, giữ identity/stats/paint).
- **CAR = loadout** (KHÔNG phải CarPart). 3 xe: CarType0 **"Azzurro Scout"** (0g, owned mặc định), CarType1 **"Mezzanotte X"** (2000g), CarType2 **"Furia Bianca"** (5000g). (Đã từng cân nhắc tách `CarDefinition` nhưng BỎ — dùng loadout trực tiếp.)

**`ShopCatalog`** (`Assets/Script/Garage/ShopCatalog.cs`, asset `Assets/Data/Shops/GaragePartsShop.asset`):
`stock : List<CarPart>` (31 part, gồm Paint), `carStock : List<PlayerCarLoadout>` (3 xe). API: `GetStockBySlot(slot)`, `HasStock(part)`.

**`PlayerInventory`** (`Assets/Script/PlayerInventory.cs`, asset `Assets/Data/PlayerInventory.asset`):
`ownedEngines/ownedSuspensions/ownedEcus/ownedOtherParts (List<CarPart>)`, `ownedWheels/ownedBrakes (List<PartStack{part,quantity}>)`, **`ownedLoadouts (List<PlayerCarLoadout>)`**, `gold`.
API: `OwnsPart`, `GetAvailableQuantity`, `GetOwnedBySlot`, `TryBuyPart` (brake mua **+2** qua `UnitsPerPurchase` — 1 set=2 caliper; khác = +1), `TryConsumePart`, `ReturnPart`, `AddGold`, `OwnsLoadout`, `TryBuyLoadout`, **`ResetInventory(resetGoldTo=-1)`**.
- Engine/Suspension/ECU/Paint/Other = permanent (ownedOtherParts cho Paint/Other). Wheels/Brakes = quantity. Cars = ownedLoadouts.
- **Gold ĐƯỢC lưu**: `RaceResultsController.GrantReward()` gọi `inventory.AddGold(prize)` + `inventory.SaveToPlayerPrefs()` sau mỗi race. `SaveToPlayerPrefs()` cũng ghi cả owned parts/cars. ⚠️ **`LoadFromPlayerPrefs()` chưa được gọi lúc khởi động** → dữ liệu đã lưu chưa được nạp lại ở phiên app mới (gap còn lại, xem §5).

---

## 2. Scene MonoBehaviours

**`GarageDisplayedCarContext`** (`Assets/Script/Garage/GarageDisplayedCarContext.cs`; GO `2311816`/comp `2311820`; KHÔNG singleton, dùng FindFirstObjectByType). Bridge UI↔data, đã wire `playerInventory`+`shopCatalog`+`carManager`.
API: `GetShopParts(slot)`, `GetOwnedParts(slot)`, `BuyPart(part)`, `EquipOwnedPart/UnequipOwnedPart(part)`, `EquipOwnedWheel/Brake(part,socketName)`, `UnequipOwnedWheel/Brake(socketName)`, `GetShopCars()`, `OwnsLoadout(l)`, `BuyLoadout(l)`, `Inventory`, `DisplayedLoadout/DisplayedLoadoutSlot`.
Events: `onPartPurchased`, `onPartEquipped`, `onPartUnequipped`, `onCarBought`, `onDisplayedCarChanged`.

**`ShopUIController`** (`Assets/Script/Garage/ShopUIController.cs`, guid `56f287fd69e94e54480fe3caaf6ca387`; gắn trên GO **"Shop"** `101378276`/comp `101378290`). Self = search root.
- `panelSlots` map: wheel→Wheels, brake→Brakes, engine→Engine, suspension→Suspension, ecu→ECU, **spray→Paint**.
- Mỗi panel CarPart: discover "Shop Button" template (qua component, name-agnostic), ẩn + cache, duplicate theo `GetShopParts(slot)`, gán icon/title(partName)/desc/price(costGold). Permanent (engine/susp/ecu/spray) mua xong khoá (`ApplyState(Purchased)`), đã own → start Purchased; wheel/brake mua lại được.
- **Panel `car` xử lý ngay trong ShopUIController** (gộp, đã BỎ CarShopController riêng): discover `_carTemplate` (panel `carPanelName="car"`), `BuildCarPanel()` duplicate theo `GetShopCars()` (loadouts), title=loadoutName/icon/price(costGold)/description; mua → `BuyLoadout` → khoá; đã own → Purchased.
- Modal kết quả: `purchaseSuccessModal`/`purchaseFailModal` (đã wire `Modal Windows > Purchase Success/Fail`, fileID `1262252047`/`922476052`).

**`PlayerMoneyText`** (`Assets/Script/Garage/PlayerMoneyText.cs`, guid `c8a1d2e3f4b5460789a0b1c2d3e4f5a6`; trên GO "Text" `3009436803463880681`/comp `101378295` tại `Shop>Content>Panel Content>Money>Normal>Text`). Hiện `gold` lên TMP; refresh on enable + onPartPurchased + poll; format `"{0:N0}"`. (UIManagerText cùng GO chỉ chỉnh font/màu.)

**`InventoryUIController`** (`Assets/Script/Garage/InventoryUIController.cs`, guid `a9b8c7d6e5f4430c1b2a3948576e5f40`; trên GO **"Inventory"** `1714841121`/comp `101378296`). Self = root.
- Populate mỗi panel từ `GetOwnedParts(slot)`; wheel/brake title = `"{tên} (N)"` (N=GetAvailableQuantity).
- **Wheel/Brake**: nút Default "Send To Garage" → `GaragePartStaging.Spawn` (spawn mô hình thật tại CartPartPlace); nút Purchased "Return" → `Clear`. KHÔNG đụng loadout. Nút Purchased wire tay.
- **Engine/Susp/ECU**: Default "Equip"→`EquipOwnedPart`, Purchased "Unequip"→`UnequipOwnedPart`; state theo `DisplayedLoadout.HasPart`.
- Refresh khi onPartPurchased/Equipped/Unequipped/onDisplayedCarChanged + `PartInventoryBridge.Changed`.

**`GaragePartStaging`** (`Assets/Script/Garage/GaragePartStaging.cs`, guid `e1f2a3b4c5d6470e8f9a0b1c2d3e4f50`; trên **"CartPartPlace"** `1829321517`/comp `101378297`). Spawn/clear mô hình vật lý:
- anchor `wheelAnchor`=`CartPartPlace>Wheels` (`1036742926`), `brakeAnchor`=`CartPartPlace>Brake` (`1976490975`).
- `Spawn(part,count)`: wheel → count × `wheelPrefab`; brake → count caliper xen kẽ L/R. `spawnWithGravity` (mặc định true) set Rigidbody không-kinematic + useGravity.
- `Clear(part)` destroy theo registry `Dictionary<CarPart,List<GameObject>>`.

**`PartInventoryBridge`** (`Assets/Script/Garage/PartInventoryBridge.cs`, static, guid `f0a1b2c3d4e5460788990a1b2c3d4e5f`). Nối attach/detach vật lý ↔ inventory:
- `Consume(part)`/`Return(part)` (chỉ Wheels/Brakes) qua `TryConsumePart`/`ReturnPart`; phát event `Changed`.
- `Suppressed` = true khi `CarLoadoutSlot.SyncSocketsFromLoadout()` chạy (sync/đổi xe) → không trừ/cộng nhầm.
- Gọi từ `WheelStats.HandleAttached/Detached` và `BrakeStats.HandleAttached/Detached`.

**`GarageCarManager`** (`Assets/Script/GarageCarManager.cs`, trên "CarPlace"). Phím **E**=next, **Q**=prev đổi xe.
- `NextCar/PrevCar` → `NextOwnedIndex`/`IsOwned`: **bỏ qua xe chưa mua** (loadout không nằm trong `ownedLoadouts`). Lấy inventory qua context. Không còn xe nào khác đã own → giữ nguyên.
- `SelectByLoadout(loadout)` → `SetActiveCar`: chọn xe theo loadout (dùng cho inventory car-select).

**Inventory spray + car** (trong `InventoryUIController`): Inventory có đủ panel wheel/brake/engine/susp/ecu/**spray**/**car**. 3 nhóm:
- **Staging** (wheel/brake/**spray**=Paint): Send→`GaragePartStaging.Spawn` / Return→`Clear`. Spray: `CarPart.sprayPrefab` (Paint_X→sprayCan_X), anchor `CartPartPlace>Spray`; Send spawn spray-can, KHÔNG trừ kho (permanent), KHÔNG gắn xe.
- **Equip** (engine/susp/ecu): `EquipOwnedPart`/`UnequipOwnedPart`.
- **Select** (car): liệt kê `GetOwnedCars()` (ownedLoadouts, KHÔNG giá), bấm → `context.SelectCar` → `GarageCarManager.SelectByLoadout` → đổi xe đang hiển thị; state Selected = `DisplayedLoadout`; đồng bộ với E/Q qua `onDisplayedCarChanged`.

---

## 3. Luồng lắp xe vật lý (wheel/brake) — quan trọng

- "Send To Garage" (Inventory) chỉ **spawn mô hình** ở CartPartPlace; KHÔNG lắp lên xe.
- Người chơi **kéo-thả** `WheelItem`/`BrakeItem` vào `WheelSocket`/`BrakeSocket` trên xe → `WheelStats`/`BrakeStats.HandleAttached` ghi loadout + `PartInventoryBridge.Consume` (−1 kho). Tháo → `Return` (+1).
- **Brake = per-caliper**: mỗi caliper (L/R) = 1 đơn vị; mua brake +2 (1 set). Cả L và R đều consume/return đối xứng. `BrakeStats.ApplySideMesh(socket.Side)` swap mesh theo bên (socket trái→mesh trái) lấy từ `brakePrefabLeft/Right` — L/R chỉ khác mesh (cùng FBX). Xe có 4 brake socket FL/RL=Left, FR/RR=Right.
- **Wheel = per-chiếc**: 1 prefab, 4 chiếc/xe, mỗi chiếc ±1.

---

## 4. Dev tool: Reset
`Assets/Script/Editor/GarageResetTools.cs` — menu **Tools ▸ Furia Rush ▸ Reset**. Chọn asset PlayerCarLoadout/PlayerInventory trong Project rồi chạy: loadout→`ResetParts` (giữ identity/stats), inventory→`ResetInventory` (giữ gold).

---

## 5. Còn thiếu / TODO
- **Persistence (đã hoạt động phần lớn):**
  - ✅ **Garage loadout vật lý** (paint / tires / brakes per-xe): `GarageSaveManager` lưu + khôi phục qua PlayerPrefs (key `GB_*`, paint/tires keys) — **round-trip đầy đủ qua phiên**.
  - ✅ **Xe đang chọn**: `ActiveLoadout.SavedCarIndex` (PlayerPrefs `ActiveCarIndex`) lưu + đọc lại.
  - ✅ **Gold**: lưu sau race (`RaceResultsController` → `SaveToPlayerPrefs`).
  - ⚠️ **Gap duy nhất**: `PlayerInventory.LoadFromPlayerPrefs()` **chưa được gọi** lúc boot → gold/owned parts/cars đã lưu nhưng chưa nạp lại ở phiên mới. Chỉ cần gọi `LoadFromPlayerPrefs(catalog)` trong Awake của 1 bootstrap là khép kín.
- **Part Slots Panel** (hiện part đang lắp của xe active) chưa làm.
- Loadout chưa gán `carPrefab` → chọn xe vẫn dùng xe có sẵn trong scene, chưa spawn theo loadout.
- Edge: tháo part được restore/sync (không qua mua) có thể +1 kho (do chưa persist) — Suppressed chặn lúc sync, nhưng detach thủ công sau đó vẫn cộng.

---

## 6. Lưu ý làm việc với scene
Scene đang mở + Unity auto-save liên tục → sửa `.unity` từ ngoài hay bị "modified since read"/clobber. Luôn grep lại theo **guid/fileID** (đừng tin số dòng). Vì lý do này, panel `car` được gộp vào `ShopUIController` (component đã chạy ổn) thay vì thêm component mới. Sau khi sửa code/scene phải **reload scene + để Unity compile**.
