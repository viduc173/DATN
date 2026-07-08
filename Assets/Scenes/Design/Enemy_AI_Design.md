# Thiết Kế Đối Thủ AI

> Tài liệu mô tả hành vi, cấp độ, và cá tính của các xe AI đối thủ trong từng scene đua.
>
> ⚠️ **Thực trạng vs thiết kế:** Phần **Tier** và **cá tính** (Rookie Blue, Shadow, Mudcrawler...) là
> **thiết kế ý tưởng** để định hướng. Trong code hiện tại, AI dùng chung script `Car_AI` (hệ
> `Off_Road_Racing`), được `Race_Manager` spawn đồng loạt với hành vi đồng nhất (chưa phân Tier/cá tính
> theo từng xe). Số lượng AI thực tế do `Race_Manager` trong mỗi scene quyết định.

---

## 1. Triết Lý Thiết Kế

Đối thủ AI không phải chướng ngại vật di chuyển — chúng là **nhân vật có cá tính**. Mỗi AI có phong cách lái riêng, phản ứng khác nhau với địa hình và tình huống. Người chơi học cách nhận biết từng đối thủ và điều chỉnh chiến thuật theo.

**3 nguyên tắc cốt lõi:**
1. **Dự đoán được nhưng không nhàm** — AI hành động nhất quán, người chơi có thể học patterns.
2. **Cạnh tranh thực sự** — AI không "đợi" người chơi, nhưng cũng không "siêu nhân" một cách vô lý.
3. **Sai lầm có chủ đích** — AI thỉnh thoảng mắc lỗi (ôm cua quá, trượt bùn) để tạo cảm giác chân thực và cơ hội cho người chơi.

---

## 2. Hệ Thống AI Hiện Tại

Script chính: [Car_AI.cs](../../Off_Road_Racing/Scripts/AI/Car_AI.cs)

### 2.1 Cơ Chế Di Chuyển

AI điều hướng theo **hệ thống waypoint** kết hợp **raycast cảm biến**:

```
┌─────────────────────────────────────────────────────┐
│                  AI DECISION LOOP                   │
│                                                     │
│  Waypoint Target ──▶ Tính hướng lái cơ bản         │
│                                                     │
│  Raycast Sensors:                                   │
│  ├── Corner Left/Right (4 units) — phát hiện góc  │
│  ├── Center (15°)                — phát hiện cua  │
│  ├── Front                       — vật cản phía trước│
│  └── Side                        — tránh xe bên   │
│            │                                        │
│            ▼                                        │
│  Điều chỉnh steering + throttle                    │
│            │                                        │
│            ▼                                        │
│  Reverse check (nếu tốc độ ≤ 10 km/h)             │
│  Respawn (nếu stuck ≥ 4s dưới 7 km/h)             │
└─────────────────────────────────────────────────────┘
```

### 2.2 Tính Năng Nitro (Racer_Nitro.cs)

AI ngẫu nhiên kích hoạt nitro mỗi ~5 giây. Có 3 cấp boost:

| Cấp | Hiệu ứng | Xác suất |
|-----|----------|---------|
| X1 | Mass / 2 (nhẹ hơn → tăng tốc) | 50% |
| X2 | Mass / 3 | 35% |
| X3 | Mass / 4 (boost cực mạnh) | 15% |

---

## 3. Cấp Độ Đối Thủ (AI Tiers)

### Tier 1 — Tay Đua Nghiệp Dư

> *"Bọn họ đua vì đam mê, không phải vì kinh nghiệm."*

- **Hành vi:** Đi theo đường waypoint cơ bản, braking muộn ở các cua, dễ bị overtake ở đoạn thẳng.
- **Điểm yếu:** Mất đà nhiều khi vào cua sắc; dễ bị respawn khi va chạm.
- **Xe tiêu biểu:** Xe stock, không lắp linh kiện.
- **Xuất hiện:** 6_Stadium_Sunny (phổ biến), 7_Stadium_Rain / 8_Beach_Sunny (50%).

**Thông số tiêu biểu:**
```
Speed:       40–55 / 100
Aggression:  Low
Nitro use:   Hiếm (mỗi 8–10s)
Error rate:  Cao (cua rộng, braking sớm)
```

### Tier 2 — Tay Đua Kinh Nghiệm

> *"Họ biết đường, biết xe, nhưng vẫn còn chỗ để bị bắt kịp."*

- **Hành vi:** Racing line khá tốt, braking đúng điểm, chủ động tạt sang khi bị áp sát.
- **Điểm yếu:** Phản ứng chậm khi bị vượt ở bên phải; thỉnh thoảng đi vào địa hình xấu.
- **Xe tiêu biểu:** 1–2 linh kiện (Sport Tires hoặc Sport Engine).
- **Xuất hiện:** 6_Stadium_Sunny (leader), 3_CyberRace (phổ biến), 7_Stadium_Rain / 8_Beach_Sunny (50%).

**Thông số tiêu biểu:**
```
Speed:       60–70 / 100
Aggression:  Medium
Nitro use:   Thỉnh thoảng (mỗi 5–7s)
Error rate:  Thấp
```

### Tier 3 — Tay Đua Chuyên Nghiệp

> *"Lạnh lùng, chính xác. Muốn thắng họ, bạn phải hoàn hảo."*

- **Hành vi:** Racing line tối ưu, braking late ở cuối đoạn thẳng, tranh line aggressively.
- **Điểm yếu:** Hiếm khi mắc lỗi, nhưng sẽ trật bánh trên bùn sâu nếu không có Off-Road Tires.
- **Xe tiêu biểu:** Full build — Turbo V2, Racing Tires, Racing Brakes.
- **Xuất hiện:** 3_CyberRace (1–2 xe đầu), 7_Stadium_Rain / 8_Beach_Sunny (1 xe đầu).

**Thông số tiêu biểu:**
```
Speed:       80–90 / 100
Aggression:  High
Nitro use:   Thường xuyên (mỗi 4–5s)
Error rate:  Rất thấp
```

---

## 4. Cá Tính Từng Đối Thủ

### 🔵 "Rookie Blue" — Tier 1
Xe màu xanh dương nhạt. Đua cẩn thận, nhường đường khi bị áp sát. Người chơi nên bắt đầu bằng cách luyện tập với Rookie Blue để hiểu cơ bản về racing line.

### 🟡 "Drifter Yellow" — Tier 2
Xe màu vàng cam. Có xu hướng **drift nhẹ** vào các cua — nhanh trên đường khô nhưng dễ trượt trên bùn. Thú vị để theo dõi và học kỹ thuật cua.

### 🔴 "Blaze Red" — Tier 2
Xe màu đỏ. Hung hăng nhất trong Tier 2 — chủ động block đường khi bị áp sát phía sau. Đây là đối thủ đầu tiên người chơi cần "phá vỡ" để cảm nhận được sự tiến bộ thực sự.

### ⚫ "Shadow" — Tier 3
Xe màu đen bóng. Lạnh lùng và không mắc sai lầm ở đường nhựa. Điểm yếu duy nhất: Shadow dùng Racing Tires (không có Off-Road) nên rất dễ trượt trên địa hình Off-Road. Trên 3_CyberRace, Shadow là kẻ cần đánh bại để về 1st.

### 🟢 "Mudcrawler" — Tier 3 (Off-Road chuyên biệt)
Xe màu xanh lá quân sự. Chỉ xuất hiện ở 7_Stadium_Rain / 8_Beach_Sunny. Độ bám đường cực cao, braking mạnh trên bùn. Gần như bất khả xâm phạm trên địa hình lầy lội, nhưng yếu rõ rệt trên đoạn đường nhựa ngắn xen kẽ trong track.

---

## 5. Phân Bố AI Theo Scene (đề xuất)

> Bảng dưới là **phân bố Tier đề xuất** theo độ khó scene — định hướng cho việc nâng cấp AI sau này.
> Hiện tại số AI thật do `Race_Manager` của từng scene quyết định, hành vi đồng nhất.

| Scene | Tier 1 | Tier 2 | Tier 3 | Tổng (đề xuất) |
|-------|--------|--------|--------|------|
| **6_Stadium_Sunny** (dễ) | 3 | 2 | 0 | 5 |
| **8_Beach_Sunny** (vừa) | 2 | 2 | 1 | 5 |
| **7_Stadium_Rain** (vừa–khó, trơn) | 2 | 2 | 1 (chuyên đường trơn) | 5 |
| **3_CyberRace** (khó, chưa vào game) | 1 | 3 | 2 | 6 |
| **4_Racing_Circuit** (khó, chưa vào game) | 1 | 2 | 2 | 5 |

---

## 6. Hành Vi Đặc Biệt

### 6.1 Respawn Thông Minh
Khi AI bị stuck > 4 giây (tốc độ ≤ 7 km/h), tự respawn về checkpoint gần nhất. Không "bay" về vị trí — xe xuất hiện với delay nhỏ để không tạo ra spawn kills.

### 6.2 Reverse Recovery
Khi tốc độ ≤ 10 km/h và bị chặn, AI tự lùi, điều chỉnh góc, rồi tiến lại. Thời gian reverse tối đa: 2 giây.

### 6.3 Nitro Timing
AI Tier 3 ưu tiên dùng nitro ở **đoạn thẳng dài** — không lãng phí ở cua. Tier 1 dùng nitro ngẫu nhiên, đôi khi ngay trước cua sắc → trượt ra ngoài → cơ hội cho người chơi.

---

## 7. Tương Lai (Planned)

| Tính năng | Mô tả | Ưu tiên |
|-----------|-------|---------|
| **Rubber-banding nhẹ** | Nếu player bị tụt hậu > 30s, AI Tier 3 giảm nitro usage | Medium |
| **Reaction to player** | AI phát hiện khi bị áp sát và chủ động defend line | High |
| **Weather adaptation** | AI Off-Road giảm tốc độ khi trời "mưa" (bùn dày hơn) | Low |
| **Memory of last race** | Shadow và Mudcrawler "nhớ" chỗ player hay vượt, thay đổi defensive line | Low |

---

*Cập nhật: 2026-05-28 | Scripts: [Car_AI.cs](../../Off_Road_Racing/Scripts/AI/Car_AI.cs) · [Racer_Nitro.cs](../../Off_Road_Racing/Scripts/Utility/Racer_Nitro.cs)*
