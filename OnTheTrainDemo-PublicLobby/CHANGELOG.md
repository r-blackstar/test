# 公开大厅模组 更新日志

## v1.0.2 - 2026-07-27

- **默认开启公开大厅模式**：安装即生效，不再需要手动启用
- 移除 F8 快捷键，改为屏幕侧边的圆形按钮，点击弹窗显示大厅信息和成员列表
- 弹窗显示当前大厅 ID、大厅名称、房主地址、成员数/上限
- 实时列出大厅成员昵称与 Steam ID，标记房主
- 仅保留 cfg 文件开关供高级用户关闭（`PublicLobby=false`）

## v1.0.1 - 2026-07-27

- 增加详细日志：大厅创建、搜索结果、成员信息
- 增加手动搜索按钮、搜索状态显示
- F8 控制面板显示 Steam 状态与大厅信息

## v1.0.0 - 2026-07-27

- 初始版本
- 通过 Harmony 补丁修改 `SteamLobby.HostLobby`，强制使用 `k_ELobbyTypePublic` 创建大厅
- 通过 Harmony 补丁修改 `SteamLobby.GetLobbiesList`，额外调用 `RequestLobbyList` 拉取公开大厅
- 注册 `LobbyMatchList_t` 回调，把公开大厅加入游戏原生 lobbyIDs 列表
