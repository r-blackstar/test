# 作弊模组 更新日志

## v1.5.11 - 2026-07-27

### 重写：采集功能语义变更——物品留在原地

**核心变更**：`GatherNearby` 不再把物品直接塞进玩家背包，而是改为「在资源点原位置生成掉落物 + 销毁资源点」。物品作为地上掉落物留在原地，玩家可以自行走过去捡。

**原因**：
- 旧实现 `LootableTerrainItemProgressive` 调用 `FinishLooting()` 只销毁对象但不给物品（物品原本是在 `DistributeItemsForTick` 里按 tick 给的）
- 旧实现 `TreeCollectable/OreCollectable` 调用 `GetDamage(inv, 99999f, point)` 会直接把物品塞进玩家背包
- 用户需求：砍/挖/采集后物品应作为掉落物留在原地，不自动进背包

**新实现**：
- 新增 `SpawnAndDestroyResource` 方法，统一处理 4 类资源点（树/矿/地表拾取/渐进采集）
- 读取资源点的物品信息：
  - `TreeCollectable`/`OreCollectable`：`collectableItemData` + `oreAmount`（+1 模拟原生 health<=0 时的额外掉落）
  - `LootableTerrainItem`/`LootableTerrainItemProgressive`：遍历 `lootableItems` 列表（`List<LootableItemEntry>`）
- 在资源点 `transform.position` 调用 `NetworkSceneObjectSpawner.SpawnDropItemClient`（耐久物品用 `SpawnDropItemClientWithDurability`）生成掉落物
- 标记 `objectServerData.isDestroyed=true` + `AddOrUpdateObject` 同步网络状态，然后 `Destroy` 资源点对象
- 若 spawner 不可用则不销毁资源点，避免物品彻底丢失

## v1.5.10 - 2026-07-27

### 修复：主菜单日志噪音

- 修复 `FindLocalInventory` 在主菜单/未进入游戏场景时（`all.Length == 0`）反复输出 `No local PlayerInventory found` 警告的问题
- 现在 `all.Length == 0` 时静默返回 null（预期情况，无需告警）
- 只有 `all.Length > 1` 且都找不到 `isLocalPlayer` 时才输出 Warning（真正异常）
- `GatherNearby` 前置检查的日志从 Warning 降级为 Msg，提示文案改为「请先进入游戏场景再使用采集功能」

## v1.5.8 - 2026-07-27

### 健壮性加固（基于全量代码审查）

**Critical 修复**：
- C2：移除 `FindLocalInventory` 的 `all[0]` fallback，避免多人模式下把物品给到错误玩家
- C3：`GatherNearby` 协程单例控制（避免并发干扰），每次迭代前检测 inv 是否已销毁
- C4：`LootableTerrainItemProgressive` 的 `player` 字段 null 检查，找不到时跳过该对象
- C5：`FreeCraftPostfix` 恢复失败时用空列表兜底，避免 UI 永久损坏

**High 修复**：
- H1：`ItemCache` 取出时验证 Unity 生命周期，避免持有已销毁 ScriptableObject
- H2：`LocalInventory` 失败冷却（2 秒），避免每帧全场景 `FindObjectsOfType` 扫描
- H3/H5：`MenuUI` 和 `ItemBrowserUI` 的 window 回调加 try-catch，IMGUI layout stack 损坏时自动恢复
- H4：`ItemBrowserUI` Shift+点击时防止 `int.MaxValue * 10` 溢出为负数
- H6：`I18n.Get` 的 `string.Format` 失败时返回 key，避免语言文件格式错误导致全局崩溃

**Medium 修复**：
- M7：`Patches.Install` 添加重复安装防护
- M9：`GatherRoutine` max cap 时用 break 而非 yield break，保留汇总日志
- M11：版本号统一更新

## v1.5.7 - 2026-07-27

- `GatherNearby` 范围缩小：radius 40m → 20m，max 40 → 30
- 新增 4 类采集对象支持：
  - `TreeCollectable` - 砍树（GetDamage 瞬采）
  - `OreCollectable` - 挖矿（GetDamage 瞬采）
  - `LootableTerrainItem` - 地表拾取物（Take 方法，蘑菇/草药等）
  - `LootableTerrainItemProgressive` - 渐进式采集（FinishLooting，金属废料等）
- 按钮文案更新：`砍树/挖矿附近` → `采集附近（树/矿/废料/拾取）`

## v1.5.6 - 2026-07-27

- **项目重命名**：`OnTheTrainDemoMod` → `OnTheTrainDemoCheat`（项目文件夹、AssemblyName、namespace、DLL 文件名同步更新）
- 全量汉化：287 个物品名称中英文对照
- 物品浏览器按 12 个分类分组显示（基础原材料→未分类）
- 移除 ESC 按键相关逻辑，避免与游戏原生 ESC 冲突

## v1.5.5 - 2026-07-27

- 物品浏览器分类分组显示，按基础到后期特殊顺序排列
- 新增 12 个分类标题语言键（`tier.10` ~ `tier.99`）

## v1.5.3 - 2026-07-27

- 加入日志记录功能：按 F5 打开物品浏览器时记录所有物品名到日志文件
- 用于获取游戏真实物品名，补充翻译

## v1.5.1 - 2026-07-27

- 物品名称翻译改用模组自带的 `zh-CN.json` 翻译文件
- 翻译格式：`"item.物品英文名": "中文翻译"`

## v1.5.0 - 2026-07-27

- 新增物品浏览器（F5 快捷键）
- 物品堆叠按类型动态计算：Single 类 32/格，x2 类 16/格，x4 类 8/格，x8 类 4/格，MaxSize 类满格
- 物品浏览器交互：左键给一格堆满，Shift+左键给 10 格堆叠，右键给 1 个

## v1.4.0 - 2026-07-26

- 新增 InfiniteVitals（满血/满饥渴/满水），仅影响本地玩家
- 新增 FreeCraft（免费制造，不消耗材料）
- GodMode 同时 patch `ApplyHealthChange` 和 `GetDamage` 方法，防御所有伤害

## v1.0.0 - 2026-07-25

- 初始版本
- 上帝模式、无限体力、无限弹药、无限燃料
- 背包格子容量无限、负重为 0
- 一键给予资源（木材/石头/煤/铁矿/铜/树枝）
