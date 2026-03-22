# NPC 系统技术文档

> 最后更新：2026-03-22  
> 适用项目：我和我的像素交大（Unity 2D 像素风俯视角游戏）

---

## 目录

1. [系统概览](#1-系统概览)
2. [架构图](#2-架构图)
3. [文件清单](#3-文件清单)
4. [数据模型层](#4-数据模型层)
5. [数据来源层](#5-数据来源层)
6. [NPC 行为层](#6-npc-行为层)
7. [NPC 管理层](#7-npc-管理层)
8. [对话交互层](#8-对话交互层)
9. [编辑器工具](#9-编辑器工具)
10. [GameManager 状态管理](#10-gamemanager-状态管理)
11. [Prefab 结构](#11-prefab-结构)
12. [资产目录结构](#12-资产目录结构)
13. [使用指南](#13-使用指南)
14. [常见问题](#14-常见问题)

---

## 1. 系统概览

NPC 系统负责在游戏场景中生成、展示和管理 NPC 角色，并提供对话交互功能。

**核心特性：**
- **双数据源**：手动编辑器配置 + HTTP API 远程拉取
- **双层缓存**：JSON 响应缓存 + 图片 PNG 缓存，实现秒加载
- **可视化数据库**：ScriptableObject 架构，编辑器内卡片式管理
- **最近 NPC 检测**：多个 NPC 在附近时，只对距离最近的一个显示交互气泡
- **打字机对话**：逐字显示文字，支持跳过和关闭
- **状态隔离**：对话锁定（isDialogueLocked）与暂停菜单（isPaused）互不干扰

---

## 2. 架构图

```
┌──────────────────────── 编辑器层 ────────────────────────┐
│  NPCDatabaseEditor    NPCEntryEditor                     │
│  (自定义 Inspector)    (大图预览 Inspector)                │
└──────────────────────────────────────────────────────────┘
                          │ 编辑
                          ▼
┌──────────────────────── 数据层 ──────────────────────────┐
│  NPCDatabase (ScriptableObject)                          │
│    ├── manualEntries: List<NPCEntry>   ← 手动条目        │
│    ├── enableApiFetch: bool                               │
│    └── apiUrl: string                                     │
│                                                           │
│  NPCEntry (ScriptableObject)                             │
│    ├── username / message / sprite                        │
│                                                           │
│  NPCData.cs                                              │
│    ├── NPCApiResponse   ← API JSON 包装                  │
│    ├── NPCRawData       ← API 原始字段                   │
│    └── NPCInfo          ← 运行时统一数据                  │
└──────────────────────────────────────────────────────────┘
                          │
              ┌───────────┴───────────┐
              ▼                       ▼
┌───────── 手动 ─────────┐  ┌──── API 远程 ────┐
│ NPCDatabase             │  │ NPCApiService     │
│ .GetManualNPCInfos()    │  │ .FetchNPCs()      │
│ ID: -1, -2, -3...       │  │ ID: 1, 2, 3...    │
└─────────┬───────────────┘  └───────┬──────────┘
          │                          │
          └────────┬─────────────────┘
                   ▼
┌──────────────────────── 管理层 ──────────────────────────┐
│  NPCManager (单例, MonoBehaviour)                         │
│    - Resources.Load Prefab                               │
│    - 按 spawnPoints 顺序生成                              │
│    - Dictionary<int, GameObject> 跟踪                     │
└──────────────────────────┬───────────────────────────────┘
                           │ Instantiate + SetData
                           ▼
┌──────────────────────── 行为层 ──────────────────────────┐
│  NPCController (每个 NPC 实例)                            │
│    - SpriteRenderer 显示角色图                            │
│    - BoxCollider2D (Trigger) 自动尺寸                     │
│    - ShowBubble() / HideBubble()                         │
│    - Info 属性供对话系统读取                               │
└──────────────────────────┬───────────────────────────────┘
                           │ OnTriggerEnter2D / Exit2D
                           ▼
┌──────────────────────── 交互层 ──────────────────────────┐
│  NPCInteraction (挂在 Player 上)                          │
│    - 维护 HashSet<NPCController> nearbyNPCs              │
│    - 每帧计算最近 NPC，独占气泡                            │
│    - 按 E 开启对话 → 打字机 → 再按 E / 点击关闭            │
│    - SetDialogueLock(true/false) 锁定/解锁移动            │
└──────────────────────────────────────────────────────────┘
```

---

## 3. 文件清单

| 文件路径 | 类型 | 职责 |
|---------|------|------|
| `Scripts/NPC/NPCData.cs` | 数据模型 | API 响应类、运行时 NPCInfo |
| `Scripts/NPC/NPCEntry.cs` | ScriptableObject | 单个 NPC 数据条目 |
| `Scripts/NPC/NPCDatabase.cs` | ScriptableObject | NPC 数据库（手动条目 + API 配置） |
| `Scripts/NPC/NPCApiService.cs` | 静态工具类 | HTTP 请求、JSON 解析、Base64 解码、双层缓存 |
| `Scripts/NPC/NPCController.cs` | MonoBehaviour | 单个 NPC 行为（Sprite、Collider、气泡） |
| `Scripts/NPC/NPCManager.cs` | MonoBehaviour 单例 | NPC 生成管理（双数据源、spawnPoints） |
| `Scripts/Player/NPCInteraction.cs` | MonoBehaviour | 对话交互系统（最近检测、打字机、状态机） |
| `Scripts/NPC/Editor/NPCDatabaseEditor.cs` | CustomEditor | 数据库可视化 Inspector |
| `Scripts/NPC/Editor/NPCEntryEditor.cs` | CustomEditor | 条目大图预览 Inspector |
| `Scripts/Core/GameManager.cs` | MonoBehaviour 单例 | 全局状态（暂停 + 对话锁） |

---

## 4. 数据模型层

### NPCData.cs

定义三个数据类：

#### NPCApiResponse
```json
{
    "success": true,
    "data": [ NPCRawData, ... ]
}
```
对应 API 的 JSON 顶层结构。

#### NPCRawData
| 字段 | 类型 | 说明 |
|------|------|------|
| id | int | NPC 唯一 ID（API 返回正数） |
| username | string | 用户名 |
| message | string | NPC 想说的话 |
| config | string | 配置信息（保留字段） |
| image | string | Base64 编码图片（含 data:image/png;base64, 前缀） |
| status | string | 审核状态 |
| created_at | string | 创建时间 |

#### NPCInfo（运行时统一数据）
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 手动条目为负数（-1, -2...），API 为正数 |
| Username | string | 用户名 |
| Message | string | 对话内容 |
| Sprite | Sprite | 角色图片（可能为 null） |

**ID 分配策略**：
- 手动条目：`-(index + 1)`，即 -1, -2, -3...
- API 条目：直接使用服务端返回的正数 ID

---

## 5. 数据来源层

### 5.1 手动数据源

#### NPCEntry.cs
ScriptableObject，存储单个 NPC 的静态数据。

**创建方式**：
- Project 右键 → Create → NPC → NPC Entry
- 或在 NPCDatabase Inspector 中点击「新建 NPC 条目」

**字段**：
| 字段 | 类型 | 说明 |
|------|------|------|
| username | string | NPC 用户名 |
| message | string（TextArea） | NPC 说的话 |
| sprite | Sprite | 角色图片（PNG 导入，Texture Type 设为 Sprite） |

#### NPCDatabase.cs
ScriptableObject，中央数据库。

**字段**：
| 字段 | 类型 | 说明 |
|------|------|------|
| manualEntries | List\<NPCEntry\> | 手动条目列表 |
| enableApiFetch | bool | 是否启用 API 拉取 |
| apiUrl | string | API 地址（默认 `http://devshowcase.site/api/approved`） |

**方法**：
- `GetManualNPCInfos()` → 将 manualEntries 转为 `List<NPCInfo>`，分配负数 ID

### 5.2 API 数据源

#### NPCApiService.cs（静态类）

**核心方法**：
```csharp
static IEnumerator FetchNPCs(string url, Action<List<NPCInfo>> onSuccess, Action<string> onError)
```

**加载策略（两阶段）**：

| 阶段 | 来源 | 行为 |
|------|------|------|
| 1 | 本地 JSON 缓存 | 如果存在，秒加载并立即回调 `onSuccess` |
| 2 | HTTP GET 请求 | 后台请求 API，成功后再次回调 `onSuccess`（更新数据） |

> `onSuccess` 可能被调用两次：第一次是缓存结果（快），第二次是网络结果（新）。

**双层缓存**：

| 缓存类型 | 路径 | 用途 |
|---------|------|------|
| JSON 响应 | `persistentDataPath/NPCCache/api_response.json` | 下次启动秒加载 |
| 图片 PNG | `persistentDataPath/NPCCache/Images/npc_{id}.png` | 避免重复 Base64 解码 |

**Sprite 创建参数**：
- `pixelsPerUnit = 32`（32×32 像素图在场景中显示为 1 个单位）
- `pivot = (0.5, 0)`（底部居中，NPC 站在点位上不飘空）
- `filterMode = FilterMode.Point`（像素风锐利边缘）

**性能诊断**：
使用 `System.Diagnostics.Stopwatch` 记录每个阶段耗时：
- 缓存加载时间
- HTTP 请求时间
- 图片解码时间
- 总耗时

**公共工具方法**：
| 方法 | 说明 |
|------|------|
| `Base64ToSprite(string, float)` | 将 Base64 字符串转为 Sprite |
| `ClearCache()` | 清除全部缓存目录 |
| `GetCacheSize()` | 返回缓存总字节数 |

---

## 6. NPC 行为层

### NPCController.cs

挂在每个 NPC 实例上，负责个体行为。

**必需组件**：`[RequireComponent] SpriteRenderer, BoxCollider2D`

**Inspector 字段**：
| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| bubbleRoot | GameObject | — | 气泡子物体引用 |
| bubbleOffset | Vector3 | (0, 0.3, 0) | 气泡相对于 Sprite 顶部的偏移 |
| triggerPadding | float | 1.5 | 触发区域比 Sprite 大多少 |

**公共属性/方法**：
| 名称 | 说明 |
|------|------|
| `Info` (NPCInfo, get) | 运行时数据，供对话系统读取 |
| `SetData(NPCInfo)` | 设置数据、更新 Sprite、自动调整 Collider/气泡 |
| `ShowBubble()` | 显示气泡（由 NPCInteraction 统一调用） |
| `HideBubble()` | 隐藏气泡 |

**自动调整逻辑**（`AdjustColliderAndBubble`）：
1. 根据 `spriteRenderer.bounds` 获取 Sprite 实际世界尺寸
2. Collider 大小 = Sprite 尺寸 + triggerPadding × 2
3. Collider 偏移 = (0, spriteHeight / 2)（因为 pivot 在脚底）
4. 气泡位置 = (0, spriteHeight, 0) + bubbleOffset

> **注意**：气泡控制权完全在 `NPCInteraction`，NPCController 本身不监听 Trigger 事件。

---

## 7. NPC 管理层

### NPCManager.cs（单例）

**职责**：双数据源加载 → 按点位顺序生成 NPC

**Inspector 字段**：
| 字段 | 类型 | 说明 |
|------|------|------|
| database | NPCDatabase | 数据库引用（留空自动从 Resources 加载） |
| npcPrefabPath | string | Prefab 在 Resources 中的路径（默认 `NPCData/NPC`） |
| spawnPoints | List\<Transform\> | 预定义生成点位 |
| manualCount | int（只读） | 已加载的手动 NPC 数量 |
| apiCount | int（只读） | 已加载的 API NPC 数量 |
| isLoading | bool（只读） | 是否正在加载 API |

**生命周期**：
```
Awake()
  ├── 单例初始化
  ├── Resources.Load<NPCDatabase>("NPCData/NPCDatabase")  (如果 database 为 null)
  ├── Resources.Load<GameObject>("NPCData/NPC")
  └── 创建 "NPCs" 容器子物体

Start()
  └── LoadAllNPCs()
        ├── 阶段1: database.GetManualNPCInfos() → SpawnNPC (负数ID)
        └── 阶段2: FetchApiNPCs() → NPCApiService.FetchNPCs → SpawnNPC (正数ID)
```

**NPC 生成逻辑**（`SpawnNPC`）：
1. 从 `spawnPoints[nextSpawnIndex]` 获取位置
2. `Instantiate` Prefab → 设为 npcParent 子物体
3. 调用 `NPCController.SetData(info)`
4. 加入 `spawnedNPCs` 字典，`nextSpawnIndex++`
5. 点位用完后生成在原点并输出警告

**增量更新**：
- API 返回已存在 ID → 更新 Sprite（而非重复生成）
- API `onSuccess` 可能调用两次（缓存+网络），两次都会正确处理

**公共方法**：
| 方法 | 说明 |
|------|------|
| `LoadAllNPCs()` | 加载全部（手动+API），可外部调用刷新 |
| `FetchApiNPCs()` | 仅触发 API 获取 |
| `ClearAllNPCs()` | 销毁所有已生成 NPC，重置状态 |

---

## 8. 对话交互层

### NPCInteraction.cs（挂在 Player 上）

**前置条件**：
- Player 必须有一个 **Collider2D（Is Trigger = true）** 用于检测 NPC 范围
- Player 必须有 **Rigidbody2D**（已有，PlayerController 中引用）

**Inspector 字段**：
| 字段 | 类型 | 说明 |
|------|------|------|
| dialogueCanvasGroup | CanvasGroup | 对话框面板（用于淡入淡出） |
| npcPortraitImage | Image | 左侧 NPC 立绘 |
| npcNameText | TextMeshProUGUI | 右上角用户名 |
| npcMessageText | TextMeshProUGUI | 右下方说的话 |
| typeSpeed | float | 打字机速度（默认 0.05 秒/字） |
| fadeDuration | float | 淡入淡出时长（默认 0.15 秒） |
| interactKey | KeyCode | 交互键（默认 E） |

### 状态机

```
         按 E（有最近 NPC）
  Idle ──────────────────────► Typing
   ▲                              │
   │                    按 E / 点击（跳过打字）
   │                              ▼
   │         按 E / 点击      Waiting
   └─────────────────────────────┘
              关闭对话
```

| 状态 | 行为 |
|------|------|
| **Idle** | 每帧更新最近 NPC 气泡；按 E 打开对话 |
| **Typing** | 打字机逐字显示 message；按 E 或鼠标左键跳过打字 |
| **Waiting** | 全文已显示；按 E 或鼠标左键关闭对话 |

### 最近 NPC 检测

```
OnTriggerEnter2D → nearbyNPCs.Add(npc)
OnTriggerExit2D  → nearbyNPCs.Remove(npc)

每帧 Update (Idle 状态):
  遍历 nearbyNPCs → 计算距离 → 找最近的
  如果最近的变了 → 旧的 HideBubble(), 新的 ShowBubble()
```

### 打字机效果

- 使用 TMP 的 `maxVisibleCharacters` 属性逐字显示
- 计时用 `Time.unscaledDeltaTime`（不受 timeScale 影响）
- 速度由 `typeSpeed` 控制（Inspector 可调）

### 对话锁定

对话时调用 `GameManager.SetDialogueLock(true)`，关闭时调用 `SetDialogueLock(false)`。
- **不修改 Time.timeScale**
- **不触发暂停菜单**
- PlayerController 检查 `isDialogueLocked` 停止移动
- GameManager 检查 `isDialogueLocked` 忽略 ESC 键

---

## 9. 编辑器工具

### NPCDatabaseEditor.cs

NPCDatabase 的自定义 Inspector，提供：

- **统计信息**：手动条目数量、API 启用状态
- **卡片式列表**：每个条目显示 64px 缩略图 + 用户名 + 消息预览
- **操作按钮**：
  - 「编辑」→ 跳转到 NPCEntry 资产
  - 「删除」→ 确认弹窗后从列表移除（不删除文件）
- **添加按钮**：
  - 「+ 添加现有条目」→ 插入空槽，手动拖入已有 NPCEntry
  - 「+ 新建 NPC 条目」→ 自动在 Entries/ 目录创建新资产并添加到列表
- **API 设置**：enableApiFetch 开关 + apiUrl 输入框

### NPCEntryEditor.cs

NPCEntry 的自定义 Inspector，提供：

- **128px 居中大图预览**
- **字段布局**：NPC 形象（Sprite）、用户名、说的话（TextArea）
- **图片尺寸信息**：显示 width × height px

---

## 10. GameManager 状态管理

### 状态字段

| 字段 | 类型 | 说明 |
|------|------|------|
| isPaused | bool | 暂停菜单状态（ESC 切换） |
| isDialogueLocked | bool | 对话锁定状态（NPC 对话切换） |

### 方法

| 方法 | 行为 |
|------|------|
| `PauseGame()` | isPaused=true, timeScale=0 |
| `ResumeGame()` | isPaused=false, timeScale=1 |
| `SetDialogueLock(bool)` | isDialogueLocked=locked，不修改 timeScale |

### 状态隔离

```
                     isPaused    isDialogueLocked    timeScale
正常游戏               false        false              1
暂停菜单               true         false              0
NPC 对话               false        true               1  (不变)
```

**PlayerController.Update** 中的移动拦截条件：
```csharp
if (GameManager.Instance.isPaused || GameManager.Instance.isDialogueLocked)
{
    rb.velocity = Vector2.zero;
    return;
}
```

**PauseMenuManager** 只监听 `isPaused`，不响应 `isDialogueLocked`。

---

## 11. Prefab 结构

### NPC.prefab（位于 Resources/NPCData/）

```
NPC (GameObject)
├── [SpriteRenderer]    ← 显示 NPC 角色图
├── [BoxCollider2D]     ← Trigger，运行时自动调整大小
├── [NPCController]     ← 脚本组件
│     bubbleRoot → Bubble
│     bubbleOffset = (0, 0.3, 0)
│     triggerPadding = 1.5
└── Bubble (子物体)
      ├── [Canvas]           ← World Space，Sorting Order: 2
      ├── [CanvasScaler]     ← DynamicPixelsPerUnit: 16
      ├── [GraphicRaycaster]
      └── Image (子物体)
            └── [Image]      ← 气泡图片
```

### 对话框 UI 结构（需手动搭建）

```
Canvas (Screen Space - Overlay)
  └── DialoguePanel (Image: 对话框.png)
        ├── [CanvasGroup]        ← 用于淡入淡出
        ├── PortraitImage        (Image, Preserve Aspect, Image Type: Simple)
        ├── NameText             (TextMeshProUGUI)
        └── MessageText          (TextMeshProUGUI)
```

---

## 12. 资产目录结构

```
Assets/
├── Resources/
│   └── NPCData/
│       ├── NPC.prefab              ← NPC Prefab
│       ├── NPCDatabase.asset       ← 数据库实例
│       └── Entries/
│           ├── 惜时.asset           ← 手动 NPC 条目
│           └── ...
├── Scripts/
│   ├── NPC/
│   │   ├── NPCData.cs
│   │   ├── NPCEntry.cs
│   │   ├── NPCDatabase.cs
│   │   ├── NPCApiService.cs
│   │   ├── NPCController.cs
│   │   ├── NPCManager.cs
│   │   └── Editor/
│   │       ├── NPCDatabaseEditor.cs
│   │       └── NPCEntryEditor.cs
│   ├── Player/
│   │   ├── PlayerController.cs
│   │   └── NPCInteraction.cs
│   └── Core/
│       └── GameManager.cs
└── Art/Sprites/UI/textures/
    ├── 对话框.png                   ← 对话框背景
    ├── UI_气泡_对话.png             ← 气泡图片
    └── ...
```

**运行时缓存路径**（`Application.persistentDataPath`）：
```
NPCCache/
├── api_response.json               ← JSON 响应缓存
└── Images/
    ├── npc_1.png                    ← 图片缓存
    ├── npc_2.png
    └── ...
```

---

## 13. 使用指南

### 13.1 添加手动 NPC

1. 选中 `Assets/Resources/NPCData/NPCDatabase.asset`
2. 在 Inspector 中点击 **「+ 新建 NPC 条目」**
3. 自动创建 NPCEntry 资产并跳转 → 填写 username、message，拖入 PNG Sprite
4. 回到 NPCDatabase 确认条目已在列表中

**PNG 导入设置要求**：
- Texture Type: **Sprite (2D and UI)**
- Pixels Per Unit: **32**（与 API Sprite 保持一致）
- Sprite Alignment: **Bottom Center**（pivot = 0.5, 0）
- Filter Mode: **Point (no filter)**（像素风）

### 13.2 配置场景

1. 场景中放一个空物体，挂载 `NPCManager` 脚本
2. 将 NPCDatabase 拖入 `database` 字段（或留空自动加载）
3. 创建空子物体作为 spawn points，拖入 `spawnPoints` 列表
4. Player 上挂载 `NPCInteraction` 脚本
5. Player 上添加一个 **CircleCollider2D（Is Trigger = true）**，半径设为 2~3
6. 搭建对话框 UI，将 CanvasGroup / Image / TMP 拖入 NPCInteraction 对应字段

### 13.3 启用/关闭 API

在 NPCDatabase Inspector 中切换 **「启用 API 获取」** 开关。
- 开启：运行时先加载手动条目，再请求 API 获取更多 NPC
- 关闭：只使用手动条目

### 13.4 清除缓存

在代码中调用：
```csharp
NPCApiService.ClearCache();
```

---

## 14. 常见问题

### Q: NPC 太小 / 太大？
调整 `NPCApiService.DefaultPixelsPerUnit`。值越小 NPC 越大：
- 32px 图 + ppu=32 → 场景中 1 单位
- 32px 图 + ppu=16 → 场景中 2 单位

手动 NPC 需要在 PNG 导入设置中保持一致的 Pixels Per Unit。

### Q: 手动 NPC 和 API NPC 气泡位置不一样？
确保手动导入的 PNG 的 Pixels Per Unit 和 Sprite Alignment 设置与 API 一致（ppu=32, pivot=Bottom Center）。

### Q: 对话框弹出暂停菜单？
对话系统使用 `SetDialogueLock` 而非 `PauseGame`，两者状态隔离。如果出现此问题，检查 NPCInteraction 中是否误调用了 `PauseGame()`。

### Q: 生僻字显示为方块 □？
当前字体 `SourceHanSansSC-7000` 只包含约 7000 个常用汉字。解决方案：
- 避免使用生僻字
- 或生成完整的 TMP 字体资产 / 添加 Fallback 字体

### Q: HTTP 404 错误？
API 地址返回 404，说明服务端接口路径变化或下线。在 NPCDatabase 中关闭 API 获取或更新 URL。

### Q: 点位用完了怎么办？
NPC 会生成在世界原点 (0,0,0) 并输出警告。增加更多 spawn point 即可。
