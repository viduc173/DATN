trong scene Assets/Scenes/5_GarageLobby_pc/GarageLobby_pc.unity
Tôi có UI_Main Menu là menu chính của cả scene này.

Phần shop sẽ là "UI_Main Menu > Part > CarInfo"

Trong Part sẽ bao gồm có:

"UI_Main Menu > Part > CarInfo"

1. Xử lí UI của CarInfo:

CarInfo sẽ hiển thị tất cả thông số của xe, logic xử lí của nó như sau:

"CarInfo > Content > Panel Content > Panels > Car > Content > List > Layout Group > Car" sẽ chứa icon, title và description của car giống y hệt bên Assets\Scenes\Design\Shop_Inventory UI_Design\UI_Design_Inventory.md đx làm, nhưng khác ở đây là chỉ hiển thị ra giao diện xe thôi không hiển thị gì thêm.

"CarInfo > Content > Panel Content > Panels > Car > Info" nó sẽ chứa CarStatsUIManager thằng này chịu trách nhiệm về thông số của xe được chọn hiện tại tăng hay giảm thì cần hiển thị trên thằng này để người dùng biết.

"CarInfo > Content > Panel Content > Part > Panels > Part > Content > List > Layout Group > Item", thằng Item này sẽ coi như là thằng templete sau đó hiển thị ra tất cả các part mà xe hiện tại đang có và gán icon của nó cũng như title và description của nó.

---

## Đối chiếu scene + hướng triển khai (đã rà, ĐỦ dữ kiện)

### 1. Car card — hiển thị xe hiện tại (display-only)

- Template: GO `9015902957605658855` tên **"Car"** (`ShopButtonManager`) tại
  `...CarInfo > Content > Panel Content > Panels > Car > Content > List > Layout Group > Car`.
- Hướng: populate **1 card** = xe đang hiển thị (`context.DisplayedLoadout`): `buttonIcon`=icon, `buttonTitle`=loadoutName, `buttonDescription`=description. Display-only → **ẩn cả `purchaseButton` + `purchasedButton`** (không wire click). Refresh khi `onDisplayedCarChanged`.

### 2. Stats — Info panel ✅ ĐÃ CHẠY (không cần code)

- Panel **"Info"** (GO `2031582416`) đã gắn **`CarStatsUIManager`** với `autoTrackActiveCar = true`:
  tự bám `GarageCarManager.ActiveSlot` (đổi xe → `onCarChanged` → retrack) và refresh khi part đổi
  (qua `ReportPartChangeAnchor` mà WheelStats/BrakeStats gọi), đọc `CarLoadoutSlot.GetEffectiveStats()`.
- ⇒ Stats tăng/giảm hiển thị tự động theo xe đang chọn. Không phải làm gì thêm.

### 3. Part list — các part xe đang lắp

- Template: GO `5676237381230060773` tên **"Achievement Item"** (Heat `AchievementItem`, KHÔNG phải ShopButtonManager) tại
  `...CarInfo > Content > Part > Panels > Part > Content > List > Layout Group > Achievement Item`.
  Fields: `iconObj (Image)`, `titleObj`/`descriptionObj (TMP)`, `lockedIndicator`/`unlockedIndicator`.
- Hướng: duplicate template theo **`DisplayedLoadout.AllParts()`** (engine, suspension, ecu, từng wheel, từng brake);
  set `iconObj.sprite`=icon, `titleObj.text`=partName, `descriptionObj.text`=description. Display-only.
  Refresh khi `onDisplayedCarChanged` + `onPartEquipped` + `onPartUnequipped`.
- ⚠️ Cần chốt: `AllParts()` trả từng wheel/brake riêng (4 wheel → 4 dòng, hay bị trùng loại). Nên **gộp theo loại + hiện số lượng** (vd "Black Racing Tires ×4") cho gọn, hay liệt kê hết? (đề xuất: gộp + đếm).

### 4. ✅ ĐÃ CODE: `CarInfoUIController` (guid `4f0d515eb7660284a824583414e1a974`, comp `101378299` trên CarInfo GO `946762793`)

- Self = root, auto-find `GarageDisplayedCarContext`. `carPanelName="Car"`, `partsPanelName="Part"`.
- **Car card**: dùng template ShopButtonManager dưới panel "Car" **trực tiếp** (1 xe, không clone): set icon/loadoutName/description từ `DisplayedLoadout`, `enablePrice=false`, **ẩn cả purchaseButton + purchasedButton** (display-only). Xe null → ẩn card.
- **Part list**: clone template `AchievementItem` (tên "Item") dưới panel "Part" theo **`DisplayedLoadout.AllParts()`** (cách B — liệt kê HẾT, mỗi wheel/brake 1 dòng kể cả trùng); set `iconObj.sprite`/`titleObj.text`/`descriptionObj.text`; tắt locked/unlocked indicator.
- **Stats**: KHÔNG đụng — `CarStatsUIManager` (panel "Info", autoTrackActiveCar) tự lo.
- Refresh: `onDisplayedCarChanged` + `onPartEquipped` + `onPartUnequipped`. Toàn bộ display-only.
- Template discover bằng **component** (ShopButtonManager / AchievementItem) nên đổi tên node thoải mái.
