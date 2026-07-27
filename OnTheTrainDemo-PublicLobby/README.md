# On The Train Demo - 公开大厅模组

On The Train Demo 游戏的公开大厅模组（MelonLoader）。

游戏原生 `SteamLobby.HostLobby` 用 `(ELobbyType)(lobbyMode==0)` 创建大厅，只能得到 `Private`/`FriendsOnly`，陌生人无法搜到。本模组通过 Harmony 补丁强制使用 `k_ELobbyTypePublic` 创建大厅，并额外调用 `RequestLobbyList()` 拉取所有公开大厅。

## 功能

- **默认开启公开大厅模式**：安装即生效，不再需要手动启用
- **屏幕右侧侧边按钮**：点击弹窗显示当前大厅信息与成员列表
- **运行时切换开关**：UI 面板内提供「关闭/开启公开大厅」按钮，无需改 cfg
- 显示当前大厅 ID、大厅名称、房主地址、成员数/上限
- 实时列出大厅成员昵称与 Steam ID，标记房主
- 加入游戏时额外调用 `RequestLobbyList` 拉取公开大厅列表

## 快捷键

无快捷键。屏幕右侧的圆形按钮，点击弹窗显示大厅信息。

## 关闭公开模式

三种方式任选其一：

1. **UI 按钮切换**（推荐）：点击侧边按钮打开面板，按「关闭公开大厅」按钮，运行时即时生效并保存到 cfg
2. **cfg 文件**：修改 `MelonLoader/Preferences/OnTheTrainDemoPublicLobby.cfg` 中 `PublicLobby = false`
3. **删除模组**：从 `Mods/` 目录删除 `OnTheTrainDemoPublicLobby.dll`

关闭后建主与搜索走游戏原方法（好友大厅），游戏原生搜索行为完全恢复。UI 不会关闭，仍可随时查看大厅信息并重新开启。

## 下载安装

到 [Releases 页面](https://github.com/r-blackstar/test/releases) 下载 `OnTheTrainDemo-PublicLobby-v1.0.3.zip`，解压后复制到游戏目录即可。

详细安装说明见 [仓库主页 README](../README.md#下载安装)。

## 构建

```powershell
dotnet build -c Release
```

需要以下 DLL 放在 `lib/` 目录（从游戏 `MelonLoader/Managed/` 复制）：
- MelonLoader.dll / 0Harmony.dll
- Assembly-CSharp.dll
- com.rlabrecque.steamworks.net.dll
- Mirror.dll
- UnityEngine.CoreModule.dll / UnityEngine.IMGUIModule.dll / UnityEngine.InputLegacyModule.dll

## 版本

见 [CHANGELOG.md](./CHANGELOG.md)

## 作者

DestinyWind
