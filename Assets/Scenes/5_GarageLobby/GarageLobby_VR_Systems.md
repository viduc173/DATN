# GarageLobby VR — Context & Hướng chuyển PC → VR

Tài liệu này chụp lại **hiện trạng** scene `GarageLobby_vr.unity` và đề xuất **hướng sửa đổi** để
hệ tương tác (cầm bánh xe / phanh / bình sơn) và UI hoạt động được trong VR.

> Đọc kèm: `GarageLobby_PC_Systems.md` (hệ PC gốc) và `Assets/Scenes/Design/VR_WorldSpace_UI_Design.md` (thiết kế UI VR tái dùng).

---

## 0. Tóm tắt nhanh (TL;DR)

| Hạng mục | Hiện trạng | Cần làm |
| --- | --- | --- |
| XR Origin + 2 tay controller | ✅ Đã có, input Position/Rotation/**Select**/**Activate** đã wire (XRI Default Input Actions) | Không |
| Locomotion (đi lại, xoay) | ✅ Continuous Move + Car Turn Provider | Không |
| `XRDirectInteractor` (cầm bằng chạm tay) | ✅ Có trên cả 2 tay (SphereCollider trigger r=0.1) | Không |
| `XRInteractionManager` trong scene | ❌ **KHÔNG có** (cả 2 interactor `m_InteractionManager: 0`) | **Thêm 1 GameObject `XRInteractionManager`** |
| `XRGrabInteractable` trên vật cầm | ⚠️ **Bật/tắt LỘN XỘN** giữa các prefab (chỗ 0, chỗ 1) | **Bật `m_Enabled: 1` đồng loạt** |
| Logic grab bánh xe / phanh | ✅ Đã code sẵn trong `WheelItem`/`BrakeItem` (grab→tháo, thả gần socket→lắp) | Chỉ cần bật interactable |
| Sơn xe khi cầm bình + bóp cò | ❌ Chưa có cầu nối Activate→ApplyPaint | **Gắn `XRPaintCanActivator`** (đã tạo sẵn) |
| Tia raycast + bấm UI bằng tay | ❌ Chưa có Ray Interactor / line / TrackedDeviceGraphicRaycaster | Xem doc UI VR |
| EventSystem | `InputSystemUIInputModule` | Đổi/bổ sung `XRUIInputModule` (xem doc UI) |
| Script PC (`PCCameraController`, `PCHotkeyManager`, `PCInteractorManager`, `MenuCursorBinder`) | Vẫn còn (nhiều cái đã disable trên XR Origin) | Để yên / disable cho gọn (vô hại trong VR vì không có chuột/phím) |

**Kết luận:** rig VR đã dựng gần xong. 3 việc chính để "cầm được đồ" trong VR:
1. Thêm `XRInteractionManager` vào scene.
2. Bật `XRGrabInteractable` đồng loạt trên mọi prefab vật cầm.
3. Gắn `XRPaintCanActivator` cho bình sơn (để bóp cò = sơn).

---

## 1. Hiện trạng rig VR trong scene

GameObject **`XR Inside Car & Input`** (fileID `588309968`, line ~62999) = XR Origin:

```
XR Inside Car & Input            (XROrigin + CharacterController + locomotion)
  ├── Input Action Manager       (kích hoạt XRI Default Input Actions)
  ├── Continuous Move Provider    (đi lại, moveSpeed 3)
  ├── Car Turn Provider           (xoay, turnSpeed 60)
  ├── Climb Provider
  ├── XR Device Simulator         (disabled — bật khi test không có kính)
  ├── (disabled) Free Look Camera / Player Movement / Grab Interactable Handler  ← script PC cũ
  └── Camera Offset
        ├── Main Camera           (fileID 625429101)  ← Tracked Pose Driver + URP + AudioListener
        ├── LeftHand Controller   (fileID 1511909640)
        └── RightHand Controller  (fileID 1033031907)
```

Mỗi **Controller** có:
- `ActionBasedController` — Position/Rotation/TrackingState/**Select**/SelectValue/**Activate** đều `m_UseReference: 1` trỏ về asset `c348712bda248c246b8c49b3db54643f` (XRI Default Input Actions). → Cầm (Select) và bóp cò (Activate) **đã sẵn sàng**.
- `XRDirectInteractor` — cầm vật bằng cách **chạm** SphereCollider (trigger, radius 0.1) của tay vào collider vật. ⚠️ `m_InteractionManager: {fileID: 0}`.
- `Rigidbody` (kinematic theo tracking) + `SphereCollider` (trigger) + `SortingGroup`.
- ❌ Chưa có `XRRayInteractor` / `LineRenderer` / `XRInteractorLineVisual` (tia bắn xa).

> **Main Camera VR dùng chung fileID `625429101`** với `playerCamera` mà các `PCInteractorObject` trỏ tới ở scene PC — nên các tham chiếu camera cũ không bị gãy.

### Điểm chặn quan trọng: thiếu `XRInteractionManager`
Cả 2 `XRDirectInteractor` đang `m_InteractionManager: 0` và **không có** `XRInteractionManager` nào trong scene. XRI 2.6.4 sẽ cố tự tạo runtime, nhưng để chắc chắn và tất định, **hãy tạo 1 GameObject rỗng tên `XR Interaction Manager`** và add component `XRInteractionManager` (guid `33b187...` — add qua menu Component, đừng gõ tay GUID). Interactor/interactable sẽ tự tìm thấy nó.

---

## 2. Vật cầm được sống trong PREFAB, không nằm thẳng trong scene

Quan trọng khi sửa: xe và phụ tùng là **prefab instance**. Grep scene `GarageLobby_vr.unity` ra **0** `WheelItem` / `XRGrabInteractable` / `PCInteractorObject` — vì chúng nằm trong các prefab:

| Nhóm | Prefab | Component liên quan |
| --- | --- | --- |
| Xe (nhúng bánh+phanh) | `Assets/Data/CarParts/Car/CarType_0/1/2.prefab` | 8 `XRGrabInteractable` mỗi xe (4 bánh + 4 phanh) |
| Bánh rời | `Assets/Prefabs/CarPart/Wheel/CarWheel_*.prefab` (8) | `WheelItem` + `XRGrabInteractable` + `PCInteractorObject` |
| Phanh | `Assets/Prefabs/CarPart/BrakeCaliper/Brake_*.prefab` (12) | `BrakeItem` + `XRGrabInteractable` + `PCInteractorObject` |
| Bình sơn | `Assets/Prefabs/CarPart/Spray/sprayCan_*.prefab` (6) | `CarPaintCan` + `XRGrabInteractable` + `PCInteractorObject` |

→ **Sửa prefab là chuẩn nhất** (lan ra mọi scene/instance). Sửa trong scene chỉ tạo override cục bộ.

### ⚠️ Trạng thái `XRGrabInteractable` đang LỘN XỘN
Kiểm tra `m_Enabled` của `XRGrabInteractable`:
- `CarType_0.prefab`: 8 cái → pattern `0,1,0,0,1,1,1,0` (4 bật, 4 tắt).
- `sprayCan_blue.prefab`: **0** (tắt).
- `CarWheel_Normal.prefab`: **0** (tắt).
- `Brake_normal_L.prefab`: **1** (bật).

→ Trong VR, cái nào `m_Enabled: 0` thì **không cầm được**. Đây gần như chắc chắn là lý do "có vật cầm được, có vật không". **Phải bật đồng loạt = 1.**

---

## 3. Logic grab đã có sẵn trong code (không phải viết mới)

`WheelItem` (`Assets/Script/Wheels/WheelItem.cs`) đã `[RequireComponent(typeof(XRGrabInteractable))]` và:
```
OnGrabbed (selectEntered)  → Detach()                     // cầm lên = tự tháo khỏi socket
OnReleased (selectExited)  → FindNearestSocket(snapDist)  // thả gần socket = tự lắp
                             → có thì AttachWheel, không thì rơi tự do
```
`WheelSocket.CheckForNearbyGrabbedWheel()` mỗi frame `OverlapSphere(snapRadius)` + `wheel.IsBeingGrabbed`
→ khi đang cầm bánh tới sát socket trống là **tự hút vào**. `IsBeingGrabbed = grabInteractable.isSelected`.

`BrakeItem` theo cùng pattern (xem memory `stadium`/brake notes). → **Bật `XRGrabInteractable` là chạy.**

**Không xung đột với PCInteractorObject:** trong build VR không có chuột (`HandlePickupInput` không kích hoạt) và F chỉ chạy nếu `allowDirectInput=true`. `PCInteractorObject` nằm im. `WheelItem.FixedUpdate` chỉ ép kinematic khi *đã gắn vào socket* nên không cản lúc đang cầm.

---

## 4. Sơn xe trong VR — cần cầu nối Activate → ApplyPaint

PC: nhìn bình + bấm F → `PCInteractorObject` → `CarPaintCan.ApplyPaint()`.
VR: cầm bình rồi **bóp cò (Activate)** phải gọi `ApplyPaint()`. Đã tạo sẵn script:

**`Assets/Script/VRAddOnScript/XRPaintCanActivator.cs`** — nghe `XRGrabInteractable.activated` → `CarPaintCan.ApplyPaint()`.
→ Gắn vào mỗi `sprayCan_*.prefab` (cùng GO với `CarPaintCan` + `XRGrabInteractable`). Chạy song song với PCInteractorObject.

---

## 5. Hướng sửa đổi đề xuất (checklist theo thứ tự)

**A. Bật grab toàn cục (prefab):**
1. [ ] Mở từng prefab vật cầm, set **mọi** `XRGrabInteractable.m_Enabled = 1`
       (8 wheel + 12 brake + 6 spray + 8×3 trong CarType_0/1/2). Xem mục §7 nếu muốn sửa hàng loạt.
2. [ ] Trên các `XRGrabInteractable` bánh/phanh: đặt **Movement Type = Instantaneous** hoặc **Velocity Tracking**
       (mượt khi cầm), bật **Throw On Detach** tùy ý. Để mặc định cũng chạy.

**B. Scene rig:**
3. [ ] Tạo GameObject `XR Interaction Manager` + component `XRInteractionManager`.
4. [ ] (Khuyên) Tắt/để disabled `PCInteractorManager`, `PCHotkeyManager`, `PCCameraController`, `MenuCursorBinder`
       cho gọn — vô hại nếu để, vì không có chuột/phím trong build VR.

**C. Bình sơn:**
5. [ ] Gắn `XRPaintCanActivator` lên 6 `sprayCan_*.prefab`.

**D. UI bằng tia (xem doc riêng `VR_WorldSpace_UI_Design.md`):**
6. [ ] Thêm `XRRayInteractor` + `LineRenderer` + `XRInteractorLineVisual` cho tay (tia render được).
7. [ ] EventSystem: dùng `XRUIInputModule`.
8. [ ] World canvas (UI_Main Menu, UI_CarStats): thêm `TrackedDeviceGraphicRaycaster`.
9. [ ] Gắn `VRWorldSpaceMenu` lên canvas để tự canh tầm mắt.

**E. Cầm xa (tùy chọn):** nếu muốn cầm bánh/phanh từ xa (không phải với tay tới), bật **Allow Grab Interaction** trên `XRRayInteractor`. Hiện chỉ có cầm-bằng-chạm (`XRDirectInteractor`).

---

## 6. Kiểm thử (test mà không cần kính)

- Bật **XR Device Simulator** (đang có sẵn trên XR Origin, đang disabled) để mô phỏng 2 tay + đầu bằng chuột/phím.
- Checklist test: chạm tay vào bánh → cầm → kéo ra (tháo) → đưa lại sát socket → nhả → lắp lại;
  cầm bình sơn → bóp cò → xe đổi màu; chĩa tia vào nút menu → bóp cò → nút bấm.

---

## 7. Sửa hàng loạt `m_Enabled` (nếu cần)

Mỗi block `XRGrabInteractable` trong prefab có dạng:
```yaml
  m_Enabled: 0          # ← đổi thành 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 0ad34abafad169848a38072baa96cdb2, type: 3}
```
GUID `XRGrabInteractable` = `0ad34abafad169848a38072baa96cdb2`. Có thể viết 1 **Editor tool** (`[MenuItem("Tools/VR/Enable All Grab Interactables")]`) duyệt prefab và bật đồng loạt — an toàn hơn sửa YAML tay. (Hỏi nếu muốn mình dựng tool này.)

---

## 8. Tham chiếu nhanh (fileID / GUID)

| Thứ | fileID / GUID | Vị trí |
| --- | --- | --- |
| XR Origin GO | `588309968` | line ~62999 |
| XROrigin comp | guid `e0cb9aa70a22847b5925ee5f067c10a9` | |
| Main Camera | `625429095` (Camera `625429101`) | line ~65503 |
| LeftHand Controller | `1511909640` | line ~110514 |
| RightHand Controller | `1033031907` | line ~84332 |
| ActionBasedController | guid `caff514de9b15ad48ab85dcff5508221` | |
| XRDirectInteractor | guid `4253f32900bcc4d499d675566142ded0` | |
| XRRayInteractor (chưa có) | guid `6803edce0201f574f923fd9d10e5b30a` | |
| XRGrabInteractable | guid `0ad34abafad169848a38072baa96cdb2` | trong prefab |
| EventSystem GO | `1702945650` | line ~119406 |
| InputSystemUIInputModule | guid `01614664b831546d2ae94a42149d80ac` | |
| TrackedDeviceGraphicRaycaster | guid `7951c64acb0fa62458bf30a60089fe2d` | (XRI) |
| UI_Main Menu (world canvas) | GO `168599941`, RenderMode 2, `m_IsActive: 0` | line ~10574 |
| `VRWorldSpaceMenu.cs` | `Assets/Script/VRAddOnScript/` | mới tạo |
| `XRPaintCanActivator.cs` | `Assets/Script/VRAddOnScript/` | mới tạo |
