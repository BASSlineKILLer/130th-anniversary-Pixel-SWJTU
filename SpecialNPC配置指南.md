# SpecialNPC 配置指南（策划版）

> 适用角色：策划 / 关卡设计  
> 文档更新日期：2026-05

---

## 一、什么是 SpecialNPC

**Special NPC（特殊 NPC）** 是指有专属对话剧情、固定站位、独特立绘的角色，区别于系统批量生成的普通 NPC。

| 对比项 | 普通 NPC | Special NPC |
|---|---|---|
| 来源 | 后台系统批量分配 | 策划手动放置在场景中 |
| 位置 | 随机移动 | 固定站在指定位置 |
| 对话 | 仅一条消息 | 多轮对话（按 E 逐条推进） |
| 立绘 | 使用头像图片 | 可配置专属立绘 |
| 群体剧情 | 不支持 | 支持多 NPC 联合对话 |
| 彩蛋标记 | 无 | 可在任务面板显示金色边框 |

---

## 二、配置流程总览

```
① 创建 SpecialNPCEntry 资产（定义角色数据）
        ↓
② 【可选】创建 DialogueScript 资产（群体剧情）
        ↓
③ 在场景中放置 SpecialNPC 游戏对象
        ↓
④ 【可选】注册到 SpecialNPCData（任务面板/故事文本）
        ↓
⑤ 确认 SpecialNPCManager 存在于场景
```

---

## 三、步骤详解

### 步骤 ① — 创建 SpecialNPCEntry 资产

**SpecialNPCEntry** 是存储单个特殊 NPC 所有数据的配置文件。

**操作：**
1. 在 Project 窗口，进入目录 `Assets/Resources/NPCData/SpecialEntries/`
2. 右键 → **Create → NPC → Special NPC Entry**
3. 将文件命名为该 NPC 的名字（如 `张三.asset`）

**字段说明：**

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `npcName` | 文本 | ✅ | NPC 显示名称，**必须唯一** |
| `worldSprite` | Sprite | ✅ | 在场景中显示的像素图 |
| `portrait` | Sprite | 可选 | 对话框中显示的立绘；为空则自动用 worldSprite |
| `dialogueLines` | 文本数组 | ✅ | 多轮对话内容，按 E 逐条推进 |
| `repeatable` | 开关 | — | 对话结束后是否允许再次触发（默认开） |
| `groupScript` | DialogueScript | 可选 | 配置后替代 dialogueLines，变为群体剧情模式 |
| `isEasterEgg` | 开关 | — | 是否为彩蛋 NPC（任务面板显示金色边框） |

**示例 — 单 NPC 对话：**
```
npcName:        张三
worldSprite:    [拖入像素图]
dialogueLines:
  [0] 你好，欢迎来到交大！
  [1] 这里的图书馆我最喜欢了。
  [2] 好好学习，天天向上！
repeatable:     ✓（打勾）
```

---

### 步骤 ② — 创建 DialogueScript 资产（群体剧情，可选）

**适用场景：** 需要两个或多个 NPC 轮流说话的剧情段落。

**操作：**
1. 进入 `Assets/Resources/NPCData/SpecialEntries/dialogue/`
2. 右键 → **Create → NPC → Dialogue Script**
3. 命名为剧情 ID（如 `roommate_intro.asset`）

**字段说明：**

| 字段 | 类型 | 说明 |
|---|---|---|
| `scriptId` | 文本 | 唯一 ID，建议英文（用于存档记录，如 `scene1_intro`） |
| `repeatable` | 开关 | 完成后是否可重复触发（默认关） |
| `medalNpcIds` | 文本数组 | ⚠️ **必填（想发勋章时）**：对话完成后向系统提交的 NPC ID 列表，填哪些 ID 就给哪些 ID 各发一枚勋章。留空则本次群体剧情**不发放任何勋章**。 |
| `dialogueLines` | 数组 | 台词序列，见下方 |

**每条台词（DialogueLine）字段：**

| 字段 | 说明 |
|---|---|
| `speakerName` | 说话者名字，必须与某个 `SpecialNPCEntry.npcName` 完全一致；**留空 = 旁白**，沿用主 NPC 立绘 |
| `text` | 台词内容 |
| `portraitOverride` | 临时覆盖立绘（可选，一般留空） |

**示例 — 双人对话：**
```
scriptId: dormitory_argument
repeatable: ✗

dialogueLines:
  [0] speakerName: 张三   text: 室友，你昨晚几点回来的？
  [1] speakerName: 李四   text: 哦，大概凌晨两点吧……
  [2] speakerName: 张三   text: 下次早点回来！
  [3] speakerName: （空）  text: 两人陷入沉默。
```

> ⚠️ **`speakerName` 必须与场景中 SpecialNPC 的 `npcName` 完全匹配**，系统才能找到对应立绘。
> 若填写后立绘仍未出现，请打开 Unity Console 查看是否有 `[NPCInteraction] 群体剧情：找不到说话者 "xxx"` 的警告，根据提示修正 `speakerName`。
> 找不到时立绘会留空（不会错误地显示别的 NPC 的脸）。

配置好后，将此 `DialogueScript` 资产拖入主 NPC 的 `SpecialNPCEntry.groupScript` 字段即可。拖入后 `dialogueLines` 字段自动被忽略。

**示例 — 群体剧情且对话结束后发勋章：**
```
scriptId:      dormitory_argument
repeatable:    ✗
medalNpcIds:   [0] 张三   [1] 李四   ← 对话完成后同时给张三和李四各发一枚勋章

dialoguelines:
  [0] speakerName: 张三  text: 室友，你昨晚几点回来的？
  [1] speakerName: 李四  text: 哦，大概凌晨两点吧……
```

---

### 步骤 ③ — 在场景中放置 SpecialNPC

1. 在 Hierarchy 中，右键 → **Create Empty**，重命名（如 `SpecialNPC_张三`）
2. 在 Inspector 中，**Add Component → SpecialNPCController**
3. 将步骤①创建的 `SpecialNPCEntry` 资产拖入 `specialData` 字段
4. 调整 GameObject 在场景中的位置到目标站位
5. 若需要 NPC **朝左**，勾选 Inspector 中的 `Face Left` 字段即可
6. 运行游戏，NPC 会自动显示 Sprite 并在玩家靠近时显示气泡提示

> **无需手动添加 SpriteRenderer 或 Collider**，`SpecialNPCController` 继承自 `NPCController`，这些组件会自动挂载。

> ⚠️ **不要**通过修改 Transform 的 Scale X 为负值来翻转 NPC——这会导致头顶气泡也一起镜像。请统一使用 `Face Left` 字段控制朝向。

---

### 步骤 ④ — 注册到 SpecialNPCData（任务面板显示，可选）

`SpecialNPCData.asset` 用于**任务面板（TaskPanel）** 展示 NPC 的简介文字和故事背景。

**操作：**
1. 在 Project 窗口找到 `Assets/Resources/SpecialNPC/SpecialNPCData.asset`，点击选中
2. 在 Inspector 的 `Entries` 列表中，点击 `+` 新增一行
3. 填写三个字段：

| 字段 | 说明 |
|---|---|
| `specialNPCEntry` | 拖入步骤①创建的 .asset 文件 |
| `panelText` | 任务面板卡片上显示的简短介绍（1-2 句） |
| `storyText` | 点击卡片后展开的详细故事背景 |

---

### 步骤 ⑤ — 确认场景中有 SpecialNPCManager

每个游戏场景中需要存在一个挂载了 `SpecialNPCManager` 组件的 GameObject。

- 通常已放置在场景的 `GameManagers` 父物体下，**一般不需要重复添加**
- 运行时可在 Console 看到日志：`[SpecialNPCManager] 已注册 X 个特殊 NPC`，确认注册数量正确

---

## 四、快速核对清单

配置完成后按此清单自查：

- [ ] `SpecialNPCEntry.npcName` 已填写且在本场景内唯一
- [ ] `worldSprite` 已拖入像素图
- [ ] `dialogueLines` 至少有 1 条台词（或 `groupScript` 已配置）
- [ ] 场景中 GameObject 的 `specialData` 字段已拖入 Entry 资产
- [ ] GameObject 已放置在场景正确位置
- [ ] NPC 朝向用 `Face Left` 字段控制，**不用** Scale X 负值
- [ ] 群体剧情想发勋章时，`DialogueScript.medalNpcIds` 不为空
- [ ] 群体剧情的每条 `speakerName` 与场景中对应 NPC 的 `npcName` 完全一致
- [ ] （可选）SpecialNPCData.asset 已添加对应条目
- [ ] 场景中存在 `SpecialNPCManager`

---

## 五、常见问题

**Q：角色站在场景里但没有图/透明的？**  
A：检查 `SpecialNPCEntry.worldSprite` 是否已拖入 Sprite 资产。

**Q：靠近 NPC 但没有触发对话？**  
A：确认 `dialogueLines` 数组不为空，且 `SpecialNPCController.specialData` 字段不为空。

**Q：群体剧情里另一个 NPC 没有显示立绘，只有名字？**  
A：检查 `DialogueLine.speakerName` 是否与对应 `SpecialNPCEntry.npcName` **完全一致**（区分大小写和全半角）。

**Q：任务面板里看不到这个 NPC 的卡片？**  
A：需要在 `SpecialNPCData.asset` 的 Entries 列表中手动添加该 NPC 的条目。

**Q：对话只触发一次，之后再也触发不了？**  
A：将 `SpecialNPCEntry.repeatable` 勾选为 ✓（开）；若用 `groupScript`，则在 `DialogueScript.repeatable` 同样勾选。

**Q：群体剧情对话完成后没有获得勋章？**  
A：检查 `DialogueScript.medalNpcIds` 数组是否已填写。这是发勋章的必填字段，留空则不发任何勋章。

**Q：NPC 翻了个方向但头顶气泡也跟着镜像了？**  
A：把 Transform Scale X 改回 `1`，改为在 Inspector 中勾选 `SpecialNPCController` 的 `Face Left` 字段来控制朝向。

**Q：群体剧情立绘显示的是错误的 NPC（或空白）？**  
A：打开 Unity Console 查找警告 `[NPCInteraction] 群体剧情：找不到说话者`，根据提示核对 `speakerName` 的拼写（区分大小写、全半角、空格）。
