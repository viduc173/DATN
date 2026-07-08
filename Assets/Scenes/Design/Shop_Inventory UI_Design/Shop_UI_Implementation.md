# Shop UI — Context triển khai

> Ghi lại toàn bộ những gì đã làm cho UI shop trong `UI_Main Menu > Part > Shop`
> (scene `Assets/Scenes/5_GarageLobby_pc/GarageLobby_pc.unity`).
> Theo design tổng [Garage_UI_Design.md](Garage_UI_Design.md); bám brief [UI_Design.md](UI_Design.md)
> và caveat [UI_Design_Note.md](UI_Design_Note.md). Index: [README.md](README.md).
>
> **Đã code phần Shop Panel + hiển thị vàng.** Inventory Panel / Part Slots Panel / luồng equip
> trong design tổng vẫn chưa làm — xem mục 7.

---

## 1. Tổng quan luồng

```
CarPart assets (Assets/Data/CarParts)        ← nguồn dữ liệu (icon, tên, mô tả, giá, slot)
        │
        ▼
ShopCatalog (GaragePartsShop.asset)          ← danh sách part đang bán
        │  GetStockBySlot(slot)
        ▼
GarageDisplayedCarContext (trong scene)      ← cầu nối: GetShopParts(slot), BuyPart(part)
        │
        ▼
ShopUIController (mới — gắn trên "Shop")      ← duplicate "Shop Button", gán data, nối nút mua
        │
        ▼
"Shop Button" clones trong từng panel          ← UI người chơi nhìn thấy
```

Tư tưởng: **không hard-code vật phẩm trong UI**. Dữ liệu nằm ở `CarPart` → `ShopCatalog`,
UI chỉ đọc và tự sinh nút. Thêm/bớt part chỉ cần sửa `ShopCatalog`, không động vào scene.

---

## 2. Tầng dữ liệu (đã có sẵn từ trước — KHÔNG dựng lại)

| Thành phần | Vị trí | Vai trò |
|---|---|---|
| `CarPart` (ScriptableObject) | `Assets/Script/CarPart.cs` | 1 linh kiện: `partName`, `description`, `icon`, `costGold`, `slot`, prefab |
| Các CarPart asset | `Assets/Data/CarParts/{Tires,Brakes,Engine,Suspension,ECU,...}` | dữ liệu thực tế từng part |
| `ShopCatalog` (SO) | `Assets/Data/Shops/GaragePartsShop.asset` | list 31 part đang bán; `GetStockBySlot(slot)`, `HasStock(part)` |
| `PlayerInventory` (SO) | `Assets/Data/PlayerInventory.asset` | `gold`, `TryBuyPart`, `OwnsPart`, `GetAvailableQuantity` |
| `GarageDisplayedCarContext` | trong scene (GameObject fileID 2311816) | `GetShopParts(slot)`, `BuyPart(part)`, `Inventory`, event `onPartPurchased` |

**Quy ước slot** (enum `CarPart.PartSlot`): Engine=0, Wheels=1, Brakes=2, Suspension=3, Body=4, Aero=5, Other=6, ECU=7, Paint=8.

**Phân loại mua hàng:**
- **Wheels / Brakes** = *quantity slot* → mua tích số lượng (`PartStack`), mua lại được.
- **Engine / Suspension / ECU** = *permanent unlock* → mua 1 lần, sở hữu vĩnh viễn.

`GarageDisplayedCarContext.BuyPart(part)` đã tự: kiểm tra còn bán → `PlayerInventory.TryBuyPart`
(check đủ tiền → trừ `costGold` → add vào inventory) → fire `onPartPurchased`.

---

## 3. Script mới: `Assets/Script/Garage/ShopUIController.cs`

(guid `56f287fd69e94e54480fe3caaf6ca387`)

### Nhiệm vụ
Gắn lên GameObject **"Shop"**, dùng chính nó làm gốc tìm kiếm. Với mỗi panel con:
1. Tìm "Shop Button" có sẵn làm **template**.
2. Ẩn template, **duplicate** 1 clone cho mỗi `CarPart` bán trong slot của panel.
3. Gán cho clone: `icon`, `title = partName`, `description`, `price = costGold`.
4. Nối nút Buy của clone vào luồng mua.

### Auto-discovery (vì sao an toàn)
- Chỉ quét `GetComponentsInChildren<ShopButtonManager>` **dưới "Shop"** → các "Shop Button"/"Item Button"
  ở `Part > Inventory` và `Part > CarInfo` **không bị đụng tới**.
- Xác định panel → slot bằng cách đi ngược cây cha tìm tên panel (so khớp không phân biệt hoa thường):

  | Tên panel (GameObject) | Slot |
  |---|---|
  | `wheel` | Wheels |
  | `brake` | Brakes |
  | `engine` | Engine |
  | `suspension` | Suspension |
  | `ecu` | ECU |

- Template được **cache** sau lần discover đầu → rebuild nhiều lần không nhầm clone thành template.

### Hành vi mua (đúng yêu cầu thiết kế)
- **Wheel / Brake**: bấm Buy → `context.BuyPart(part)`. Nút **giữ trạng thái Default** → mua lại được để tích thêm.
- **Engine / Suspension / ECU**: bấm Buy thành công → `SetState(Purchased)` **khoá nút**, không mua lại.
  Part đã sở hữu từ trước → khởi tạo luôn ở trạng thái `Purchased`.

### Lưu ý kỹ thuật
- Nối vào sự kiện **`onPurchaseClick`** (fire khi bấm nút Buy), KHÔNG dùng `onPurchase`
  (vì `onPurchase` chỉ fire sau `Purchase()` qua modal, mà modal đang null) — xem caveat #3 trong [UI_Design_Note.md](UI_Design_Note.md).
- Set `useLocalization = false` trên clone để tên/mô tả động không bị bảng dịch ghi đè.
- `enablePrice = true` (template gốc đang tắt giá).
- Chỉ chạy ở **Play mode** (không `[ExecuteInEditMode]`). Có ContextMenu **"Rebuild Shop"** để test thủ công trong editor.

### Field cấu hình (Inspector)
| Field | Mặc định | Ý nghĩa |
|---|---|---|
| `context` | (trống → auto-find) | `GarageDisplayedCarContext` |
| `shopRoot` | (trống → = chính nó) | gốc tìm kiếm |
| `panelSlots` | 5 dòng điền sẵn | map tên panel → slot |
| `disableLocalization` | true | tắt localization trên nút sinh ra |
| `rebuildOnEnable` | true | rebuild mỗi lần object enable |
| `purchaseSuccessModal` | (trống) | `ModalWindowManager` mở khi mua **thành công** (`Modal Windows > Purchase Success`) |
| `purchaseFailModal` | (trống) | `ModalWindowManager` mở khi mua **thất bại** (thiếu tiền / hết hàng) |

> Modal kết quả: trong `OnBuyClicked`, `BuyPart` thành công → `purchaseSuccessModal.OpenWindow()`;
> thất bại → `purchaseFailModal.OpenWindow()`. 2 field này **cần gán bằng tay trong Inspector**
> (kéo `Purchase Success` / `Purchase Fail` vào), vì 2 modal mới thêm chưa nằm ổn định trong file scene
> để wire fileID tự động. Modal phải có component `ModalWindowManager` (Heat UI) để `OpenWindow()` chạy.

---

## 4. Đã wire vào scene (sửa trực tiếp `.unity`)

- GameObject **"Shop"**: fileID `101378276`, RectTransform `101378279`.
- Thêm component **ShopUIController** fileID `101378290` vào `m_Component` của "Shop".
- Block MonoBehaviour `&101378290` chèn ngay trước `SceneRoots`: `m_GameObject = 101378276`,
  script guid `56f2...387`, `context`/`shopRoot` = fileID 0 (auto), `panelSlots` = 5 entry.
- 5 template "Shop Button" dưới Shop đều có `purchaseButton` hợp lệ. GameObject fileID:
  - wheel `9015902957090018521`, brake `462287174`, engine `9015902957787347692`,
    suspension `9015902957067882802`, ecu `9015902958460349230`.

Đường dẫn panel trong scene:
```
UI_Main Menu > Part > Shop > Content > Panel Content > Panels
   > {wheel|brake|engine|suspension|ecu} > Content > List > Layout Group > Shop Button
```

---

## 5. Cách dùng / kiểm thử

1. Mở lại scene trong Unity để nạp thay đổi trên đĩa (đừng Ctrl+S đè trước khi nạp).
2. Bấm **Play** → các nút sinh ra trong từng panel shop. (Runtime, vì cần gold/inventory thật.)
3. Mua thử: đủ tiền → trừ vàng + part vào inventory; engine/susp/ecu khoá nút sau khi mua.
4. Đổi danh mục bán: sửa list `stock` trong `Assets/Data/Shops/GaragePartsShop.asset`.

---

## 6. Hiển thị số tiền: `Assets/Script/Garage/PlayerMoneyText.cs`

(guid `c8a1d2e3f4b5460789a0b1c2d3e4f5a6`)

- Gắn lên GameObject **"Text"** tại `Shop > Content > Panel Content > Money > Normal > Text`
  (GO fileID `3009436803463880681`).
- Đọc `PlayerInventory.gold` (qua `GarageDisplayedCarContext.Inventory`) và ghi vào TextMeshPro
  của chính GO đó (`label` = TMP comp `3009436803463878676`).
- Cập nhật khi: enable, có `onPartPurchased`, và poll mỗi frame nếu `gold` đổi (bắt cả thay đổi
  từ nguồn khác như phần thưởng đua). Có ContextMenu "Refresh".
- `format` mặc định `"{0:N0}"` (vd `1,000`). Đổi sang `"{0}"` nếu muốn số trần, hoặc `"$ {0:N0}"`.
- Các refs (`context`/`inventory`) auto-resolve; đã wire `label` + gắn component sẵn trong scene.
- `UIManagerText` trên cùng GO chỉ quản font/màu theo theme → không ghi đè nội dung text.

---

## 8. Bổ sung shop: spray + car

### Spray (= Paint CarPart, giống engine/ecu)
- Thêm map **`"spray" → Paint`** vào `ShopUIController.panelSlots` (script default + serialized trong scene).
- Paint CarPart đã có trong `GaragePartsShop.asset`; permanent purchase + khoá nút dùng lại logic engine/ecu. Không code mới.

### Car (= PlayerCarLoadout — hệ riêng)
- **`PlayerCarLoadout`**: thêm `costGold` (int) + `icon` (Sprite) để bán được.
- **`PlayerInventory`**: thêm `ownedLoadouts : List<PlayerCarLoadout>` + `OwnsLoadout(l)` + `TryBuyLoadout(l)` (check gold → trừ → add) + Save/Load.
- **`ShopCatalog`**: thêm `carStock : List<PlayerCarLoadout>` (danh sách car đang bán).
- **`GarageDisplayedCarContext`**: thêm `GetShopCars()`, `OwnsLoadout(l)`, `BuyLoadout(l)` (fire `onCarBought`).
- **Panel `car` xử lý NGAY TRONG `ShopUIController`** (gộp, KHÔNG dùng controller riêng): `ShopUIController` discover thêm `_carTemplate` (template dưới panel tên `carPanelName="car"`), `BuildCarPanel()` duplicate "Shop Button" theo `GetShopCars()` (loadouts), set title=`loadoutName`/icon/price/description; mua → `BuyLoadout` + khoá nút (Purchased); đã sở hữu → start Purchased. Lý do gộp: `ShopUIController` là component chạy ổn định trong scene; tránh phải wire 1 component riêng (`CarShopController` đã bỏ).
- Set `costGold`/`icon`/`description` cho 3 loadout asset; `carStock` = 3 car; `ownedLoadouts` ban đầu = CarType0.

### Chọn xe theo sở hữu (E/Q)
- `GarageCarManager.NextCar/PrevCar` (phím E/Q) giờ **bỏ qua xe chưa mua**: `NextOwnedIndex` chỉ chuyển sang slot có `loadout` nằm trong `PlayerInventory.ownedLoadouts` (`IsOwned`). Lấy inventory qua `GarageDisplayedCarContext.Inventory`. Không còn xe nào khác đã sở hữu → giữ nguyên.
- **Chưa làm**: panel `car` trong Inventory để hiển thị xe đã mua (data đã vào `ownedLoadouts`).

---

## 9. Dev tool: Reset + Start New Game (test)
`Assets/Script/Editor/GarageResetTools.cs` — menu **Tools ▸ Furia Rush ▸ …**:
- **Reset**: chọn asset `PlayerCarLoadout`/`PlayerInventory` trong Project (chọn nhiều được) rồi chạy. Loadout → `ResetParts()` (xoá part đã lắp, GIỮ tên/icon/giá/mô tả/stats/paint); Inventory → `ResetInventory()` (xoá đồ + xe, GIỮ gold).
- **Start New Game**: reset TẤT CẢ về trạng thái new-game (tự tìm asset, có dialog xác nhận):
  - 3 loadout: `wheels = Tires_Stock ×4`, `brakes = Brakes_Normal ×4`, xoá engine/suspension/ecu/paint.
  - Inventory: xoá hết đồ, `gold = 100`, `ownedLoadouts = [Loadout_CarType0]`.

---

## 7. Chưa làm (theo design tổng [Garage_UI_Design.md](Garage_UI_Design.md))

Phần Shop đã xong; phần còn lại của garage UI mới ở mức thiết kế:

- **Inventory Panel** (Garage_UI_Design §6): list part đã sở hữu theo slot, đọc `GetOwnedParts(slot)`;
  card stat-only → `EquipOwnedPart`, card wheel/brake → hiện quantity rồi chọn socket.
- **Part Slots Panel** (§6): hiện part đang lắp của xe active (Engine, Suspension, ECU, Wheels FL/FR/RL/RR, Brakes FL/FR/RL/RR).
- **Luồng equip/unequip** (§5): `EquipOwnedWheel/Brake(part, socketName)`, `UnequipOwnedWheel/Brake(socketName)`,
  `EquipOwnedPart(part)` — context đã có sẵn các method này, chỉ thiếu UI gọi.
- **Lưu/khôi phục qua phiên (cập nhật):** `PlayerInventory.SaveToPlayerPrefs()` **ĐÃ được gọi** sau mỗi race
  (`RaceResultsController.GrantReward()` cộng gold rồi save). Garage loadout (paint/tires/brakes) + xe đang chọn
  cũng đã persist (`GarageSaveManager` + `ActiveLoadout`). ⚠️ Gap còn lại: `LoadFromPlayerPrefs()` **chưa được
  gọi lúc boot** → gold/đồ đã lưu chưa nạp lại ở phiên mới. Chỉ cần gọi `LoadFromPlayerPrefs(catalog)` trong
  Awake một bootstrap là khép kín.
