# 01 — Màn khởi đầu: `SlashScene_vr`

> Scene: `Assets/Scenes/1_SlashScene/SlashScene_vr.unity`. Vai trò: **màn splash / menu khởi đầu** để bước
> vào hệ thống VR. Đây là scene đầu tiên người chơi thấy khi đeo kính.
>
> Đọc kèm: [04_HaTang_VR_Chung.md](04_HaTang_VR_Chung.md) (XR rig + UI world-space) ·
> [00_TongQuan_HeThong_VR.md](00_TongQuan_HeThong_VR.md) (luồng màn hình).

---

## 1. Vai trò trong luồng

```
[Người chơi đeo kính] → SlashScene_vr (splash + logo + "Press Any Key")
       │  bóp cò / bấm nút bất kỳ
       ▼
SceneChanger.LoadScene("GarageLobby_vr")   → vào Garage
```

Đây là điểm vào duy nhất của game. Không có gameplay; chỉ giới thiệu + chờ người chơi xác nhận để chuyển
sang **GarageLobby_vr** (đã xác thực trong scene: trường `SceneName: GarageLobby_vr`).

---

## 2. Thành phần trong scene

| Nhóm | GameObject / thành phần | Vai trò |
|---|---|---|
| **Rig VR** | `XR Inside Car & Input` (XR Origin) → `Camera Offset` → `Main Camera` + `LeftHand Controller` + `RightHand Controller` (+ model `hands:Lhand`/`hands:Rhand`) | Theo dõi đầu + 2 tay; giống rig chung mọi scene (xem [04](04_HaTang_VR_Chung.md)). |
| **Tia UI** | GO `Ray` (con RightHand) — `XRRayInteractor` + `XRInteractorLineVisual` | Bắn tia để bấm UI splash. |
| **Splash UI** | `Splash Screen`, `Init Screen`, `Game Logo` / `Logo Holder`, `Background`, `Press Any Key` | Màn hình chờ + logo game + dòng "Press Any Key". |
| **Menu** | `UI_Menu` (world-space canvas) + `EventSystem` (`XRUIInputModule`) | Menu UI; dùng module VR để tia bấm được. |
| **Điều hướng** | `SceneChanger` (Heat UI) | Hàm `LoadScene` → load `GarageLobby_vr`. |
| **VR add-on** | `VRMenuToggle`, `VRWorldSpaceMenu` | Bật/tắt + canh menu vừa tầm mắt (xem [04](04_HaTang_VR_Chung.md) §3–4). |
| **Khác** | `Audio Manager`, `Hotkey`/`Hotkeys`, `Shadow`/`Shadows`, `Sphere` (skybox/nền) | Âm thanh, phím tắt, trang trí nền. |

> Skybox dùng material tối `BlackSkybox.mat` (cùng folder) → nền đen làm nổi logo.

---

## 3. Cơ chế "Press Any Key" → vào game

1. Vào scene: rig VR active, splash + logo hiển thị, dòng **"Press Any Key"** nhấp nháy mời người chơi.
2. Người chơi **bóp cò tay** hoặc bấm nút bất kỳ (hoặc dùng tia bấm nút trên `UI_Menu`).
3. Sự kiện gọi `SceneChanger.LoadScene` với tham số scene **`GarageLobby_vr`**.
4. Unity load scene Garage → người chơi xuất hiện trong garage.

> Vì là VR, **mọi UI phải là World Space** — Screen-Space Overlay không render trong kính (xem nguyên tắc
> ở [04](04_HaTang_VR_Chung.md) §2). Splash/logo của scene này được dựng dạng world-space đặt trước mặt.

---

## 4. Cần biết thêm thì xem đâu

- Rig VR + tia + cách UI world-space bấm được: [04_HaTang_VR_Chung.md](04_HaTang_VR_Chung.md).
- Toàn bộ luồng màn hình + scene đích: [00_TongQuan_HeThong_VR.md](00_TongQuan_HeThong_VR.md) §2.
- Màn kế tiếp (Garage): [02_Man_GarageLobby_VR.md](02_Man_GarageLobby_VR.md).

## 5. Hạn chế / ghi chú khi viết đồ án
- Scene splash dùng lại `SceneChanger` của Heat UI (asset UI có sẵn) — không có logic riêng phức tạp,
  chủ yếu là **điểm vào + chuyển cảnh**.
- Có thể trình bày như "màn Main Menu" trong chương kiến trúc, nhấn mạnh: trong VR mọi menu là world-space
  và được bấm bằng tia laser thay cho con trỏ chuột.
