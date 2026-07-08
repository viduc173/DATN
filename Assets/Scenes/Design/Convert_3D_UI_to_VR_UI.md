# Hướng dẫn: Chuyển UI game 3D thường → UI dùng được trong VR

Quy trình **tái dùng cho mọi scene**: biến một UI UGUI (Screen Space hoặc World Space của bản 3D/PC)
thành UI **bấm được bằng tia VR**, **vừa tầm mắt + bám theo người chơi**, **bật bằng nút tay**, và
(tùy chọn) **hiển thị đè vật cản**. Lần đầu áp dụng: `GarageLobby_vr.unity` (xem `README_GarageLobby_VR.md`).

Scripts dùng chung nằm ở `Assets/Script/VRAddOnScript/` + shader `Assets/Shaders/UI_Overlay.shader`.
Có thể copy serialization rig chuẩn từ scene tham chiếu `Assets/Scenes/0_BackUpScene/1 Start Scene.unity`.

---

## 0. Điều kiện cần (rig VR phải có sẵn trong scene)
- **XR Origin** (XR Interaction Toolkit) + locomotion (Continuous Move / Turn Provider) nếu cần đi lại.
- **Main Camera** dưới Camera Offset (tag `MainCamera`), có Tracked Pose Driver.
- 2 **Controller** với `ActionBasedController` (Position/Rotation/**Select**/**Activate** đã wire — XRI Default Input Actions).
- `XRInteractionManager` **tự sinh lúc runtime** nếu thiếu (vẫn nên thêm 1 cái cho tất định).

---

## 1. Đổi Canvas sang World Space
- Canvas `Render Mode`: **World Space** (Screen Space-Overlay KHÔNG hiển thị trong VR).
- World-space canvas thường để `localScale` ~ **0.0005–0.002** (canvas sizeDelta lớn → scale nhỏ).
- Vị trí: đặt trước mặt người chơi (sẽ để script lo, xem bước 6). `Event Camera` = Main Camera (cho chắc).

## 2. EventSystem → XRUIInputModule
- Trên **EventSystem**: gỡ `StandaloneInputModule`/`InputSystemUIInputModule`, **Add `XRUIInputModule`**.
- Module này vẫn xử lý chuột (PC test được) lẫn tia XR. Không cần kéo thả gì (tự gom mọi XRRayInteractor có *Enable UI Interaction*).

## 3. Canvas → thêm TrackedDeviceGraphicRaycaster
- Trên canvas World Space: **Add `TrackedDeviceGraphicRaycaster`** (giữ `GraphicRaycaster` cũ cho chuột PC).
- Đây là thứ DUY NHẤT cần cho UGUI bắt được tia. **KHÔNG cần gắn gì lên từng nút.**

## 4. Tạo TIA (ray) render được trên tay
> ⚠️ 1 GameObject chỉ chứa **1** `XRBaseInteractor`. Nếu controller đã có `XRDirectInteractor` (cầm đồ),
> **ĐỪNG** add ray thẳng vào controller — tạo **GameObject CON** rồi add ray vào con đó (nó tự tìm
> `ActionBasedController` ở cha qua `GetComponentInParent<XRBaseController>`).

1. Chuột phải controller (vd RightHand) → Create Empty → tên `Ray` → **Reset** Transform.
2. Add `XRRayInteractor`: bật **Enable Interaction with UI GameObjects** ✅; `Line Type` = Straight; `Max Raycast Distance` ~30. (Tùy chọn: `Raycast Mask` chỉ layer UI để tia không cầm nhầm đồ.)
3. Add `XRInteractorLineVisual` (tự kéo theo `LineRenderer`) — đây là phần **render tia nhìn thấy**.
   - `Line Renderer` material = **Default-Line** (`fileID 10306, guid 0000…f000…`).
   - **Tia luôn hiện kể cả không trỏ vào nút:** đặt **Invalid Color Gradient → alpha key0 = 1** (mặc định = 0 nên tia biến mất khi trỏ chỗ trống). Valid = màu khi trúng nút.

## 5. Nút / Slider — KHÔNG gắn gì lên từng cái
- `Button`/`Toggle`/`Slider` UGUI và nút theme (vd Heat UI `ButtonManager`, implement `IPointerClickHandler`) **bấm/kéo được ngay** nhờ raycaster + module + tia. Không cần component phụ.
- Panel con (Tab, Chapters…) nằm CHUNG canvas → raycaster cấp canvas phủ luôn, không cần thêm.
- *Thay thế (nút khối 3D):* dùng `VirtualButton` + collider trên object, `VRButtonInteractor` trên tay (Physics.Raycast). Đừng trộn 2 hệ trên cùng 1 nút.

## 6. Vừa tầm mắt + xoay vòng bám theo — `VRWorldSpaceMenu`
- Add lên **canvas root**. Giữ panel cách mặt `distance`, **orbit bám hướng nhìn**, luôn quay về người chơi, tự scale theo khoảng cách.
- Knobs: `distance` (1.2–2.0), `baseScale` (to/nhỏ, canvas lớn → số nhỏ ~0.0006), `verticalOffset` (-0.1), `followMode` (OrbitFollow / PlaceOnEnable), `deadzoneAngle`/`orbitSmoothing` (độ “lười”/tốc độ bám).

## 7. Bật/tắt menu bằng nút tay — `VRMenuToggle`
- Add lên 1 object **LUÔN ACTIVE** (vd EventSystem / XR Origin) — KHÔNG lên chính menu (vì menu tắt thì script tắt theo).
- Trỏ: `menu` = canvas UI, `rayObject` = GO `Ray` (để mở menu là hiện luôn tia), `placer` = `VRWorldSpaceMenu`.
- `hideOnStart` = true → vào game ẩn, bấm nút hiện. `bindingPath` mặc định `<XRController>{LeftHand}/primaryButton` (nút A/X trái); đổi sang `{RightHand}`/`secondaryButton` nếu muốn.

## 8. (Tùy chọn) Hiển thị đè vật cản — `VRUIRenderOnTop` + shader `UI/Overlay Always On Top`
- Add `VRUIRenderOnTop` lên canvas (applyOnStart). UI vẽ đè vật cản 3D (ZTest Always), **không phá material gốc**:
  Image `UI/Default`→overlay (giống hệt), material tùy biến→clone+ZTest, TMP→clone fontMaterial set `_ZTestMode=Always`; có **Revert**.
- ⚠️ Shader để **Queue "Transparent"** (KHÔNG Overlay) và **đừng đổi renderQueue TMP** → thứ tự vẽ nội bộ UI theo hierarchy, **tránh phần tử che đen nhau**.
- Build: thêm shader vào **Project Settings → Graphics → Always Included Shaders** (tránh bị strip).

## 9. Nâng độ phân giải UI (cho rõ nét, giữ nguyên tỷ lệ)
1. **CanvasScaler → Dynamic Pixels Per Unit** 1 → **3** (an toàn nhất, đánh trúng chữ/đồ hoạ mờ; không đổi layout).
2. **Atlas font TMP**: regen ở atlas lớn (2048²) + sampling point size lớn / Dynamic SDF → chữ TMP cực nét.
3. **URP Render Scale** 1 → 1.3–1.5 + **MSAA 4x** trong RP asset → nét toàn bộ VR (ảnh hưởng cả game, tốn GPU).
4. **Mật độ điểm ảnh kép**: nhân `sizeDelta` ×2 **và** `baseScale` ÷2 (giữ kích thước thế giới + tỷ lệ).
5. Sprite/icon mờ → tăng Max Size, tắt mipmap cho UI sprite.

---

## 🔘 Tự động hoá (1-click) — `Tools/VR/Convert Main Menu to VR`
Bước 1–8 cho **`UI_Main Menu`** đã được gói vào 1 menu Editor:
`Assets/Script/VRAddOnScript/Editor/VRMainMenuConverter.cs`.
- Mở scene VR cần chuyển (canvas tên đúng **`UI_Main Menu`**) → menu **Tools/VR/Convert Main Menu to VR** → **Ctrl+S**.
- Nó tự: Canvas→World Space (+sizeDelta=referenceResolution, pivot giữa, DynamicPPU≥3), thêm `TrackedDeviceGraphicRaycaster`
  + `VRWorldSpaceMenu` + `VRUIRenderOnTop`; EventSystem→`XRUIInputModule`; thêm GO `Ray` (XRRayInteractor+LineVisual,
  invalid alpha=1) vào **mọi** `RightHand Controller` (scene đua có 3 rig CarType); thêm `VRMenuToggle` lên EventSystem
  (wire `menu`/`placer`/danh sách `rayObjects`); thêm shader vào Always Included Shaders. **Idempotent** — chạy lại không nhân đôi.
- `VRMenuToggle` có thêm field `rayObjects` (mảng) để bật/tắt nhiều tia cùng lúc cho scene nhiều rig.
- **Tự nhận loại scene** (bước 6): scene ĐUA (có `RaceResultsController`) thì `UI_Main Menu` là *container*
  chứa modal (bảng kết quả tự mở + modal Exit/Pause) → tool GIỮ canvas active và gắn **`VRRaceMenu`** thay vì
  `VRMenuToggle`: bấm **A/X tay trái** mở/đóng modal Exit (thay phím **Tab** của `PCHotkeyManager` — vô dụng
  trong VR), và **tự hiện tia khi có modal mở** (kết quả hoặc exit), ẩn khi đang lái. Nút Exit/Back trong modal
  (Heat UI `SceneChanger.LoadScene` / `RaceResultsController.ReturnToGarage`) bấm được bằng tia vì modal nằm
  trong canvas world-space. ⚠️ Nếu tool đoán sai modal exit → kéo đúng ModalWindowManager vào field `exitModal`.

---

## ✅ Checklist nhanh
- [ ] Canvas = World Space (scale ~0.001)
- [ ] EventSystem dùng `XRUIInputModule`
- [ ] Canvas có `TrackedDeviceGraphicRaycaster`
- [ ] 1 tay có GO con: `XRRayInteractor` + `XRInteractorLineVisual` (+LineRenderer), bật Enable UI Interaction, Invalid color alpha=1
- [ ] `VRWorldSpaceMenu` trên canvas (chỉnh distance/baseScale)
- [ ] `VRMenuToggle` trên object always-active (menu + rayObject + placer)
- [ ] (tùy chọn) `VRUIRenderOnTop` + shader trong Always Included Shaders
- [ ] Dynamic Pixels Per Unit = 3 (+ atlas/render scale nếu cần nét hơn)
- [ ] `Input Action Manager` (XR Origin) bật map **UI**; có `XRInteractionManager`

## ⚠️ Cạm bẫy hay gặp
- **“A GameObject can only contain one XRBaseInteractor”** → ray phải ở GO con (bước 4).
- **Tia hiện mà bấm không ăn** → thiếu `TrackedDeviceGraphicRaycaster`, hoặc EventSystem chưa phải `XRUIInputModule`, hoặc `Input Action Manager` chưa bật map UI.
- **Tia biến mất khi không trỏ nút** → Invalid Color alpha = 0 → set = 1.
- **Phần tử UI che đen nhau sau khi render-on-top** → lệch render queue → giữ shader Queue "Transparent", đừng đổi renderQueue TMP.
- **Sửa file `.unity` ngoài Unity** → RELOAD scene (đừng Ctrl+S đè); fileID đổi mỗi lần Unity re-save → dò anchor theo `m_Name`/guid, đừng hardcode.
