# 00 — MASTER README (Tổng hợp toàn bộ tài liệu thiết kế)

> **Mục đích:** File chỉ mục tổng (master index) gom **TẤT CẢ** tài liệu `.md`, scene, và asset
> cấu hình của dự án game đua xe **Furia Rush** (Unity 6, VHD_DATN_V2). Dùng để:
> 1. Cung cấp đủ ngữ cảnh viết **quyển đồ án** (báo cáo tốt nghiệp).
> 2. Cho một AI khác **đọc theo đường dẫn full path** và tổng hợp thành thiết kế hệ thống.
>
> **Quy ước đường dẫn:** mọi đường dẫn ghi dạng tuyệt đối từ gốc máy
> `e:\Workspace\Unity\VHD_DATN_V2\...`. AI/người đọc có thể mở trực tiếp.
>
> **Tổng quan thiết kế** được phỏng theo nhóm tài liệu trong `6_Stadium_Sunny` (các pipeline)
> và folder `Assets\Scenes\Design`.
>
> *Cập nhật: 2026-06-04*

---

## 1. Tóm tắt game

**Furia Rush** — game đua xe 3D (Unity 6), hỗ trợ điều khiển PC (chuột/bàn phím) và VR (XR Toolkit).
Vòng lặp cốt lõi: **Garage (tùy chỉnh/mua xe + linh kiện) → Chọn chặng đua → Đua → Nhận thưởng Gold → quay lại Garage**.

Ba trụ hệ thống:
- **Garage / Shop / Inventory:** mua xe + linh kiện, lắp/tháo vật lý (bánh xe, phanh), sơn xe, đổi xe.
- **Stats & Loadout:** mỗi xe có 6 chỉ số 0–100 (maxSpeed, acceleration, grip, braking, handling, nitro);
  linh kiện cộng/trừ chỉ số; khi vào race map sang physics EVP `VehicleController`.
- **Race:** đếm ngược → đua → xếp hạng theo checkpoint/lap → kết thúc → bảng kết quả + thưởng.
  Thêm cơ chế bonus tăng tốc, nitro (Shift), spawn bonus theo vòng.

---

## 2. Luồng màn hình (Scene Flow) — dự kiến trong game

```
   1_SlashScene  ──▶  5_GarageLobby_pc  ──▶  ┌─ 4_Racing_Circuit  (đường đua #1, CHƯA vào game)
   (splash/start)     (hub: shop, tune,      ├─ 3_CyberRace       (đường đua #2, CHƯA vào game)
                       chọn xe, chọn chặng)   ├─ 6_Stadium_Sunny   (đang dùng — scene chuẩn tham chiếu)
                            ▲                  ├─ 7_Stadium_Rain    (biến thể mưa)
                            │                  └─ 8_Beach_Sunny     (biển, nắng)
                            └──────────────────────  (về đích / thoát → quay lại Garage)
```

**Thứ tự:** `1 → 5 → (4 / 3 / 6 / 7 / 8)`.
- Mọi scene đua khi kết thúc đều quay về `5_GarageLobby_pc` (qua `RaceSettings.endSceneName`).
- `6_Stadium_Sunny` là **scene đua chuẩn (reference)** — đầy đủ pipeline nhất; các scene đua khác phỏng theo nó.

---

## 3. Catalog SCENE (đường dẫn full + trạng thái)

| Scene | Đường dẫn file `.unity` | Vai trò | Trạng thái |
|---|---|---|---|
| **1_SlashScene** | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\1_SlashScene\SlashScene.unity` | Màn splash / khởi động (vào đầu game) | Đang dùng |
| **5_GarageLobby_pc** | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\5_GarageLobby_pc\GarageLobby_pc.unity` | Hub trung tâm: shop, inventory, tune xe, sơn, chọn xe, chọn chặng | Đang dùng (chính) |
| **4_Racing_Circuit** | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\4_Racing_Circuit\Racing_Circuit.unity` | Đường đua #1 (circuit) | **CHƯA cho vào game** |
| **3_CyberRace** | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\3_CyberRace\CyberRace.unity` | Đường đua #2 (cyberpunk) | **CHƯA cho vào game** |
| **6_Stadium_Sunny** | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\6_Stadium_Sunny\Stadium_Sunny.unity` | Đua sân vận động, trời nắng — **scene chuẩn tham chiếu** | Đang dùng |
| **7_Stadium_Rain** | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\7_Stadium_Rain\Stadium_Rain.unity` | Biến thể mưa của Stadium | Đang dùng |
| **8_Beach_Sunny** | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\8_Beach_Sunny\Beach_Sunny.unity` | Đua bãi biển, trời nắng | Đang dùng |

**Scene phụ / không nằm trong luồng chính:** `0_BackUpScene`, `2_GarageLobby`, `SampleScene.unity`, `Testing Scene`
(thư mục `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\`).

### Asset cấu hình race đi kèm scene (`RaceSettings` / `LevelSettings`)

| Asset | Đường dẫn full |
|---|---|
| RaceSettings (CyberRace) | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\3_CyberRace\RaceSettings_CyberRace.asset` |
| LevelSettings (CyberRace) | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\3_CyberRace\LevelSettings_CyberRace.asset` |
| RaceSettings (Stadium Sunny) | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\6_Stadium_Sunny\RaceSettings_Stadium.asset` |
| RaceSettings (Stadium Rain) | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\7_Stadium_Rain\RaceSettings_Stadium_Rain.asset` |
| RaceSettings (Beach Sunny) | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\8_Beach_Sunny\RaceSettings_Beach_Sunny.asset` |

---

## 4. Catalog TÀI LIỆU THIẾT KẾ (folder `Assets\Scenes\Design`)

> Đây là tầng "thiết kế hệ thống" — đọc theo nhóm dưới đây để hiểu kiến trúc tổng.

### 4.1. Thiết kế gameplay & game design (đọc trước)

| # | File (full path) | Nội dung cốt lõi |
|---|---|---|
| D1 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\ScreenFlow_GameProgress.md` | **Luồng màn hình + tiến trình game.** 1_SlashScene → 5_GarageLobby_pc → (4/3/6/7/8) → bảng kết quả → Garage. Sơ đồ core loop, điều kiện chuyển màn. **Đã cập nhật theo 5 scene thật.** |
| D2 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Race_Levels_Design.md` | **Thiết kế 5 scene đua thật** (6_Stadium_Sunny, 8_Beach_Sunny, 7_Stadium_Rain, 3_CyberRace, 4_Racing_Circuit): bối cảnh, mặt đường, thử thách, đề xuất linh kiện, **laps + thưởng thật** từ asset RaceSettings, thứ tự chơi đề xuất. |
| D3 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Enemy_AI_Design.md` | **Thiết kế đối thủ AI:** triết lý, cơ chế waypoint + raycast, nitro AI (Racer_Nitro), 3 Tier (Nghiệp dư/Kinh nghiệm/Chuyên nghiệp), 5 cá tính (Rookie Blue, Drifter Yellow, Blaze Red, Shadow, Mudcrawler), phân bố theo chặng, hành vi đặc biệt (respawn, reverse recovery). |
| D4 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Economy_Reward_System.md` | **Kinh tế & phần thưởng:** Gold/XP, **cơ chế thưởng thật** (`prizeByPosition` cố định theo hạng + bảng thưởng thật từng scene), danh mục linh kiện + giá, mở khóa scene (soft, chưa gate cứng), động lực người chơi, cân bằng kinh tế. |

### 4.2. Kiến trúc kỹ thuật hệ stats / linh kiện (lõi gameplay)

| # | File (full path) | Nội dung cốt lõi |
|---|---|---|
| D5 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\RaceStats_Architecture.md` | **Kiến trúc tổng hệ chỉ số xe.** Data model: `CarStats` (5 stat 0–100), `CarPart`, `PlayerCarLoadout`, `LevelSettings`. **Mapping abstract 0–100 → physics `VehicleController`** (Option C: relative-to-baseline + clamp, có composite grip/handling). Eligibility gate, flow Garage→Race, persistence options, extension points, refactor plan. ⚠️ §3/§9 là bản refactor **đề xuất** — xem D6 cho mapping THỰC TẾ đang chạy. |
| D6 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Loadout_Stats_Balancing.md` | **Cân bằng stats linh kiện (THỰC TẾ).** Mapping hiện tại trong `LevelController.cs` (1 điểm stat = bao nhiêu "lực"). ⚠️ **BẪY wheels/brakes cộng ×4**. Bảng `statBonus` khuyến nghị theo slot×tier, nguyên tắc "mạnh lên thấy rõ". **§6: stat thứ 6 `nitro`** và cách `NitroController` quy đổi stat→nitro; giá trị đã áp vào `Assets/Data`. |
| D7 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\CarPart_Shop_Architecture.md` | **Hệ linh kiện & mua sắm (data + scene layer).** `CarPart` / `PlayerCarLoadout` / `PlayerInventory`; hierarchy `CartPartPlace/Wheels`; per-car loadout memory (`CarLoadoutSlot`); component trên WheelItem; luồng mua; checklist thêm bánh/xe mới. |

### 4.3. Hướng dẫn quy trình scene đua

| # | File (full path) | Nội dung cốt lõi |
|---|---|---|
| D8 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\OffRoadRacing_Scene_Refactor_Guide.md` | **Quy trình refactor scene demo Off_Road_Racing → scene đua custom.** So sánh scene gốc vs custom, 7 root group, xoá objects thừa, thêm nhóm System (RacePositionTracker/MatchWaitTime/RaceCountDown) + PlayerCarManager + UIController HUD, 33 scripts thêm, **quy trình 10 bước**, các lỗi thường gặp. Case study: `1_Stadium_Sunny.unity` → `Stadium_Sunny.unity`. |

> *(Ghi chú: D8 tham chiếu `OffRoad_Scene_Conversion_Recipe.md` — quy trình 6 bước về Race_Manager/AI; nội dung đó cũng được tóm trong auto-memory `offroad_scene_conversion_recipe.md`.)*

### 4.4. Folder con UI Shop / Inventory — `Assets\Scenes\Design\Shop_Inventory UI_Design`

> ⭐ **Đọc nhanh nhất:** file FULL (S-FULL) là bản self-contained tổng hợp toàn bộ hệ Shop/Inventory.

| # | File (full path) | Nội dung |
|---|---|---|
| S0 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Shop_Inventory UI_Design\README.md` | Index nội bộ folder + thứ tự đọc + phạm vi "đã làm / chưa code". |
| **S-FULL** | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Shop_Inventory UI_Design\Shop_Inventory_System_FULL.md` | **TỔNG HỢP toàn bộ:** data layer (CarPart/PlayerCarLoadout/ShopCatalog/PlayerInventory), scene MonoBehaviours (ShopUIController, InventoryUIController, GaragePartStaging, PartInventoryBridge, GarageCarManager, PlayerMoneyText, GarageDisplayedCarContext), luồng lắp xe vật lý, dev tool Reset, TODO/persistence gap. |
| S1 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Shop_Inventory UI_Design\Garage_UI_Design.md` | Design tổng UI garage: part model, inventory/shop data, flow mua+equip, layout 3 panel (Part Slots / Inventory / Shop). |
| S2 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Shop_Inventory UI_Design\UI_Design.md` | Brief gốc Shop Button. |
| S3 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Shop_Inventory UI_Design\UI_Design_Note.md` | Đối chiếu brief shop với scene/code thật + caveat `ShopButtonManager`. |
| S4 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Shop_Inventory UI_Design\Shop_UI_Implementation.md` | Đã triển khai: ShopUIController (tự sinh nút) + PlayerMoneyText. |
| S5 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Shop_Inventory UI_Design\UI_Design_Inventory.md` | Brief gốc Inventory (Item Show Button: send/return, equip/unequip). |
| S6 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Shop_Inventory UI_Design\UI_Design_Inventory_Note.md` | Đối chiếu brief inventory + gap. |
| S7 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Shop_Inventory UI_Design\Inventory_UI_Implementation.md` | Đã triển khai: InventoryUIController + GaragePartStaging + UnequipOwnedPart. |
| S8 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Shop_Inventory UI_Design\Inventory_UI_Fixes.md` | Fix: nút đổi state, lắp/tháo bánh ±inventory (PartInventoryBridge), spawn trọng lực, brake per-caliper swap mesh theo side. |
| S9 | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\Shop_Inventory UI_Design\UI_Design_CarInfo.md` | CarInfo (read-only): car card + stats + part list (`CarInfoUIController`). |

> ✅ **Đã hợp nhất:** trước đây có 2 bản `Garage_UI_Design.md` (một ở `Design\`, một trong folder con) chỉ khác
> format bảng. Bản ngoài đã xóa; bản chuẩn duy nhất là S1 trong folder con (đường dẫn ở hàng S1 trên).

---

## 5. Catalog TÀI LIỆU KỸ THUẬT THEO SCENE

### 5.1. Scene `3_CyberRace`

| File (full path) | Nội dung |
|---|---|
| `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\3_CyberRace\README.md` | Báo cáo + hướng dẫn scene CyberRace: hệ `LevelSettings`/`LevelController` (master config per scene), luồng dữ liệu loadout→physics, hệ xếp hạng tự viết (`RacePositionTracker`), `RaceSettings`, lap detection (forward/backward wrap), checkpoint trigger, **danh sách bug đã sửa** (B1–B12, countdown UI, driving). |

### 5.2. Scene `5_GarageLobby_pc`

| File (full path) | Nội dung |
|---|---|
| `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\5_GarageLobby_pc\GarageLobby_PC_Systems.md` | **Hệ thống tương tác garage PC.** (1) Bánh xe attach/detach (WheelSocket/WheelItem, ghost preview, layer 7/11, luồng F-key + chuột trái, bug đã gặp + fix). (2) Sơn xe (CarPaintCan, active-car resolve qua GarageCarManager, bảng màu GUID). (3) Engine/Suspension/ECU (data-only), UI_CarStats live stats, BrakeCaliper (per-side), brake save/load contract. (4) Hệ PCInteractor (Object/Manager/Hotkey/Camera). (5) Layer reference. |

### 5.3. Scene `6_Stadium_Sunny` — bộ pipeline (scene chuẩn tham chiếu)

| File (full path) | Nội dung |
|---|---|
| `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\6_Stadium_Sunny\Stadium_CarLoad_Pipeline.md` | **Load đúng xe player từ garage.** `ActiveLoadout` (SavedCarIndex PlayerPrefs + Current static), `LoadSceneController` (exec order -110) activate đúng xe → push sang RacePositionTracker/MatchWaitTime; `LoadoutPaintApplier` áp màu sơn; ràng buộc index. |
| `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\6_Stadium_Sunny\Stadium_Results_Pipeline.md` | **Kết thúc chặng:** RaceResultsController nghe `onPlayerRaceFinished` → cộng thưởng theo `finishPosition` (RaceSettings.prizeByPosition) → điền bảng Achievements (Rank_1..12) → mở ModalWindowManager. Dừng xe (StopAllVehicles), con trỏ/camera. **PHẢI chạy menu `Tools/Stadium/Setup Results Board` 1 lần.** |
| `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\6_Stadium_Sunny\Stadium_SpeedBoost_Pipeline.md` | **Bonus tăng tốc THẬT:** `SpeedBoost.cs` nâng `maxSpeedForward` EVP + lực đẩy, nối qua BonusEvent queue → BonusReceiver, tự gắn vào xe Player active. Chỉnh độ mạnh ở GameObject `BonusEvent`. |
| `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\6_Stadium_Sunny\Stadium_BonusSpawn_Pipeline.md` | **Spawn bonus mỗi vòng:** `LapBonusSpawner` nhân bản `Object.prefab` gần checkpoint (Z hướng checkpoint kế tiếp), nghe `onLapCompleted`, số/lap = `RaceSettings.bonusesPerLap`. |
| `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\6_Stadium_Sunny\Stadium_Nitro_Pipeline.md` | **Nitro PC (giữ Shift):** `NitroController` trên PlayerController, nhiên liệu có hạn hồi liên tục, tái dùng SpeedBoost, VFX `Nitro` gắn vào `CarType_*>NitroFx_Anchor`. Shift đã gỡ khỏi phanh. Mọi tham số quy đổi từ stat xe. |

> Các scene đua khác (`7_Stadium_Rain`, `8_Beach_Sunny`, `4_Racing_Circuit`) phỏng theo cùng pipeline này —
> hiện chưa có doc riêng; dùng bộ Stadium_*.md làm khuôn.

---

## 6. Hệ thống cốt lõi — tóm tắt nhanh (cheat sheet cho đồ án)

### 6.1. Data model (ScriptableObject)
- **`CarStats`** — 6 chỉ số 0–100: `maxSpeed, acceleration, grip, braking, handling, nitro`. Có operator +/− tự clamp.
  File: `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\Interact Tool\CarStats.cs`.
- **`CarPart`** — 1 linh kiện (slot Engine/Wheels/Brakes/Suspension/Body/Aero/Other/ECU/**Paint**), `statBonus`, `costGold`, `tier`, prefab tham chiếu. File: `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\CarPart.cs`. Assets: `e:\Workspace\Unity\VHD_DATN_V2\Assets\Data\CarParts\`.
- **`PlayerCarLoadout`** — "xe + linh kiện đang lắp" của 1 xe; `baseStats + Σ parts = GetEffectiveStats()`; `paint`. 3 xe: Azzurro Scout (0g), Mezzanotte X (2000g), Furia Bianca (5000g). Assets: `e:\Workspace\Unity\VHD_DATN_V2\Assets\Data\Loadouts\`.
- **`PlayerInventory`** — kho + ví gold. Asset: `e:\Workspace\Unity\VHD_DATN_V2\Assets\Data\PlayerInventory.asset`.
- **`ShopCatalog`** — danh mục bán. Asset: `e:\Workspace\Unity\VHD_DATN_V2\Assets\Data\Shops\GaragePartsShop.asset`.
- **`LevelSettings` / `RaceSettings`** — config 1 màn (laps, end scene, prize, bonusesPerLap, stat requirements).

### 6.2. Mapping stat → physics (đang chạy, từ `LevelController.cs`)
| Stat 0–100 | Field VehicleController | tại 50 (xe gốc) | +1 điểm (trên 50) ≈ |
|---|---|---|---|
| maxSpeed | maxSpeedForward | 27.78 m/s (100 km/h) | +2 km/h |
| acceleration | maxDriveForce | 2000 N | +60 N |
| grip | tireFriction | 1.0 | +0.01 |
| braking | maxBrakeForce | 3000 N | +60 N |
| handling | maxSteerAngle | 35° | +0.4° |
| nitro | *(không map lái — NitroController đọc)* | — | bình + hồi nitro |

⚠️ Wheels & Brakes có **4 cái/xe → cộng ×4**. Engine/Suspension/ECU = 1 cái.

### 6.3. Kinh tế
- Đơn vị: **Gold** (XP tương lai). Công thức: `Total = Base × RankMultiplier + Bonus đặc biệt`.
- Thưởng theo hạng lưu ở `RaceSettings.prizeByPosition` (vd top3 `{1000,600,300}`).

### 6.4. Race scene — thành phần
- `RacePositionTracker` (xếp hạng + lap + finish + load scene), `MatchWaitTime` (countdown + khóa/mở xe),
  `LevelController` (apply loadout→physics), `UIReceiver` (HUD), `LoadSceneController` (chọn xe), `RaceResultsController` (bảng kết quả + thưởng).
- Xe player = hệ **EVP** (`VehicleController`, tag "Player"). AI = hệ **Off_Road_Racing** (`EasyCarController`/`CarAIController`, `Race_Manager` spawn).

### 6.5. Persistence (save/load — ĐÃ hoạt động phần lớn)
Cơ chế save/load đã chạy thật (xem `GarageLobby_PC_Systems.md` §2.9 brake save/load + `6_Stadium_Sunny/Stadium_CarLoad_Pipeline.md`):
- ✅ **Garage loadout vật lý** (paint / tires / brakes per-xe): `GarageSaveManager` lưu (`RecordPaint` + PlayerPrefs) và **khôi phục** (`RestorePaint/RestoreTires/RestoreBrakes` lúc Start) — key `GB_{carName}_{socketName}` + paint/tires keys → **round-trip đầy đủ qua phiên app**.
- ✅ **Xe đang chọn**: `ActiveLoadout.SavedCarIndex` (PlayerPrefs `ActiveCarIndex`) lưu + đọc lại → garage và scene đua dùng đúng xe.
- ✅ **Gold**: `RaceResultsController.GrantReward()` gọi `AddGold + SaveToPlayerPrefs` sau mỗi race (lưu cả owned parts/cars).
- ⚠️ **Gap duy nhất:** `PlayerInventory.LoadFromPlayerPrefs()` **chưa được gọi lúc boot** → gold/đồ/xe đã lưu chưa được nạp lại ở phiên app mới. Chỉ cần gọi `LoadFromPlayerPrefs(catalog)` trong Awake một bootstrap là khép kín.

---

## 7. Bản đồ Script chính (đường dẫn full)

| Script | Đường dẫn full | Vai trò |
|---|---|---|
| CarStats | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\Interact Tool\CarStats.cs` | 6 chỉ số xe 0–100 |
| CarPart | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\CarPart.cs` | SO linh kiện |
| PlayerCarLoadout | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\PlayerCarLoadout.cs` | SO xe + parts |
| PlayerInventory | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\PlayerInventory.cs` | Kho + gold |
| LevelSettings | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\LevelSettings.cs` | Config màn (mới) |
| LevelController | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\LevelController.cs` | Apply loadout→physics |
| RaceSettings | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\RaceSettings.cs` | Config màn (laps/prize/bonus) |
| RacePositionTracker | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\RacePositionTracker.cs` | Xếp hạng + lap + finish |
| MatchWaitTime | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\MatchWaitTime.cs` | Countdown + khóa/mở xe |
| UIReceiver | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\UIReceiver.cs` | HUD thứ hạng |
| LoadSceneController | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\LoadSceneController.cs` | Chọn/activate xe player |
| ActiveLoadout | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\ActiveLoadout.cs` | Bridge xe garage→race |
| LoadoutPaintApplier | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\LoadoutPaintApplier.cs` | Áp màu sơn ở race |
| RaceResultsController | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\RaceResultsController.cs` | Bảng kết quả + thưởng |
| SpeedBoost | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scripts\SpeedBoost.cs` | Tăng tốc thật (bonus/nitro) |
| BonusReceiver / BonusEvent | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scripts\BonusReceiver.cs` · `...\BonusEvent.cs` | Hệ bonus queue |
| LapBonusSpawner | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scripts\LapBonusSpawner.cs` | Spawn bonus theo lap |
| NitroController | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scripts\NitroController.cs` | Nitro PC (Shift) |
| GarageCarManager | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\GarageCarManager.cs` | Đổi xe E/Q |
| GarageDisplayedCarContext | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\Garage\GarageDisplayedCarContext.cs` | Bridge UI↔data |
| ShopUIController | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\Garage\ShopUIController.cs` | Sinh nút shop |
| InventoryUIController | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\Garage\InventoryUIController.cs` | Panel inventory |
| GaragePartStaging | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\Garage\GaragePartStaging.cs` | Spawn/clear mô hình |
| PartInventoryBridge | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\Garage\PartInventoryBridge.cs` | Nối attach/detach↔kho |
| ShopCatalog | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\Garage\ShopCatalog.cs` | SO danh mục bán |
| CarLoadoutSlot | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\CarLoadoutSlot.cs` | Loadout của xe trong scene |
| GarageSaveManager | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\GarageSaveManager.cs` | Save/restore garage |
| Wheel/Brake system | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Script\Brakes\` (BrakeSocket/Item/Stats/Bootstrap) + Wheel scripts | Tháo/lắp vật lý |
| Car_AI / Race_Manager | `e:\Workspace\Unity\VHD_DATN_V2\Assets\Off_Road_Racing\Scripts\AI\` (Car_AI.cs, Race_Manager.cs) | AI đối thủ |
| VehicleController (EVP) | `e:\Workspace\Unity\VHD_DATN_V2\Assets\EVP5\Scripts\VehicleController.cs` | Physics xe player |

> Hai thư mục script: `Assets\Script\` (game logic chính) và `Assets\Scripts\` (bonus/boost/nitro/spawn).

---

## 8. Gợi ý đọc để viết đồ án (theo chương)

| Chương đồ án | Tài liệu nên đọc |
|---|---|
| Tổng quan & luồng game | §2 file này + D1 (ScreenFlow) |
| Thiết kế gameplay | D2 (Levels), D3 (AI), D4 (Economy) |
| Kiến trúc hệ chỉ số xe | D5 (RaceStats Architecture) + D6 (Balancing thực tế) |
| Hệ linh kiện & shop | D7 + S-FULL (Shop_Inventory_System_FULL) + S1 (Garage_UI_Design) |
| Cài đặt garage (tương tác) | GarageLobby_PC_Systems.md + S4/S7/S8 |
| Hệ thống đua (race loop) | 3_CyberRace/README + bộ Stadium_*.md (CarLoad, Results, SpeedBoost, BonusSpawn, Nitro) |
| Quy trình dựng scene đua | D8 (OffRoadRacing_Scene_Refactor_Guide) |
| Hạn chế & hướng phát triển | §6.5 (chỉ còn gap `LoadFromPlayerPrefs` lúc boot) + TODO trong S-FULL + Part Slots Panel + 4_Racing_Circuit chưa wire + AI Tier/cá tính chưa implement |

---

## 9. Lưu ý nhất quán giữa các tài liệu

- **Tên scene:** TẤT CẢ tài liệu kiến trúc đã được cập nhật sang **tên scene thật** (5 scene đua:
  6_Stadium_Sunny, 8_Beach_Sunny, 7_Stadium_Rain, 3_CyberRace, 4_Racing_Circuit). Luồng:
  `1_SlashScene → 5_GarageLobby_pc → (4 / 3 / 6 / 7 / 8)`.
- **Mapping stats:** D5 §3/§9 là **bản refactor đề xuất** (chưa áp); mapping **đang chạy** là D6 §1. Trích D6 khi mô tả hệ thực tế.
- **Garage_UI_Design.md:** đã hợp nhất còn 1 bản (trong folder con — xem S1).
- **AI Tier/cá tính:** D3 là thiết kế ý tưởng; code hiện dùng AI đồng nhất (`Car_AI` + `Race_Manager`).
- **Save/load:** đã hoạt động (garage paint/tires/brakes + xe chọn + gold). Gap còn lại: gọi `LoadFromPlayerPrefs` lúc boot (§6.5).
- File này (`00_MASTER_README.md`) nằm tại
  `e:\Workspace\Unity\VHD_DATN_V2\Assets\Scenes\Design\00_MASTER_README.md`.
