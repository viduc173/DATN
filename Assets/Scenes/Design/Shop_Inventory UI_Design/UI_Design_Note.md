# UI_Design – Note kiểm tra & lưu ý (Shop Button)

> File này bổ sung/chỉnh cho `UI_Design.md` sau khi đọc scene `Assets/Scenes/5_GarageLobby_pc/GarageLobby_pc.unity` và script `ShopButtonManager.cs`.

## 1. Kết quả đối chiếu context

| Mục trong UI_Design.md | Thực tế trong scene/code | Trạng thái |
|---|---|---|
| Menu chính `UI_Main Menu` | Có (`m_Name: UI_Main Menu`) | ✅ Đúng |
| `Part`, `Shop` | Có (`Part`, `Shop`) | ✅ Đúng |
| Chuỗi `Shop > Content > Panel Content > Panels > <loại>` | Các tên `Content`, `Panel Content`, `Panels` đều tồn tại | ✅ Đúng (tên generic của bộ Heat UI) |
| 5 panel: `wheel`, `brake`, `engine`, `suspension`, `ecu` | Đủ cả 5 (chữ thường), mỗi tên xuất hiện 2 lần: 1 nút nav + 1 panel nội dung | ✅ Đúng |
| Chuỗi `wheel > Content > List > Layout Group > "Shop Button"` | Đúng — leaf tên **"Shop Button"** nằm trong `... > List > Layout Group` | ✅ Đúng |
| Tên item template = **"Shop Button"** | Đúng. Có **5 GameObject tên "Shop Button"** mang component `ShopButtonManager` | ✅ Đúng → xem mục 2 |
| Prefab bánh xe ở `Assets/Prefabs/CarPart/Wheel` | Đúng, có 8 prefab | ✅ Đúng |
| Prefab brake "tương tự Wheel" | Brake nằm ở `Assets/Prefabs/CarPart/**BrakeCaliper**`, là **cặp L/R + .mat** | ⚠️ Khác, xem mục 4 |
| engine/suspension/ecu chỉ đổi state khi mua | Không có folder prefab tương ứng dưới `CarPart` → khớp ý "chỉ flip state" | ✅ Đúng |
| Sự kiện `OnPurchase` | Đúng có `onPurchase`, nhưng cơ chế thực tế phức tạp hơn | ⚠️ Xem mục 3 |

## 2. Tên & cấu trúc thật của item template

- Component shop = `Michsky.UI.Heat.ShopButtonManager`
  (script: `Assets/Heat - Complete Modern UI/Scripts/UI Elements/ShopButtonManager.cs`, guid `85d9d6a0868c13243ba8f3abbae7ed13`).
- Trong scene có **10 instance** ShopButtonManager, gồm 2 nhóm tên:
  - **5 cái tên "Shop Button"** → đây là **template chính cần duplicate** (theo đúng path `... > List > Layout Group > Shop Button`). `buttonTitle` hiện còn "Sample Title" → chưa populate dữ liệu thật.
  - **4 cái tên "Item Button"** + **1 cái name rỗng** (`buttonTitle: "Basic Furry"`) là **item mẫu sót lại của bộ Heat UI** → nên xoá/bỏ qua, **đừng** dùng làm gốc duplicate để tránh nhầm.
- ⇒ Khi code (script load wheel/brake) cần **duplicate GameObject tên "Shop Button"** trong `... > List > Layout Group`.

> Lưu ý cho lần đọc sau: scene đang được chỉnh trực tiếp trong Unity nên **số dòng & tên có thể đổi** giữa các phiên — luôn grep lại theo guid `85d9d6a0868c13243ba8f3abbae7ed13` thay vì tin số dòng cũ.

## 3. Lưu ý QUAN TRỌNG về luồng mua (đọc kỹ trước khi gắn event)

`ShopButtonManager` chỉ có **2 state**: `State.Default` và `State.Purchased`
(KHÔNG có state tên `"purchase"` như trong context — giá trị đúng là `State.Purchased`).

Các sự kiện/method liên quan:
- `onPurchaseClick` – fire khi bấm nút "Buy".
- `onPurchase` – fire **sau khi** đã chuyển state sang Purchased.
- `Purchase()` – method có sẵn: **tự `SetState(Purchased)` rồi mới Invoke `onPurchase`** → tức là nó **khoá nút lại**.
- Luồng mặc định (`InitializePurchaseEvents`): nếu `useModalWindow == true` **và** `purchaseModal != null` thì bấm Buy → mở modal → `onConfirm` → `Purchase()`.
  ⚠️ Trong scene hiện tại **`purchaseModal = None`** dù `useModalWindow = 1` → bấm Buy hiện chỉ Invoke `onPurchaseClick` (đang rỗng), **không mở modal nào cả**. Cần gán modal hoặc tự xử lý.

### Hệ quả cho 2 nhóm sản phẩm

**A. engine / suspension / ecu (mua 1 lần, mua xong khoá):**
- Dùng được luồng `Purchase()` có sẵn vì nó tự flip sang `Purchased` và ẩn nút mua.
- Cách làm: wire kiểm tra tiền vào `onPurchaseClick` (hoặc `modal.onConfirm`); nếu đủ tiền → trừ tiền + gọi `Purchase()`. Nếu KHÔNG đủ tiền thì **đừng gọi `Purchase()`** (vì nó sẽ khoá nút sai).

**B. wheel / brake (mua nhiều loại, không khoá nút):**
- ⚠️ **KHÔNG dùng trực tiếp `Purchase()`** vì nó sẽ set `Purchased` và khoá nút → không mua lại / mua loại khác được.
- Thay vào đó: gắn hàm tự viết (check tiền → trừ tiền → thêm vào inventory) vào `onPurchaseClick`, và **giữ nguyên state = Default**.
- Nếu muốn vẫn dùng modal xác nhận: gán `purchaseModal`, nhưng listener `onConfirm` phải trỏ tới hàm custom (không phải `Purchase()`).

## 4. Lưu ý khi load prefab vào shop

**Wheel** – `Assets/Prefabs/CarPart/Wheel` (8 prefab, 1 button / prefab):
`CarWheel_BlackFast`, `CarWheel_Grey`, `CarWheel_Grey_Normal`, `CarWheel_Normal`,
`CarWheel_OffRoad_Good`, `CarWheel_Offroad_Great`, `CarWheel_Offroad_Normal`, `CarWheel_blue`
(+ `WheelGhostMaterial.mat` – bỏ qua, không phải prefab bán).

**Brake** – `Assets/Prefabs/CarPart/BrakeCaliper` (**KHÔNG phải "Brake"**):
- Là **cặp trái/phải**: `Brake_normal_L/R`, `Brake_normal2_L/R`, `Brake_good_L/R`, `Brake_goodPlus_L/R`, `Brake_great_L/R`, `Brake_greatPlus_L/R` + các file `.mat`.
- ⇒ Khi tạo button không nên tạo 1 button/prefab (sẽ bị đôi do L/R). Phải **gom theo loại** (normal, normal2, good, goodPlus, great, greatPlus) → 6 button, mỗi loại tham chiếu cặp L+R.

## 5. Cấu hình mặc định của "Shop Button" cần chỉnh khi gán data động

Trên template "Shop Button" hiện tại:
- `enablePrice = 0` → **giá KHÔNG hiện**. Muốn hiện giá phải set `enablePrice = true`.
- `enableIcon = 1`, `enableTitle = 1`, `enableDescription = 1`, `enableFilter = 1`.
- `useLocalization = 1` + có `titleLocalizationKey/descriptionLocalizationKey` (đang là `SampleShopItemTitle`).
  ⚠️ Khi localization bật, `buttonTitle`/`buttonDescription` **có thể bị ghi đè** theo bảng dịch khi đổi ngôn ngữ. Với item shop sinh động (tên = tên bánh xe), nên **set `useLocalization = false`** (hoặc `useCustomContent = true`) để text không bị reset.
- Title/desc hiện vẫn là `"Sample Title"` / `"Sample description..."` → panel **chưa được populate dữ liệu thật**, đúng với việc phần load-by-script vẫn còn phải làm.

### API tiện dùng khi set data bằng code
- `SetIcon(Sprite)`, `SetText(string)`, `SetPrice(string)` – mỗi hàm tự gọi `UpdateUI()`.
- Set field trực tiếp (`buttonTitle`, `buttonDescription`, `priceText`, `buttonIcon`, `enablePrice = true`) thì **phải gọi `UpdateUI()`** sau đó.
- Khoá / mở: `SetState(ShopButtonManager.State.Purchased)` / `State.Default`.
- Để `purchasedIndicator` + nút "Purchased" hoạt động, 3 field `purchaseButton`, `purchasedButton`, `purchasedIndicator` phải được wire (template "Shop Button" đã wire sẵn; item mẫu "Basic Furry" thì chưa).

## 6. Bổ sung shop: spray + car

Panel hiện có dưới Shop: `suspension, wheel, car, ecu, engine, brake, spray` (+ Background). Cả `spray` và
`car` đều đã có template "Shop Button". Inventory thì CHƯA có panel `spray`/`car`.

### Spray (= Paint CarPart, giống engine/ecu) — gần như miễn phí
- Spray chính là **Paint CarPart**: `Assets/Data/CarParts/Paint/Paint_{Black,Blue,Grey,Orange,Red,White}` (slot = Paint = 8),
  có `costGold`/`icon`, **đã nằm trong** `GaragePartsShop.asset` stock.
- Paint là **permanent slot** (≠ Wheels/Brakes) → `TryBuyPart` mua 1 lần, lưu vào `ownedOtherParts`; `OwnsPart` hoạt động.
- ⇒ Chỉ cần thêm map **`"spray" → Paint`** vào `ShopUIController.panelSlots`. Logic mua/khoá nút y hệt engine/ecu.

### Car (= PlayerCarLoadout — ĐẶC BIỆT)
- Car **không phải CarPart** → KHÔNG dùng ShopCatalog (List<CarPart>) hay ShopUIController được.
- Mỗi car = **`PlayerCarLoadout`** asset (`Assets/Data/Loadouts/Loadout_CarType{0,1,2}`). Hiện loadout
  **CHƯA có** `costGold`/`icon` → phải thêm.
- PlayerInventory **CHƯA có** trường lưu loadout đã sở hữu → thêm **`ownedLoadouts : List<PlayerCarLoadout>`**.
- Cần data nguồn cho car shop: thêm **`carStock : List<PlayerCarLoadout>`** vào `ShopCatalog` (danh sách car đang bán).
- Mua 1 lần (giống engine/ecu): đủ tiền → trừ gold → thêm vào `ownedLoadouts` → khoá nút. Đã sở hữu thì start Purchased.
- ⇒ Car panel cần **controller riêng** (`CarShopController`), không nhét vào `ShopUIController`. (Không thêm "car" vào
  `ShopUIController.panelSlots` → ShopUIController bỏ qua panel car.)
- "Car vào inventory" = vào `ownedLoadouts`. UI inventory hiện **chưa có panel car** để hiển thị → cần tạo panel
  inventory cho car sau (data đã sẵn sàng). Liên hệ tương lai: `GarageCarManager` chỉ cho chọn car đã sở hữu.

---

*Liên quan: brief gốc [UI_Design.md](UI_Design.md) · design tổng [Garage_UI_Design.md](Garage_UI_Design.md) · phần đã code [Shop_UI_Implementation.md](Shop_UI_Implementation.md) · index [README.md](README.md).*
