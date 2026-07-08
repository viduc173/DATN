# VR World-Space UI — Thiết kế tái dùng (tia raycast + canh tầm mắt + bấm nút)

Cách làm 1 menu World-Space dùng được trong VR: **tự canh vừa tầm mắt theo khoảng cách**,
có **tia laser render được** từ tay, và **nút bấm được bằng cò**. Áp dụng cho mọi scene.

> Lần đầu áp dụng: scene `GarageLobby_vr.unity`, canvas `UI_Main Menu` (World Space, đang `m_IsActive: 0`).
> Script đi kèm: `Assets/Script/VRAddOnScript/VRWorldSpaceMenu.cs`.

---

## Nguyên tắc: vì sao Canvas phải là World Space + nút thường vẫn bấm được

Trong VR không có "màn hình 2D" → **Screen-Space Overlay canvas không hiển thị**. Phải để
**Canvas = World Space**. Tin tốt: **nút UGUI thường (`Button`/`Toggle`/`Slider`) KHÔNG cần gắn thêm gì**.
Cái duy nhất cần là đổi *bộ thu raycast ở cấp Canvas* để tia VR "chạm" được UI. Cụ thể 3 mảnh:

```
[Tay VR] XRRayInteractor + LineRenderer + XRInteractorLineVisual   ← bắn + VẼ tia
        │  (Select/Activate action = bóp cò → tính là "click")
        ▼
[EventSystem] XRUIInputModule                                      ← dịch tia → sự kiện UI (hover/click)
        ▼
[Canvas world-space] TrackedDeviceGraphicRaycaster                ← cho tia "trúng" được UGUI
        ▼
[Button thường]  ← KHÔNG cần component thêm. Chỉ cần là Graphic raycastTarget (mặc định đã có).
```

→ **Trả lời "nút phải gắn thêm gì?": Không gắn gì lên từng nút.** Chỉ thêm
`TrackedDeviceGraphicRaycaster` **một lần ở Canvas gốc**, rồi mọi `Button` con tự bấm được.

---

## Phần A — Tia raycast render được (trên tay)

Hiện 2 controller mới có `XRDirectInteractor` (cầm bằng chạm). Để bấm UI từ xa cần thêm **tia**.

> ⚠️ **GOTCHA quan trọng:** `XRDirectInteractor` và `XRRayInteractor` đều kế thừa `XRBaseInteractor`,
> mà **1 GameObject chỉ được 1 `XRBaseInteractor`**. Add thẳng `XRRayInteractor` vào `RightHand Controller`
> sẽ báo lỗi *"a GameObject can only contain one XRBaseInteractor"*. → **Phải để ray lên 1 GameObject CON.**
> Ray ở con tự tìm `ActionBasedController` ở cha qua `GetComponentInParent<XRBaseController>` (đã verify trong
> `XRBaseControllerInteractor.FindControllerComponent`), nên KHÔNG cần gán field XR Controller.

**Các bước:**
0. Chuột phải **RightHand Controller** → **Create Empty** → tên `UI Ray Interactor` → **Reset** Transform (0,0,0).
   (Đặt component ở GameObject con này, KHÔNG đặt thẳng lên controller.)
1. Add `XRRayInteractor`
   - **Line Type**: Straight Line, **Max Raycast Distance**: ~10.
   - **Enable Interaction with UI GameObjects**: ✅ (BẮT BUỘC để bấm UGUI).
   - **Raycast Mask**: chỉ để layer UI (5) nếu muốn tia chỉ trúng UI, không cầm nhầm đồ.
   - Để **Allow Grab Interaction = OFF** nếu chỉ dùng cho UI (tránh cầm đồ bằng tia).
2. Add `LineRenderer` (XRI tự thêm khi add line visual) — đây là phần **render được nhìn thấy**.
3. Add `XRInteractorLineVisual`
   - **Line Length** ~10, **Width** ~0.005–0.01.
   - **Valid/Invalid Color Gradient**: ví dụ xanh khi trúng nút, trắng/đỏ khi không.
   - **Override Line Length / Stop Line At First Raycast Hit**: ✅ để tia dừng đúng tại nút.
   - **Reticle**: gán prefab chấm tròn nhỏ để thấy điểm trúng (tùy chọn).

`ActionBasedController` cùng GO đã wire **Select + Activate** (XRI Default Input Actions) → bóp cò = click UI.
Không cần code thêm cho việc bấm.

> Đặt ray trên 1 tay (thường tay phải) để tránh 2 tia tranh nhau con trỏ. Tay kia giữ `XRDirectInteractor` để cầm đồ.

---

## Phần B — EventSystem dùng `XRUIInputModule`

Scene đang có `InputSystemUIInputModule`. Tia của XRRayInteractor chỉ lái được UGUI qua **`XRUIInputModule`** (module của XRI):
- Trên GameObject **EventSystem**: **Remove** `InputSystemUIInputModule`, **Add** `XRUIInputModule`.
- `XRUIInputModule` vẫn xử lý cả chuột nên bản PC không hỏng.
- Không cần kéo thả gì thêm: nó tự gom mọi `XRRayInteractor` có "Enable UI Interaction".

---

## Phần C — Canvas World-Space

Trên **mỗi** canvas world-space muốn bấm được (UI_Main Menu, UI_CarStats, …):
1. **Add `TrackedDeviceGraphicRaycaster`** (giữ `GraphicRaycaster` cũ cũng được — nó lo chuột cho PC).
2. **Canvas → Event Camera**: với TrackedDeviceGraphicRaycaster không bắt buộc, nhưng gán Main Camera VR cho chắc.
3. Đảm bảo các nút có ảnh/`Image` làm **Raycast Target** (mặc định đã bật).

---

## Phần D — Canh "vừa tầm mắt" theo khoảng cách (`VRWorldSpaceMenu`)

Gắn **`VRWorldSpaceMenu`** lên **GameObject gốc của Canvas world-space**. Nó tự:
- Đặt panel **ngang tầm mắt**, lùi ra trước mặt `distance` (m), hơi thấp hơn đường nhìn (`verticalOffset`).
- **Billboard** quay về phía người chơi (mặc định thẳng đứng — chỉ xoay trục Y).
- **Scale theo khoảng cách** (`constantAngularSize`) → UI luôn chiếm **cùng một góc nhìn**, đặt gần/xa đều "vừa mắt".
  Đây cũng là cách **sửa lỗi canvas đang để `localScale = 0`**: cứ để script set scale lúc runtime.

| Field | Ý nghĩa | Gợi ý |
| --- | --- | --- |
| `distance` | Khoảng cách trước mặt | 1.2–2.0 m |
| `verticalOffset` | Lệch dọc so với mắt | −0.15 (thấp hơn 1 chút) |
| `followMode` | `PlaceOnEnable` (đặt 1 lần) / `LazyFollow` (bám theo) | Menu → PlaceOnEnable |
| `keepUpright` | Giữ panel thẳng | Bật |
| `baseScale` | localScale "đẹp" tại referenceDistance | ~0.0015 (world canvas) |
| `referenceDistance` | Khoảng cách canh baseScale | = `distance` |
| `recenterAngle` / `followSmoothing` | Chỉ cho LazyFollow | 35° / 6 |

**Cách dùng:**
- Mở canvas (SetActive true) → tự đặt trước mặt 1 lần.
- Cho 1 nút/nút-tay gọi `VRWorldSpaceMenu.Recenter()` để kéo menu về khi người chơi đi chỗ khác.
- Theo convention sẵn có trong dự án (`CarStatsUIManager.PlacePanelBetweenPlayerAndPart`): đặt 1 lần rồi neo,
  không bám liên tục (đỡ say VR). `LazyFollow` chỉ bật khi thật sự cần menu luôn trước mặt.

---

## Phần E — Lựa chọn thay thế: nút vật lý 3D (`VirtualButton`) — đã có sẵn trong dự án

Nếu muốn nút **khối 3D** (lồi lên khi hover) thay vì UGUI phẳng, dự án đã có hệ riêng:
- Gắn `VirtualButton` (`Assets/Script/VirtualButton.cs`) + 1 **Collider** lên object nút 3D; nối hành động vào `onButtonClick`.
- Trên tay phải gắn `VRButtonInteractor` (`Assets/Script/VRButtonInteractor.cs`, cần `HandInputValue`): tự bắn tia từ tay, hover làm nút nổi lên, bóp cò (`triggerThreshold`) → `OnButtonPressed`.
- Không cần TrackedDeviceGraphicRaycaster/XRUIInputModule cho hệ này (nó dùng `Physics.Raycast` thuần).

→ **Khi nào dùng cái nào:** menu UGUI có sẵn (như `UI_Main Menu` của Heat UI) → dùng Phần A–D.
Nút khối trang trí trong không gian garage → dùng `VirtualButton`. Đừng trộn 2 hệ trên cùng 1 nút.

---

## Checklist mang sang scene khác

- [ ] Canvas = **World Space** (RenderMode 2).
- [ ] Add `TrackedDeviceGraphicRaycaster` lên Canvas gốc.
- [ ] Add `VRWorldSpaceMenu` lên Canvas gốc (chỉnh distance/baseScale).
- [ ] EventSystem dùng `XRUIInputModule` (1 lần/scene).
- [ ] 1 tay có `XRRayInteractor` + `LineRenderer` + `XRInteractorLineVisual`, bật **Enable UI Interaction**.
- [ ] Có `XRInteractionManager` trong scene.
- [ ] Nút: dùng `Button` UGUI bình thường — **không gắn thêm gì**.

## GUID tham chiếu (add qua menu Component, không gõ tay)

| Component | GUID |
| --- | --- |
| XRRayInteractor | `6803edce0201f574f923fd9d10e5b30a` |
| TrackedDeviceGraphicRaycaster | `7951c64acb0fa62458bf30a60089fe2d` |
| InputSystemUIInputModule (đang dùng) | `01614664b831546d2ae94a42149d80ac` |
| XRDirectInteractor | `4253f32900bcc4d499d675566142ded0` |
| ActionBasedController | `caff514de9b15ad48ab85dcff5508221` |
