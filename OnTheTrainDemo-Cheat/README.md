# On The Train Demo - Cheat Mod

On The Train Demo 游戏的作弊模组（MelonLoader），包含无敌、无限体力、免费制造、物品浏览器等功能。

## 功能

| 快捷键 | 功能 |
|--------|------|
| F5 | 物品浏览器（按分类浏览所有物品，点击给予） |
| F6 | 训练器菜单（作弊开关 + 一键资源） |

### 作弊功能

- 上帝模式（免伤）
- 满血/满饥渴/满水
- 体力无限
- 弹药无限
- 火车燃料无限
- 背包格子容量无限（需重启）
- 免费制造（不消耗材料）

### 物品浏览器

- 显示所有已加载物品，按游戏进度分类（基础材料 -> 加工材料 -> 食物 -> 工具 -> 武器 -> 建筑 -> 火车 -> 剧情）
- 点击 = 给一格堆满；Shift+点击 = 给10堆；右键 = 给1个
- 支持搜索过滤

### 国际化

- 支持多语言（zh-CN / en-US）
- 语言文件可热重载
- 内嵌语言文件，首次启动释放到磁盘供编辑

## 下载安装

到 [Releases 页面](https://github.com/r-blackstar/test/releases) 下载 `OnTheTrainDemo-Cheat-v1.5.6.zip`，解压后复制到游戏目录即可。

详细安装说明见 [仓库主页 README](../README.md#下载安装)。

## 构建

```powershell
dotnet build -c Release
```

需要以下 DLL 放在 `lib/` 目录（从游戏 `MelonLoader/Managed/` 复制）：
- MelonLoader.dll / 0Harmony.dll
- UnityEngine.CoreModule.dll / UnityEngine.UI.dll / UnityEngine.IMGUIModule.dll / UnityEngine.InputLegacyModule.dll / UnityEngine.TextRenderingModule.dll

## 版本

- v1.5.6：项目重命名 OnTheTrainDemoMod→OnTheTrainDemoCheat、全量汉化（287 个物品）、分类显示、移除 ESC 逻辑
- v1.5.5：物品浏览器分类分组显示
- v1.5.0+：无敌/无限体力/免费制造/物品浏览器

## 作者

DestinyWind
