# NPC 系统使用手册（策划版）

> 本文档面向策划，说明如何配置 NPC，不需要改任何代码。

---

## 系统概述

NPC 有两种来源：

| 来源 | 说明 |
|------|------|
| **手动条目** | 策划在编辑器里直接填写，本地固定出现 |
| **API 自动拉取** | 运行时从后端接口获取玩家提交的 NPC，随机分配到各场景 |

两种来源的 NPC 会混合后**随机打乱，均匀分配**到配置的各个游戏场景中。

---

## 核心资产位置

| 资产 | 路径 | 作用 |
|------|------|------|
| `NPCDatabase.asset` | `Resources/NPCData/NPCDatabase` | 总开关 + 手动 NPC 列表 |
| `NPCEntry`（每个手动 NPC） | 任意位置（推荐 `Resources/NPCData/Entries/`） | 单个 NPC 的名字/台词/图片 |
| `NPCDistributor`（场景中） | MainMenu 场景 Hierarchy | 控制哪些场景有 NPC、负责跨场景分配 |
| `NPCManager`（场景中） | 每个游戏场景的 Hierarchy | 控制本场景内的点位和容量 |

---

## 一、配置 NPCDatabase（总数据库）

在 Project 窗口找到 `NPCDatabase.asset`，选中后在 Inspector 中可以看到：

### 字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| **Manual Entries（手动条目）** | 列表 | 直接在这里添加/删除手动 NPC 条目 |
| **Enable Api Fetch** | 勾选框 | 是否开启运行时从后端拉取 NPC（正式版保持勾选） |
| **Api Url** | 文本 | 后端接口地址，一般不需要改 |
| **Clear Cache On Start** | 勾选框 | 调试用：勾选后下次 Play 会强制重新从网络拉取，测试完记得取消勾选 |

---

## 二、添加手动 NPC（单人配置）

### 方法一：通过 NPCDatabase 添加（推荐）

1. 打开 `NPCDatabase.asset` Inspector
2. 在 **Manual Entries** 列表点击 `+`
3. 点击新行旁边的圆形图标，选择已有的 `NPCEntry` 资产；或者先创建一个新的（见下方）

### 方法二：先创建 NPCEntry 再添加

1. 在 Project 窗口右键 → **Create → NPC → NPC Entry**
2. 填写以下字段：

| 字段 | 说明 |
|------|------|
| **Username** | NPC 显示名字 |
| **Message** | NPC 对话内容（对白框里显示的话） |
| **Sprite** | NPC 外观图片（PNG，需先导入 Unity 并将 Texture Type 设为 Sprite） |

3. 把创建好的 `NPCEntry` 资产拖入 `NPCDatabase` 的 **Manual Entries** 列表

> 💡 手动 NPC 使用负数 ID，与 API 来的正数 ID 不冲突，不会重复。

---

## 三、配置哪些场景有 NPC（NPCDistributor）

`NPCDistributor` 挂在 **MainMenu 场景**的某个 GameObject 上，全局只有一个，跨场景保留。

在它的 Inspector 中：

| 字段 | 说明 |
|------|------|
| **Database** | 拖入 `NPCDatabase.asset`；留空则自动从 Resources 路径加载 |
| **Game Scene Names** | 列表，填写需要分配 NPC 的场景名称（精确匹配，区分大小写） |

**示例配置：**

```
Game Scene Names:
  - 1-1
  - 2-0
  - 2-1
  - 2-2
```

所有 NPC（手动 + API）会随机打乱后轮流分配到列表中的每个场景。场景越多、每个场景分到的 NPC 越少。

---

## 四、配置场景内生成点位（NPCManager）

每个游戏场景（如 `1-1`、`2-0`）中都有一个 `NPCManager` 组件，控制本场景的点位和容量。

### 字段说明

| 字段 | 说明 |
|------|------|
| **Npc Prefab Path** | Prefab 路径，默认 `NPCData/NPC`，一般不改 |
| **Spawn Points** | 生成点位列表，拖入场景中的 Transform（空物体即可） |
| **Spawn Point Capacities** | 每个点位最多放几个 NPC（索引与上面的列表对应；留空则每个点位默认容量 1） |
| **Spawn Spread Radius** | 同一点位多个 NPC 的随机偏移半径（单位：Unity 单位），避免重叠 |

### 配置步骤

1. 在场景 Hierarchy 里找到挂有 `NPCManager` 的 GameObject
2. 在场景中创建若干空 GameObject 作为生成点（建议命名为 `SpawnPoint_1` 等，放在想要 NPC 出现的位置）
3. 把这些空 GameObject 拖入 `Spawn Points` 列表
4. 在 `Spawn Point Capacities` 里为每个点位填写容量数字（如 `2` 表示该点最多出现 2 个 NPC）

> ⚠️ 如果 `Spawn Points` 为空，本场景的 NPC 全部会被丢弃并在 Console 报警告。

---

## 五、多场景配置总览（checklist）

每新增一个有 NPC 的场景，需要做的事：

- [ ] 在 **NPCDistributor** 的 `Game Scene Names` 列表里添加该场景名
- [ ] 在该场景里确认有 **NPCManager** 组件
- [ ] 在该场景里放好生成点位，并配置到 `Spawn Points` 列表
- [ ] 按需配置 `Spawn Point Capacities`

---

## 六、常见问题

**Q：场景里有 NPC 但没有图像显示？**  
A：检查 NPCEntry 的 Sprite 字段是否已填，且图片的 Texture Type 是 Sprite（不是 Default）。

**Q：API 的 NPC 没有图像？**  
A：API 来的 NPC 图片是运行时解码的，编辑器 Play 模式下会受网络影响。确认 `Enable Api Fetch` 已勾选，Console 里无 API 请求错误。

**Q：NPC 出现在错误的场景？**  
A：NPC 是随机分配的，每次运行分配结果不同。如果要固定某个 NPC 出现在特定场景，需要将其配置为该场景 NPCDistributor 独享的手动条目（暂不支持，需程序扩展）。

**Q：Console 报 `找不到 Prefab: Resources/NPCData/NPC`？**  
A：确认 `NPC.prefab` 已放在 `Assets/Resources/NPCData/` 目录下（必须通过 Unity Editor 的 Project 窗口移动，不能用系统文件管理器）。
