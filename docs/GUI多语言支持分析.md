# DemulShooter GUI 多语言支持分析

## 一、项目概况

- **GUI 技术栈**：Windows Forms（.NET Framework 4.8）
- **主要界面**：`DemulShooter_GUI` 项目
  - 主窗口：`Wnd_DemulShooterGui`（多 Tab 配置界面）
  - 用户控件：`GUI_Player`、`GUI_Button`、`GUI_RawInputMouse`、`GUI_RawInputHID`、`GUI_AnalogCalibration`
- **资源形式**：每个窗体/控件均有对应 `.resx`，主程序有 `Properties\Resources.resx`（当前主要放二进制/文件资源，非界面文案）

## 二、当前界面文本来源

### 2.1 已通过资源取值的控件（极少）

主窗体 `Wnd_DemulShooterGui.resx` 中仅有 **3 个** Label 的文本通过 `resources.GetString(...)` 从 resx 读取：

- `label15.Text`：Act Labs 校准说明（英文长文本）
- `label42.Text`：Mission Impossible 扳机说明
- `label67.Text`：Operation G.H.O.S.T 独立 ACTION 按钮说明

Designer 中对应代码形如：`this.label15.Text = resources.GetString("label15.Text");`

### 2.2 硬编码在 Designer 中的文本（主体）

界面上的绝大部分文案直接在 `*.Designer.cs` 里以字符串字面量赋值，例如：

- **主窗体** `Wnd_DemulShooterGui.Designer.cs`：约 **120+** 处 `.Text = "..."`  
  包括：Tab 标题（如 "P1 Config"、"Analog device calibration"）、按钮（"Save Config"、"Patch !"）、标签、GroupBox、RadioButton 等。
- **用户控件**：
  - `GUI_RawInputMouse.Designer.cs`：约 6 处
  - `GUI_RawInputHID.Designer.cs`：约 10 处
  - `GUI_Player.Designer.cs`：1 处（如 "P1 Device :"）
  - `GUI_Button.Designer.cs`：1 处（如 "1"）
  - `GUI_AnalogCalibration.Designer.cs`：约 12 处

这些均未使用 `CurrentUICulture` 或语言相关 resx，因此目前**仅显示一种语言**（主要为英文）。

### 2.3 代码中的硬编码字符串

- **主窗体** `Wnd_DemulShooterGui.cs`：
  - 窗口标题：`this.Text = "DemulShooter_GUI " + 版本号;`
  - **约 58 处** `MessageBox.Show(...)`，文案和标题均为英文（如 "Configuration saved !"、"DemulShooter Error"、"Impossible to save DemulShooter config file." 等），以及各类错误/提示信息。
- 无 `Thread.CurrentThread.CurrentUICulture` 或 `CultureInfo` 设置，也未使用 `ResourceManager.GetString` 获取消息文本。

### 2.4 本地化相关配置现状

- 所有窗体/用户控件的 **Localizable 均为默认 false**（未在 Designer 或代码中启用）。
- 不存在按语言拆分的 resx（如 `Wnd_DemulShooterGui.zh-CN.resx`、`*.en.resx` 等）。
- `Properties\Resources.Designer.cs` 中已有 `ResourceManager` 和 `Culture` 属性，但当前只用于图标、DLL、ini 等非文案资源。

## 三、多语言支持可行性结论

**结论：GUI 支持多语言在技术上完全可行，且与现有技术栈兼容良好。**

依据：

1. **.NET WinForms 自带本地化机制**  
   - 通过 `Form/UserControl.Localizable = true` 可为每个界面生成“语言—资源”对应关系（如 `FormName.zh-CN.resx`），设计器会为可本地化属性生成 resx 条目。  
   - 运行时在加载窗体前设置 `Thread.CurrentThread.CurrentUICulture`（及可选 `CurrentCulture`），即可按当前语言加载对应 resx，无需改控件布局。

2. **现有结构已部分使用 resx**  
   - 主窗体已用 resx 存放 3 个 Label 的文本，并经由 `ComponentResourceManager` 加载，说明项目已具备“从 resx 读文案”的流程。  
   - 扩展为“所有控件文案 + 代码中的消息/标题”都从资源读取，是同一套机制的推广。

3. **无与多语言冲突的架构**  
   - 未发现依赖“界面文字固定为英文”的逻辑；MessageBox 和窗体标题改为从资源键取值即可。

## 四、推荐实现思路

### 4.1 方案 A：WinForms 标准本地化

- **步骤概要**  
  1. 对主窗体及所有用户控件：在属性窗口中设置 **Localizable = True**，**Language** 先保留默认（如 English）。  
  2. 重新生成/整理一次，使当前所有界面文本进入默认语言的 resx（如 `Wnd_DemulShooterGui.resx` 或生成的 `Wnd_DemulShooterGui.en.resx`，视 VS 行为而定）。  
  3. 为每种目标语言添加语言页（如“中文”），在设计器里为同一控件填写翻译；或复制 resx 为 `FormName.zh-CN.resx` 等，在 XML 中翻译 `<value>`。  
  4. 在程序启动时（如 `Program.Main` 或主窗体构造函数最早处）根据配置/系统语言设置：  
     `Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");`（示例）。  
  5. 代码中的字符串（MessageBox、`this.Text` 等）迁到统一资源文件（如 `Properties\Strings.resx` + `Strings.zh-CN.resx`），通过 `Resources.GetString("Key")` 或类似方式获取，避免硬编码。

- **优点**：与 Visual Studio 设计器集成好，后续加语言主要维护 resx；符合 .NET 惯例，便于协作和工具支持。

### 4.2 方案 B：集中资源文件 + 手动绑定

- 不启用 Localizable，保持 Designer 中控件文本为“占位”或默认英文。  
- 新建集中资源（如 `Strings.resx` / `Strings.zh-CN.resx`），为每个界面文本和消息定义键。  
- 在窗体 Load/Shown 或初始化时，用代码为每个控件赋 `Text = Strings.XXX`；MessageBox 和标题同样用 `Strings`。  
- 启动时根据语言设置 `CurrentUICulture`，并确保加载的是对应语言的资源集。

- **优点**：所有可翻译字符串集中在一处，便于管理和交给翻译；不依赖设计器语言页。  
- **缺点**：需要手写较多绑定代码，且若以后在 Designer 里改文案容易遗漏同步到资源键。

### 4.4 上游仓库更新时，哪种方案更省事？（重要）

若本仓库会持续从上游（原版 DemulShooter）合并更新，**方案 B 明显更利于跟进上游**。

| 维度 | 方案 A（Localizable + 每窗体多语言 resx） | 方案 B（集中 Strings + 运行时绑定） |
|------|-------------------------------------------|-------------------------------------|
| **上游改动的文件** | 上游会改 `*.Designer.cs`、可能改 `*.resx` | 上游主要改 `*.Designer.cs`、业务 `.cs` |
| **我们改动的文件** | 每个窗体的 Designer.cs 都变成用 `resources.GetString`，且每个窗体多出 `FormName.zh-CN.resx` 等 | 只增加我们自己的 `Strings.resx`、`Strings.zh-CN.resx` 和少量“应用文案”的代码（如 Form_Load 里绑定） |
| **合并冲突** | **容易冲突**：同一段 Designer.cs 我们改成资源键，上游改成新文案/新控件，两边结构不一致，每次合并都要手工解决 | **冲突少**：Designer.cs 可直接采用上游版本；我们只维护自己的 Strings 和绑定逻辑，上游通常不会动这些文件 |
| **上游新增控件/新界面** | 要改 Designer、默认 resx、以及每种语言的 resx，还要和上游对同一文件的修改做合并 | 接受上游 Designer 改动，在我们这边加几条绑定 + 在 Strings 里加新键和翻译即可 |
| **上游只改文案** | 默认语言 resx 与上游“默认文本”要同步，各语言 resx 也要跟着补/改 | 只更新 Strings 里对应键的默认值或翻译 |

**结论（上游同步场景）**：  
- **方案 A**：每次上游动界面，你都要在“我们的 resx + 多语言 resx”和“上游的 Designer/resx”之间反复合并，冲突多、容易漏改。  
- **方案 B**：Designer 和窗体结构尽量跟上游一致，多语言完全收口在我们自己的 `Strings.*.resx` 和少量绑定代码里，上游更新后只需“接上游的界面改动 + 补/改我们自己的资源与绑定”，维护成本低得多。  

因此，**若以“后续跟进上游更新”为主**，更推荐采用 **方案 B**。

### 4.3 语言切换与持久化

- **入口**：在设置或主界面增加“语言 / Language”选项（如 ComboBox：English、简体中文等）。  
- **持久化**：将当前选择写入配置文件（与现有 DemulShooter 配置同一或单独小节），下次启动时读取并设置 `CurrentUICulture`。  
- **即时生效**：若希望不重启即切换，可在切换时重新打开主窗体或对已打开窗体再次应用资源（通过 `ComponentResourceManager.ApplyResources` 或重建窗体）。

## 五、工作量与注意点

| 类别 | 数量级 | 说明 |
|------|--------|------|
| Designer 中的控件文本 | 约 150+ 处 | 主窗体约 120+，其余为用户控件；启用 Localizable 后可由设计器/resx 统一管理 |
| 代码中 MessageBox | 约 58 处 | 需改为从资源键取文案和标题，并统一键命名（如 Msg_ConfigSaved、Title_Error） |
| 窗体/控件数 | 1 主窗体 + 5 用户控件 | 每个需参与本地化或集中资源绑定 |
| 新增/修改文件 | 每语言一组 resx | 如 zh-CN、en（若与默认不同）等；若用集中 Strings，则 Strings.resx + Strings.zh-CN.resx 等 |

- **编码**：resx 使用 UTF-8，可正确保存中文等字符。  
- **现有 3 个从 resx 读取的 Label**：可保留现有键名，在对应语言的 resx 中提供翻译即可。  
- **DsDiag**：若也需要多语言，其结构与 GUI 项目类似，可按同样方式处理（Localizable + 启动时设置 CurrentUICulture，或集中 Strings）。

## 六、总结

- **多语言支持完全可行**：现有为 WinForms + resx，无技术障碍。  
- **当前状态**：绝大多数界面文本为 Designer 硬编码，代码中约 58 处 MessageBox 及窗体标题也为硬编码；仅 3 个 Label 已走 resx。  
- **方案选择**：  
  - 若**需要持续跟进上游仓库更新**：推荐 **方案 B**（集中 Strings + 运行时绑定），与上游的 Designer 改动解耦，合并冲突少、维护成本低。  
  - 若**不常合并上游、以本分支独立开发为主**：可采用方案 A（WinForms 标准本地化），与设计器集成更好。  
- 无论哪种方案，都建议：启动时按配置设置 `CurrentUICulture`，代码中 MessageBox/标题等统一从资源键取值，并在设置中提供语言选项与持久化。

---

## 七、方案 B 已实施说明

本项目已按 **方案 B** 实现多语言支持：

- **资源文件**：`DemulShooter_GUI\Properties\Strings.resx`（默认英文）、`Strings.zh-CN.resx`（简体中文），通过 `Strings.Designer.cs` 的 `Strings.Get(key)` 与 `Strings.Culture` 使用。
- **语言切换**：在程序目录的 **config.ini** 中增加一行（无则使用系统/默认语言）：
  - `gui_language=zh-CN` → 简体中文
  - `gui_language=en` 或留空 → 英文
- **启动逻辑**：`Program.Main` 在创建主窗体之前读取 `gui_language`，设置 `Thread.CurrentThread.CurrentUICulture` 与 `Strings.Culture`，主窗体和子控件在 Load 时通过 `ApplyLocalization()` 从 `Strings` 拉取文案。
- **编译**：建议使用 **Visual Studio** 编译 .NET Framework 4.8 项目。若使用 `dotnet build` 出现 MSB3823/MSB3822，可改用 VS 构建，或在 csproj 中启用 `GenerateResourceUsePreserializedResources` 并引用 NuGet 包 `System.Resources.Extensions`。

---

## 八、使用中文：需要替换原版的哪些文件

中文界面**已经内嵌**在本仓库编译出的 `DemulShooter_GUI.exe` 里，不需要单独的语言包。只要用本仓库的编译结果替换原版里对应文件，并设置配置即可。

### 1. 编译本仓库

- 用 **Visual Studio** 打开解决方案，选择 **Release | x86**（或你使用的平台），先编译 **DsCore**，再编译 **DemulShooter_GUI**。
- 编译完成后，在 **DemulShooter_GUI\bin\Release\** 下会得到（至少）：
  - **DemulShooter_GUI.exe**
  - **DsCore.dll**（因 GUI 引用了 DsCore，一般会复制到输出目录）
  - **zh-CN** 文件夹（内含中文资源 DLL，用于界面显示简体中文）

### 2. 替换原版里的文件

在**原版 DemulShooter** 的安装目录（即你平时运行 DemulShooter_GUI 的文件夹）里：

| 原版里的文件或文件夹 | 用本仓库编译出的内容替换 |
|---------------------|---------------------------|
| **DemulShooter_GUI.exe** | 用 `DemulShooter_GUI\bin\Release\DemulShooter_GUI.exe` 覆盖 |
| **DsCore.dll**（若该目录下存在） | 用 `DemulShooter_GUI\bin\Release\DsCore.dll` 覆盖 |
| **zh-CN** 文件夹 | 将 `DemulShooter_GUI\bin\Release\zh-CN` **整个文件夹**复制到原版目录下（与 exe 同级）；若原版已有 zh-CN 文件夹则覆盖。 |

**使用中文时必须复制 zh-CN 文件夹**，否则程序会回退到英文界面。其它文件（如 `DemulShooter.exe`、`DemulShooterX64.exe`、`config.ini` 等）**不必**从本仓库替换，保留原版即可。

### 3. 让界面显示中文

在同一目录下找到 **config.ini**（没有就新建一个），确保其中有：

```ini
gui_language=zh-CN
```

保存后，**重新运行 DemulShooter_GUI.exe**，界面即为简体中文。

### 4. 小结

- **必须替换/复制**：`DemulShooter_GUI.exe`、同目录下的 `DsCore.dll`（若存在）、以及 **zh-CN 文件夹**（与 exe 同级）。
- **必须设置**：`config.ini` 里 `gui_language=zh-CN`。
- **说明**：中文文案在 **zh-CN** 文件夹内的资源 DLL 中，未复制该文件夹时界面会显示英文。
