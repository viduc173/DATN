# Stadium Sunny — Pipeline Nitro (PC bấm Shift)

> Tài liệu mô tả hệ **nitro điều khiển bằng PC**: giữ **Left Shift** để phun nitro (nâng trần tốc độ +
> đẩy lực tiến), nhiên liệu có hạn — phun là tụt, nhả là hồi. VFX nitro (`PlayerController > Nitro`) tự gắn
> vào `CarType_* > NitroFx_Anchor` của xe đang dùng và bật/tắt theo trạng thái phun.
> Unity 6. Triển khai 2026-06-03. Tái dùng cơ chế `SpeedBoost` của `Stadium_SpeedBoost_Pipeline.md`.

---

## 0. Bối cảnh — yêu cầu

- PC: **bấm/giữ Shift → phun nitro**. Nitro chỉ có **một lượng nhỏ**, dùng hết phải **đợi hồi lại**.
- Hồi **liên tục** — còn nhiên liệu (> 0) là phun được, không bắt buộc đợi đầy (lựa chọn thiết kế).
- VFX = object `PlayerController > Nitro`, kích hoạt kiểu **SetActive** (giống cách "Bonus" được active ra).
- VFX **gắn vào `CarType_* > NitroFx_Anchor`** của xe Player đang active.

---

## 1. Cấu trúc liên quan

```
PlayerController (GameObject, luôn active)
├── Bonus   (m_IsActive=0)  ← VFX cũ, TimedObjectController tự tắt (KHÔNG đụng)
├── Nitro   (m_IsActive=0)  ← VFX nitro (7 con particle). Script gắn sang xe + bật khi phun.
├── ...
└── NitroController          ← THÊM MỚI (component trên chính PlayerController)

PlayerCarManager
└── CarType_0 / CarType_1 / CarType_2   (1 cái active)
        └── ... > NitroFx_Anchor          ← điểm gắn VFX nitro (đã có sẵn mỗi xe)
```

⚠️ **Left Shift trước đây là PHANH** (`PlayerDriverInputFromKeyboard.CarInput`) — đã **gỡ bỏ** khỏi phanh
để dành cho nitro. Phanh giờ chỉ còn **S / Mũi tên xuống**.

---

## 2. Luồng — `NitroController.cs` (MỚI)

`Assets/Scripts/NitroController.cs`, gắn trên **PlayerController**. `[DefaultExecutionOrder(-70)]`
(sau `LoadSceneController -110` / `LevelController -100` → xe player đã active khi nitro khởi tạo).

```
Start()
  ├─ RecomputeFromStats(): stat 'nitro'→bình+hồi, stat 'acceleration'→lực đẩy (đọc ActiveLoadout.Current)
  ├─ currentCharge = maxCharge
  ├─ nitroVfx = (đã wire) hoặc con tên "Nitro"; SetActive(false)
  └─ TryAttachVfxToCar(): tìm xe Player active → NitroFx_Anchor → SetParent + reset localTransform

Update()  [mỗi frame]
  ├─ ResolveActiveCar(): VehicleController active + tag Player (giống SpeedBoost). Xe đổi → gắn lại VFX.
  ├─ wantFire = giữ Shift  &&  có xe  &&  input chưa bị disable (kết thúc chặng)  &&  còn charge
  ├─ nếu phun: currentCharge -= dt; (cạn → khoá tới khi hồi đạt refireChargeThreshold, vd 50%)
  ├─ nếu KHÔNG phun: sau rechargeDelay → currentCharge += rechargeRate*dt (tới maxCharge); mở khoá khi đạt ngưỡng
  └─ nitroVfx.SetActive(đang phun)

FixedUpdate()  [khi đang phun]
  └─ SpeedBoost.GetForActivePlayer().ActivateBoost(nitroMultiplier, 0.2s, nitroAccel)
       → nâng maxSpeedForward = base*1.5 + đẩy lực tiến; nhả phím → tự khôi phục sau ~0.2s
```

> **Vì sao tái dùng `SpeedBoost`:** trong EVP, `maxSpeedForward` là **trần engine** — vượt trần thì engine
> sinh lực hãm ngược. Nên nitro phải **vừa nâng trần vừa đẩy lực** (đúng cơ chế SpeedBoost), nếu chỉ
> `AddForce` thuần sẽ bị engine ghì lại. Gọi `ActivateBoost` mỗi FixedUpdate với duration ngắn (0.2s) =
> giữ boost khi đang phun, nhả phím là boost tự hết → đuôi êm. Dùng chung cơ chế với bonus pickup.

---

## 3. Cấu hình — component `NitroController` trên PlayerController

| Field | Mặc định | Ý nghĩa |
|---|---|---|
| `nitroKey` | `LeftShift` (304) | Phím phun nitro. |
| `nitroVfx` | `Nitro` (đã wire) | Object VFX bật/tắt khi phun. Trống → tự tìm con tên "Nitro". |
| `nitroAnchorName` | `NitroFx_Anchor` | Tên anchor trên xe để gắn VFX. |
| `tankSecondsAtMinStat` | 1.2 | Dung lượng bình (giây) khi stat `nitro`=0. |
| `tankSecondsAtMaxStat` | 3.2 | Dung lượng bình khi stat `nitro`=100 (base 1.2 + tối đa 2). |
| `rechargeAtMinStat` | 0.02 | Tốc độ hồi (charge/giây) khi stat `nitro`=0. |
| `rechargeAtMaxStat` | 0.8 | Tốc độ hồi khi stat `nitro`=100. |
| `rechargeDelay` | 0.5 | Trễ sau khi ngừng phun mới bắt đầu hồi (giây). |
| `refireChargeThreshold` | **0.5** | Phun CẠN → khoá tới khi hồi đạt tỉ lệ này mới phun lại. 0.5 = 50% thanh. |
| `nitroAccelAtMinAccel` | 4 | Lực đẩy (ENGINE) khi stat `acceleration`=0. |
| `nitroAccelAtDefAccel` | 8 | Lực đẩy khi `acceleration`=50 (base 8). |
| `nitroAccelAtMaxAccel` | 16 | Lực đẩy khi `acceleration`=100 (accel cao → tốc độ nitro cao hơn). |
| `nitroMultiplier` | 1.125 | Nhân trần tốc độ khi phun (+12.5%; trần còn scale theo `maxSpeed` stat). |
| `tirePushBonusAtMaxGrip` | 2 | TIRES (grip) góp thêm lực đẩy khi grip=100 (phần nhỏ so với engine). |
| `tireSpeedBonusAtMaxGrip` | 0.05 | TIRES (grip) góp thêm hệ số tốc độ nitro khi grip=100 (→ ~x1.175). |
| `onNitroStart/Stop` | (trống) | UnityEvent cho UI thanh nitro / âm thanh (tuỳ chọn). |

⭐ **Tất cả TÍNH TỪ STAT xe** (`RecomputeFromStats()` lúc Start), ánh xạ theo món đồ shop:
**CAR+ECU**(`nitro`)→bình+hồi; **ENGINE**(`acceleration`)→lực đẩy chính; **TIRES**(`grip`)→góp nhỏ đẩy+tốc độ;
**maxSpeed**→trần tốc độ. Các field trên là **range quy đổi**, không phải giá trị cuối. Xem
`Loadout_Stats_Balancing.md` §6. Property public `ChargeNormalized`/`IsFiring`/`MaxCharge` cho UI bind.

**Chỉnh theo linh kiện (shop):** ECU/car cộng `statBonus.nitro` (thời gian); engine cộng `acceleration`
(lực đẩy); tires cộng `grip` (góp nhỏ). **Chỉnh range:** các field `*AtMin/Max/DefStat`/`*AtMaxGrip` trên component.

---

## 4. Code đã thêm / sửa

| File | Thay đổi |
|---|---|
| `Assets/Scripts/NitroController.cs` | **MỚI** — đọc Shift, quản nhiên liệu, **quy đổi stat→nitro** (`RecomputeFromStats`), gắn VFX vào `NitroFx_Anchor`, boost qua `SpeedBoost`. |
| `Assets/Script/Interact Tool/CarStats.cs` | Thêm stat thứ 6 **`nitro`** (default 0) + vào operator +/−. Điều khiển bình + tốc độ hồi nitro. |
| `Assets/Script/Player_Drive_Input/PlayerDriverInputFromKeyboard.cs` | Gỡ `LeftShift` khỏi `brake` (Shift giờ là nitro). Phanh còn `S` / `DownArrow`. |
| `Assets/Scenes/6_Stadium_Sunny/Stadium_Sunny.unity` | Thêm `NitroController` lên `PlayerController` (wire `nitroVfx`=Nitro, range bình/hồi/đẩy); set `Nitro` inactive. |

> Tái dùng (không sửa): `SpeedBoost.cs` (cơ chế boost), `Stadium_SpeedBoost_Pipeline.md`.

---

## 5. Verify

1. Compile sạch. Mở scene Stadium, Play.
2. Lái xe (W), **giữ Shift**:
   - VFX nitro (`Nitro`) bật ra ở đuôi xe (vị trí `NitroFx_Anchor`).
   - Xe **vọt nhanh hơn** rõ rệt (trần tốc độ +50% + lực đẩy).
   - Giữ lâu → nitro tụt; cạn thì hết phun (VFX tắt).
3. Nhả Shift vài giây → nitro hồi lại (sau `rechargeDelay`), bấm lại phun tiếp được.
4. Bấm **S** vẫn phanh bình thường (Shift không còn phanh).
5. Về đích (kết thúc chặng): input bị disable → giữ Shift KHÔNG phun nữa (guard `activeCarInput.enabled`).
6. (Tinh chỉnh) đổi `nitroMultiplier`/`maxCharge` trên component thấy độ mạnh/độ dài thay đổi.

⚠️ Nếu VFX không hiện: kiểm tra xe active có con tên đúng **`NitroFx_Anchor`** (script tìm theo tên, đệ quy).
Bật `showDebugLog` trên `NitroController` để xem log gắn anchor / tìm xe.

---

## 6. Liên quan
- `Stadium_SpeedBoost_Pipeline.md` — cơ chế `SpeedBoost` (nitro dùng lại để boost vật lý thật).
- `Stadium_Results_Pipeline.md` — kết thúc chặng disable input (nitro tự khoá theo).
- `Loadout_Stats_Balancing.md` — khuyến nghị giá trị stat linh kiện (5 thông số xe).
