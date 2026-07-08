# Shop / Inventory UI — Design

Bộ tài liệu thiết kế cho UI mua sắm + kho đồ trong garage
(scene `Assets/Scenes/5_GarageLobby_pc/GarageLobby_pc.unity`, `UI_Main Menu > Part`).

## Thứ tự đọc

> 👉 **Cần tóm tắt nhanh / dán sang chat khác?** Đọc [Shop_Inventory_System_FULL.md](Shop_Inventory_System_FULL.md) — tổng hợp self-contained toàn bộ hệ thống.

| # | File | Nội dung | Trạng thái |
|---|---|---|---|
| 1 | [Garage_UI_Design.md](Garage_UI_Design.md) | **Design tổng** (master): part model, inventory/shop data, luồng mua + equip, layout 3 panel (Part Slots / Inventory / Shop) | Thiết kế |
| 2 | [UI_Design.md](UI_Design.md) | Brief gốc cho **Shop Button** (yêu cầu ban đầu của shop) | Thiết kế |
| 3 | [UI_Design_Note.md](UI_Design_Note.md) | Đối chiếu brief shop với scene/code thật + caveat khi gắn `ShopButtonManager` | Ghi chú kỹ thuật |
| 4 | [Shop_UI_Implementation.md](Shop_UI_Implementation.md) | **Đã triển khai gì**: ShopUIController (tự sinh nút) + PlayerMoneyText (hiện vàng), wiring vào scene | Đã code |
| 5 | [UI_Design_Inventory.md](UI_Design_Inventory.md) | Brief gốc cho **Inventory** (Item Show Button: send/return wheel-brake, equip/unequip engine-susp-ecu) | Thiết kế |
| 6 | [UI_Design_Inventory_Note.md](UI_Design_Inventory_Note.md) | Đối chiếu brief inventory với scene/code + gap (unequip permanent, spawn/clear mô hình, tên template) | Ghi chú kỹ thuật |
| 7 | [Inventory_UI_Implementation.md](Inventory_UI_Implementation.md) | **Đã triển khai gì**: InventoryUIController + GaragePartStaging (spawn/clear ở CartPartPlace) + context.UnequipOwnedPart | Đã code |
| 8 | [Inventory_UI_Fixes.md](Inventory_UI_Fixes.md) | **Fix**: nút đổi state (purchasedIndicator null), lắp/tháo bánh +/- inventory (PartInventoryBridge), spawn có trọng lực, brake per-caliper + swap mesh theo side | Đã code |
| 9 | [UI_Design_CarInfo.md](UI_Design_CarInfo.md) | **CarInfo** (read-only): car card + stats (CarStatsUIManager auto) + part list. Đã code `CarInfoUIController` | Đã code |
| ★ | [Shop_Inventory_System_FULL.md](Shop_Inventory_System_FULL.md) | **Tổng hợp self-contained** toàn bộ hệ thống (data + scripts + scene + behaviour + TODO) — để dán sang chat khác | Đã code |

## Phạm vi

- **Đã làm:** Shop Panel (tự sinh nút bán wheel/brake/engine/susp/ecu/**spray**=Paint + **car**=loadout) + số vàng — xem (4). Inventory Panel (Send/Return spawn-clear mô hình ở CartPartPlace, equip/unequip engine-susp-ecu) — xem (7). Lắp/tháo vật lý +/- inventory (8). E/Q chỉ đổi xe đã mua. Tool **Tools ▸ Furia Rush ▸ Reset**.
- **Chưa code:** Part Slots Panel (part đang lắp của xe active), persistence (lưu qua phiên), panel `car`/`spray` trong Inventory.

> Tài liệu liên quan (ngoài folder này): hệ data linh kiện/mua sắm ở `../CarPart_Shop_Architecture.md`,
> kinh tế/phần thưởng ở `../Economy_Reward_System.md`.
