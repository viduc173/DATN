> **Đây là brief gốc cho Shop Button.** Design tổng (Shop + Inventory + Equip) xem
> [Garage_UI_Design.md](Garage_UI_Design.md); caveat kỹ thuật ở [UI_Design_Note.md](UI_Design_Note.md);
> phần đã code ở [Shop_UI_Implementation.md](Shop_UI_Implementation.md). Tổng quan: [README.md](README.md).

trong scene Assets/Scenes/5_GarageLobby_pc/GarageLobby_pc.unity
Tôi có UI_Main Menu là menu chính của cả scene này.

Phần shop sẽ là "UI_Main Menu > Part > Inventory"

Trong Part sẽ bao gồm có:

"UI_Main Menu > Part > Inventory"

1. Xử lí UI của inventory:
   Các icon được bán cần hiển thị trong: "Inventory" > "Content" > "Panel Content" > "Panels" > "wheel"

Ví dụ với thằng "wheel" sẽ có "wheel" > "Content" > "List" > "Layout Group" > "Item Show Button", trong này sẽ hiển thị các bánh xe mà người chơi đang có.
Thì thằng Wheel và thằng brake sẽ đặc biệt ở chỗ trong button manager của nó có 2 nút tương ứng là SendToGarage: default và Return To Inventory: purchased. Khi bấm SendToGarage sẽ sinh ra tất cả các wheel tương ứng có trong inventory ra scene, khi đó cần phải lưu trữ lại đã gán bao nhiêu wheel vào xe người chơi, và khi người chơi tháo 1 bánh xe ra cần ghi nhận nó lại vào inventory. Khi bấm ReturnToInventory các bánh xe sẽ được trả về kho đồ. Để triển khai tính năng này cần một hàm lưu trữ nào đó trong scene, tức là cần lưu xem trong garage hiện tại có bao nhiêu bánh xe và brake được lấy ra từ trong inventory.

Lưu ý khi hiện ra một loại bánh xe nào đó trong inventory cần hiển thị thêm (12) tức là trong inventory có 12 chiếc bánh xe loại này, tương tự với các loại khác.

Đối với thằng engine, suspension và ecu thì hơi phức tạp, nó cần kiểm tra xe đang xuất hiện hiện tại. tương lai sẽ có 1 scene ghi lại thời điểm hiện tại xe nào đang xuất hiện và đã gắn linh kiện nào chưa, nếu chưa gắn thì hiện nút chưa gắn, và khi người chơi bấm vào xe đó sẽ ghi nhận là đã gắn linh kiện đó vào trong xe, nhưng khi đổi xe khác mà chưa gắn thì lại ghi nhận là chưa gắn. Đối với 3 thằng đặc biệt thì có thể equip và unequip các part này (mỗi category chỉ được gắn 1 part duy nhất, không có chuyện cùng lúc gắn 2 part suspension vào trong 1 con car).
