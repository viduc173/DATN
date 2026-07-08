# Inventory UI — Fixes & bổ sung

> Sửa 3 lỗi báo cáo + bổ sung đồng bộ inventory khi lắp/tháo bánh. Tiếp nối
> [Inventory_UI_Implementation.md](Inventory_UI_Implementation.md). Index: [README.md](README.md).

---

## Lỗi 1 — Bấm "Send To Garage" không đổi trạng thái nút

**Nguyên nhân:** template `wheel`/`brake` ("Item Show Button") có `purchasedIndicator = None`.
`ShopButtonManager.UpdateState()` `return` sớm nếu `purchasedIndicator == null` → `SetState()`
không đổi nút (Send ⇄ Return) gì cả. (engine/ecu/suspension có indicator nên không dính.)

**Fix:** `InventoryUIController` tự toggle 2 nút thay vì dựa vào `SetState` — hàm `ApplyState(button, purchased)`:
set `button.state` + bật/tắt `purchaseButton` / `purchasedButton` / `purchasedIndicator` (null-safe).
Áp dụng cho cả wheel/brake lẫn engine/susp/ecu, lúc khởi tạo và sau mỗi hành động.

## Lỗi 2 — Lắp/tháo bánh-brake không cộng/trừ inventory

**Nguyên nhân:** lắp bánh là thao tác vật lý (kéo `WheelItem` vào `WheelSocket`).
Luồng `WheelItem.onAttached → WheelStats.HandleAttached → CarLoadoutSlot.EquipTires` chỉ cập nhật
**loadout**, không hề đụng `PlayerInventory`. Tháo cũng vậy.

**Fix:** thêm `Assets/Script/Garage/PartInventoryBridge.cs` (static):
- `Consume(part)` / `Return(part)`: trừ/cộng `PlayerInventory` (chỉ slot Wheels/Brakes), phát event `Changed`.
- `Suppressed` (bool): bật khi loadout được dựng lại bằng code để KHÔNG đụng kho.

Hook:
- `WheelStats.HandleAttached` → `PartInventoryBridge.Consume(partData)`; `HandleDetached` → `Return`.
- `BrakeStats.HandleAttached` → `Consume`; `HandleDetached` → `Return`.
- `CarLoadoutSlot.SyncSocketsFromLoadout()` được bọc `Suppressed = true` (try/finally) → khi
  **đổi xe / sync** spawn-attach bánh theo loadout sẽ KHÔNG bị trừ/cộng kho nhầm.
- Guard tự nhiên: `PlayerInventory.TryConsumePart` chỉ trừ khi còn quantity > 0 → bánh mặc định/baked
  (không nằm trong kho) tự động bị bỏ qua.

**Đồng bộ UI:** `InventoryUIController` nghe `PartInventoryBridge.Changed` → `Rebuild()` để số `(N)` cập nhật ngay
sau khi lắp/tháo (vì thao tác vật lý không fire event của context).

## Lỗi 3 — Bánh/brake spawn ra không có trọng lực

**Nguyên nhân:** `GaragePartStaging` set `Rigidbody.isKinematic = true` (kinematic → bỏ qua gravity).

**Fix:** đổi field `freezeSpawned` → **`spawnWithGravity`** (mặc định true). Khi spawn:
`rb.isKinematic = !spawnWithGravity; rb.useGravity = spawnWithGravity;` → mặc định bánh rơi theo trọng lực.
Muốn đứng yên thì tắt `spawnWithGravity` trong Inspector (GaragePartStaging trên CartPartPlace).

---

## Lỗi 4 — Brake đếm kho không nhất quán → chọn mô hình "per-caliper"

**Bối cảnh:** 1 brake `CarPart` có `brakePrefabLeft` + `brakePrefabRight` (2 mesh khác nhau, cùng FBX,
mirror theo X). Xe có **4 brake socket**: FL, RL = **Left**; FR, RR = **Right** (`BrakeSocket.Side`).
Cả 2 prefab đều mang `BrakeStats` cùng `partData`.

**Bug ban đầu:** mua +1 nhưng spawn ra 2 caliper, mỗi caliper tự `Consume`/`Return` → tháo L hoặc R đều +1
→ kho lệch ("đẻ" thêm khi tháo, và spawn lặp khi vào lại).

**Mô hình đã chốt (per-caliper — mỗi caliper = 1 đơn vị, giống bánh xe):**
- **Mua brake → +2** (`PlayerInventory.UnitsPerPurchase`): 1 set = 2 caliper. Khớp với 2 caliper spawn ra.
- **`BrakeStats`**: cả L và R đều `Consume` khi lắp, `Return` khi tháo (đối xứng) → bảo toàn:
  mua 2 → lắp 2 (−2) → tháo 2 (+2). Hết "đẻ" kho.
- **`GaragePartStaging`**: spawn đúng `quantity` caliper (xen kẽ L/R), không phải `quantity` đôi.
- Đổ đầy xe (4 phanh) = 4 caliper = mua 2 lần.

**Tùy biến theo socket (chính xác L/R):** caliper khi gắn **tự đổi mesh** theo `socket.Side`
(`BrakeStats.ApplySideMesh`): socket trái → mesh trái, socket phải → mesh phải — lấy mesh từ
`partData.brakePrefabLeft/Right`. ⇒ người chơi cầm caliper nào thả vào socket nào cũng hiển thị đúng bên.
Vị trí/xoay do `BrakeSocket` xử lý sẵn; chỉ swap mesh. **Không sửa prefab.**

- Bánh xe (wheel) KHÔNG đổi: 1 prefab, đếm từng chiếc (4 chiếc/xe).
- ⚠️ Cần test mắt: swap mesh L↔R khi gắn hiển thị đúng (2 mesh là mirror cùng FBX, gốc local nên khớp; nếu lệch thì căn lại).

## Files đã đổi
- `Assets/Script/Garage/PartInventoryBridge.cs` *(mới)* — guid `f0a1b2c3d4e5460788990a1b2c3d4e5f`.
- `Assets/Script/Garage/InventoryUIController.cs` — `ApplyState`, nghe `PartInventoryBridge.Changed`.
- `Assets/Script/Garage/GaragePartStaging.cs` — `spawnWithGravity`.
- `Assets/Script/WheelStats.cs` — Consume/Return.
- `Assets/Script/Brakes/BrakeStats.cs` — Consume/Return per-caliper (đối xứng) + `ApplySideMesh` (swap mesh theo socket side khi gắn).
- `Assets/Script/PlayerInventory.cs` — `UnitsPerPurchase`: mua brake +2 (set 2 caliper).
- `Assets/Script/CarLoadoutSlot.cs` — bọc `SyncSocketsFromLoadout` bằng `Suppressed`.
- Scene: field `GaragePartStaging.freezeSpawned` → `spawnWithGravity: 1`.

## Lưu ý / edge còn lại
- Bánh `Stock`/mặc định không nằm trong kho nên lắp/tháo chúng không đổi số (đúng).
- Nếu một bánh **không-stock** được set làm bánh initial của socket và có trong kho, lần auto-attach lúc
  load (ngoài cửa sổ bootstrap) có thể trừ 1 — hiếm; cần persistence để xử lý triệt để.
- Lắp xong nếu bấm "Return To Inventory" của loại đó, `GaragePartStaging.Clear` có thể destroy luôn bánh
  đã gắn lên xe (vì vẫn track theo reference). Cân nhắc tách trạng thái "đã mount" khi làm persistence.
