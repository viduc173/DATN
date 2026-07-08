# Sơ đồ hoạt động đơn giản của dự án Furia Rush

Tài liệu này chia hoạt động của dự án thành các luồng nhỏ, mỗi luồng chỉ giữ các bước chính để dễ đưa vào báo cáo hoặc trình bày.

## 1. Luồng tổng quan

```mermaid
flowchart LR
    Start((Bắt đầu)) --> Splash[Splash Scene]
    Splash --> Garage[Garage]
    Garage --> SelectRace[Chọn chặng đua]
    SelectRace --> Race[Đua xe]
    Race --> Result[Kết quả]
    Result --> Reward[Nhận Gold]
    Reward --> Garage
```

## 2. Luồng khởi động game

```mermaid
flowchart TD
    Start((Mở game)) --> Splash[SlashScene]
    Splash --> Choice{Người chơi chọn}
    Choice -->|Play| Garage[GarageLobby]
    Choice -->|Exit| End((Thoát game))
```

## 3. Luồng trong Garage

```mermaid
flowchart TD
    Garage[GarageLobby] --> ShowCar[Hiển thị xe]
    ShowCar --> Action{Người chơi thao tác}

    Action -->|Chọn xe| SelectCar[Lưu xe đang chọn]
    Action -->|Mua đồ| Shop[Mở Shop]
    Action -->|Lắp đồ| Equip[Lắp / tháo linh kiện]
    Action -->|Sơn xe| Paint[Sơn xe]
    Action -->|Chọn chặng| RaceSelect[Chọn màn đua]

    SelectCar --> ShowCar
    Shop --> ShowCar
    Equip --> ShowCar
    Paint --> ShowCar
    RaceSelect --> LoadRace[Load scene đua]
```

## 4. Luồng mua linh kiện

```mermaid
flowchart TD
    Shop[Mở Shop] --> ChoosePart[Chọn linh kiện]
    ChoosePart --> CheckGold{Đủ Gold?}
    CheckGold -->|Không| Notice[Thông báo không đủ Gold]
    CheckGold -->|Có| Buy[Trừ Gold]
    Buy --> AddInventory[Thêm vào kho]
    AddInventory --> Save[Lưu dữ liệu]
    Save --> Garage[Quay lại Garage]
    Notice --> Shop
```

## 5. Luồng lắp / tháo linh kiện

```mermaid
flowchart TD
    Garage[Garage] --> PickPart[Chọn linh kiện trong kho]
    PickPart --> Socket[Đưa vào vị trí lắp]
    Socket --> Equip{Lắp hợp lệ?}
    Equip -->|Không| Return[Trả về kho]
    Equip -->|Có| ApplyPart[Lắp vào xe]
    ApplyPart --> UpdateStats[Cập nhật chỉ số xe]
    UpdateStats --> SaveLoadout[Lưu cấu hình xe]
    SaveLoadout --> Garage
    Return --> Garage
```

## 6. Luồng vào màn đua

```mermaid
flowchart TD
    RaceSelect[Chọn chặng đua] --> LoadScene[Load race scene]
    LoadScene --> LoadCar[Load xe đã chọn]
    LoadCar --> ApplyStats[Áp chỉ số xe]
    ApplyStats --> Countdown[Đếm ngược]
    Countdown --> StartRace[Bắt đầu đua]
```

## 7. Luồng trong khi đua

```mermaid
flowchart TD
    StartRace[Bắt đầu đua] --> Racing[Người chơi lái xe]
    Racing --> Event{Sự kiện}

    Event -->|Qua checkpoint| Checkpoint[Cập nhật tiến trình]
    Event -->|Dùng nitro| Nitro[Tăng tốc]
    Event -->|Nhặt bonus| Bonus[Kích hoạt bonus]
    Event -->|Hoàn thành vòng| Lap[Cập nhật lap]

    Checkpoint --> Ranking[Cập nhật thứ hạng]
    Nitro --> Racing
    Bonus --> Racing
    Lap --> FinishCheck{Hoàn thành lap cuối?}
    Ranking --> Racing

    FinishCheck -->|Chưa| Racing
    FinishCheck -->|Rồi| Finish[Về đích]
```

## 8. Luồng kết thúc màn đua

```mermaid
flowchart TD
    Finish[Về đích] --> StopCars[Dừng xe]
    StopCars --> SortRank[Sắp xếp thứ hạng]
    SortRank --> ShowResult[Hiển thị bảng kết quả]
    ShowResult --> GiveReward[Cộng Gold]
    GiveReward --> SaveReward[Lưu dữ liệu]
    SaveReward --> BackGarage[Quay về Garage]
```

## 9. Các script chính theo từng luồng

| Luồng | Script / asset chính |
|---|---|
| Chuyển scene | `SceneChanger`, `RaceSettings` |
| Garage | `GarageCarManager`, `GarageSaveManager` |
| Shop / Inventory | `ShopUIController`, `InventoryUIController`, `PlayerInventory` |
| Linh kiện xe | `WheelSocket`, `BrakeSocket`, `CarPart`, `PlayerCarLoadout` |
| Load xe vào race | `ActiveLoadout`, `LoadSceneController`, `LevelController` |
| Đua xe | `VehicleController`, `RacePositionTracker`, `CheckpointTrigger` |
| Nitro / bonus | `NitroController`, `SpeedBoost`, `BonusReceiver` |
| Kết quả | `RaceResultsController` |
