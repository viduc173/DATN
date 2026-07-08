> **Đây là brief gốc cho Shop Button.** Design tổng (Shop + Inventory + Equip) xem
> [Garage_UI_Design.md](Garage_UI_Design.md); caveat kỹ thuật ở [UI_Design_Note.md](UI_Design_Note.md);
> phần đã code ở [Shop_UI_Implementation.md](Shop_UI_Implementation.md). Tổng quan: [README.md](README.md).

trong scene Assets/Scenes/5_GarageLobby_pc/GarageLobby_pc.unity
Tôi có UI_Main Menu là menu chính của cả scene này.

Phần shop sẽ là "UI_Main Menu > Part"

Trong Part sẽ bao gồm có:

"UI_Main Menu > Part > Shop"

1. Xử lí UI của shop:
   Các icon được bán cần hiển thị trong: "Shop" > "Content" > "Panel Content" > "Panels" > "wheel"

Ví dụ với thằng "wheel" sẽ có "wheel" > "Content" > "List" > "Layout Group" > "Shop Button", thì wheel sẽ cần load các loại bánh xe từ "Assets/Prefabs/CarPart/Wheel" và duplicate thằng "Shop Button" sau đó gán icon cho script "ShopButtonManager" của thằng "Shop Button", cần gắn cả price và description và title là tên loại bánh xe vào chung. Khi người dùng bấm nút mua hàng nó đã có event OnPurchase thì gán cho hàm nào đó kiểm tra số tiền của người dùng nếu đủ tiền thì trừ tiền người dùng và inventory sẽ có sản phẩm tương ứng.

Tương tự với "UI_Main Menu > Part > Brake"

Còn riêng đối với 3 thằng cá biệt là "engine", "suspension", "ecu" thì khi mua sẽ chuyển trạng thái item state của "shop button" sang "purchase" và không cho mua lại nữa.

---

## Bổ sung: panel "spray" và "car"

**spray** ("UI_Main Menu > Part > Shop > ... > Panels > spray"):
Giống hệt engine/ecu — mua 1 lần, mua xong khoá nút (Purchased), không mua lại. Spray chính là các
**Paint CarPart** (`Assets/Data/CarParts/Paint/*`, slot = Paint) — đã có sẵn trong shop catalog.

**car** ("UI_Main Menu > Part > Shop > ... > Panels > car"):
Cũng mua 1 lần duy nhất (giống engine/ecu). **Nhưng car ĐẶC BIỆT**: mỗi car là một
**`PlayerCarLoadout`** trong `Assets/Data/Loadouts` (không phải CarPart). Sau khi mua, car (loadout)
được thêm vào **inventory** — cần thêm một **trường mới** trong PlayerInventory để lưu các loadout đã sở hữu.
