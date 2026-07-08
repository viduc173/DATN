# GarageLobby VR — README đầy đủ

Scene `GarageLobby_vr.unity` chuyển từ bản PC sang **VR** (XR Interaction Toolkit 2.6.4).
Tài liệu này gom **toàn bộ** hệ thống + cách dùng/tinh chỉnh/tái dùng + xử lý sự cố.

> Liên quan: `GarageLobby_VR_Systems.md` (kiến trúc chi tiết) · `../Design/VR_WorldSpace_UI_Design.md` (thiết kế UI VR tái dùng).

---

## 1. Rig VR

```
XR Inside Car & Input  (XR Origin + CharacterController + Continuous Move + Car Turn Provider)
  └── Camera Offset
        ├── Main Camera           (tag MainCamera, Tracked Pose Driver)  — Camera.main
        ├── LeftHand Controller   (ActionBasedController + XRDirectInteractor)  ← nút A/X bật menu
        └── RightHand Controller  (ActionBasedController + XRDirectInteractor)
              └── … └── Ray        (XRRayInteractor + LineRenderer + XRInteractorLineVisual)  ← tia bấm UI
```
- Input = **XRI Default Input Actions** (`c348712bda…`). `XRInteractionManager` tự sinh lúc runtime.
- ⚠️ 1 GameObject chỉ chứa **1** `XRBaseInteractor` → ray phải ở **GO con** của controller (cha giữ XRDirectInteractor).

---

## 2. Cầm phụ tùng (bánh / phanh / bình sơn)

- Logic grab có sẵn trong code: `WheelItem`/`BrakeItem` (grab→tháo, thả gần socket→lắp); `WheelSocket` hút bánh khi đang cầm tới gần.
- **Đã bật toàn bộ `XRGrabInteractable`** (50 cái) trên prefab: `Data/CarParts/Car/CarType_0/1/2`, `Prefabs/CarPart/Wheel|BrakeCaliper|Spray/*`.
- **Cầm = chạm tay + bóp grip** (XRDirectInteractor). Muốn cầm xa → bật *Allow Grab* trên XRRayInteractor.
- **Sơn xe:** mỗi `sprayCan_*` có `XRPaintCanActivator` → cầm bình + **bóp cò (Activate)** = `CarPaintCan.ApplyPaint()`.

---

## 3. Menu UI trong VR (UI_Main Menu — world space)

Tia laser tay phải bấm được UI UGUI. **Không gắn gì lên từng nút** — chỉ 3 mảnh cấp hệ thống:

| Mảnh | Ở đâu |
| --- | --- |
| `XRRayInteractor` + `LineRenderer` + `XRInteractorLineVisual` | GO **Ray** (con RightHand Controller) |
| `XRUIInputModule` | **EventSystem** (đã đổi từ InputSystemUIInputModule) |
| `TrackedDeviceGraphicRaycaster` | canvas **UI_Main Menu** |

- Nút = **Heat UI `ButtonManager`** (IPointerClickHandler) → ray bấm được. **11 Slider** UGUI → ray kéo được.
- Panel **Chapters** nằm chung canvas → raycaster cấp canvas phủ luôn.
- Nếu tia hiện mà bấm không ăn: kiểm tra `Input Action Manager` (trên XR Origin) bật map **UI** của XRI Default Input Actions.

---

## 4. Ba chức năng

### 4.1 Bấm A (tay trái) bật/tắt menu + hiện tia — `VRMenuToggle`
- Gắn trên **EventSystem** (luôn active). Trỏ: `menu` = UI_Main Menu, `rayObject` = GO **Ray**, `placer` = VRWorldSpaceMenu.
- `hideOnStart = true` → vào game **menu + tia ẩn**; bấm **A/X tay trái** → hiện cả menu lẫn tia (và kéo menu ra trước mặt).
- **Tia luôn hiện khi menu mở**, kể cả không trỏ vào nút (xem 4.x). Đổi nút → field `bindingPath` (vd `{RightHand}/primaryButton`, `secondaryButton`).

### 4.2 Menu cách mặt & xoay vòng bám theo — `VRWorldSpaceMenu` (OrbitFollow)
- Gắn trên canvas UI_Main Menu. Giữ panel cách mặt `distance` (m), **xoay vòng (orbit) bám theo hướng nhìn**, luôn quay mặt về người chơi.
- **Vùng chết** `deadzoneAngle` 8° + hysteresis `settleAngle` 2° → chỉ xoay khi quay đầu đủ nhiều ⇒ mượt, đỡ say.
- **Tự scale theo khoảng cách** (`constantAngularSize`) để luôn “vừa mắt”.

**Knobs tinh chỉnh LIVE (Inspector lúc Play):**
| Field | Ý nghĩa | Mặc định |
| --- | --- | --- |
| `distance` | xa/gần mặt | 1.5 m |
| `baseScale` | TO/NHỎ panel (canvas sizeDelta lớn → số nhỏ) | 0.0006 |
| `verticalOffset` | cao/thấp so với mắt | -0.1 |
| `deadzoneAngle` / `orbitSmoothing` | độ “lười” / tốc độ bám | 8° / 4 |
> Muốn đặt 1 lần không bám → `followMode = PlaceOnEnable`.

### 4.3 Tia luôn hiện (kể cả không trỏ vào nút)
- Mặc định `XRInteractorLineVisual` để **invalid color = trong suốt** ⇒ tia biến mất khi trỏ vào khoảng trống.
- Đã sửa: **invalid color alpha = 1** (tia trắng khi không trúng UI, xanh khi trúng nút). Tia luôn nhìn thấy khi menu mở.
- Chỉnh màu/độ dài: trên `XRInteractorLineVisual` của GO Ray (Valid/Invalid Color Gradient, Line Length 3m).

### 4.4 UI hiển thị đè vật cản — `VRUIRenderOnTop` + shader `UI/Overlay Always On Top`
- Gắn trên canvas, `applyOnStart = true`. Làm UI **vẽ đè lên vật cản 3D** (ZTest Always), không cần camera phụ.
- **Không phá material gốc:**
  - Image `UI/Default` → material overlay (shader y hệt + ZTest Always ⇒ nhìn giống hệt).
  - Material **tùy biến** → **clone** rồi chỉ bật ZTest (giữ nguyên hiệu ứng); shader không có ZTest thì **giữ nguyên**.
  - TMP → **clone** fontMaterial, set `_ZTestMode = Always` (giữ nguyên thuộc tính chữ).
  - **Đảo ngược được** (chuột phải component → `Revert`); mỗi graphic 1 material instance (không phá stencil Mask).
- Shader giữ **Queue "Transparent"** (KHÔNG dùng Overlay) và **không đổi renderQueue của TMP** ⇒ thứ tự vẽ nội bộ UI theo hierarchy như cũ, **tránh phần tử này che đen phần tử kia** (lỗi ảnh Chapters bị đen đã fix do đây).

---

## 5. Scripts & shader (tái dùng cho scene khác)

| File | Việc |
| --- | --- |
| `Assets/Script/VRAddOnScript/VRWorldSpaceMenu.cs` | Đặt/orbit-follow + billboard + scale theo khoảng cách cho world canvas |
| `Assets/Script/VRAddOnScript/VRMenuToggle.cs` | Nút tay VR bật/tắt UI + tia (mặc định A tay trái) |
| `Assets/Script/VRAddOnScript/XRPaintCanActivator.cs` | Cầm bình sơn + bóp cò → ApplyPaint |
| `Assets/Script/VRAddOnScript/VRUIRenderOnTop.cs` | UI vẽ đè vật cản (giữ material gốc) |
| `Assets/Shaders/UI_Overlay.shader` | UI/Default + ZTest Always (Queue Transparent) |

**Đem UI VR sang scene khác:**
1. Canvas = **World Space** → thêm `TrackedDeviceGraphicRaycaster` + `VRWorldSpaceMenu`.
2. EventSystem dùng `XRUIInputModule`.
3. 1 tay: GO con chứa `XRRayInteractor` + `XRInteractorLineVisual` (+LineRenderer), bật *Enable UI Interaction*; muốn tia luôn hiện thì set **Invalid Color alpha = 1**.
4. Bật bằng nút: `VRMenuToggle` lên 1 object always-active, trỏ `menu` + `rayObject`.
5. (tùy chọn) `VRUIRenderOnTop` để đè vật cản.

---

## 6. Test nhanh
1. Để Unity **compile** rồi **RELOAD scene** (xem §8).
2. Play (hoặc bật **XR Device Simulator** trên XR Origin nếu không có kính).
3. **Bấm A/X tay trái** → menu + tia hiện trước mặt; quay đầu → menu xoay vòng bám theo; tia luôn thấy.
4. Chĩa tia vào nút/slider → bóp cò → bấm/kéo.
5. Với tay chạm bánh/phanh → grip cầm/tháo; cầm bình sơn → cò để sơn.
6. Đi tới sau xe/vật cản → menu vẫn hiện đè (4.4).
7. Tinh chỉnh `distance`/`baseScale` (VRWorldSpaceMenu) cho vừa mắt.

---

## 7. Xử lý sự cố
| Hiện tượng | Nguyên nhân & cách sửa |
| --- | --- |
| Tia có nhưng bấm UI không ăn | `Input Action Manager` chưa bật map UI; hoặc canvas thiếu `TrackedDeviceGraphicRaycaster` |
| Tia biến mất khi không trỏ nút | Invalid Color Gradient alpha = 0 → set alpha key0 = 1 |
| Phần tử UI che đen nhau | Mixed render queue → giữ shader Queue "Transparent", đừng đổi renderQueue của TMP |
| Hình ảnh bị che (sau vật) vẫn không thấy | Material tùy biến không có ZTest nên VRUIRenderOnTop bỏ qua → cần shader-variant ZTest cho material đó |
| Cầm đồ không được | `XRGrabInteractable.m_Enabled` phải = 1; layer nằm trong raycast mask; với tay tới đủ gần |
| Tia cầm nhầm đồ thay vì bấm UI | Giới hạn `Raycast Mask` của XRRayInteractor về layer UI |

---

## 8. ⚠️ Sửa scene file ngoài Unity (quan trọng)
Scene hay mở sẵn trong Unity. Sau khi `.unity` bị sửa ngoài (script/tay):
- **RELOAD scene** trong Unity (prompt “modified externally → Reload”), **ĐỪNG Ctrl+S đè** kẻo mất chỉnh sửa trên đĩa.
- fileID trong scene **đổi mỗi lần Unity re-save** (vd UI_Main Menu từng là `168599941` → `374856339`) ⇒ script sửa YAML phải **dò anchor theo nội dung** (m_Name/guid), không hardcode số.
- Scene kết thúc bằng object `SceneRoots` (bình thường).

---

## 9. Tham chiếu nhanh (GUID / anchor — có thể đổi khi re-save)
| Thứ | GUID / anchor |
| --- | --- |
| VRWorldSpaceMenu | `5d2972c1d175146468b4c5d88e9097c9` |
| VRMenuToggle | `7c4e9a1f2b6d4e8a9c3f1b5d7e2a4c61` |
| VRUIRenderOnTop | `3f8b2d6a9c1e4750b8d2f6a1c9e34b72` |
| XRPaintCanActivator | `4d3a183abcc17d34eb69925e9c079ed3` |
| UI_Overlay.shader | `6d4c2b0015020c94c929ab8f2360d394` |
| XRGrabInteractable | `0ad34abafad169848a38072baa96cdb2` |
| XRRayInteractor / LineVisual | `6803edce0201f574f923fd9d10e5b30a` / `e988983f96fe1dd48800bcdfc82f23e9` |
| XRUIInputModule / TrackedDeviceGraphicRaycaster | `ab68ce6587aab0146b8dabefbd806791` / `7951c64acb0fa62458bf30a60089fe2d` |
| GO Ray (tia) | `588809276` |
| Scene tham chiếu rig chuẩn (copy serialization) | `Assets/Scenes/0_BackUpScene/1 Start Scene.unity` |
