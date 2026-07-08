# Luồng Màn Hình & Tiến Trình Trò Chơi

> Tài liệu thiết kế tổng quan luồng điều hướng và vòng lặp gameplay của toàn bộ game **Furia Rush**.
> Cập nhật theo **5 scene đua thật** trong build.

---

## 0. Danh sách scene thật

| Scene | File `.unity` | Vai trò | Trạng thái |
|-------|--------------|---------|-----------|
| **1_SlashScene** | `Assets/Scenes/1_SlashScene/SlashScene.unity` | Màn splash / khởi động | Đang dùng |
| **5_GarageLobby_pc** | `Assets/Scenes/5_GarageLobby_pc/GarageLobby_pc.unity` | Hub: shop, inventory, tune, sơn, chọn xe, chọn chặng | Đang dùng |
| **4_Racing_Circuit** | `Assets/Scenes/4_Racing_Circuit/Racing_Circuit.unity` | Đường đua circuit (đua #1) | Chưa cho vào game |
| **3_CyberRace** | `Assets/Scenes/3_CyberRace/CyberRace.unity` | Đường đua cyberpunk (đua #2) | Chưa cho vào game |
| **6_Stadium_Sunny** | `Assets/Scenes/6_Stadium_Sunny/Stadium_Sunny.unity` | Sân vận động, trời nắng — **scene chuẩn tham chiếu** | Đang dùng |
| **7_Stadium_Rain** | `Assets/Scenes/7_Stadium_Rain/Stadium_Rain.unity` | Sân vận động, trời mưa (mặt đường trơn) | Đang dùng |
| **8_Beach_Sunny** | `Assets/Scenes/8_Beach_Sunny/Beach_Sunny.unity` | Bãi biển, trời nắng | Đang dùng |

---

## 4.3.4 Luồng Màn Hình (Screen Flow)

```
                        ┌─────────────┐
                   ●    │             │    ●
                Start   │ 1_SlashScene│   Exit
                Game    │  (splash)   │   Game
                  │     └──────┬──────┘    ▲
                  ▼            │           │
        ┌──────────────────┐  │   ┌────────────────┐
        │  1_SlashScene    │──┴──▶│   Exit Game    │
        │ (logo / Play)    │      └────────────────┘
        └────────┬─────────┘
                 │ Play
                 ▼
        ┌──────────────────────┐
        │  5_GarageLobby_pc    │◀────────────────────────────┐
        │ (shop, tune, paint,  │                             │
        │  chọn xe, chọn chặng)│                             │
        └────────┬─────────────┘                             │
                 │                                           │
         ┌───────▼────────┐                                  │
         │  Race Selection│                                  │
         │   (chọn chặng) │                                  │
         └┬───┬───┬───┬──┬┘                                  │
          │   │   │   │  │                                   │
   ┌──────▼┐ ┌▼─┐ ┌▼──┐ ┌▼────┐ ┌▼──────┐                    │
   │4_Rac- │ │3_│ │6_ │ │7_   │ │8_Beach│                    │
   │ing_   │ │Cy│ │Sta│ │Sta  │ │_Sunny │                    │
   │Circuit│ │be│ │d_ │ │d_   │ │       │                    │
   │(chưa) │ │r │ │Sun│ │Rain │ │       │                    │
   └───┬───┘ └┬─┘ └─┬─┘ └──┬──┘ └───┬───┘                    │
       └──────┴─────┴──────┴────────┘                        │
                       │                                     │
               ┌───────▼──────┐                              │
               │  Race Scene  │                              │
               │   (Playing)  │                              │
               └───────┬──────┘                              │
                       │ Về đích / Thoát                     │
                       └─────────────────────────────────────┘
                              Quay lại Garage
```

**Thứ tự dự kiến:** `1_SlashScene → 5_GarageLobby_pc → (4 / 3 / 6 / 7 / 8)`.
Mọi scene đua khi kết thúc đều quay về `5_GarageLobby_pc` (qua `RaceSettings.endSceneName = "GarageLobby_pc"`).

---

## 4.2.3 Tiến Trình Trò Chơi (Gameflow)

```
           ●
         Start
         Game
           │
           ▼
  ┌──────────────────┐          ┌─────────────┐    ●
  │  1_SlashScene    │─────────▶│  Exit Game  │──▶Exit
  │   (splash/Play)  │          └─────────────┘
  └────────┬─────────┘
           │ Play
           ▼
  ┌──────────────────┐
  │ 5_GarageLobby_pc │◀──────────────────────────────┐
  │  (mua xe + linh  │                               │
  │  kiện, sơn, tune)│                               │
  └────────┬─────────┘                               │
           │                                         │
           ▼                                         │
  ┌──────────────────┐     ┌──────────────────────────────┐
  │  Race Selection  │────▶│        Option Menu           │
  │      Menu        │     │  ┌──────────────────────┐    │
  └────────┬─────────┘     │  │  Exit To Main Menu   │    │
           │               │  └──────────────────────┘    │
           ▼               │  ┌──────────────────────┐    │
  ┌──────────────────┐     │  │   Equip / Tune Car   │    │
  │   Race Scene     │     │  └──────────────────────┘    │
  │   (Playing)      │◀────┘  ┌──────────────────────┐    │
  └────────┬─────────┘        │   Buy Part / Paint   │    │
           │                  └──────────────────────┘    │
           ▼                                              │
  ┌─────────────────┐                                     │
  │  Race Results   │                                     │
  │ (Rank board +   │                                     │
  │  Gold thưởng)   │                                     │
  └────────┬────────┘                                     │
           │                                              │
           └──────────────────────────────────────────────┘
                          Quay lại Garage
```

### Vòng lặp cốt lõi (Core Loop)

```
  5_GarageLobby_pc
      │
      │  Mua / lắp linh kiện, sơn, chọn xe
      ▼
  Chọn chặng đua (4/3/6/7/8)
      │
      │  Vào race
      ▼
  Đua (Playing) — nitro (Shift) + bonus tăng tốc
      │
      │  Về đích lap cuối
      ▼
  Bảng kết quả (Rank board) + nhận Gold theo hạng
      │
      │  Quay về Garage
      └──────────────────▶ (lặp lại)
```

### Điều kiện chuyển màn

| Sự kiện | Chuyển tới |
|---------|-----------|
| Bấm **Play** ở 1_SlashScene | 5_GarageLobby_pc |
| Bấm **Exit** ở 1_SlashScene | Thoát game |
| Chọn chặng đua từ menu | Scene đua tương ứng (4/3/6/7/8) |
| Về đích lap cuối | Bảng kết quả `RaceResultsController` → (nút) quay về 5_GarageLobby_pc |
| Thoát giữa chừng | 5_GarageLobby_pc (không nhận thưởng) |
| Bấm **Exit Game** ở Garage | Thoát game |

> **Ghi chú kỹ thuật:** scene đua kiểu Stadium (6/7/8) **không auto-load** về garage
> (`autoLoadSceneOnFinish = false`) — thay vào đó mở bảng kết quả Achievements rồi người chơi bấm nút về.
> Scene CyberRace (3) thì `autoLoadSceneOnFinish = true` (tự về sau `loadSceneDelay` giây).
> Xem `6_Stadium_Sunny/Stadium_Results_Pipeline.md` và `3_CyberRace/README.md`.

---

*Cập nhật: 2026-06-04*
