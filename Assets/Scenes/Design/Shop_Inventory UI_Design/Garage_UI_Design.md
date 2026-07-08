# Garage UI - Design Doc

**Scene:** `Assets/Scenes/5_GarageLobby_pc/GarageLobby_pc.unity`  
**Data source:** loadout la source of truth cho xe dang hien thi. Inventory chi luu part da so huu, shop chi luu part dang ban.

---

## 1. Part Model

`CarPart.PartSlot` dang dung cho garage:

| Slot | Visual | Ownership | Equip behavior |
|---|---|---|---|
| Wheels | Co mesh tai `WheelSocket` | Co quantity. Mua them thi tang so luong | UI chon socket/vi tri roi equip. Equip se tru 1 quantity |
| Brakes | Co prefab tai `BrakeSocket` | Co quantity. Mua them thi tang so luong | UI chon socket/vi tri roi equip. Equip se tru 1 quantity |
| Engine | Stat-only | Unlock vinh vien, mua 1 lan moi CarPart | Nam trong inventory va co the equip vao nhieu xe |
| Suspension | Stat-only | Unlock vinh vien, mua 1 lan moi CarPart | Nam trong inventory va co the equip vao nhieu xe |
| ECU | Stat-only | Unlock vinh vien, mua 1 lan moi CarPart | Nam trong inventory va co the equip vao nhieu xe |

Paint van la flow rieng qua material/loadout paint. Aero/Body de sau, khong dua vao shop phase nay.

---

## 2. Inventory Data

`Assets/Data/PlayerInventory.asset` khong dung list phang lam UI source nua. Asset chia theo slot:

- `ownedEngines`
- `ownedSuspensions`
- `ownedEcus`
- `ownedWheels` (`part + quantity`)
- `ownedBrakes` (`part + quantity`)
- `ownedOtherParts`

`ownedParts` cu duoc giu hidden de migrate asset cu. UI nen goi:

- `PlayerInventory.GetOwnedBySlot(slot)`
- `PlayerInventory.OwnsPart(part)`
- `PlayerInventory.TryBuyPart(part)`

Quy tac moi:

- `Engine`, `Suspension`, `ECU`: moi `CarPart` chi mua/unlock 1 lan. Khi da unlock, part do co the equip vao bat ky xe nao, nhieu xe co the dung chung mot part.
- `Wheels`, `Brakes`: moi lan mua tang `quantity` them 1. Khi equip vao xe thi inventory tru 1. Khi thao ra thi inventory cong lai 1.
- Loadout cua tung xe la noi luu part dang lap o xe do. Inventory chi luu part con available.

---

## 3. Shop Data

Shop la ScriptableObject:

`Assets/Script/Garage/ShopCatalog.cs`

Asset hien tai:

`Assets/Data/Shops/GaragePartsShop.asset`

Field:

- `shopName`
- `stock: List<CarPart>`

Gia lay truc tiep tu `CarPart.costGold`, tier lay tu `CarPart.tier`. UI shop chi hien cac part co trong `stock`.

Shop phase nay ban:

- Wheels
- Brakes
- Engine
- Suspension
- ECU

---

## 4. Current Displayed Car

UI khong tu tim loadout bang string. Dung bridge:

`Assets/Script/Garage/GarageDisplayedCarContext.cs`

Script nay doc xe dang hien thi tu `GarageCarManager.Instance.ActiveSlot`, roi expose:

- `DisplayedCarSlot`
- `DisplayedLoadoutSlot`
- `DisplayedLoadout`
- `GetOwnedParts(slot)`
- `GetShopParts(slot)`
- `BuyPart(part)`
- `EquipOwnedPart(part)`
- `EquipOwnedWheel(part, wheelSocketName)`
- `EquipOwnedBrake(part, brakeSocketName)`
- `UnequipOwnedWheel(wheelSocketName)`
- `UnequipOwnedBrake(brakeSocketName)`

Gan script nay vao object quan ly UI garage. Inspector can reference:

- `PlayerInventory.asset`
- `GaragePartsShop.asset`
- `GarageCarManager` tren `CarPlace` neu khong muon auto resolve singleton

---

## 5. Purchase / Equip Flow

### Engine / Suspension / ECU

1. Player click part trong shop.
2. UI goi `GarageDisplayedCarContext.BuyPart(part)`.
3. `PlayerInventory.TryBuyPart(part)` tru gold va them vao list unlock dung slot.
4. Part xuat hien trong inventory.
5. Player chon part trong inventory de lap vao xe dang hien thi: UI goi `EquipOwnedPart(part)`.
6. `CarLoadoutSlot.EquipPart(part)` update loadout cua xe dang hien thi.
7. Stats UI refresh qua event/stats changed.

Ket qua: part stat-only mua 1 lan, sau do co the gan cho nhieu xe khac nhau.

### Wheels / Brakes

1. Player click mua trong shop.
2. UI goi `BuyPart(part)`.
3. Inventory tang quantity cua part do len 1.
4. Player chon vi tri tren UI/socket.
5. UI goi:
   - `EquipOwnedWheel(part, wheelSocketName)`
   - hoac `EquipOwnedBrake(part, brakeSocketName)`
6. Context tru 1 quantity cua part moi. Neu socket dang co part cu thi part cu duoc return ve inventory.
7. `CarLoadoutSlot` rebuild loadout theo socket; `SyncSocketsFromLoadout()` render lai visual.

Khi thao ra:

- UI goi `UnequipOwnedWheel(wheelSocketName)` hoac `UnequipOwnedBrake(brakeSocketName)`.
- Part dang lap duoc remove khoi loadout va return ve inventory quantity +1.

---

## 6. UI Layout

Main garage UI gom 3 panel:

- **Part Slots Panel:** hien part dang lap cua xe active theo Engine, Suspension, ECU, Wheels FL/FR/RL/RR, Brakes FL/FR/RL/RR.
- **Inventory Panel:** filter theo slot dang chon, doc tu `PlayerInventory.GetOwnedBySlot`.
- **Shop Panel:** filter theo slot dang chon, doc tu `ShopCatalog.GetStockBySlot`.

Nut/click mapping:

- Shop card: `BuyPart(part)`
- Inventory card stat-only: `EquipOwnedPart(part)`
- Inventory card wheel/brake: show quantity available; chon socket truoc, sau do goi method socket-specific.
- Unequip button tren socket row: goi `UnequipOwnedWheel` / `UnequipOwnedBrake`.

---

## 7. Implementation Notes

- Loadout cua xe active van nam trong `CarLoadoutSlot.loadout`.
- `GarageCarManager` doi xe thi context doc `ActiveSlot` moi, UI refresh danh sach dang lap.
- Khong init default wheel/brake/engine tu shop hay inventory. Neu loadout rong thi xe rong.
- Engine/Suspension/ECU khong co prefab, chi cong `CarStats` qua `PlayerCarLoadout.GetEffectiveStats()`.
- Wheel/brake visual chi spawn tu loadout khi `GarageSaveManager.SyncCar` / `CarLoadoutSlot.SyncSocketsFromLoadout()` chay.
