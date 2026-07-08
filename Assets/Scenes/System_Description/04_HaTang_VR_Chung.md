# 04 — Hạ tầng VR dùng chung (XR rig + UI world-space)

> Mô tả lớp **hạ tầng VR** dùng chung cho MỌI scene (`SlashScene_vr`, `GarageLobby_vr`, 3 màn đua). Đây là
> phần "biến game 3D thường thành game VR": XR rig, locomotion, cầm vật bằng tay, và UI world-space bấm
> bằng tia laser. Lõi gameplay (stats/kinh tế/xếp hạng) KHÔNG nằm ở đây — xem [05](05_LoiDuLieu_Stats_KinhTe_AI.md).
>
> **Nguồn:** `Design/VR_WorldSpace_UI_Design.md`, `Design/Convert_3D_UI_to_VR_UI.md`,
> `5_GarageLobby/README_GarageLobby_VR.md`, `5_GarageLobby/GarageLobby_VR_Systems.md`.
> Công nghệ: **XR Interaction Toolkit 2.6.4** (Unity 6).

---

## 1. XR Rig (XR Origin) — bộ khung theo dõi đầu + 2 tay

Mọi scene VR dùng cùng một cấu trúc rig (GameObject gốc thường tên `XR Inside Car & Input`):

```
XR Inside Car & Input            (XR Origin + CharacterController + locomotion)
  ├── Input Action Manager       (kích hoạt "XRI Default Input Actions" — bao gồm map UI)
  ├── Continuous Move Provider    (đi lại, moveSpeed ~3)
  ├── Car Turn Provider           (xoay người, turnSpeed ~60)
  ├── Climb Provider
  ├── XR Device Simulator         (disabled — bật để test bằng chuột/phím khi KHÔNG có kính)
  └── Camera Offset
        ├── Main Camera           (tag MainCamera, Tracked Pose Driver, URP, AudioListener) = Camera.main
        ├── LeftHand Controller   (ActionBasedController + XRDirectInteractor)
        └── RightHand Controller  (ActionBasedController + XRDirectInteractor)
              └── Ray             (GameObject CON: XRRayInteractor + LineRenderer + XRInteractorLineVisual)
```

- **Input:** `ActionBasedController` mỗi tay đã wire Position/Rotation/TrackingState/**Select** (cầm) /
  **Activate** (bóp cò) qua asset **XRI Default Input Actions** → cầm và bóp cò sẵn sàng, không cần code.
- **Cầm bằng chạm:** mỗi tay có `XRDirectInteractor` + `SphereCollider` (trigger, bán kính ~0.1) — chạm tay
  vào vật + bóp grip = cầm.
- ⚠️ **1 GameObject chỉ chứa 1 `XRBaseInteractor`.** Vì tay đã có `XRDirectInteractor`, **tia phải đặt ở
  GameObject CON** (`Ray`). Ray ở con tự tìm `ActionBasedController` ở cha qua `GetComponentInParent`.
- **`XRInteractionManager`:** cần có 1 cái trong scene để interactor/interactable tìm thấy nhau (XRI có thể
  tự sinh runtime, nhưng nên thêm tường minh cho tất định).

### Test không cần kính
Bật **XR Device Simulator** (có sẵn trên XR Origin, mặc định disabled) để mô phỏng đầu + 2 tay bằng chuột/phím.

---

## 2. Vì sao UI phải là World Space

Trong VR **không có "màn hình 2D"** → **Screen-Space Overlay canvas KHÔNG hiển thị** trong kính (chỉ thấy
trên monitor). ⇒ Mọi canvas phải đặt **Render Mode = World Space**, là một tấm phẳng đặt trong không gian.
Tin tốt: **nút UGUI thường (`Button`/`Toggle`/`Slider`) không cần gắn thêm gì** — chỉ cần đổi bộ thu raycast
ở cấp Canvas để tia VR "chạm" được.

---

## 3. UI bấm được bằng tia laser — 3 mảnh cấp hệ thống

Để tia tay phải bấm/kéo được UGUI, chỉ cần **3 mảnh** (KHÔNG gắn gì lên từng nút):

```
[Tay VR] XRRayInteractor + LineRenderer + XRInteractorLineVisual   ← bắn + VẼ tia (bóp cò = "click")
        ▼
[EventSystem] XRUIInputModule                                      ← dịch tia → sự kiện UI (hover/click)
        ▼
[Canvas world-space] TrackedDeviceGraphicRaycaster                ← cho tia "trúng" được UGUI
        ▼
[Button/Slider thường]  ← KHÔNG cần component thêm (chỉ cần là Graphic raycastTarget, mặc định đã bật)
```

| Mảnh | Đặt ở đâu |
|---|---|
| `XRRayInteractor` + `LineRenderer` + `XRInteractorLineVisual` | GO `Ray` (con của tay phải) |
| `XRUIInputModule` | `EventSystem` (thay cho `InputSystemUIInputModule`) |
| `TrackedDeviceGraphicRaycaster` | mỗi canvas world-space muốn bấm được |

- Nút theme **Heat UI `ButtonManager`** (implement `IPointerClickHandler`) và **Slider** UGUI bấm/kéo được ngay.
- **Tia luôn hiện kể cả không trỏ vào nút:** đặt **Invalid Color Gradient alpha = 1** (mặc định = 0 nên tia
  biến mất khi trỏ vào khoảng trống). Valid color = màu khi trúng nút.
- Nếu tia hiện mà bấm không ăn: kiểm tra `Input Action Manager` đã bật map **UI**; canvas có
  `TrackedDeviceGraphicRaycaster`; EventSystem đúng là `XRUIInputModule`.

---

## 4. Canh menu "vừa tầm mắt" + bật/tắt bằng nút tay

### 4.1 `VRWorldSpaceMenu` (đặt trên canvas root)
Giữ panel **cách mặt `distance` mét**, **orbit bám theo hướng nhìn**, luôn quay mặt về người chơi, **tự scale
theo khoảng cách** (`constantAngularSize`) để luôn vừa mắt. Có **vùng chết** (`deadzoneAngle` ~8° +
hysteresis `settleAngle` ~2°) → chỉ xoay khi quay đầu đủ nhiều ⇒ mượt, đỡ say VR.

| Field | Ý nghĩa | Mặc định |
|---|---|---|
| `distance` | xa/gần mặt | 1.5 m |
| `baseScale` | to/nhỏ panel (canvas lớn → số nhỏ) | 0.0006 |
| `verticalOffset` | cao/thấp so với mắt | -0.1 |
| `deadzoneAngle` / `orbitSmoothing` | độ "lười" / tốc độ bám | 8° / 4 |
| `followMode` | `OrbitFollow` (bám) hoặc `PlaceOnEnable` (đặt 1 lần) | OrbitFollow |

### 4.2 `VRMenuToggle` (đặt trên object LUÔN ACTIVE, vd EventSystem)
- Bấm **A/X tay trái** → bật/tắt menu + hiện tia (và kéo menu ra trước mặt). `hideOnStart = true` → vào
  scene menu + tia ẩn.
- Trỏ: `menu` = canvas UI, `rayObject` = GO `Ray`, `placer` = `VRWorldSpaceMenu`.
- Đổi nút qua `bindingPath` (vd `{RightHand}/secondaryButton`).
- **Phải đặt trên object luôn active** (không đặt lên chính menu, vì menu tắt thì script tắt theo).

### 4.3 `VRRaceMenu` (biến thể cho màn ĐUA)
Trong scene đua, `UI_Main Menu` là **container** chứa modal (bảng kết quả + modal Exit/Pause) nên **giữ canvas
active** (không tắt cả canvas). `VRRaceMenu` thay `VRMenuToggle`: bấm **A/X tay trái** mở/đóng **modal Exit**
(thay phím Tab của bản PC), và **tự hiện tia khi có modal mở** (kết quả hoặc exit), ẩn tia khi đang lái.
Xem [03_Man_Dua_Xe_VR.md](03_Man_Dua_Xe_VR.md) §7.

---

## 5. Cầm vật bằng tay (bánh / phanh / bình sơn)

- Logic tháo/lắp đã có sẵn trong code (`WheelItem`/`BrakeItem`): **cầm lên = tự tháo** khỏi socket; **thả gần
  socket = tự lắp** (`WheelSocket.CheckForNearbyGrabbedWheel()` hút bánh khi đang cầm tới gần).
- VR chỉ cần **bật `XRGrabInteractable`** trên các prefab vật cầm là chạy. Các nhóm prefab:
  - Xe (nhúng 4 bánh + 4 phanh): `Data/CarParts/Car/CarType_0/1/2.prefab`.
  - Bánh rời: `Prefabs/CarPart/Wheel/CarWheel_*.prefab`.
  - Phanh: `Prefabs/CarPart/BrakeCaliper/Brake_*.prefab`.
  - Bình sơn: `Prefabs/CarPart/Spray/sprayCan_*.prefab`.
- **Cầm = chạm tay + bóp grip.** Muốn cầm xa → bật *Allow Grab Interaction* trên `XRRayInteractor`.
- **Sơn xe:** mỗi `sprayCan_*` có `XRPaintCanActivator` → cầm bình + **bóp cò (Activate)** = `CarPaintCan.ApplyPaint()`.

Chi tiết áp dụng trong garage: [02_Man_GarageLobby_VR.md](02_Man_GarageLobby_VR.md).

---

## 6. UI hiển thị đè vật cản — `VRUIRenderOnTop` + shader `UI/Overlay Always On Top`
Làm UI **vẽ đè lên vật cản 3D** (ZTest Always), không cần camera phụ, **không phá material gốc** (clone +
bật ZTest, có Revert). Shader giữ Queue "Transparent" (không dùng Overlay) → tránh phần tử UI che đen nhau.

---

## 7. Bộ script VR add-on (tái dùng cho mọi scene)

| File | Việc |
|---|---|
| `Assets/Script/VRAddOnScript/VRWorldSpaceMenu.cs` | Đặt/orbit-follow + billboard + scale theo khoảng cách cho world canvas |
| `Assets/Script/VRAddOnScript/VRMenuToggle.cs` | Nút tay bật/tắt UI + tia (mặc định A tay trái) |
| `Assets/Script/VRAddOnScript/XRPaintCanActivator.cs` | Cầm bình sơn + bóp cò → `ApplyPaint` |
| `Assets/Script/VRAddOnScript/VRUIRenderOnTop.cs` | UI vẽ đè vật cản (giữ material gốc) |
| `Assets/Shaders/UI_Overlay.shader` | UI/Default + ZTest Always (Queue Transparent) |
| `Assets/Script/VRAddOnScript/Editor/VRMainMenuConverter.cs` | Editor tool **Tools/VR/Convert Main Menu to VR**: tự dựng toàn bộ rig UI VR cho canvas tên `UI_Main Menu` (idempotent) |

### Tool 1-click: `Tools/VR/Convert Main Menu to VR`
Gói cả quy trình: Canvas → World Space (+DynamicPPU≥3), thêm `TrackedDeviceGraphicRaycaster` +
`VRWorldSpaceMenu` + `VRUIRenderOnTop`; EventSystem → `XRUIInputModule`; thêm GO `Ray` (XRRayInteractor +
LineVisual, invalid alpha=1) vào **mọi** RightHand Controller (scene đua có nhiều rig); thêm shader vào Always
Included Shaders. **Tự nhận loại scene:** scene đua (có `RaceResultsController`) → gắn `VRRaceMenu` thay
`VRMenuToggle`.

---

## 8. Checklist mang UI VR sang scene mới
- [ ] Canvas = World Space (scale ~0.001), `TrackedDeviceGraphicRaycaster` + `VRWorldSpaceMenu` trên canvas root.
- [ ] EventSystem dùng `XRUIInputModule`.
- [ ] 1 tay có GO con `Ray`: `XRRayInteractor` + `XRInteractorLineVisual` (+LineRenderer), bật Enable UI Interaction, invalid alpha=1.
- [ ] `VRMenuToggle` (hoặc `VRRaceMenu` nếu là màn đua) trên object always-active.
- [ ] Có `XRInteractionManager`; `Input Action Manager` bật map UI.
- [ ] (tùy chọn) `VRUIRenderOnTop` + shader trong Always Included Shaders.
- [ ] Tăng nét: Dynamic Pixels Per Unit = 3; (nếu cần) URP Render Scale 1.3–1.5 + MSAA 4x.

## 9. Cạm bẫy hay gặp (để mục "khó khăn & giải pháp" trong đồ án)
- **"A GameObject can only contain one XRBaseInteractor"** → ray phải ở GO con.
- **Tia hiện mà bấm không ăn** → thiếu `TrackedDeviceGraphicRaycaster` / EventSystem chưa phải `XRUIInputModule` / chưa bật map UI.
- **Tia biến mất khi không trỏ nút** → Invalid Color alpha = 0 → set = 1.
- **Phần tử UI che đen nhau** sau render-on-top → giữ shader Queue "Transparent", đừng đổi renderQueue TMP.
- **Cầm đồ không được** → `XRGrabInteractable.m_Enabled` phải = 1, layer trong raycast mask.
- **Sửa file `.unity` ngoài Unity** → RELOAD scene (đừng Ctrl+S đè); fileID đổi mỗi lần re-save → dò anchor theo `m_Name`/guid, đừng hardcode.
