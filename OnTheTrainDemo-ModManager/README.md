# On The Train Demo - 模组管理器

游戏内模组管理器，按 F1 显示当前已加载的所有 MelonLoader 模组信息。

## 功能

- 按 F1 显示/关闭模组管理器面板
- 列出所有已加载成功的模组：
  - 模组名称
  - 版本号
  - 作者
  - 所在 DLL 文件名
- 显示 MelonLoader 框架版本与游戏信息
- 支持按模组名/作者过滤搜索

## 快捷键

| 快捷键 | 功能 |
|--------|------|
| F1 | 显示/关闭模组管理器面板 |

## 安装

本模组不单独发布，已包含在以下模组压缩包中：
- 公开大厅模组压缩包
- 作弊模组压缩包

## 编译

```powershell
# 将游戏 Managed 目录的 dll 复制到 lib/ 后
dotnet build -c Release
```

## 版本

见 [CHANGELOG.md](./CHANGELOG.md)

## 作者

DestinyWind
