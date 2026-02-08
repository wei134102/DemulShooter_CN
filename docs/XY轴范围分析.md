# DemulShooter X 轴 / Y 轴范围分析

## 一、输入来源（RawInput）

### 1.1 鼠标设备（RIM_TYPEMOUSE）

| 轴 | 最小值 | 最大值 | 说明 |
|----|--------|--------|------|
| X | 0 | **0x0000FFFF (65535)** | 固定范围，见 `RawInputController.cs` 第 503–505 行 |
| Y | 0 | **0x0000FFFF (65535)** | 同上 |

- 鼠标轴为**相对移动**（`lLastX`, `lLastY`），但程序内部按上述绝对范围处理。
- 来源：`DsCore\RawInput\RawInputController.cs` 第 496–506 行。

### 1.2 HID 设备（光枪、摇杆等，RIM_TYPEHID）

| 轴 | 最小值 | 最大值 | 说明 |
|----|--------|--------|------|
| X | **LogicalMin** | **LogicalMax**（或修正值） | 来自 HID 报告描述符 |
| Y | **LogicalMin** | **LogicalMax**（或修正值） | 同上 |

- **LogicalMin / LogicalMax**：由设备 HID 描述符决定，不同设备不同。
- **修正逻辑**（`Correct_Axis_Max`）：
  - 若 `LogicalMax == -1`（0xFFFFFFFF，如部分 DualShock 3 / WiiMote 驱动）→ 修正为 `0x0000FFFF`。
- **负值处理**（`CorrectNegative_Value`）：
  - 设备轴为负范围（如 `[-2048, 2048]`）时，保证 16 位有符号值正确扩展为 32 位。
- 来源：`DsCore\RawInput\RawInputController.cs` 第 454–486 行。

---

## 二、手动校准覆盖（AnalogManual）

当 **AnalogAxisRangeOverride** 开启（config.ini 中 `P1Analog_Axis_Range_Override = 1` 等）时，使用手动校准范围替代设备原始范围。

### 2.1 配置项（config.ini）

```
P1Analog_Manual_Xmin = <值>
P1Analog_Manual_Xmax = <值>
P1Analog_Manual_Ymin = <值>
P1Analog_Manual_Ymax = <值>
```

- 对应 `PlayerSettings` 的 `AnalogManual_Xmin/Xmax/Ymin/Ymax`。
- 默认均为 0；通过 GUI 的「Analog device calibration」页校准后写入。

### 2.2 校准逻辑（GUI_AnalogCalibration）

- **初始化**：`Xmin = Int32.MaxValue`, `Xmax = Int32.MinValue`，Y 同理。
- **校准过程**：实时采样 `RIController.Computed_X/Y`，扩展 min/max。
- **Default 按钮**：从设备读取 `Axis_X_Min/Max`、`Axis_Y_Min/Max` 作为默认范围。
- 来源：`DemulShooter_GUI\GUI_AnalogCalibration.cs` 第 122–178 行。

### 2.3 应用条件

- 仅对 **HID 设备** 且 **AnalogAxisRangeOverride == true** 时生效。
- 鼠标设备不参与该覆盖逻辑。
- 来源：`DemulShooter\DemulShooterWindow.cs` 第 1068–1077 行。

---

## 三、映射到屏幕坐标（ScreenScale）

### 3.1 线性映射公式

从 `[fromMin, fromMax]` 映射到 `[toMin, toMax]`：

```
val = Clamp(val, fromMin, fromMax)
frac = (val - fromOff) / (fromMax - fromOff)  或  (fromOff - fromMin)
结果 = toOff + (toMax - toOff) * frac  或  (toOff - toMin) * frac
```

- 支持 `fromMax < fromMin`（反向轴）。
- 支持中间点 `fromOff` / `toOff`（目前多数调用中 `fromOff = fromMin`, `toOff = toMin`）。

### 3.2 主程序中的目标范围

| 源范围 | 目标范围 |
|--------|----------|
| X: [Axis_X_Min, Axis_X_Max] 或 [AnalogManual_Xmin, AnalogManual_Xmax] | [0, ScreenWidth] |
| Y: [Axis_Y_Min, Axis_Y_Max] 或 [AnalogManual_Ymin, AnalogManual_Ymax] | [0, ScreenHeight] |

- `ScreenWidth` / `ScreenHeight` 来自目标游戏的窗口或主屏分辨率。
- 来源：`DemulShooter\DemulShooterWindow.cs` 第 1070–1077 行。

### 3.3 GUI 中的预览范围

- X: `[Axis_X_Min, Axis_X_Max]` → `[0, Screen.PrimaryScreen.WorkingArea.Width]`
- Y: `[Axis_Y_Min, Axis_Y_Max]` → `[0, Screen.PrimaryScreen.WorkingArea.Height]`

- 来源：`DemulShooter_GUI\Wnd_DemulShooterGui.cs` 第 641–642 行。

---

## 四、各类游戏 / 插件的输出范围

### 4.1 标准屏幕分辨率

- 多数游戏：X ∈ [0, ScreenWidth]，Y ∈ [0, ScreenHeight]。
- 部分会做 Y 轴翻转：`Y out = TotalResY - Y`。

### 4.2 标准化坐标 [0, 1] 或 [-1, 1]

- **Unity 插件**：`Axis_X / Screen.width`、`Axis_Y / Screen.height` → [0, 1]。
- **Blue Estate**：`(2.0 * X / TotalResX) - 1.0` → X、Y ∈ [-1, 1]，再乘以 1000 传出。
- **Heavy Fire**：X ∈ [-fRatio, fRatio]，用于掩护判定。

### 4.3 固定分辨率（如 System 357）

- **Sailor Zombie / DeadStorm / Dark Escape**：内部 `dMaxX`、`dMaxY` 等，并对输出做 clamp。
- 来源：`DemulShooterX64\Games\Game_S357*.cs`。

---

## 五、汇总表

| 阶段 | X 轴范围 | Y 轴范围 | 备注 |
|------|----------|----------|------|
| **鼠标原始** | 0 ~ 65535 | 0 ~ 65535 | 固定 |
| **HID 原始** | LogicalMin ~ LogicalMax | 同上 | 设备相关，Max 可能被修正为 65535 |
| **手动校准** | AnalogManual_Xmin ~ Xmax | AnalogManual_Ymin ~ Ymax | 仅 HID 且启用覆盖时 |
| **屏幕映射** | 0 ~ ScreenWidth | 0 ~ ScreenHeight | 游戏/主屏分辨率 |
| **Unity 输出** | 0 ~ 1（归一化） | 0 ~ 1（归一化） | 按 Screen.width/height |

---

## 六、常见设备参考

| 设备类型 | 典型 X 范围 | 典型 Y 范围 |
|----------|-------------|-------------|
| AimTrak（鼠标模式） | 0 ~ 65535 | 0 ~ 65535 |
| Act-Labs（HID） | 设备描述符，常见 0 ~ 65535 或类似 | 同上 |
| 部分摇杆（带负值） | -32768 ~ 32767 或 -2048 ~ 2048 | 同上 |
| WiiMote + 驱动 | 可能 LogicalMax = -1 → 修正为 65535 | 同上 |
| **GUN4IR 蓝牙** | **-127 ~ +127** | 同上，需用 0~254  remap 见下文 |

---

## 七、GUN4IR 蓝牙等 [-127, 127] 设备的 0~254 映射

部分无线 HID 光枪（如 GUN4IR 蓝牙）报告 X/Y 为 **-127 ~ +127**，光标可能只在屏幕右下 1/4 移动。可改用 **0~254** 映射覆盖整屏。

### 配置步骤

1. 在 **config.ini** 中为该玩家启用手动轴覆盖并设置 0~254：

```ini
P1Analog_Calibration_Override = True
P1Analog_Manual_Xmin = 0
P1Analog_Manual_Xmax = 254
P1Analog_Manual_Ymin = 0
P1Analog_Manual_Ymax = 254
```

2. 程序会自动将设备原始范围 [-127, 127] 线性映射到 [0, 254]，再映射到屏幕。

### 映射公式

- 原始值 -127 → 0  
- 原始值 0    → 127  
- 原始值 +127 → 254  

然后 [0, 254] 映射到 [0, ScreenWidth] × [0, ScreenHeight]，实现全屏覆盖。
