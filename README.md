# On The Train Demo - 模组合集

[On The Train Demo](https://store.steampowered.com/app/On_The_Train_Demo) 游戏的 MelonLoader 模组合集，包含公开大厅模组和作弊模组。

## 模组列表

| 模组 | 版本 | 快捷键 | 功能 |
|------|------|--------|------|
| **公开大厅模组** (OnTheTrainDemoPublicLobby) | v1.0.1 | F8 | 让陌生人能搜到并加入你的游戏大厅 |
| **作弊模组** (OnTheTrainDemoMod) | v1.5.6 | F5/F6 | 无敌/无限体力/免费制造/物品浏览器等 |

## 下载安装

### 方式一：下载完整包（推荐，开箱即用）

到 [Releases 页面](https://github.com/r-blackstar/test/releases) 下载对应的压缩包：

- `OnTheTrainDemo-PublicLobby-v1.0.1.zip` — MelonLoader 框架 + 公开大厅模组
- `OnTheTrainDemo-CheatMod-v1.5.6.zip` — MelonLoader 框架 + 作弊模组（含中文语言文件）

#### 安装步骤

1. **找到游戏目录**：在 Steam 库右键「On The Train Demo」→「管理」→「浏览本地文件」
2. **解压压缩包**：将下载的 zip 解压，你会看到 `version.dll`、`MelonLoader` 文件夹、`Mods` 文件夹
3. **复制到游戏目录**：把解压出的所有文件/文件夹复制到游戏根目录（覆盖同名文件）
4. **启动游戏**：双击 `On The Train Demo.exe`，首次启动会初始化 MelonLoader（几秒钟）
5. **验证**：启动后出现 MelonLoader 控制台窗口即安装成功

复制后游戏目录结构：

```
On The Train Demo/
├── On The Train Demo.exe
├── version.dll                    ← 新增（MelonLoader 注入器）
├── MelonLoader/                   ← 新增（框架目录）
├── Mods/                          ← 新增（模组目录）
│   ├── OnTheTrainDemoPublicLobby.dll   ← 公开大厅模组
│   └── OnTheTrainDemoMod.dll           ← 作弊模组（按需）
│   └── lang/                            ← 作弊模组语言文件
│       ├── zh-CN.json
│       └── en-US.json
├── On The Train Demo_Data/
└── ...
```

> **同时安装两个模组**：下载两个压缩包，分别解压复制到游戏目录即可，`MelonLoader` 文件夹只需复制一次（内容相同）。

### 方式二：仅源码（开发者）

```powershell
git clone https://github.com/r-blackstar/test.git
cd test/OnTheTrainDemo-PublicLobby
# 将游戏 Managed 目录的 dll 复制到 lib/
dotnet build -c Release
```

## 使用说明

### 公开大厅模组（F8）

| 功能 | 说明 |
|------|------|
| 开启/关闭公开大厅 | 切换公开模式（默认关闭） |
| 邀请好友 | 弹出 Steam 覆盖层邀请对话框 |
| 离开当前大厅 | 退出当前大厅 |
| 手动搜索大厅 | 手动触发公开大厅搜索 |

**工作原理**：游戏原生建主只能创建 Private/FriendsOnly 大厅，陌生人搜不到。本模组通过 Harmony 补丁：
- 建主时强制用 `k_ELobbyTypePublic` 创建大厅
- 加入游戏时额外调用 `RequestLobbyList()` 拉取公开大厅

**重要**：陌生人搜索方也必须安装本模组，否则游戏的「加入游戏」只搜好友列表。

### 作弊模组（F5/F6）

| 快捷键 | 功能 |
|--------|------|
| F5 | 物品浏览器（按分类浏览所有物品） |
| F6 | 训练器菜单（无敌/无限体力/免费制造等） |

## 卸载

删除以下文件即可完全卸载：
- 游戏根目录的 `version.dll`
- 游戏根目录的 `MelonLoader` 文件夹
- `Mods/` 目录下对应的模组 DLL

## 常见问题

**Q: 启动游戏没出现 MelonLoader 控制台？**
A: 检查 `version.dll` 是否在游戏根目录，且未被杀毒软件拦截/删除。

**Q: 控制台显示「SteamManager.Initialized = False」？**
A: 必须通过 Steam 启动游戏，不能直接运行 exe。

**Q: 搜索不到公开大厅？**
A: 确认搜索方也安装了模组且开启公开模式。Demo 版玩家基数小，可能没有其他公开大厅。

**Q: 作弊模组物品名是英文？**
A: 检查 `Mods/lang/zh-CN.json` 是否存在，或在 F6 菜单中切换语言。

## 版本历史

### 公开大厅模组
- v1.0.1：增加详细日志、手动搜索按钮、搜索状态显示
- v1.0.0：初始版本

### 作弊模组
- v1.5.6：全量汉化（287 个物品）、分类显示、移除 ESC 逻辑
- v1.5.5：物品浏览器分类分组显示
- v1.5.0+：无敌/无限体力/免费制造/物品浏览器

## 作者

DestinyWind

## 相关链接

- [MelonLoader](https://melonwiki.xyz/) - 游戏模组加载器
- [On The Train Demo (Steam)](https://store.steampowered.com/app/On_The_Train_Demo)
