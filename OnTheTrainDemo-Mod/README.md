# On The Train Demo Mod (MelonLoader)

基于 **MelonLoader** 框架开发的《On The Train Demo》作弊 / 训练器模组（v1.5.6）。

开发者：**DestinyWind**

这是一个用于单人 / 联机游戏的作弊模组，通过 **静态 Harmony 补丁**实现常驻功能（无敌、无限体力等），
通过 **IMGUI 菜单**提供一键给物品、物品浏览器等交互能力。所有文案支持多语言（默认简体中文）。

> 仅用于学习交流，请勿在联机对战中对其他玩家造成不良体验。

---

## 快捷键

| 按键 | 功能 |
|---|---|
| **F5** | 物品浏览器（浏览 / 搜索 / 一键给予所有已加载物品） |
| **F6** | 训练器菜单（作弊开关、一键资源、语言切换） |

---

## 功能一览

### 常驻作弊（Harmony 静态补丁，开启时几乎零开销）

| 功能 | 说明 |
|---|---|
| God Mode（无敌） | 拦截伤害来源，免疫丧尸攻击 / 饥渴掉血，治疗仍生效 |
| Infinite Vitals（满血/满饥渴/满水） | 每帧将本地玩家 Hp / Food / Water 锁定至上限 |
| Infinite Stamina（体力无限） | 跳过体力消耗循环 |
| Infinite Ammo（弹药无限） | 注入武器 `InfiniteAmmo` 标志，射击不消耗弹药 |
| Infinite Train Fuel（火车燃料无限） | 阻止火车燃料 SyncVar 下调 |
| Infinite Slot Capacity（格子容量无限） | 每格堆叠上限设为 `int.MaxValue`（需重启生效） |
| Free Craft（免费制造） | 制造时跳过材料消耗，与游戏创造模式效果一致 |

### 按钮触发（菜单内点击）

| 功能 | 说明 |
|---|---|
| 一键给资源 | 木材 / 石头 / 煤 / 铁矿石 / 铜 / 树枝 一键 +50 |
| 自定义给物品 | 输入物品名 + 数量给予任意物品 |
| 物品浏览器 | F5 打开，按分类浏览全部物品，点击给予一格堆满 / Shift+点击给予 10 堆 / 右键给予 1 个 |
| 砍树 / 挖矿附近 | 对附近树木、矿点调用致命一击（尽力而为） |
| 列出物品名 | 在控制台打印所有可用物品名 |
| 跳到白天 | 实验性，推进游戏昼夜循环 |
| 信息叠层 | 实时显示 FPS 与本地玩家坐标 |

### 其他特性

- **配置持久化**：所有开关通过 MelonPreferences 保存，重启后保留。
- **多语言（i18n）**：菜单文案外置 JSON 语言文件，默认简体中文，可自行新增语言；菜单内可即时切换 / 重载。
- **健壮性**：所有补丁注册包在 try/catch 内，找不到目标时输出警告并跳过，不影响其他功能。
- **静态实现**：常驻作弊均为一次性注册的 Harmony 补丁，无逐帧反射、无 `FindObjectsOfType` 轮询，不会掉帧。

---

## 目录结构

```
OnTheTrainDemo-Mod/
├── OnTheTrainDemoMod.csproj   # net472 工程 + MelonLoader/Harmony 引用 + 内嵌 lang 资源
├── src/                       # 模组源码
│   ├── Main.cs                # MelonMod 入口（F5/F6 监听）
│   ├── Settings.cs            # MelonPreferences 配置项
│   ├── Patches.cs             # 静态 Harmony 补丁（God/Vitals/Stamina/Ammo/Fuel/Inventory/FreeCraft）
│   ├── Cheats.cs              # 按钮触发动作（SkipToMorning、获取坐标）
│   ├── Items.cs               # 一键给物品 / 砍挖 / 物品浏览器数据源
│   ├── ItemBrowserUI.cs       # F5 物品浏览器 IMGUI
│   ├── MenuUI.cs              # F6 训练器菜单与信息叠层
│   ├── I18n.cs                # 国际化（内嵌资源 + 外部文件 + 极简 JSON 解析）
│   └── ReflectionUtil.cs      # 运行时反射工具
├── lang/                      # 语言文件（同时作为内嵌资源打包）
│   ├── zh-CN.json             # 简体中文（默认）
│   └── en-US.json             # 英文
├── lib/                       # 编译期引用程序集（Private=false，不打包进产物，需自行准备）
├── build/                     # 构建脚本 / MelonLoader 发行包（不纳入版本管理）
├── examples/                  # Harmony 补丁示例（不编译）
└── README.md
```

---

## 构建

### 前置条件

- **.NET 8 SDK**（或更高）— 用于编译 net472 目标。
- 工程通过 `Microsoft.NETFramework.ReferenceAssemblies` 包提供 net472 引用程序集，
  无需单独安装 Visual Studio 或 .NET Framework Targeting Pack，跨平台可编译。

### 准备 `lib/` 目录

工程以 `Private=false` 引用 `lib/` 下的程序集（不打包进产物，运行时由游戏 / MelonLoader 提供）。
首次构建前，请将以下 DLL 放入 `lib/` 目录：

| DLL | 来源 |
|---|---|
| `MelonLoader.dll` | [MelonLoader.x64.zip](https://github.com/LavaGang/MelonLoader/releases) 解压后的 `net472/MelonLoader.dll`（Mono 游戏用 net472 分支） |
| `0Harmony.dll` | 同上，解压后的 `net472/0Harmony.dll` |
| `UnityEngine.CoreModule.dll` | 游戏安装目录 `*_Data/Managed/` 下拷贝 |
| `UnityEngine.IMGUIModule.dll` | 同上 |
| `UnityEngine.InputLegacyModule.dll` | 同上 |
| `UnityEngine.UI.dll` | 同上 |
| `UnityEngine.TextRenderingModule.dll` | 同上 |

> 国内网络下载 MelonLoader 较慢时，可使用 gh-proxy 镜像加速。
> 游戏为 Unity **Mono** 后端，故使用 net472；Il2Cpp 游戏才用 net6.0。

### 编译

```bash
dotnet build -c Release
```

产物：`bin/Release/OnTheTrainDemoMod.dll`（含内嵌语言文件）。

---

## 部署到游戏

将构建产物部署到游戏目录的 `Mods/` 下：

```
<游戏根目录>/Mods/
├── OnTheTrainDemoMod.dll      # 模组主程序集
└── lang/
    ├── zh-CN.json             # 简体中文（可选；首次启动会从 DLL 内嵌资源自动释放）
    └── en-US.json             # 英文（可选）
```

> 首次启动后，模组会自动把内嵌的 `zh-CN.json` / `en-US.json` 释放到 `Mods/lang/`（若已存在则不覆盖），
> 之后可直接编辑这些 JSON 文件自定义文案，菜单内点"重载语言文件"即时生效。

游戏需已安装 MelonLoader（将 `MelonLoader.x64.zip` 解压到游戏根目录，使 `version.dll` 与游戏 exe 同级）。

---

## 配置

首次运行后在 `MelonLoader/UserData/OnTheTrainDemo.cfg` 生成，可直接文本编辑：

```ini
[On The Train Demo]
GodMode = false
InfiniteVitals = false
InfiniteStamina = false
InfiniteAmmo = false
InfiniteFuel = false
InfiniteInventoryCapacity = false
FreeCraft = false
ShowOverlay = true
Language = zh-CN
```

---

## 多语言（i18n）

- 默认 `zh-CN`（简体中文）。改 `Language = en-US` 切换英文。
- 语言文件为扁平 `string -> string` JSON，键名格式如 `cheat.godmode`、`item.Wood`。
- 新增语言：复制 `zh-CN.json` 改名（如 `ja-JP.json`），翻译 value，配置 `Language = ja-JP` 即可。
- 菜单内有语言选择器（多语言按钮网格）和"重载语言文件"按钮，编辑后无需重启。
- 物品名翻译键格式为 `item.<itemName>`，大小写不敏感匹配。

---

## 技术说明

- **静态 Harmony 补丁**：常驻作弊在 `OnInitializeMelon` 一次性注册，运行时零逐帧反射、
  零 `FindObjectsOfType` 轮询。每个前缀只读一个 `Settings` 开关，关闭时几乎零开销，开启时短路原方法。
- **反射定位**：模组不在编译期引用 `Assembly-CSharp.dll`，补丁目标用反射按类名 / 方法名定位，
  对游戏小版本更新有较好兼容性。补丁目标的字段名均经反编译确认。
- **无负重系统**：经反编译核查，游戏本体无负重系统，背包只受"格子堆叠容量"限制，
  故 Infinite Slot Capacity 直接将每格 `maxCapacity` 设为 `int.MaxValue` 即可。
- **国际化**：`I18n.cs` 内置极简扁平 JSON 解析器，不依赖外部库，net472 开箱即用，
  支持标准转义。加载优先级：外部文件 > 内嵌资源 > 内置英文兜底。
