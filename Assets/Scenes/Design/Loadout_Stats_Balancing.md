# Khuyến nghị cân bằng Stats linh kiện — để lắp đồ "mạnh lên thấy rõ"

> Trả lời câu hỏi: *"1 xe có 5 thông số (base + cộng thêm của linh kiện), nên cho linh kiện cộng bao nhiêu
> để người chơi cảm giác lắp vào mạnh hơn hẳn?"*
> Dựa trên **mapping THỰC TẾ hiện tại** trong `LevelController.cs` (không phải bản refactor ở
> `RaceStats_Architecture.md` §9 — bản đó chưa làm).
> Ngày: 2026-06-03. Liên quan: `CarStats.cs`, `CarPart.cs`, `PlayerCarLoadout.cs`, `LevelController.cs`.

---

## 1. Mapping hiện tại: 1 điểm stat = bao nhiêu "lực" thật?

`LevelController.ApplyStatsTo()` map abstract 0–100 → physics EVP (piecewise, **50 = mặc định xe gốc**):

| Stat (0–100) | Field VehicleController | tại 0 | tại 50 | tại 100 | **+1 điểm (trên 50) ≈** |
|---|---|---|---|---|---|
| `maxSpeed` | `maxSpeedForward` | 13.89 m/s (50 km/h) | 27.78 (100 km/h) | 55.56 (200 km/h) | **+2 km/h** |
| `acceleration` | `maxDriveForce` | 1000 N | 2000 N | 5000 N | **+60 N** lực kéo |
| `grip` | `tireFriction` | 0.5 | 1.0 | 1.5 | **+0.01** ma sát |
| `braking` | `maxBrakeForce` | 1000 N | 3000 N | 6000 N | **+60 N** phanh |
| `handling` | `maxSteerAngle` | 20° | 35° | 55° | **+0.4°** góc lái |
| `nitro` ⭐ | *(không map physics)* | — | — | — | điều khiển NITRO — xem §6 |

→ **Lever cảm nhận mạnh nhất:** `maxSpeed` (tốc độ tối đa) và `acceleration` (ga bốc). Đây là 2 stat nên
cho engine cộng nhiều nhất — người chơi thấy ngay. `grip`/`handling` cảm nhận trong cua, `braking` trước cua gấp.

⭐ **Stat thứ 6 `nitro`** (default **0**) KHÔNG map sang `VehicleController` ở `LevelController` — nó được
`NitroController` đọc để quy đổi ra dung lượng bình + tốc độ hồi nitro. `acceleration`/`maxSpeed` cũng tác
động lên nitro. Chi tiết §6.

---

## 2. ⚠️ BẪY QUAN TRỌNG NHẤT: Wheels & Brakes cộng ×4

`PlayerCarLoadout.GetEffectiveStats()` cộng dồn **TỪNG** part. Mà:
- **Engine / Suspension / ECU = 1 cái/slot** → cộng 1 lần.
- **Wheels = 4 cái** (FL, FR, RL, RR) + **Brakes = 4 cái** → cộng **4 LẦN**.

> Nếu 1 lốp có `grip = +10` → lắp đủ 4 lốp = **+40 grip**. Tương tự brakes.
> ⇒ statBonus mỗi part **wheel/brake phải ≈ 1/4** mức tăng tổng mong muốn. Nếu set +10 cho mỗi lốp,
> 4 lốp đẩy grip từ 50 → 90 (chỉ riêng lốp) — quá mạnh, dễ chạm trần 100 và phí các slot khác.

---

## 3. Bảng giá trị `statBonus` khuyến nghị (theo slot × tier)

Thiết kế để **xe full tier-3 đạt ~77–85** ở stat chuyên biệt (rõ ràng mạnh hơn base 50, nhưng CHƯA chạm
trần 100 → còn dư địa cho "tune hoàn hảo"). Giá trị wheels/brakes là **MỖI PART** (nhớ ×4 khi đủ bộ).

### Engine (1 part) — chủ lực tốc độ + ga
| Tier | maxSpeed | acceleration | Ghi chú |
|---|---|---|---|
| 1 | +5  | +8  | Stock+ |
| 2 | +10 | +15 | Bốc rõ |
| 3 | +18 | +22 | Đỉnh |

### Suspension (1 part) — lái + bám
| Tier | handling | grip |
|---|---|---|
| 1 | +6  | +4  |
| 2 | +12 | +8  |
| 3 | +18 | +12 |

### ECU (1 part) — tinh chỉnh ga + chút tốc độ
| Tier | acceleration | maxSpeed |
|---|---|---|
| 1 | +4  | +3 |
| 2 | +8  | +6 |
| 3 | +12 | +9 |

### Wheels — **MỖI lốp** (×4 = tổng trong ngoặc)
| Tier | grip / lốp | handling / lốp | Tổng đủ bộ |
|---|---|---|---|
| 1 | +2   | +1   | grip +8, hand +4 |
| 2 | +3.5 | +2   | grip +14, hand +8 |
| 3 | +5   | +2.5 | grip +20, hand +10 |

### Brakes — **MỖI phanh** (×4 = tổng)
| Tier | braking / phanh | Tổng đủ bộ |
|---|---|---|
| 1 | +3 | +12 |
| 2 | +5 | +20 |
| 3 | +7 | +28 |

### Kết quả xe full tier-3 (từ base 50) & cảm giác thật
| Stat | 50 → | Physics đổi | Cảm nhận |
|---|---|---|---|
| maxSpeed | **77** (engine+18, ecu+9) | 100 → ~154 km/h | Đường thẳng vọt hẳn |
| acceleration | **84** (engine+22, ecu+12) | 2000 → ~4040 N (≈×2) | Xuất phát/thoát cua phóng mạnh |
| grip | **82** (susp+12, wheels+20) | friction 1.0 → 1.32 | Vào cua ít trượt |
| braking | **78** (brakes+28) | 3000 → 4680 N | Phanh ăn, vào cua sâu hơn |
| handling | **78** (susp+18, wheels+10) | góc lái 35° → 46° | Bẻ cua gắt hơn |

---

## 4. Nguyên tắc để "mạnh lên thấy rõ"

1. **Đừng dồn 1 part 1 stat khổng lồ** (vd engine maxSpeed +40): chạm trần 100, các slot sau vô dụng.
   Rải đều theo bảng trên để mỗi lần lắp đều thấy nhích.
2. **Mỗi tier nên hơn tier trước ~1.5–2 lần** về tổng điểm → cảm giác "đáng tiền" khi nâng cấp.
3. **Trade-off tạo cá tính** (CarStats cho phép âm): vd "Lốp Drift" `grip −3 / handling +4`; "Giáp nặng"
   `braking +6 / acceleration −5`. Người chơi phải chọn, không phải part nào cũng toàn dương.
4. **Hạ base xe stock để nâng cấp đã hơn** *(tuỳ chọn)*: đặt `PlayerCarLoadout.baseStats ≈ 42–45` thay vì 50
   → khoảng cách tới ~85 lớn hơn, xe "zin" cảm giác cần độ. Để 3 dòng xe khác cá tính: cho mỗi xe base lệch
   (xe tốc độ: maxSpeed 55 / handling 42; xe bám cua: grip 55 / maxSpeed 45...).
5. **maxSpeed & acceleration là đòn bẩy cảm nhận lớn nhất** → ưu tiên cho engine. grip/handling/braking
   tinh tế hơn, hợp suspension/wheels/brakes.
6. **Test bằng log:** `LevelController` đã in `spd/acc/grip/brake/hand` + physics field khi vào race —
   so trước/sau khi lắp để chỉnh số cho khớp cảm giác.

---

## 5. Chỉnh ở đâu
- Mỗi linh kiện: asset `CarPart` → field **`statBonus`** (giờ **6 ô**, có `nitro`) + `tier` + `costGold`.
- Xe gốc: asset `PlayerCarLoadout` → **`baseStats`** (nitro default 0).
- Muốn đổi "1 điểm stat = bao nhiêu lực": sửa các hằng `*_MIN/DEF/MAX` trong `LevelController.cs` (§1).
  (Nếu sau này áp bản refactor ở `RaceStats_Architecture.md` §9 — mapping nhân tương đối + composite —
  thì bảng §1 đổi, nhưng nguyên tắc §2–§4 vẫn đúng.)
- Tham số nitro (range bình/hồi/đẩy): component **`NitroController`** trên `PlayerController` (§6).

---

## 6. ⭐ Nitro phụ thuộc Stat xe

`NitroController.RecomputeFromStats()` quy đổi stat loadout → thông số nitro lúc vào race:

### 6.1. Món đồ bán → Stat → Tác động nitro

Ánh xạ theo đúng **món đồ trong shop** (mỗi món cộng 1 stat, stat đó điều khiển 1 phần nitro):

| Món bán | Cộng stat | → Phần nitro điều khiển | Quy đổi (NitroController) |
|---|---|---|---|
| **CAR (base) + ECU** | `nitro` | **Thời gian** (bình + hồi) | bình `Lerp(1.2s, 3.2s, nitro/100)`; hồi `Lerp(0.02, 0.8, nitro/100)`/giây |
| **ENGINE** | `acceleration` | **Lực đẩy** (chủ lực) | `nitroAccel`: accel 0→4, **50→8 (base)**, 100→16 (piecewise) |
| **TIRES** (wheels) | `grip` | **góp NHỎ** lực đẩy + tốc độ | đẩy `+2×(grip/100)`; hệ số tốc độ `+0.05×(grip/100)` |
| *(gián tiếp)* maxSpeed | `maxSpeed` | **trần tốc độ** khi phun | trần = `maxSpeedForward × hệ_số`, maxSpeedForward đã scale theo maxSpeed |

Công thức gộp:
```
maxCharge    = Lerp(1.2, 3.2, nitro/100)               ← CAR + ECU
rechargeRate = Lerp(0.02, 0.8, nitro/100)              ← CAR + ECU
nitroAccel   = MapStat(acceleration, 4,8,16) + 2×(grip/100)        ← ENGINE + (nhỏ) TIRES
hệ số tốc độ = 1.125 + 0.05×(grip/100)                  ← base + (nhỏ) TIRES
trần nitro   = maxSpeedForward(maxSpeed) × hệ số tốc độ  ← MAXSPEED
```

> **Thời gian nitro chỉ do stat `nitro` (CAR base + ECU) quyết định** → linh kiện ECU cộng `statBonus.nitro`
> = "cộng thêm thời gian nitro". `nitro = 0` (xe stock) = bình **1.2s** + hồi **0.02/s** (≈ refill 60s — gần
> như 1 phát rồi hết chặng) ⇒ nitro mạnh hay không **phụ thuộc nâng cấp** — chủ đích thiết kế.
> **Lực đẩy chủ yếu từ ENGINE** (acceleration); **TIRES (grip) chỉ góp một phần nhỏ** đẩy + tốc độ (chỉnh
> bằng `tirePushBonusAtMaxGrip` / `tireSpeedBonusAtMaxGrip` trên `NitroController`).

### 6.2. Giá trị `nitro` ĐÃ ÁP vào `Assets/Data` (dè dặt — tránh game quá dễ)

**Đặt nitro trên slot 1-cái** (ECU chủ lực, Engine phụ) — TRÁNH wheels/brakes vì cộng ×4 (xem §2).
Giá trị thấp hơn khuyến nghị "mạnh" ban đầu để xe full đồ chỉ đạt `nitro ≈ 60` (bình ~2.4s, hồi ~0.49/s),
không cho nitro liên tục → giữ độ khó.

| Asset | Slot | `statBonus.nitro` |
|---|---|---|
| `ECU_Stock` | ECU | 0 |
| `ECU_Stage1` | ECU | **+12** |
| `ECU_Stage2` | ECU | **+22** |
| `ECU_Race` | ECU | **+32** |
| `Engine_Stock` | Engine | 0 |
| `Engine_Sport` | Engine | **+5** |
| `Engine_Racing` | Engine | **+8** |

**Base nitro mỗi xe** (`PlayerCarLoadout.baseStats.nitro`) — stock có nitro yếu-nhưng-dùng-được:

| Xe | base nitro | Stock (chưa độ): bình / hồi |
|---|---|---|
| `Loadout_CarType0` (Azzurro Scout) | **15** | ~1.5s / 0.137/s |
| `Loadout_CarType1` (Mezzanotte X) | **12** | ~1.4s / 0.116/s |
| `Loadout_CarType2` (Furia Bianca) | **20** | ~1.6s / 0.176/s |

→ Full đồ (vd CarType2 base 20 + ECU_Race 32 + Engine_Racing 8) = `nitro 60` → bình **~2.4s**, hồi **~0.49/s**
(refill ~4.9s, đạt 50% ~2.4s). Đủ "đáng tiền" khi nâng cấp nhưng KHÔNG biến nitro thành vô hạn.

> Muốn nitro mạnh hơn nữa: nâng các số trên (hoặc base nitro). Muốn khó hơn: giảm base nitro về ~0–8.

### 6.3. Lưu ý
- Đổi nitro stat **không** đụng physics lái (`LevelController` bỏ qua field nitro) → an toàn, chỉ ảnh hưởng nitro.
- `acceleration` (ENGINE) "kép": vừa tăng `maxDriveForce` (lái thường, §1) vừa là **chủ lực đẩy nitro**.
- `grip` (TIRES) cũng "kép": bám đường (§1) + **góp nhỏ** lực đẩy & tốc độ nitro — KHÔNG cần cấu hình thêm,
  cứ lắp lốp grip cao là nitro nhỉnh hơn chút. Muốn tires ảnh hưởng nitro nhiều/ít hơn: chỉnh
  `tirePushBonusAtMaxGrip` / `tireSpeedBonusAtMaxGrip` trên `NitroController`.
- Stock car nitro yếu là **cố ý**. Muốn xe zin có nitro dùng được, set `PlayerCarLoadout.baseStats.nitro ≈ 30–40`.
- Range bình/hồi/đẩy chỉnh ở `NitroController` (`tankSecondsAtMin/MaxStat`, `rechargeAtMin/MaxStat`,
  `nitroAccelAtMin/Def/MaxAccel`, `tirePushBonusAtMaxGrip`, `tireSpeedBonusAtMaxGrip`).
