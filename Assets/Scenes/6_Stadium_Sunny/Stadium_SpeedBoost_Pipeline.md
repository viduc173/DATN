# Stadium Sunny — Pipeline Bonus tăng tốc (Speed Boost)

> Tài liệu mô tả cách scene `6_Stadium_Sunny` biến **bonus nhặt được trên đường** thành **tăng tốc THẬT
> tạm thời** cho xe Player. Trước đây nhặt bonus chỉ bật **hiệu ứng (VFX)** chứ tốc độ không đổi —
> tài liệu này bổ sung phần cơ chế tốc độ.
> Unity 6. Triển khai 2026-06-03. Bổ trợ cho `Stadium_CarLoad_Pipeline.md` + `Stadium_Results_Pipeline.md`.

---

## 0. Bối cảnh — cái gì đã có, cái gì thiếu

Trong scene có **3 bonus pickup**. Mỗi pickup là 1 GameObject có `PlayerTriggerEvent`
(collider trigger, `targetTag = "Player"`, `triggerOnce = 1`, `canRetrigger = 0`). Khi xe Player
(`CarType_*`, tag "Player") chạm vào, `onPlayerEnter` chạy **3 việc**:

1. `SetActive(true)` object **"Effect"** → bật VFX tăng tốc (chỉ hình ảnh).
2. `SetActive(false)` object **"item"** → ẩn mesh bonus đã nhặt.
3. `BonusEvent.TriggerBonusUnityEvent()` → đẩy 1 bonus vào hàng đợi toàn cục.

**Thiếu:** không có gì làm xe chạy nhanh hơn. `BonusReceiver.HandleSpeedBonus` chỉ có `// TODO`,
và `TriggerBonusUnityEvent` đẩy bonus type `"default"` value `1` duration `0` → không khớp case "speed"
và duration 0 → vô hại. Tài liệu này nối phần còn thiếu.

---

## 1. Kiến trúc & luồng

```
Bonus pickup (PlayerTriggerEvent.onPlayerEnter)   [xe Player chạm trigger]
  ├─ Effect.SetActive(true)        ← VFX (giữ nguyên, không đổi)
  ├─ item.SetActive(false)         ← ẩn bonus đã ăn
  └─ BonusEvent.TriggerBonusUnityEvent()
        └─ TriggerBonus(defaultBonusType="speed", defaultBonusValue=1.6, defaultBonusDuration=3)
              └─ enqueue vào BonusEvent.bonusQueue (singleton, DontDestroyOnLoad)
                    ▼
BonusController (GameObject luôn active, tag "Player")  ← KHÔNG phải xe; là bộ điều phối
  └─ BonusReceiver  [poll queue mỗi checkInterval = 0.1s]
        └─ ProcessBonus → HandleBonusType("speed") → HandleSpeedBonus(bonus)
              └─ SpeedBoost.GetForActivePlayer().ActivateBoost(1.6, 3s)
                    ▼
SpeedBoost  [tự AddComponent vào xe Player active]
   • maxSpeedForward = base * 1.6   (nâng trần tốc độ)
   • FixedUpdate: AddRelativeForce tiến tới khi đạt trần mới (cú "đẩy")
   • hết 3s → khôi phục maxSpeedForward gốc
```

> **Vì sao tách `BonusController` khỏi xe:** chỉ 1 trong 3 `CarType_*` active mỗi lần chơi (xem
> `Stadium_CarLoad_Pipeline.md`). `BonusController` luôn active + poll queue nên kênh nhận bonus
> độc lập với việc đang dùng xe nào. `SpeedBoost` mới là thứ chạm vào xe — và nó tự gắn vào **đúng
> xe đang active**.

---

## 2. Cơ chế tăng tốc "thật" — `SpeedBoost.cs` (MỚI)

`Assets/Scripts/SpeedBoost.cs`. Đặt trên xe EVP (`VehicleController`). Hai phần kết hợp:

| Phần | Làm gì |
|---|---|
| **Nâng trần** | `maxSpeedForward = base * speedMultiplier` (mặc định 1.6 = +60%). EVP cho phép xe vượt top-speed thường. |
| **Lực đẩy** | Mỗi `FixedUpdate`, cộng `AddRelativeForce(forward * extraAcceleration, Acceleration)` **cho tới khi** vận tốc đạt trần mới → cảm giác nitro, không chỉ nâng trần suông. |

- Hết `duration` (mặc định 3s) → khôi phục `maxSpeedForward` gốc, tắt VFX (nếu gán `boostVfx`).
- **Cache base 1 lần** lúc bắt đầu boost → nhặt bonus liên tiếp **không nhân chồng** hệ số. Nhặt khi
  đang boost = **gia hạn** (lấy thời gian dài hơn), không cộng dồn tốc độ.
- **Không cần đặt sẵn trong scene.** `SpeedBoost.GetForActivePlayer()` dùng
  `FindObjectsByType<VehicleController>` (Exclude inactive → chỉ ra xe đang active; AI dùng
  `EasyCarController` nên không lọt) rồi `AddComponent` nếu xe chưa có. `OnEnable` đăng ký
  `static ActiveInstance`; `OnDisable` khôi phục trần + nhả instance (đổi xe / cuối chặng an toàn).

### Tham số (Inspector trên `SpeedBoost`, hoặc chỉnh giá trị bonus — xem §4)
| Field | Mặc định | Ý nghĩa |
|---|---|---|
| `speedMultiplier` | 1.6 | Hệ số nhân trần tốc độ (bị bonusValue override khi gọi từ BonusReceiver). |
| `duration` | 3 | Giây boost (bị bonusDuration override). |
| `extraAcceleration` | 12 | Lực tiến (m/s²) đẩy xe đạt trần mới. Tăng = bốc hơn. |
| `boostVfx` | (trống) | VFX tuỳ chọn bật/tắt theo boost. Scene đang dùng VFX "Effect" riêng nên để trống. |

---

## 3. Code đã thêm / sửa

| File | Thay đổi |
|---|---|
| `Assets/Scripts/SpeedBoost.cs` | **MỚI** — cơ chế boost EVP (nâng trần + lực đẩy + tự khôi phục); `static GetForActivePlayer()` tự gắn vào xe Player active. |
| `Assets/Scripts/BonusReceiver.cs` | `HandleSpeedBonus` (trước là TODO) → `SpeedBoost.GetForActivePlayer().ActivateBoost(bonusValue, duration)`. |
| `Assets/Scripts/BonusEvent.cs` | Thêm 3 field `defaultBonusType/Value/Duration` (mặc định `speed`/`1.6`/`3`); `TriggerBonusUnityEvent()` dùng chúng thay cho `"default",1,0` cũ. |
| `Assets/Scenes/6_Stadium_Sunny/Stadium_Sunny.unity` | Set sẵn `BonusEvent`: `defaultBonusType=speed`, `defaultBonusValue=1.6`, `defaultBonusDuration=3`. |

> Wiring 3 bonus pickup **không phải đụng tới** — chúng vẫn gọi `TriggerBonusUnityEvent()` như cũ;
> nay hàm đó tạo bonus "speed" thay vì "default".

---

## 4. Cách chỉnh độ mạnh / thời gian boost

- **1 chỗ cho toàn scene:** GameObject **`BonusEvent`** → `defaultBonusValue` (hệ số tốc độ) +
  `defaultBonusDuration` (giây). Đây là nguồn mà mọi bonus pickup dùng.
- **Cảm giác bốc** (gia tốc đạt trần): sau khi Play & nhặt bonus 1 lần, `SpeedBoost` được tự gắn lên
  xe → chỉnh `extraAcceleration` trên component đó (hoặc sửa default trong `SpeedBoost.cs`).
- Muốn **mỗi pickup mạnh/yếu khác nhau:** hiện tất cả dùng chung default của `BonusEvent`. Khi cần,
  thay `TriggerBonusUnityEvent()` bằng wiring tới một helper nhận tham số riêng, hoặc tách nhiều
  GameObject `BonusEvent` config khác nhau.

---

## 5. Cách verify end-to-end

1. Compile sạch. Mở scene Stadium.
2. Play, lái xe Player chạm 1 bonus pickup:
   - VFX "Effect" hiện (như trước).
   - Console: `Bonus triggered: Type=speed ...` → `Received Bonus: Type=speed ...` →
     `[SpeedBoost] CarType_X: x1.6 maxSpeed ...` (bật `showDebugLog` nếu không thấy).
   - **Xe vọt nhanh hơn rõ rệt ~3s** rồi về tốc độ thường (`hết boost, maxSpeed về ...`).
3. Nhặt bonus liên tiếp → boost **gia hạn**, KHÔNG nhân chồng tốc độ (xe không nhanh vô hạn).
4. Cuối chặng `RaceResultsController.StopAllVehicles()` phanh cứng — nếu đang boost, xe bị disable →
   `SpeedBoost.OnDisable` khôi phục trần (không để lại side-effect).

⚠️ **Ràng buộc:** xe Player phải là hệ EVP (`VehicleController`) + tag "Player" (đã đúng cho cả 3
`CarType_*`). AI (hệ `EasyCarController`) **không** ăn bonus tốc độ — đúng yêu cầu (chỉ Player).

---

## 6. Liên quan
- `Stadium_CarLoad_Pipeline.md` — chỉ 1 `CarType_*` active; lý do `SpeedBoost` resolve theo xe active.
- `Stadium_Results_Pipeline.md` — `StopAllVehicles()` cuối chặng (tương tác với boost ở §5.4).
- `Assets/Scripts/BonusEvent.cs` / `BonusReceiver.cs` — hệ bonus queue toàn cục có sẵn.
