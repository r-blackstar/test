# On The Train Demo - Public Lobby Mod

On The Train Demo 游戏的公开大厅模组（MelonLoader）。

游戏原生 `SteamLobby.HostLobby` 用 `(ELobbyType)(lobbyMode==0)` 创建大厅，只能得到 `Private`/`FriendsOnly`，陌生人无法搜到。本模组通过 Harmony 补丁强制使用 `k_ELobbyTypePublic` 创建大厅，并额外调用 `RequestLobbyList()` 拉取所有公开大厅。

## 功能

- **F8** 打开/关闭公开大厅控制面板
- 开启公开模式后，建主时大厅类型改为 `Public`，陌生人可搜到
- 加入游戏时额外调用 `RequestLobbyList` 拉取公开大厅列表
- 显示 Steam 状态、当前大厅 ID、搜索结果等调试信息

## 安装

1. 确保已安装 [MelonLoader](https://melonwiki.xyz/) 到 On The Train Demo
2. 将 `OnTheTrainDemoPublicLobby.dll` 复制到游戏的 `Mods/` 目录
3. 启动游戏，按 F8 打开面板

## 构建

```powershell
dotnet build -c Release
```

需要以下 DLL 放在 `lib/` 目录（从游戏 `MelonLoader/Managed/` 复制）：
- MelonLoader.dll
- 0Harmony.dll
- Assembly-CSharp.dll
- com.rlabrecque.steamworks.net.dll
- Mirror.dll
- UnityEngine.CoreModule.dll / UnityEngine.IMGUIModule.dll / UnityEngine.InputLegacyModule.dll

## 版本

- v1.0.1：增加详细日志、手动搜索按钮、搜索状态显示
- v1.0.0：初始版本

## 作者

DestinyWind
