# 00 — Tổng quan hệ thống VR (Furia Rush VR)

> **Mục đích:** Mô tả CHI TIẾT luồng hệ thống của game đua xe VR **Furia Rush** (Unity 6, XR Interaction
> Toolkit 2.6.4) để làm cơ sở viết quyển đồ án tốt nghiệp. Tài liệu này là **chỉ mục gốc** — đọc file này
> trước, rồi theo các con trỏ sang từng tài liệu con (cùng thư mục `System_Description`) và các tài liệu
> nguồn (`Assets/Scenes/Design`, các README trong scene).
>
> *Lập 2026-06-17. Bản VR (các scene đuôi `_vr`).*

---

## 1. Game là gì

**Furia Rush VR** là game đua xe 3D chơi bằng kính thực tế ảo. Người chơi đứng/ngồi trong **garage ảo**,
**cầm tay (controller)** để tháo–lắp linh kiện xe (bánh, phanh), **bóp cò sơn xe**, dùng **tia laser từ tay**
để bấm menu mua sắm; sau đó chọn một chặng và **tự lái xe** đua với AI, về đích nhận **Gold**, quay lại garage
nâng cấp xe. Vòng lặp:

```
Garage (mua/độ/sơn/chọn xe) → Chọn chặng → Đua (nitro + bonus) → Bảng kết quả + Gold → quay lại Garage
```

Ba trụ hệ thống:
- **Garage / Shop / Inventory:** mua xe + linh kiện, lắp/tháo **vật lý bằng tay VR**, sơn xe, đổi xe.
- **Stats & Loadout:** mỗi xe có **6 chỉ số 0–100** (maxSpeed, acceleration, grip, braking, handling, nitro);
  linh kiện cộng/trừ chỉ số; vào race quy đổi sang physics EVP `VehicleController`.
- **Race:** đếm ngược → đua → xếp hạng theo checkpoint/lap → về đích → bảng kết quả + thưởng. Có bonus tăng
  tốc nhặt trên đường, nitro (cò tay/Shift), spawn bonus theo vòng.

---

## 2. Luồng màn hình VR (Scene Flow)

```
   SlashScene_vr            GarageLobby_vr               3 màn đua VR (cùng hệ thống, khác model)
  (splash / start)   ─▶   (hub: shop/độ/sơn/    ─┬─▶  6_Stadium_Sunny  (Stadium_Sunny_vr)  — màn chuẩn tham chiếu
   "Press Any Key"          chọn xe/chọn chặng)   ├─▶  3_CyberRace       (CyberRace_vr)
        │                         ▲               └─▶  2_Racing_Circuit  (Racing_Circuit_vr)
        │                         │
        └─ SceneChanger ──────────┴──────────── về đích / thoát → quay lại GarageLobby_vr
           load GarageLobby_vr
```

- **SlashScene_vr → GarageLobby_vr:** bấm phím/nút bất kỳ (UI "Press Any Key") → `SceneChanger` (Heat UI)
  load scene `GarageLobby_vr`. (Đã xác thực trong scene: `SceneName: GarageLobby_vr`.)
- **GarageLobby_vr → màn đua:** chọn chặng trên menu world-space (tia laser bấm) → load scene đua tương ứng.
- **Màn đua → GarageLobby_vr:** kết thúc chặng (về đích lap cuối) hoặc bấm Exit trên modal VR
  → quay lại `GarageLobby_vr` (qua `RaceSettings.endSceneName` / `RaceResultsController.ReturnToGarage()`).
- **`6_Stadium_Sunny` là màn đua CHUẨN (reference):** đầy đủ pipeline nhất; 2 màn đua kia phỏng theo nó,
  **chỉ khác model/bối cảnh**, hệ thống giống hệt.

### Thứ tự scene trong Build Settings (thực tế)
Bản PC và bản VR cùng nằm trong Build Settings (`ProjectSettings/EditorBuildSettings.asset`). Các scene VR:
`SlashScene` (chung), `Stadium_Sunny_vr`, `GarageLobby_vr`, `CyberRace_vr`, `Racing_Circuit_vr`.
> ⚠️ Scene `endSceneName` muốn load được PHẢI có trong Build Settings.

---

## 3. Cấu trúc bộ tài liệu này (đọc gì để biết gì)

| File (cùng thư mục) | Nội dung |
|---|---|
| **00_TongQuan_HeThong_VR.md** | (file này) Tổng quan + luồng màn hình + chỉ mục. |
| **01_Man_SlashScene_VR.md** | Màn khởi đầu: splash, "Press Any Key", chuyển sang Garage. |
| **02_Man_GarageLobby_VR.md** | Garage VR: cầm/độ linh kiện, sơn xe, menu shop/inventory bằng tia, chọn xe, chọn chặng. |
| **03_Man_Dua_Xe_VR.md** | 3 màn đua VR: load xe, đếm ngược, xếp hạng/lap, nitro, bonus, bảng kết quả + thưởng, menu VR khi đua. |
| **04_HaTang_VR_Chung.md** | Hạ tầng VR dùng chung mọi scene: XR rig, locomotion, grab, UI world-space (tia + canh tầm mắt + render đè). |
| **05_LoiDuLieu_Stats_KinhTe_AI.md** | Lõi dữ liệu (không phụ thuộc VR/PC): 6 stat → physics, linh kiện + giá, 3 xe, kinh tế/thưởng, AI đối thủ. |

### Con trỏ sang tài liệu NGUỒN (đọc khi cần đào sâu)

| Chủ đề | Tài liệu nguồn |
|---|---|
| Garage VR (rig, grab, sơn, UI) | `5_GarageLobby/README_GarageLobby_VR.md` · `5_GarageLobby/GarageLobby_VR_Systems.md` |
| Garage PC gốc (logic nền) | `5_GarageLobby/GarageLobby_PC_Systems.md` |
| UI VR tái dùng | `Design/VR_WorldSpace_UI_Design.md` · `Design/Convert_3D_UI_to_VR_UI.md` |
| Pipeline màn đua (chuẩn) | `6_Stadium_Sunny/Stadium_CarLoad_Pipeline.md` · `Stadium_Results_Pipeline.md` · `Stadium_SpeedBoost_Pipeline.md` · `Stadium_BonusSpawn_Pipeline.md` · `Stadium_Nitro_Pipeline.md` |
| Hệ xếp hạng/lap/đếm vòng | `3_CyberRace/README.md` |
| Stats / linh kiện / mapping physics | `Design/RaceStats_Architecture.md` · `Design/Loadout_Stats_Balancing.md` · `Design/CarPart_Shop_Architecture.md` |
| Shop / Inventory UI (toàn bộ) | `Design/Shop_Inventory UI_Design/Shop_Inventory_System_FULL.md` |
| Kinh tế & thưởng | `Design/Economy_Reward_System.md` |
| Thiết kế 5 màn đua | `Design/Race_Levels_Design.md` |
| Đối thủ AI | `Design/Enemy_AI_Design.md` |
| Chỉ mục tổng (bản PC) | `Design/00_MASTER_README.md` |

---

## 4. Hai hệ điều khiển song song: PC và VR

Dự án có **hai bản scene song song** cho mỗi màn: bản PC (`*.unity`) và bản VR (`*_vr.unity`). Phần **lõi
gameplay giống hệt nhau** (stats, kinh tế, xếp hạng, thưởng, nitro, bonus — xem [05](05_LoiDuLieu_Stats_KinhTe_AI.md)).
Khác biệt **chỉ ở lớp nhập liệu / tương tác / UI**:

| Hành động | PC | VR |
|---|---|---|
| Di chuyển trong garage | chuột/WASD (`PCCameraController`) | tay cầm: Continuous Move + Car Turn Provider |
| Cầm linh kiện | nhìn + bấm F (`PCInteractorObject`) | chạm tay + bóp grip (`XRDirectInteractor` + `XRGrabInteractable`) |
| Sơn xe | nhìn bình + F | cầm bình + bóp cò (`XRPaintCanActivator`) |
| Bấm menu | chuột | tia laser tay phải (`XRRayInteractor` + `XRUIInputModule` + `TrackedDeviceGraphicRaycaster`) |
| Mở menu | luôn hiện / phím | bấm A/X tay trái (`VRMenuToggle` / `VRRaceMenu`) |
| Lái xe | bàn phím | (lái bằng input đã wire của rig đua) |
| Phanh / Nitro | S/Mũi tên / Shift | tương ứng input VR |

> Script PC (`PCInteractorManager`, `PCHotkeyManager`, `PCCameraController`, `MenuCursorBinder`) vẫn còn
> trong scene VR nhưng **vô hại** (không có chuột/phím trong build VR) — phần lớn đã disable. Chi tiết hạ
> tầng VR: xem [04_HaTang_VR_Chung.md](04_HaTang_VR_Chung.md).

---

## 5. Gợi ý ánh xạ sang chương đồ án

| Chương đồ án | Đọc tài liệu |
|---|---|
| Tổng quan & luồng game VR | File này (§1–§2) |
| Hạ tầng VR (XR Toolkit) | [04_HaTang_VR_Chung.md](04_HaTang_VR_Chung.md) |
| Màn khởi đầu | [01_Man_SlashScene_VR.md](01_Man_SlashScene_VR.md) |
| Hệ garage / shop / độ xe VR | [02_Man_GarageLobby_VR.md](02_Man_GarageLobby_VR.md) |
| Hệ thống đua (race loop) VR | [03_Man_Dua_Xe_VR.md](03_Man_Dua_Xe_VR.md) |
| Kiến trúc chỉ số xe & kinh tế | [05_LoiDuLieu_Stats_KinhTe_AI.md](05_LoiDuLieu_Stats_KinhTe_AI.md) |
| Hạn chế & hướng phát triển | §6 của các file con + §"Hạn chế" trong [05](05_LoiDuLieu_Stats_KinhTe_AI.md) |
