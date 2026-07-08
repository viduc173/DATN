# Stadium Sunny — Pipeline kết thúc chặng: bảng kết quả + tiền thưởng

> Tài liệu mô tả cách scene `6_Stadium_Sunny` xử lý **kết thúc chặng đua**: ghi nhận khi player chạm vạch
> đích lap cuối → cộng tiền thưởng theo hạng → điền bảng kết quả (UI Achievements) → mở panel bằng animation
> `ModalWindowManager`. Kèm các fix về con trỏ chuột / camera khi mở UI.
> Unity 6. Triển khai 2026-06-03. Bổ trợ cho `Stadium_CarLoad_Pipeline.md`.

---

## 0. Bối cảnh — yêu cầu

- Mỗi chặng có **tiền thưởng** + **số vòng** lưu trong 1 asset đi kèm scene.
- **Top 3** mới được thưởng (thực tế chỉ Player có ví → Player được thưởng theo hạng về đích nếu ≤ 3).
- Kết thúc chặng = **đúng lúc xe Player chạm vạch đích của lap cuối**. AI về sau Player → ghi đúng thứ tự,
  không quan trọng.
- Hết chặng → tự mở panel **Achievements** (dùng lại làm bảng kết quả) bằng animation của `ModalWindowManager`,
  điền tên tay đua theo thứ tự về đích vào `Rank_1..Rank_12` (icon giữ số thứ hạng).
- 1 scene tối đa **12 xe** → bảng có **12 dòng** (`Rank_1..Rank_12`). `RaceResultsController` tự điền theo số
  dòng thực tế nên thêm/bớt dòng không cần sửa code. **Ít xe hơn 12** → các dòng thừa bị **ẩn hẳn**
  (`hideEmptyRows = true`, mặc định) — Layout Group tự dồn các dòng còn lại lên, không để khoảng trống.

---

## 1. Kiến trúc & luồng

```
RacePositionTracker (GO "Score Manager")
  └─ player chạm vạch đích lap cuối → MarkRacerFinished(player)
        └─ onPlayerRaceFinished (UnityEvent)            [bắn GIỮA UpdateAllPositions, TRƯỚC CalculateRankings]
              ▼
RaceResultsController.HandlePlayerFinished()             [tự AddListener trong OnEnable]
   1) SortedStandings()  ← snapshot xếp hạng, sort bằng finishPosition/totalProgress (chính xác ngay)
   2) StopAllVehicles()  ← phanh cứng + dừng hẳn mọi xe (player + AI) — xem §6.1
   3) GrantReward()      ← prize = RaceSettings.GetPrize(player.finishPosition); inventory.AddGold + Save
   4) PopulateBoard()    ← ghi ranked[i].racerName vào AchievementItem.titleObj; ghi tiền thưởng của hạng
                            (RaceSettings.GetPrize(position)) vào 'Rank_X > Gold > Title' (position = sibling
                            index +1, thứ tự sibling đúng Rank_1..Rank_12 → khớp icon số + prizeByPosition)
   5) achievementsWindow.OpenWindow()  ← animation "In" (fade alpha 0→1 + pop "Content")
```

> **Vì sao dùng `finishPosition` chứ không phải `GetPlayerPosition()`:** `onPlayerRaceFinished` bắn bên trong
> `UpdateAllPositions()`, **trước** `CalculateRankings()` cùng frame → `currentPosition` còn là giá trị frame
> trước. `finishPosition` được set ngay trong `MarkRacerFinished` nên chính xác tại thời điểm về đích.
> `PopulateBoard` cũng tự sort lại snapshot (finished theo finishPosition, chưa finished theo totalProgress)
> để không lệ thuộc timing.

---

## 2. Asset cấu hình — `RaceSettings_Stadium.asset`

Nằm cùng folder scene. `RacePositionTracker.raceSettings` **và** `RaceResultsController.raceSettings` đều
trỏ vào nó (trước đây tracker bị trỏ NHẦM sang `RaceSettings_CyberRace.asset`).

| Field | Ý nghĩa |
|---|---|
| `totalLaps` | Số vòng để cán đích (override `totalLaps` trên tracker). |
| `prizeByPosition` (int[]) | Tiền thưởng gold theo hạng: index 0 = hạng 1, 1 = hạng 2... **Số phần tử = số hạng được thưởng** (vd 3 phần tử `{1000,600,300}` = top 3). Tự chỉnh tuỳ ý. |
| `autoLoadSceneOnFinish` | **= false** ở Stadium (vì mở Achievements thay vì auto-load về Garage). |
| `endSceneName` | Scene quay về (mặc định `GarageLobby_pc`) — dùng khi gọi `ReturnToGarage()`. |
| `loadSceneDelay`, `lapWrapThreshold` | Như RaceSettings gốc. |

`RaceSettings.GetPrize(position)` (1-based) trả 0 nếu hạng ngoài mảng → không thưởng.

---

## 3. Code đã thêm / sửa

| File | Thay đổi |
|---|---|
| `Assets/Script/RaceSettings.cs` | Thêm `int[] prizeByPosition` (default `{1000,600,300}`) + `GetPrize(int position)`. |
| `Assets/Script/RaceResultsController.cs` | **MỚI** — nghe `onPlayerRaceFinished`, cộng thưởng theo `finishPosition`, điền `Rank_*` titles, gọi `OpenWindow()`. Refs auto-resolve, có `ReturnToGarage()` để wire nút back. ContextMenu `DEBUG: Trigger Finish` để test. |
| `Assets/Script/Editor/StadiumResultsSetup.cs` | **MỚI** — menu `Tools/Stadium/Setup Results Board`: tạo asset + gán tracker, duplicate `Rank_5`→`Rank_6..12` + icon số 6-12, set title placeholder, thêm + wire `RaceResultsController`, **gán ModalWindow.controller cho Animator panel** (`FixModalAnimator`). Idempotent. `LastRank = 12` (sửa hằng này nếu muốn nhiều/ít dòng hơn). |
| `Assets/Script/MenuCursorController.cs` | Thêm option `driveByModalState` — con trỏ tự bám `ModalWindowManager.isOn`. |
| `Assets/Script/Player_Drive_Input/PlayerDriverInputFromKeyboard.cs` | `HandleRotateInput` thêm guard `if (Cursor.lockState != Locked) return` — không xoay camera khi đang mở UI. |

---

## 4. Wiring trong scene (đã thực hiện trực tiếp trong `.unity`)

### 4.1. Bảng kết quả (UI Achievements)
- `Rank_1..Rank_5`: có sẵn, icon = số 1..5. `Rank_6..Rank_12`: duplicate từ `Rank_5`, icon = số 6..12
  (`Assets/2D/Number/{n}.png`; số 10/11/12 đã recolor về **trắng** cho đồng bộ 1-9). Tất cả nằm dưới:
  `Achievements > Content > Panel Content > Panels > All >
  Content > List > Layout Group`. Title runtime sẽ ghi đè.
- `RaceResultsController` (trên GO "Score Manager"): `tracker`, `achievementsWindow` (MWM panel Achievements),
  `inventory` (`Assets/Data/PlayerInventory.asset`), `rankBoardContainer` (Layout Group), `raceSettings`
  (Stadium), `autoOpenWindow = 1`.

### 4.2. Mở panel có animation
- Panel **Achievements** có `ModalWindowManager` (`startBehaviour = Disable` → ẩn lúc start).
- ⚠️ **Animator của panel phải dùng `ModalWindow.controller`** (state "In"/"Out"), KHÔNG phải `MainPanel.controller`
  (state "Panel In"/"Panel Out"). MWM gọi `Play("In")`; nếu controller sai → không có state "In" → alpha kẹt
  ở 0 → panel vô hình. (Đã đổi controller trực tiếp trong scene.)

### 4.3. Panel con "All" — đã DISABLE Animator
- ⚠️ GO "All" có Animator dùng `SubPanel.controller`, default state **"Start"** = clip `SubPanel_Out.anim`
  set CanvasGroup alpha = 0. Scene **không có `PanelManager`** để Play("Panel In") → runtime ẩn hết ranks
  (dù editor alpha = 1). **Fix:** set Animator của "All" `m_Enabled = 0` → giữ alpha = 1.

### 4.4. Con trỏ & camera
- `MenuCursorController` (GO luôn active): bật `driveByModalState = 1`. Mở panel bất kỳ (Tab/Escape/nút)
  → hiện chuột; đóng hết panel → tự khóa chuột lại. Không cần wire UnityEvent (panel exit là prefab instance,
  không wire `onClose` tay được).
- `PlayerDriverInputFromKeyboard`: camera chỉ xoay khi `Cursor.lockState == Locked`.

---

## 5. Kiểm tra `RacePositionTracker` (logic kết thúc chặng)

Đã review — **đúng** cho yêu cầu:
- Xe spawn SAU vạch (`spawnedBehindLine`) → lần băng vạch đầu = xuất phát (không tính lap). Đủ `totalLaps`
  vòng → `MarkRacerFinished` → bắn `onPlayerRaceFinished` đúng lúc Player chạm vạch đích lap cuối.
- AI về sau Player được cấp `finishPosition` tăng dần (đúng thứ tự).
- AI tự đăng ký qua `Race_Manager.cs` `RegisterAIRacer(.., "AI {i}")`; checkpoint auto-detect từ
  `Checkpoint_Manager`.

⚠️ **Ràng buộc lúc Play:** xe Player phải đặt **sau vạch đích** (grid xuất phát). Nếu đặt trước/ngay vạch
→ `spawnedBehindLine = false` → finish sớm 1 vòng.

---

## 6.1. Dừng xe khi kết thúc chặng

`RaceResultsController.StopAllVehicles()` (bật/tắt bằng `stopVehiclesOnFinish`, mặc định bật) gọi
`StopVehicle()` cho **mọi** xe trong bảng (player + AI):
1. **Triệt tiêu quán tính**: zero `linearVelocity` + `angularVelocity` mọi Rigidbody trong xe.
2. **Player** (EVP `VehicleController` qua `PlayerDriveInputManager`): `SetValue(0,0,1,1,0)` = **phanh cứng**
   (brake=1, handbrake=1, throttle/steer=0) + **disable `PlayerDriverInputFromKeyboard`** (ngừng đọc WASD/chuột
   để không ghi đè lệnh phanh; cũng dừng luôn việc xoay camera).
3. **AI** (`EasyCarController` + `CarAIController`): disable `CarAIController` + set `throttleInput=0`,
   `handBrake=true`, `Clutch=true` → đứng yên.

> Hai hệ xe khác nhau: player = EVP (namespace `EVP`), AI = Off_Road_Racing (namespace `ALIyerEdon`).
> Chỉ tác động lên xe đang **active** (xe player đang dùng + AI đã spawn); các `CarType_*` inactive không bị đụng.

## 6. Cách verify end-to-end

1. Compile sạch. Mở scene Stadium (nếu Unity báo scene đổi ngoài → Reload).
2. (Tuỳ chọn re-run) `Tools/Stadium/Setup Results Board` — idempotent, gồm cả bước gán ModalWindow.controller.
3. Chỉnh `prizeByPosition` + `totalLaps` trong `RaceSettings_Stadium.asset`.
4. Play → cho Player về đích lap cuối (hoặc ContextMenu `DEBUG: Trigger Finish` trên `RaceResultsController`):
   - Panel Achievements **fade + pop** hiện ra.
   - `Rank_*` hiển thị thứ tự về đích (Player tô vàng). Tên trống/"---" = chưa có AI (kiểm tra spawner).
   - Gold tăng đúng mức nếu Player top 3 (xem `PlayerMoneyText` / `PlayerInventory`).
   - Chuột tự unlock (panel bấm được, camera đứng yên).
   - **Mọi xe dừng hẳn** ngay khi panel hiện (player phanh cứng, AI đứng yên).
5. Bấm Tab mở panel exit → chuột hiện; đóng panel → chuột tự khóa, camera lái lại bình thường.

---

## 6.2. Phiên bản VR (`Stadium_Sunny_vr.unity`) — mở bảng kết quả & exit bằng tay cầm
Bản VR khác bản PC ở chỗ **không có chuột/phím Tab**. `UI_Main Menu` (chứa cả **Achievements** = bảng kết quả
lẫn **Modal Windows** = modal Exit/Pause) được chuyển sang **world-space** (xem `Design/Convert_3D_UI_to_VR_UI.md`).
- `UI_Main Menu` **GIỮ ACTIVE** (KHÔNG ẩn cả canvas như menu garage) — vì tắt nó là mất luôn modal kết quả + exit.
- `PCHotkeyManager`/`HotkeyController` (Tab → mở exit) đã **inactive** trong scene VR → thay bằng
  **`VRRaceMenu`** (trên EventSystem): bấm **A/X tay trái** mở/đóng modal Exit (`ModalWindowManager.OpenWindow/CloseWindow`);
  **tự hiện tia** khi có modal mở (kết quả tự mở khi về đích, hoặc exit), ẩn tia khi đang lái.
- Bảng kết quả vẫn auto-mở qua `RaceResultsController` như cũ; nút Exit/Back trong modal bấm được bằng **tia** (world-space).
- Gắn tự động bằng menu **Tools/VR/Convert Main Menu to VR** (tự nhận scene đua → dùng VRRaceMenu).

## 7. Liên quan
- `Stadium_CarLoad_Pipeline.md` — load đúng xe Player từ garage + `MatchWaitTime`/`RacePositionTracker`.
- `RaceSettings_CyberRace.asset` — asset cùng kiểu của scene CyberRace (KHÔNG dùng cho Stadium nữa).
- `Assets/Data/PlayerInventory.asset` — ví gold của Player (`AddGold` + `SaveToPlayerPrefs`).
