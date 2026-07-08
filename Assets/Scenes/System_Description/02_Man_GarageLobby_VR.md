# 02 — Màn Garage: `GarageLobby_vr`

> Scene: `Assets/Scenes/5_GarageLobby/GarageLobby_vr.unity`. Vai trò: **hub trung tâm** của game. Tại đây
> người chơi: mua xe + linh kiện (Shop), xem kho (Inventory), **tháo/lắp linh kiện vật lý bằng tay VR**, sơn
> xe, đổi xe đang dùng, và chọn chặng đua. Đây là màn dày tính năng nhất.
>
> **Nguồn chi tiết:** `5_GarageLobby/README_GarageLobby_VR.md` (rig + grab + UI VR đầy đủ) ·
> `5_GarageLobby/GarageLobby_VR_Systems.md` (kiến trúc chuyển PC→VR) · `5_GarageLobby/GarageLobby_PC_Systems.md`
> (logic nền) · `Design/Shop_Inventory UI_Design/Shop_Inventory_System_FULL.md` (toàn bộ shop/inventory) ·
> `Design/CarPart_Shop_Architecture.md`. Lõi stats/giá/xe: [05](05_LoiDuLieu_Stats_KinhTe_AI.md).
> Hạ tầng VR chung: [04](04_HaTang_VR_Chung.md).

---

## 1. Tổng quan tính năng trong Garage

```
                         GarageLobby_vr
   ┌──────────────┬──────────────┬───────────────┬───────────────┐
   │  SHOP        │  INVENTORY   │  ĐỘ XE VẬT LÝ  │  SƠN XE        │  CHỌN XE / CHỌN CHẶNG
   │ (mua part+xe)│ (kho + gold) │ (lắp/tháo tay) │ (cầm bình+cò)  │  (đổi xe, vào race)
   └──────────────┴──────────────┴───────────────┴───────────────┘
   Menu world-space "UI_Main Menu"        Tương tác tay trực tiếp        GarageCarManager + SceneChanger
   (bấm bằng tia laser)                   (XRGrabInteractable)
```

Hai kiểu tương tác song song:
- **UI menu (tia laser):** mở bảng Shop/Inventory, mua, equip/unequip part data-only (engine/suspension/ECU),
  "Send to Garage", chọn xe, chọn chặng.
- **Tương tác tay trực tiếp (grab):** cầm bánh/phanh lắp lên xe, cầm bình sơn bóp cò.

---

## 2. Rig VR & menu (tóm tắt — chi tiết ở [04](04_HaTang_VR_Chung.md))

- Rig `XR Inside Car & Input` (XR Origin) + Camera Offset + 2 tay; locomotion Continuous Move + Car Turn.
- Tay trái: `XRDirectInteractor` (cầm). Tay phải: thêm GO con `Ray` để bấm UI.
- Menu **`UI_Main Menu`** = World Space; bấm bằng tia (3 mảnh: `XRRayInteractor` + `XRUIInputModule` +
  `TrackedDeviceGraphicRaycaster`). Bật/tắt bằng **A/X tay trái** (`VRMenuToggle`), canh tầm mắt bằng
  `VRWorldSpaceMenu`, render đè vật cản bằng `VRUIRenderOnTop`.
- Panel Chapters (chọn chặng) nằm chung canvas → raycaster cấp canvas phủ luôn.

---

## 3. Cầm & độ linh kiện vật lý bằng tay

### 3.1 Cơ chế grab
- **Cầm = chạm tay vào vật + bóp grip** (`XRDirectInteractor` + `XRGrabInteractable` trên vật).
- `WheelItem` / `BrakeItem` đã code sẵn: **cầm lên → `Detach()`** (tự tháo khỏi socket); **thả gần socket →
  `FindNearestSocket(snapDist)`** → có socket thì `AttachWheel`, không thì rơi tự do.
- `WheelSocket.CheckForNearbyGrabbedWheel()` mỗi frame `OverlapSphere(snapRadius)` + `wheel.IsBeingGrabbed`
  (`= grabInteractable.isSelected`) → khi cầm bánh tới sát socket trống là **tự hút vào**.
- `BrakeItem` theo cùng pattern. **Brake là per-caliper:** mỗi caliper trái/phải là 1 đơn vị, mesh lấy theo
  side từ `brakePrefabLeft/Right`.

### 3.2 Vật cầm sống trong PREFAB (không trong scene)
Xe & phụ tùng là **prefab instance** — grep scene không thấy `WheelItem`/`XRGrabInteractable` trực tiếp.
Sửa interactable phải sửa **prefab** (lan ra mọi scene): xem bảng prefab ở [04](04_HaTang_VR_Chung.md) §5.
Mọi `XRGrabInteractable` (50 cái) đã được bật.

### 3.3 Liên kết grab vật lý ↔ dữ liệu loadout/kho
Khi lắp/tháo bánh-phanh, hệ thống tự cập nhật **dữ liệu**:
```
Lắp bánh lên socket → WheelStats.HandleAttached()
   → socket.GetComponentInParent<CarLoadoutSlot>() (tìm xe)
   → slot.EquipTires(part)  +  PartInventoryBridge.Consume(part)  (-1 kho)
Tháo bánh ra            → WheelStats.HandleDetached()
   → slot.UnequipTires()  +  PartInventoryBridge.Return(part)     (+1 kho)
```
- `PartInventoryBridge` có cờ `Suppressed` (bật khi `CarLoadoutSlot.SyncSocketsFromLoadout()`) để không
  trừ/cộng nhầm kho lúc khôi phục trạng thái lúc Start.

---

## 4. Sơn xe (VR)
- PC: nhìn bình + F. **VR: cầm bình sơn + bóp cò (Activate)** → `XRPaintCanActivator` nghe
  `XRGrabInteractable.activated` → `CarPaintCan.ApplyPaint()`.
- `CarPaintCan` resolve **xe đang active** qua `GarageCarManager`, đổi material body theo bảng màu.
- Có 6 bình màu (`sprayCan_*`): Black/Blue/Grey/Orange/Red/White. "Send To Garage" từ Inventory spawn bình
  (permanent, không trừ kho — sơn là cosmetic).

---

## 5. Shop & Inventory (menu bằng tia)

### 5.1 Lớp dữ liệu (ScriptableObject)
| Asset | Vai trò |
|---|---|
| `CarPart` (`Assets/Data/CarParts/...`) | 1 linh kiện: slot (Engine/Wheels/Brakes/Suspension/Body/Aero/Other/ECU/**Paint**), `statBonus`, `costGold`, `tier`, prefab tham chiếu. |
| `PlayerCarLoadout` (`Assets/Data/Loadouts/`) | "Xe + part đang lắp" của 1 xe: `baseStats` + Σ parts = `GetEffectiveStats()`; `paint`, `engine`, `suspension`, `ecu`, `wheels[]`, `brakes[]`. 3 xe (xem [05](05_LoiDuLieu_Stats_KinhTe_AI.md)). |
| `ShopCatalog` (`Assets/Data/Shops/GaragePartsShop.asset`) | Danh mục bán: ~31 part + 3 xe. `GetStockBySlot`, `HasStock`. |
| `PlayerInventory` (`Assets/Data/PlayerInventory.asset`) | Kho + ví **gold**. Wheels/Brakes lưu dạng `PartStack{part, quantity}`; engine/susp/ecu/other dạng List; `ownedLoadouts` (xe đã mua). API: `TryBuyPart`, `TryConsumePart`, `ReturnPart`, `AddGold`, `TryBuyLoadout`. |

### 5.2 Các MonoBehaviour scene
| Script | Vai trò |
|---|---|
| `GarageDisplayedCarContext` | **Bridge UI ↔ data.** `GetShopParts/GetOwnedParts(slot)`, `BuyPart`, `EquipOwnedPart/Unequip`, `BuyLoadout`; bắn event `onPartPurchased`/`onPartEquipped`/`onCarBought`/`onDisplayedCarChanged`. |
| `ShopUIController` (GO "Shop") | Tự sinh nút từ template "Shop Button" theo `GetShopParts(slot)`; panel: wheel/brake/engine/suspension/ecu/spray/**car**. Modal mua thành công/thất bại. |
| `InventoryUIController` (GO "Inventory") | Liệt kê đồ sở hữu. Wheel/Brake/Spray: "Send To Garage" → `GaragePartStaging.Spawn`. Engine/Susp/ECU: "Equip"/"Unequip" (data-only). Car: chọn → `GarageCarManager.SelectByLoadout`. |
| `GaragePartStaging` (GO "CartPartPlace") | Spawn/clear mô hình part tại anchor (wheelAnchor = CartPartPlace>Wheels, brakeAnchor = CartPartPlace>Brake). |
| `PartInventoryBridge` (static) | Nối attach/detach ↔ kho (`Consume`/`Return`). |
| `PlayerMoneyText` | Hiển thị gold lên TMP (format `{0:N0}`), refresh on enable + `onPartPurchased`. |
| `GarageCarManager` (GO "CarPlace") | Đổi xe (E=next/Q=prev ở PC; ở VR chọn từ inventory), bỏ qua xe chưa mua; `SetActiveCar` ghi `ActiveLoadout`. |
| `GarageSaveManager` | Lưu/khôi phục garage (paint/tires/brakes per-xe) qua PlayerPrefs key `GB_{carName}_{socketName}`. |

### 5.3 Luồng mua → lắp (ví dụ bánh xe)
```
1. Mở menu (A/X) → tab Shop → panel "wheel" → bấm "Mua" (tia)
       → GarageDisplayedCarContext.BuyPart → PlayerInventory.TryBuyPart (trừ gold)
2. Tab Inventory → panel "wheel" → "Send To Garage"
       → GaragePartStaging.Spawn → 4 mô hình bánh xuất hiện ở CartPartPlace
3. Cầm bánh bằng tay → đưa tới WheelSocket trên xe → thả → tự lắp
       → WheelStats.HandleAttached → EquipTires + Consume(-1 kho)
4. Vào race → LevelController/Level+stats đọc loadout → quy đổi physics (xem [05] + [03])
```

---

## 6. Chọn xe & vào chặng đua
- **Đổi xe:** chọn xe từ Inventory (car panel) → `GarageCarManager.SelectByLoadout` → đổi xe hiển thị +
  ghi `ActiveLoadout.SavedCarIndex` (PlayerPrefs `ActiveCarIndex`) + `ActiveLoadout.Current` (static).
- **Vào chặng:** panel Chapters trên menu → bấm chặng (tia) → `SceneChanger` load scene đua VR tương ứng
  (`Stadium_Sunny_vr` / `CyberRace_vr` / `Racing_Circuit_vr`).
- Xe đang chọn được màn đua đọc lại để load đúng xe + áp đúng stats + màu sơn (xem [03](03_Man_Dua_Xe_VR.md) §2).

---

## 7. Lưu trữ (persistence) — trạng thái thực tế
- ✅ **Loadout vật lý** (paint/tires/brakes mỗi xe): `GarageSaveManager` lưu + khôi phục qua PlayerPrefs → giữ qua phiên app.
- ✅ **Xe đang chọn:** `ActiveLoadout.SavedCarIndex` (PlayerPrefs).
- ✅ **Gold:** `RaceResultsController.GrantReward()` gọi `AddGold + SaveToPlayerPrefs` sau mỗi race.
- ⚠️ **Gap duy nhất:** `PlayerInventory.LoadFromPlayerPrefs()` **chưa được gọi lúc boot** → gold/đồ/xe đã lưu
  chưa được nạp lại ở phiên app mới. Chỉ cần gọi `LoadFromPlayerPrefs(catalog)` trong `Awake` một bootstrap là khép kín.

---

## 8. Dev tool
`Tools ▸ Furia Rush ▸ Reset` (`Assets/Script/Editor/GarageResetTools.cs`): reset loadout (giữ identity/stats)
hoặc inventory (giữ gold).

---

## 9. Hạn chế / hướng phát triển (cho đồ án)
- Chưa có **Part Slots Panel** (hiển thị part đang lắp của xe active).
- Loadout `carPrefab` chưa gán → chọn xe vẫn dùng xe có sẵn trong scene, chưa spawn từ loadout.
- Persistence còn gap `LoadFromPlayerPrefs` lúc boot (§7).
- Cầm xa (grab bằng tia) hiện tắt — chỉ cầm-bằng-chạm; có thể bật *Allow Grab Interaction* để mở rộng.

## 10. Cần biết thêm thì xem đâu
- Toàn bộ shop/inventory (data + controller + UI tree): `Design/Shop_Inventory UI_Design/Shop_Inventory_System_FULL.md`.
- Rig VR + tia + grab + sơn (đầy đủ, kèm GUID/fileID): `5_GarageLobby/README_GarageLobby_VR.md`.
- Cách chuyển PC→VR (hiện trạng + checklist): `5_GarageLobby/GarageLobby_VR_Systems.md`.
- Logic nền PC (attach/detach, paint, brake save/load): `5_GarageLobby/GarageLobby_PC_Systems.md`.
- Stats/giá/3 xe/mapping physics: [05_LoiDuLieu_Stats_KinhTe_AI.md](05_LoiDuLieu_Stats_KinhTe_AI.md).
