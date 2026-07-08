# 05 — Lõi dữ liệu: Stats, Linh kiện, Kinh tế, AI

> Lớp **dữ liệu & cân bằng** dùng chung cho cả bản PC lẫn VR (KHÔNG phụ thuộc cách điều khiển). Đây là phần
> nên dùng cho **chương kiến trúc hệ thống** của đồ án.
>
> **Nguồn:** `Design/RaceStats_Architecture.md`, `Design/Loadout_Stats_Balancing.md`,
> `Design/CarPart_Shop_Architecture.md`, `Design/Economy_Reward_System.md`, `Design/Race_Levels_Design.md`,
> `Design/Enemy_AI_Design.md`, `Design/Shop_Inventory UI_Design/Shop_Inventory_System_FULL.md`.

---

## 1. Mô hình dữ liệu (ScriptableObject)

| Loại | File | Vai trò |
|---|---|---|
| `CarStats` | `Assets/Script/Interact Tool/CarStats.cs` | Struct 6 chỉ số 0–100: `maxSpeed, acceleration, grip, braking, handling, nitro`. Operator +/− tự clamp. |
| `CarPart` | `Assets/Script/CarPart.cs` | 1 linh kiện: slot (Engine/Wheels/Brakes/Suspension/Body/Aero/Other/ECU/Paint), `statBonus`, `costGold`, `tier`, prefab. |
| `PlayerCarLoadout` | `Assets/Script/PlayerCarLoadout.cs` | "Xe + part lắp": `baseStats + Σ parts = GetEffectiveStats()`; `paint`. |
| `PlayerInventory` | `Assets/Script/PlayerInventory.cs` | Kho + ví gold. |
| `ShopCatalog` | `Assets/Script/Garage/ShopCatalog.cs` | Danh mục bán. |
| `RaceSettings` / `LevelSettings` | `Assets/Script/RaceSettings.cs` / `LevelSettings.cs` | Cấu hình 1 màn (laps, end scene, prize, bonusesPerLap, stat requirements). |

**Quy ước stat:** **50 = baseline xe gốc** (y hệt prefab `VehicleController`), 0 = tệ nhất chơi được, 100 = max tier.
**`statBonus.X = 0` = không override** (cộng 0 vào tổng, không gây bug).

---

## 2. Mapping 6 stat (0–100) → physics EVP `VehicleController`

> ⚠️ Có HAI bản mapping trong tài liệu nguồn: bản **đang chạy** (`Loadout_Stats_Balancing.md §1`, theo
> `LevelController.cs` hiện tại) và bản **refactor đề xuất** (`RaceStats_Architecture.md §3/§9`, composite +
> clamp). Khi mô tả hệ **thực tế**, dùng bảng dưới (bản đang chạy).

| Stat 0–100 | Field VehicleController | tại 0 | tại 50 (gốc) | tại 100 | +1 điểm (trên 50) ≈ |
|---|---|---|---|---|---|
| `maxSpeed` | `maxSpeedForward` | 13.89 m/s | 27.78 m/s (100 km/h) | 55.56 m/s (200 km/h) | +2 km/h |
| `acceleration` | `maxDriveForce` | 1000 N | 2000 N | 5000 N | +60 N |
| `grip` | `tireFriction` | 0.5 | 1.0 | 1.5 | +0.01 |
| `braking` | `maxBrakeForce` | 1000 N | 3000 N | 6000 N | +60 N |
| `handling` | `maxSteerAngle` | 20° | 35° | 55° | +0.4° |
| `nitro` | *(không map lái — `NitroController` đọc)* | — | — | — | bình + hồi nitro |

- **Áp dụng:** `LevelController` (execOrder -100) `Awake()` đọc `ActiveLoadout.Current.GetEffectiveStats()` →
  `ApplyStatsTo(vehicleController)`. (Bản refactor đề xuất dùng cache base + Lerp ×0.5..×2.0 + clamp tuyệt đối +
  composite grip/handling — xem `RaceStats_Architecture.md`.)
- ⚠️ **BẪY cộng ×4:** Wheels & Brakes có **4 cái/xe → statBonus cộng 4 lần**. Engine/Suspension/ECU = 1 cái.
  Mỗi part wheel/brake phải ≈ **1/4** mức tăng tổng mong muốn.
- **Lever cảm nhận mạnh nhất:** `maxSpeed` + `acceleration` → ưu tiên Engine.

---

## 3. Stat thứ 6 — `nitro` (`Loadout_Stats_Balancing.md §6`)

`NitroController.RecomputeFromStats()` quy đổi stat loadout → thông số nitro (không map sang lái):
```
maxCharge    = Lerp(1.2, 3.2, nitro/100)   (giây bình)        ← CAR base + ECU
rechargeRate = Lerp(0.02, 0.8, nitro/100)  (charge/giây)      ← CAR base + ECU
nitroAccel   = MapStat(accel, 4, 8, 16) + 2×(grip/100)        ← ENGINE + (nhỏ) TIRES
speedMulti   = 1.125 + 0.05×(grip/100)                         ← base + (nhỏ) TIRES
```
- **Base nitro mỗi xe:** Azzurro Scout 15 · Mezzanotte X 12 · Furia Bianca 20.
- **Chỉ lắp nitro trên ECU + Engine** (không trên Wheels/Brakes vì cộng ×4 → quá mạnh).
- Giá trị `statBonus.nitro` đã áp: ECU_Stage1 +12, ECU_Stage2 +22, ECU_Race +32; Engine_Sport +5, Engine_Racing +8.

---

## 4. Kinh tế & phần thưởng (`Economy_Reward_System.md`)

### 4.1 Đơn vị & công thức
- Đơn vị: **Gold**. Thưởng theo hạng lưu ở `RaceSettings.prizeByPosition` (int[], index 0 = hạng 1).
- `RaceResultsController.GrantReward()`: `prize = GetPrize(finishPosition)` → `inventory.AddGold(prize)` →
  `SaveToPlayerPrefs()`. **Chỉ Player có ví** (AI không nhận thưởng).

### 4.2 Bảng thưởng từng màn đua
| Màn | Asset RaceSettings | Laps | 1st | 2nd | 3rd | 4th+ | bonus/lap | autoLoad |
|---|---|---|---|---|---|---|---|---|
| **6_Stadium_Sunny** | RaceSettings_Stadium | 1 | 1000 | 600 | 300 | 0 | 3 | false |
| **7_Stadium_Rain** | RaceSettings_Stadium_Rain | 1 | 1000 | 600 | 300 | 0 | — | false |
| **8_Beach_Sunny** | RaceSettings_Beach_Sunny | 1 | **1200** | 500 | 100 | 0 | 3 | false |
| **3_CyberRace** | RaceSettings_CyberRace | **3** | 1000 | 600 | 300 | 0 | — | true (3s) |
| **2/4_Racing_Circuit** | *chưa tạo* | TBD | TBD | — | — | — | — | — |

### 4.3 Cân bằng (giả định về 2nd ở Stadium = 600g/race)
- Mua xe Mezzanotte X (2000g): ~4 race. Furia Bianca (5000g): ~9 race. Part cao cấp (1200–3000g): ~2–5 race.
- Ý đồ: "gần đủ tiền" sau 2–3 race — không quá dễ, không quá nản.

---

## 5. Ba xe player

| Tên | CarType | Giá | base nitro | Ghi chú |
|---|---|---|---|---|
| **Azzurro Scout** | CarType0 | 0g | 15 | Owned mặc định |
| **Mezzanotte X** | CarType1 | 2000g | 12 | Cần mua, thiên tốc độ |
| **Furia Bianca** | CarType2 | 5000g | 20 | Cần mua, cân bằng cao |

> Asset loadout: `Assets/Data/Loadouts/`. Mua xe qua `PlayerInventory.TryBuyLoadout` (lưu `ownedLoadouts`).

---

## 6. Danh mục linh kiện & giá (`Economy_Reward_System.md`)

> Giá/stat dưới là **thiết kế** trong tài liệu; giá trị thực nằm trong asset `Assets/Data/CarParts/*` +
> `ShopCatalog` (`GaragePartsShop.asset`). Wheels = 4 chiếc/set, Brakes = 4 caliper (mua +2/lần).

**Engine (1 part):** Stock 0 · Sport 800 (spd+15,acc+10) · Turbo V1 1500 (spd+25,acc+20) · Turbo V2 3000 (spd+40,acc+30) · Off-Road 1200 (acc+15,handling+10)
**Tires (×4):** Stock 0 · Sport 500 (grip+10,hand+8) · Racing 1200 (grip+20,spd+5,hand+15) · Off-Road 800 (grip+5,brk+12) · Mud 1500 (grip+25,brk+20)
**Brakes (×4):** Normal 0 · Sport 600 (brk+15) · Racing 1400 (brk+30) · Heavy 900 (brk+20, spd−5)
**Suspension (1):** Stock 0 · Sport 700 (hand+12,grip+5) · Off-Road 1100 (hand+20)
**ECU (1):** Stock 0 · Stage1 (acc+4,spd+3,nitro+12) · Stage2 (acc+8,spd+6,nitro+22) · Race (acc+12,spd+9,nitro+32)
**Paint/Spray (slot Paint):** Black/Blue/Grey/Orange/Red/White — cosmetic, không ảnh hưởng stat.

> Khuyến nghị statBonus theo tier (để "mạnh lên thấy rõ", xe full tier-3 đạt ~77–85 ở stat chuyên biệt):
> xem bảng chi tiết trong `Design/Loadout_Stats_Balancing.md` §4–§5.

---

## 7. Đối thủ AI (`Enemy_AI_Design.md`)

### 7.1 Cơ chế (ĐÃ code — `Car_AI.cs` hệ Off_Road_Racing)
```
Waypoint target → hướng lái cơ bản
   + Raycast sensors: Corner L/R, Center (15°), Front, Side → điều chỉnh steering/throttle
   + Reverse recovery: tốc độ ≤ 10 km/h & bị chặn → lùi + chỉnh + tiến (max 2s)
   + Respawn: stuck ≥ 4s (≤ 7 km/h) → respawn checkpoint gần nhất
Nitro AI (Racer_Nitro.cs): X1 (Mass/2, 50%) · X2 (Mass/3, 35%) · X3 (Mass/4, 15%)
Race_Manager: spawn AI đồng loạt, RegisterAIRacer(.., "AI {i}").
```

### 7.2 Thiết kế Tier/cá tính (CHƯA code — ý tưởng cho đồ án)
| Tier | Cá tính | Speed | Aggression | Nitro | Xe |
|---|---|---|---|---|---|
| 1 Nghiệp dư | Rookie Blue 🔵 | 40–55 | Low | hiếm (8–10s) | Stock |
| 2 Kinh nghiệm | Drifter Yellow 🟡 / Blaze Red 🔴 | 60–70 | Medium | 5–7s | 1–2 part |
| 3 Chuyên nghiệp | Shadow ⚫ / Mudcrawler 🟢 | 80–90 | High | 4–5s | Full build |

> Hiện code dùng **AI đồng nhất**; phân Tier/cá tính + phân bố theo chặng (Stadium dễ → Cyber khó) là thiết kế chưa triển khai.

---

## 8. Hạn chế / hướng phát triển (toàn hệ)
- `LevelController` hiện override tuyệt đối; bản refactor (relative + clamp + composite grip/handling) chưa áp.
- AI Tier/cá tính chưa code; `2_Racing_Circuit` chưa wire RaceSettings; soft-gate stat chưa khóa cứng.
- Persistence: còn gap gọi `PlayerInventory.LoadFromPlayerPrefs()` lúc boot.
- Bonus đặc biệt (clean race, personal best, first-clear) mới là ý tưởng.

## 9. Cần biết thêm thì xem đâu
- Kiến trúc stat đầy đủ + mapping refactor: `Design/RaceStats_Architecture.md`.
- Cân bằng stat thực tế + nitro: `Design/Loadout_Stats_Balancing.md`.
- Linh kiện/shop (data + scene): `Design/CarPart_Shop_Architecture.md` + `Design/Shop_Inventory UI_Design/Shop_Inventory_System_FULL.md`.
- Kinh tế/thưởng: `Design/Economy_Reward_System.md`. Thiết kế màn đua: `Design/Race_Levels_Design.md`. AI: `Design/Enemy_AI_Design.md`.
