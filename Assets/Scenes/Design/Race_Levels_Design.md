# Thiết Kế Các Màn Đua (5 Scene Thật)

> Mô tả chi tiết bối cảnh, địa hình, thử thách và yêu cầu xe cho **từng scene đua thật** trong game.
> Số vòng (`totalLaps`) và tiền thưởng (`prizeByPosition`) lấy trực tiếp từ asset `RaceSettings_*` đi kèm scene.

---

## Tổng Quan

| # | Scene | File `.unity` | Bối cảnh | Độ khó | Laps | Thưởng top3 | Trạng thái |
|---|-------|--------------|----------|--------|------|-------------|-----------|
| 1 | **6_Stadium_Sunny** | `Assets/Scenes/6_Stadium_Sunny/Stadium_Sunny.unity` | Sân vận động, trời nắng | ⭐☆☆ Dễ | 1 | 1000 / 600 / 300 | Đang dùng (chuẩn) |
| 2 | **8_Beach_Sunny** | `Assets/Scenes/8_Beach_Sunny/Beach_Sunny.unity` | Bãi biển, trời nắng | ⭐⭐☆ Vừa | 1 | 1200 / 500 / 100 | Đang dùng |
| 3 | **7_Stadium_Rain** | `Assets/Scenes/7_Stadium_Rain/Stadium_Rain.unity` | Sân vận động, trời mưa (trơn) | ⭐⭐☆ Vừa–Khó | 1 | 1000 / 600 / 300 | Đang dùng |
| 4 | **3_CyberRace** | `Assets/Scenes/3_CyberRace/CyberRace.unity` | Thành phố cyberpunk, ban đêm | ⭐⭐⭐ Khó | 3 | 1000 / 600 / 300 | Chưa cho vào game |
| 5 | **4_Racing_Circuit** | `Assets/Scenes/4_Racing_Circuit/Racing_Circuit.unity` | Đường đua circuit chuyên nghiệp | ⭐⭐⭐ Khó | TBD | TBD | Chưa cho vào game |

> **Hệ kỹ thuật:** 6/7/8 dựng trên asset `Off_Road_Racing` (AI `Race_Manager`/`Car_AI`, xe player EVP `VehicleController`).
> Quy trình dựng: xem `OffRoadRacing_Scene_Refactor_Guide.md`. Scene 3 (CyberRace) dùng hệ xếp hạng tự viết
> (`RacePositionTracker`) — xem `3_CyberRace/README.md`.

---

## 1. 6_Stadium_Sunny — Sân Vận Động, Trời Nắng (scene khởi đầu / chuẩn)

> *"Nơi mọi tay đua bắt đầu. Ánh nắng, đường rõ, không có gì để đổ lỗi ngoài chính mình."*

### 1.1 Bối Cảnh & Không Khí
Đường đua trong một **sân vận động ngoài trời, trời nắng** — mặt đường khô, tầm nhìn tốt, ánh sáng mặt trời
gắt. Đây là scene **chuẩn tham chiếu (reference)** của dự án: đầy đủ pipeline nhất (load xe từ garage, bảng kết
quả + thưởng, bonus tăng tốc, spawn bonus theo vòng, nitro). Lý tưởng để người chơi làm quen điều khiển,
nitro (Shift), và nhặt bonus.

**Aesthetic:** ngoài trời, nắng, mặt sân khô.

### 1.2 Đặc Điểm
| Yếu tố | Chi tiết |
|--------|---------|
| **Mặt đường** | Khô, bám tốt — grip mặc định |
| **Số vòng** | 1 (`RaceSettings_Stadium.asset` → `totalLaps = 1`) |
| **Bonus** | Spawn **3 bonus/vòng** (`bonusesPerLap = 3`) gần checkpoint — ăn vào tăng tốc thật ~3s |
| **Kết thúc** | `autoLoadSceneOnFinish = false` → mở bảng kết quả Achievements, người chơi bấm nút về garage |

### 1.3 Thử Thách Chính
1. **Làm quen nitro:** giữ Shift để phun nitro — học quản lý nhiên liệu (cạn phải đợi hồi).
2. **Tận dụng bonus:** nhặt bonus rải quanh checkpoint để vọt qua AI.
3. **Về đích trong top 3** để nhận thưởng (1000 / 600 / 300 Gold).

### 1.4 Đề Xuất Xe
Xe stock vẫn hoàn thành tốt. Nâng cấp Engine (maxSpeed/accel) cho cảm giác bốc rõ nhất ở đường thẳng sân.

### 1.5 Phần Thưởng (thật)
| Vị trí | Gold |
|--------|------|
| 1st | 1000 |
| 2nd | 600 |
| 3rd | 300 |
| 4th+ | 0 (ngoài top3 không thưởng) |

> Tài liệu pipeline đầy đủ: `6_Stadium_Sunny/Stadium_CarLoad_Pipeline.md`, `Stadium_Results_Pipeline.md`,
> `Stadium_SpeedBoost_Pipeline.md`, `Stadium_BonusSpawn_Pipeline.md`, `Stadium_Nitro_Pipeline.md`.

---

## 2. 8_Beach_Sunny — Bãi Biển, Trời Nắng

> *"Cát, nắng và tốc độ. Mặt đường đổi liên tục — đừng tin vào bánh xe quá nhiều."*

### 2.1 Bối Cảnh & Không Khí
Đường đua ven **bãi biển trời nắng** — xen kẽ mặt đường cứng và đoạn cát/ẩm, cảnh biển hai bên. Độ khó nhỉnh
hơn Stadium Sunny: mặt đường thay đổi độ bám, đòi hỏi điều tiết ga/phanh tốt hơn. Thưởng hạng 1 cao nhất
trong các scene đang dùng (1200) — như một "chặng điểm cao".

**Aesthetic:** biển, nắng, cát vàng, nước xanh.

### 2.2 Đặc Điểm
| Yếu tố | Chi tiết |
|--------|---------|
| **Mặt đường** | Hỗn hợp đường cứng + đoạn cát/ẩm (grip biến thiên) |
| **Số vòng** | 1 (`RaceSettings_Beach_Sunny.asset` → `totalLaps = 1`) |
| **Bonus** | **3 bonus/vòng** (`bonusesPerLap = 3`) |
| **Kết thúc** | `autoLoadSceneOnFinish = false` → bảng kết quả Achievements |

### 2.3 Thử Thách Chính
1. **Quản lý grip trên cát:** vào cua sớm hơn, tránh phanh gấp trên đoạn trơn.
2. **Tốc độ đường thẳng ven biển:** tận dụng nitro + bonus.
3. **Cạnh tranh phần thưởng cao:** top 3 ăn 1200 / 500 / 100.

### 2.4 Đề Xuất Xe
Lốp grip cao (Tires) + Suspension giúp ổn định trên đoạn cát; Engine cho tốc độ đường thẳng.

### 2.5 Phần Thưởng (thật)
| Vị trí | Gold |
|--------|------|
| 1st | 1200 |
| 2nd | 500 |
| 3rd | 100 |
| 4th+ | 0 |

---

## 3. 7_Stadium_Rain — Sân Vận Động, Trời Mưa

> *"Cùng đường đua, nhưng nước mưa biến mọi sai lầm thành cú trượt dài."*

### 3.1 Bối Cảnh & Không Khí
Biến thể **trời mưa** của sân vận động — mặt đường **ướt, trơn** (grip giảm), tầm nhìn giảm nhẹ, phản chiếu đèn
trên mặt nước. Cùng bố cục track với Stadium Sunny nhưng khó hơn rõ rệt do mất bám: phanh muộn → trượt, vào
cua nhanh → văng.

**Aesthetic:** sân vận động, mưa, mặt đường bóng nước, tông xám-xanh.

### 3.2 Đặc Điểm
| Yếu tố | Chi tiết |
|--------|---------|
| **Mặt đường** | Ướt, trơn — grip thực tế thấp hơn Stadium Sunny |
| **Số vòng** | 1 (`RaceSettings_Stadium_Rain.asset` → `totalLaps = 1`) |
| **Kết thúc** | `autoLoadSceneOnFinish = false` → bảng kết quả Achievements |

### 3.3 Thử Thách Chính
1. **Phanh sớm hơn:** khoảng cách phanh dài hơn trên đường ướt — Brakes tốt giúp nhiều.
2. **Giữ grip:** lốp grip cao quan trọng hơn ở đây so với Stadium Sunny.
3. **Hạn chế nitro vào cua:** phun nitro giữa cua dễ trượt văng.

### 3.4 Đề Xuất Xe
**Tires (grip)** + **Brakes** ưu tiên cao; Suspension giúp ổn định; tránh build chỉ thiên tốc độ.

### 3.5 Phần Thưởng (thật)
| Vị trí | Gold |
|--------|------|
| 1st | 1000 |
| 2nd | 600 |
| 3rd | 300 |
| 4th+ | 0 |

---

## 4. 3_CyberRace — Thành Phố Tương Lai (chưa cho vào game)

> *"Đêm neon, tốc độ ánh sáng. Nếu bạn chưa nâng cấp xe — đừng đến đây."*

### 4.1 Bối Cảnh & Không Khí
Đường cao tốc **đô thị tương lai ban đêm** — neon, hologram quảng cáo, nhà kính cao tầng. Tốc độ cao nhất trong
các scene; racing line và braking point quan trọng. **3 vòng đua** (dài nhất).

**Aesthetic:** cyberpunk, neon tím/xanh, đường nhựa bóng.

### 4.2 Đặc Điểm
| Yếu tố | Chi tiết |
|--------|---------|
| **Mặt đường** | Nhựa, tốc độ cao |
| **Số vòng** | **3** (`RaceSettings_CyberRace.asset` / `LevelSettings_CyberRace.asset` → `totalLaps = 3`) |
| **Hệ kỹ thuật** | Hệ xếp hạng tự viết (`RacePositionTracker` + `LevelController` + `LevelSettings`) — xem `3_CyberRace/README.md` |
| **Kết thúc** | `autoLoadSceneOnFinish = true` → tự về `GarageLobby_pc` sau `loadSceneDelay = 3` giây |
| **Stat gate** | `LevelSettings_CyberRace.statRequirements` hiện = 0 (không gate cứng) |

### 4.3 Thử Thách Chính
1. **S-curve tốc độ cao:** cần Turbo/Engine + lốp grip để giữ line.
2. **3 vòng:** quản lý nitro cho cả chặng dài, không phun cạn sớm.
3. **Braking ở tốc độ cao:** Racing Brakes quan trọng.

### 4.4 Đề Xuất Xe
Engine (maxSpeed + accel) ⭐⭐⭐, Tires (grip) ⭐⭐⭐, Brakes ⭐⭐⭐, Suspension ⭐⭐.

### 4.5 Phần Thưởng
Dùng default `RaceSettings` (1000 / 600 / 300 top3) trừ khi chỉnh `prizeByPosition` trong asset.

---

## 5. 4_Racing_Circuit — Đường Đua Circuit (chưa cho vào game)

> *"Đường đua tiêu chuẩn: không cát, không mưa, không neon — chỉ có bạn và đồng hồ bấm giờ."*

### 5.1 Bối Cảnh & Không Khí
Đường đua **circuit chuyên nghiệp** (khép kín, nhiều loại cua) — sạch, rõ, thiên về kỹ thuật racing line thuần
túy. Chưa có asset `RaceSettings_*` đi kèm (cần tạo khi đưa vào game).

**Aesthetic:** trường đua hiện đại, khán đài, mặt đường nhựa.

### 5.2 Trạng Thái & Việc Cần Làm
| Hạng mục | Tình trạng |
|----------|-----------|
| Scene `.unity` | Đã có (`Racing_Circuit.unity`) |
| `RaceSettings_RacingCircuit.asset` | ❌ Chưa tạo — cần để cấu hình laps/thưởng/bonus |
| Pipeline (load xe, kết quả, bonus, nitro) | ❌ Chưa wire (theo khuôn `OffRoadRacing_Scene_Refactor_Guide.md`) |
| Thêm vào Build Settings | ❌ Chưa |

### 5.3 Đề Xuất Thiết Kế (khi đưa vào game)
- Số vòng 2–3 (circuit ngắn → nhiều vòng).
- Cân bằng mọi stat — đây là chặng "đo tổng hợp" build xe.
- Thưởng tương đương Cyber/Stadium (1000 / 600 / 300).

---

## 6. So Sánh Các Scene Đang Dùng

| | 6_Stadium_Sunny | 8_Beach_Sunny | 7_Stadium_Rain |
|--|-----------------|---------------|----------------|
| **Mặt đường** | Khô, bám tốt | Hỗn hợp cát/cứng | Ướt, trơn |
| **Độ khó** | Dễ | Vừa | Vừa–Khó |
| **Yếu tố quyết định** | Tốc độ + nitro | Quản lý grip | Phanh + grip |
| **Stat ưu tiên** | maxSpeed/accel | grip + handling | braking + grip |
| **Laps** | 1 | 1 | 1 |
| **Thưởng 1st** | 1000 | 1200 | 1000 |

---

## 7. Thứ Tự Chơi Đề Xuất

```
6_Stadium_Sunny     8_Beach_Sunny       7_Stadium_Rain        (tương lai)
(làm quen, nitro) ─▶ (grip trên cát) ─▶ (đường trơn)    ─▶  3_CyberRace / 4_Racing_Circuit
       │                  │                   │
       │ Engine cơ bản    │ Tires + Susp       │ Brakes + Tires grip cao
       ▼                  ▼                   ▼
   Học điều khiển      Học điều tiết        Làm chủ xe trên
   + nhặt bonus        ga/phanh             mặt đường khó
```

**Nguyên tắc:** không scene nào bị "lock cứng" (hiện `statRequirements = 0`), nhưng vào scene khó với xe chưa
nâng cấp sẽ về hạng thấp liên tục — đủ tạo động lực mua linh kiện.

---

*Cập nhật: 2026-06-04 | Liên kết: `Enemy_AI_Design.md` · `Economy_Reward_System.md` · `ScreenFlow_GameProgress.md` · `OffRoadRacing_Scene_Refactor_Guide.md`*
