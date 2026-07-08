# Hệ Thống Kinh Tế & Phần Thưởng

> Thiết kế vòng lặp kinh tế: kiếm tiền từ đường đua → mua linh kiện → xe mạnh hơn → tiếp cận chặng đua khó hơn.

---

## 1. Tổng Quan Vòng Lặp Kinh Tế

```
  ┌─────────────────────────────────────────────────────────┐
  │                   VÒNG LẶP KINH TẾ                      │
  │                                                         │
  │   Đua & về đích tốt                                     │
  │          │                                              │
  │          ▼                                              │
  │   Nhận Gold + Bonus                                     │
  │          │                                              │
  │          ▼                                              │
  │   Garage Lobby → Mua linh kiện                         │
  │          │                                              │
  │          ▼                                              │
  │   Xe mạnh hơn → Đua chặng khó hơn                      │
  │          │                                              │
  │          └──────────────────────────▶ (lặp lại)         │
  └─────────────────────────────────────────────────────────┘
```

---

## 2. Đơn Vị Tiền Tệ

| Đơn vị | Ký hiệu | Nguồn | Dùng để |
|--------|---------|-------|---------|
| **Gold** | 🪙 | Hoàn thành race, bonus thứ hạng | Mua linh kiện, unlock chặng mới |
| **XP** *(tương lai)* | ⭐ | Mỗi vòng đua hoàn thành | Tăng cấp xe, unlock màu sơn |

---

## 3. Hệ Thống Phần Thưởng Race

### 3.1 Cơ chế THẬT — thưởng cố định theo hạng (`prizeByPosition`)

Hiện game **không** dùng công thức base×multiplier. Mỗi scene có asset `RaceSettings_*` với mảng
**`prizeByPosition`** (int[]): index 0 = hạng 1, 1 = hạng 2, ... Số phần tử = số hạng được thưởng.
`RaceSettings.GetPrize(position)` (1-based) trả 0 nếu hạng ngoài mảng → ngoài top được thưởng không nhận gì.
Trên thực tế chỉ **Player** có ví (`PlayerInventory`) nên Player nhận thưởng theo hạng về đích nếu ≤ số hạng thưởng.

Khi Player về đích lap cuối: `RaceResultsController.GrantReward()` →
`inventory.AddGold(GetPrize(finishPosition))` → `inventory.SaveToPlayerPrefs()` (lưu ngay).

### 3.2 Bảng thưởng thật theo scene đang dùng

| Scene | Asset `RaceSettings` | 1st | 2nd | 3rd | 4th+ |
|-------|----------------------|-----|-----|-----|------|
| **6_Stadium_Sunny** | `RaceSettings_Stadium.asset` | 1000 | 600 | 300 | 0 |
| **7_Stadium_Rain** | `RaceSettings_Stadium_Rain.asset` | 1000 | 600 | 300 | 0 |
| **8_Beach_Sunny** | `RaceSettings_Beach_Sunny.asset` | 1200 | 500 | 100 | 0 |
| **3_CyberRace** (chưa vào game) | `RaceSettings_CyberRace.asset` | 1000 | 600 | 300 | 0 (default RaceSettings) |
| **4_Racing_Circuit** (chưa vào game) | *chưa tạo* | — | — | — | — |

> Chỉnh thưởng: mở asset `RaceSettings_*` tương ứng → sửa mảng `prizeByPosition`. Thêm/bớt phần tử = thêm/bớt
> số hạng được thưởng.

### 3.3 Bonus Đặc Biệt *(thiết kế đề xuất — CHƯA implement)*

Các loại bonus dưới đây là **ý tưởng mở rộng**, hiện chưa có trong code (`RaceResultsController` chỉ cộng thưởng
theo hạng). Liệt kê để định hướng phát triển:

| Điều kiện | Thưởng thêm (đề xuất) |
|-----------|------------|
| Hoàn thành **không va chạm** (clean race) | +100 Gold |
| Vượt tất cả AI trong vòng cuối | +150 Gold |
| Cải thiện thành tích cá nhân (Personal Best) | +80 Gold |
| Hoàn thành race đầu tiên trên chặng mới | +200 Gold (one-time) |

### 3.4 Bonus tăng tốc trong race (khác với thưởng Gold)

Trong lúc đua còn có **bonus pickup** rải quanh checkpoint (`bonusesPerLap`, mặc định 3/vòng) — ăn vào **tăng
tốc thật ~3s**, KHÔNG cộng Gold. Đây là phần thưởng "tức thời trong trận" để tạo cơ hội vượt. Xem
`6_Stadium_Sunny/Stadium_SpeedBoost_Pipeline.md` + `Stadium_BonusSpawn_Pipeline.md`.

---

## 4. Danh Mục Linh Kiện & Giá

Mỗi loại linh kiện tác động trực tiếp lên **CarStats (thang 0–100)** rồi được map sang physics của `VehicleController`. Xem chi tiết mapping tại [RaceStats_Architecture.md](RaceStats_Architecture.md).

### 4.1 Engine (Động Cơ)

| Linh kiện | Giá | Tác động | Phù hợp mặt đường |
|-----------|-----|----------|--------------|
| Stock Engine | 0 🪙 | — (mặc định) | Mọi mặt đường |
| Sport Engine | 800 🪙 | Speed +15, Accel +10 | Đường khô (6_Stadium_Sunny) |
| Turbo V1 | 1.500 🪙 | Speed +25, Accel +20 | Tốc độ cao (3_CyberRace) |
| Turbo V2 | 3.000 🪙 | Speed +40, Accel +30 | Tốc độ cao (3_CyberRace) |
| Off-Road Engine | 1.200 🪙 | Accel +15, Handling +10 | Đường trơn/cát (7_Rain · 8_Beach) |

### 4.2 Tires (Lốp)

| Linh kiện | Giá | Tác động | Phù hợp mặt đường |
|-----------|-----|----------|--------------|
| Stock Tires | 0 🪙 | — (mặc định) | Mọi mặt đường |
| Sport Tires | 500 🪙 | Grip +10, Handling +8 | Đường khô / tốc độ cao |
| Racing Tires | 1.200 🪙 | Grip +20, Speed +5, Handling +15 | Tốc độ cao (3_CyberRace) |
| Off-Road Tires | 800 🪙 | Grip +5, Braking +12 | Đường trơn/cát (7_Rain · 8_Beach) |
| Mud Tires | 1.500 🪙 | Grip +25 (trên bùn), Braking +20 | Đường trơn/cát (7_Rain · 8_Beach) |

### 4.3 Brakes (Phanh)

| Linh kiện | Giá | Tác động | Phù hợp mặt đường |
|-----------|-----|----------|--------------|
| Normal Brakes | 0 🪙 | — (mặc định) | Mọi mặt đường |
| Sport Brakes | 600 🪙 | Braking +15 | Đường khô / tốc độ cao |
| Racing Brakes | 1.400 🪙 | Braking +30 | Tốc độ cao (3_CyberRace) |
| Heavy Brakes | 900 🪙 | Braking +20, Speed −5 | Đường trơn/cát (7_Rain · 8_Beach) |

### 4.4 Suspension (Giảm Xóc)

| Linh kiện | Giá | Tác động | Phù hợp mặt đường |
|-----------|-----|----------|--------------|
| Stock Suspension | 0 🪙 | — (mặc định) | Mọi mặt đường |
| Sport Suspension | 700 🪙 | Handling +12, Grip +5 | Đường khô / tốc độ cao |
| Off-Road Suspension | 1.100 🪙 | Handling +20 (địa hình xấu) | Đường trơn/cát (7_Rain · 8_Beach) |

---

## 5. Yêu Cầu Mở Khóa Chặng Đua

Mỗi chặng đua có **ngưỡng chỉ số xe tối thiểu** để đảm bảo trải nghiệm cạnh tranh được, không bị AI bỏ xa ngay từ đầu.

| Scene | Yêu cầu (đề xuất) | Trạng thái gate trong code |
|-------|-------------------|----------------------------|
| **6_Stadium_Sunny** | Không yêu cầu | Khởi đầu |
| **8_Beach_Sunny** | Grip ≥ 55 (đề xuất) | Chưa gate (soft) |
| **7_Stadium_Rain** | Grip ≥ 60, Braking ≥ 55 (đề xuất) | Chưa gate (soft) |
| **3_CyberRace** | Speed ≥ 65, Accel ≥ 60 (đề xuất) | `LevelSettings.statRequirements` hiện = 0 (không gate) |
| **4_Racing_Circuit** | Cân bằng tổng hợp (đề xuất) | Chưa có asset settings |

> **Trạng thái thực tế:** hiện **chưa có gate cứng** — `LevelSettings_CyberRace.statRequirements` đặt = 0 và các
> scene Stadium dùng `RaceSettings` (không có field stat requirement). Cơ chế `LevelEligibility.Validate()`
> (xem `RaceStats_Architecture.md` §4) đã có sẵn để bật gate nếu muốn.
>
> **Thiết kế ý đồ:** Người chơi không bị "khóa cứng" — vẫn vào được mọi scene, nhưng vào scene khó với xe yếu
> sẽ về hạng thấp. Tạo **cảm giác tiến bộ** thay vì rào cản cứng nhắc.

---

## 6. Động Lực Người Chơi (Player Motivation)

### 6.1 Vòng Lặp Ngắn (Short Loop — mỗi phiên chơi)
> *"Tôi muốn về 1st để có thêm Gold."*

- Mỗi race kéo dài ~3–5 phút.
- Phần thưởng thấy ngay sau race — cảm giác tiến bộ tức thì.
- Mua ngay 1 linh kiện nhỏ → thấy sự khác biệt ở race tiếp theo.

### 6.2 Vòng Lặp Trung (Mid Loop — nhiều phiên)
> *"Tôi đang dành dụm để mua xe Furia Bianca rồi chinh phục 7_Stadium_Rain."*

- Linh kiện đắt hơn tạo mục tiêu tích luỹ.
- Unlock chặng mới là cột mốc rõ ràng để hướng tới.
- Thử nghiệm combo linh kiện khác nhau → xe "cảm giác" khác nhau.

### 6.3 Vòng Lặp Dài (Long Loop — hoàn thành game)
> *"Tôi muốn build xe hoàn hảo và về 1st ở cả các scene đua."*

- Bảng xếp hạng cá nhân (Personal Best) cho từng chặng.
- Thành tích "Clean Race" khuyến khích chơi sạch, không va chạm.
- Bộ sưu tập màu sơn / ngoại hình xe.

### 6.4 Vòng Phản Hồi Cảm Xúc

```
Mua linh kiện → Vào race → Cảm thấy xe mạnh hơn → Về thứ hạng tốt hơn
      ▲                                                        │
      └────────────────── Nhận thưởng Gold ───────────────────┘
                          (phản hồi tích cực)
```

---

## 7. Cân Bằng Kinh Tế (Economy Balance)

### Thời gian tích luỹ để mua linh kiện / xe cao cấp

Giả sử người chơi trung bình về **2nd** mỗi race ở 6_Stadium_Sunny (= **600 Gold**/race):
- Mở khóa xe **Mezzanotte X** (2000 Gold): cần ~4 race.
- Mở khóa xe **Furia Bianca** (5000 Gold): cần ~9 race.
- Một linh kiện cao cấp (~1.200–3.000 Gold): cần ~2–5 race.

Về **1st** ở 8_Beach_Sunny (1200 Gold) rút ngắn đáng kể thời gian tích lũy → khuyến khích chơi giỏi.

> **Nguyên tắc thiết kế:** Không quá dễ (mất cảm giác thành tích), không quá gian nan (gây nản). Mục tiêu: người chơi cảm thấy "gần đủ tiền" sau mỗi 2-3 race.

---

*Cập nhật: 2026-05-28 | Liên kết: [CarPart_Shop_Architecture.md](CarPart_Shop_Architecture.md) · [RaceStats_Architecture.md](RaceStats_Architecture.md)*
