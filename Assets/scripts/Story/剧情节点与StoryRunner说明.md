# 剧情节点与 StoryRunner 使用说明

## 本次范围

仅新增脚本及本说明，没有创建剧情资产、挂载组件或修改现有 UI、场景、Prefab。
命名空间为 `WitchTrial.Story`。剧情资产保存内容和连接关系，StoryRunner 保存当前进度。
这是一套手动推进的核心流程，不包含文字界面、打字机、自动播放、存档、音频播放或审判特效。

## 文件职责

| 文件 | 职责 |
| --- | --- |
| StoryNode.cs | 所有剧情节点的基类，editorNotes 用于作者备注 |
| DialogueNode.cs | 多句对话、说话人、可选立绘和语音引用、下一节点 |
| ChoiceNode.cs | 分支提示、选项文字和各自的目标节点 |
| TrialNode.cs | 证言片段、可点击关键词、反驳理由、成功与失败目标 |
| EndNode.cs | 显式结束节点，包含 endingId 和结尾说明 |
| StoryGraph.cs | 入口及可达节点校验 |
| StoryRunner.cs | 当前节点、当前句、审判选择及跳转决策 |
| Editor/StoryRunnerChecks.cs | 无需额外测试包的编辑器核心检查 |

## 配置一段剧情

1. 在 Project 中右键：Create → Witch Trial → Story。
2. 创建 Graph，并创建 Dialogue、Choice、Trial、End 等节点资产。
3. 在 Inspector 填写节点内容，通过拖拽资产连接 next、success、failure。
4. 将开始节点拖到 Graph 的 entry。
5. 后续接入场景时，在独立、持续激活的对象上挂 StoryRunner，将 Graph 拖到 story。
6. 默认不会自动开始。UI 订阅事件后调用 `runner.Play()`；勾选 playOnStart 则在 Start 自动开始。

建议的最小剧情连接：

```text
开场对话 → 选择
           ├─ 追问 → 审判
           │          ├─ 正确 → 成功反馈对话 → End
           │          └─ 错误 → 失败反馈对话 → 同一个审判（重试）
           └─ 离开 → 另一结尾 End
```

Dialogue 的 lines 至少一句。玩家第一次进入时显示第 0 句；
每次 Advance 显示下一句，最后一句之后才跳到 next。
空目标不会被当成正常结束，必须使用 End。

Choice 的索引从 0 开始。选择一个选项只进入其目标，不会同时推进目标节点。
所有选择都是作者直接配置的跳转；本阶段没有条件分支或剧情变量。

## 审判配置示例

证言：“昨晚我从未离开宿舍。”

statement 按顺序配置三个片段：

| text | keywordId |
| --- | --- |
| 昨晚我 | 留空 |
| 从未离开 | alibi |
| 宿舍。 | 留空 |

keywords 配置：

- id：alibi
- prompt：哪项证据能推翻这句话？
- correctReasonId：door_record

reasons 配置：

| id | text |
| --- | --- |
| door_record | 门禁记录显示你在 22:10 离开宿舍 |
| guess | 我觉得你在说谎 |

success 连接成功反馈 Dialogue，failure 连接失败反馈 Dialogue。
失败对话的 next 可以接回 Trial；重新进入时已选关键词会清空。
每个关键词都必须配置一个存在的正确理由，理由列表由该 Trial 的所有关键词共享。
ID 区分大小写；关键词 ID 和理由 ID 各自在节点内唯一。

UI 根据 statement 的 keywordId 是否为空，决定显示普通文字或红色可点击文字。
Runner 不直接处理 TMP 或鼠标坐标。可用 TMP 的 link ID 传回 keywordId，
也可以为关键词创建按钮。生成富文本时，UI 应正确转义作者输入。

完整交互顺序：

1. NodeEntered 收到 TrialNode，UI 按片段展示证言。
2. 点击红字，调用 SelectKeyword("alibi")。
3. KeywordSelected 发布对应关键词，UI 显示 prompt 和理由按钮。
4. 点击理由，调用 SubmitReason("door_record")。
5. TrialResolved 先发布对错，随后 NodeEntered 发布成功或失败反馈节点。
6. 正常推进反馈对话，进入结尾或返回审判。

TrialResolved 适合触发即时音效、记录结果；如果需要停留播放动画，
由 UI 暂存展示内容并屏蔽输入，或将文字反馈放在目标 Dialogue。
Runner 本阶段没有异步等待动画的节点。

## 对外接口

| 调用/属性 | 含义 |
| --- | --- |
| Play() | 校验并从 Inspector 配置的 Graph 入口重新开始 |
| Play(graph) | 校验并切换到指定 Graph，从头开始 |
| Advance() | 仅对话状态可用；下一句或进入 next |
| Choose(index) | 仅选择状态可用；进入对应分支 |
| SelectKeyword(id) | 仅审判状态可用；选择或更换关键词 |
| SubmitReason(id) | 已选择关键词后提交理由，判定并跳转 |
| Stop() | 清空当前进度，回到 Idle；可再次 Play |
| CurrentNode / State | 当前节点和运行状态 |
| CurrentLine / LineIndex | 当前对话句及从 0 开始的索引；非对话时为 null / -1 |
| SelectedKeywordId | 当前审判已选关键词；离开或重新进入时清空 |
| LastError | 被拒绝操作的原因，用于调试或提示 |

所有操作返回 bool。true 表示操作被受理，false 表示拒绝。
**SubmitReason 返回 true 不代表回答正确**，正确与否读取 TrialResolved。
未选关键词、不存在的 ID、越界索引或在错误状态下操作都会被拒绝，进度不变。
Play 校验失败也会保留原来的剧情进度。

State 包括 Idle、Dialogue、Choice、Trial、Completed。
到达 End 时保留 CurrentNode，以便读取结尾内容；之后不会接受 Advance。
再次 Play 才重新开始。Stop 不触发 Completed。
禁用组件不会自动清空状态；需要结束时显式调用 Stop。

## UI 事件接入

```csharp
using UnityEngine;
using WitchTrial.Story;

public class StoryViewExample : MonoBehaviour
{
    [SerializeField] private StoryRunner runner;

    private void OnEnable()
    {
        runner.NodeEntered += ShowNode;
        runner.DialogueLineChanged += ShowLine;
        // 视图晚于 Runner 启用时，主动读取当前状态补画。
        if (runner.CurrentNode != null) ShowNode(runner.CurrentNode);
        if (runner.CurrentLine != null) ShowLine(runner.CurrentLine);
    }

    private void OnDisable()
    {
        runner.NodeEntered -= ShowNode;
        runner.DialogueLineChanged -= ShowLine;
    }

    private void ShowNode(StoryNode node)
    {
        // 按 DialogueNode / ChoiceNode / TrialNode / EndNode 切换展示。
        // Choice 显示 choices，Trial 显示 statement，End 显示 summary。
    }

    private void ShowLine(DialogueLine line)
    {
        // 展示 speaker、text；portrait 和 voice 由 UI/音频系统自行处理。
    }

    public void OnNextClicked()
    {
        // 若正在打字，应先补全文字，本次不调用 Advance。
        runner.Advance();
    }
}
```

其余事件：

- KeywordSelected(TrialKeyword)：展示关键词问题及理由。
- TrialResolved(bool)：审判答案结果。
- Completed(EndNode)：到达结尾时触发一次。
- Stopped()：主动停止时清理显示。

进入节点时先更新所有状态，再派发 NodeEntered。
对话随后派发 DialogueLineChanged，结尾随后派发 Completed。
事件是同步的；不要在事件回调中再次调用推进方法，此类重入会被拒绝。
回调用于刷新 UI，下一次用户输入再推进。UI 应在动画期间禁用按钮，避免跨帧双击跳句。
单个订阅者抛异常会写入 Unity 控制台，其他订阅者仍会收到事件。

## 数据与校验约定

- 运行期间不要修改节点资产内容或连接；Runner 只维护进度，不克隆整张图。
- Graph.Validate() 返回错误列表，检查入口、断链、空对话、空选项、
  关键词/理由 ID、证言关键词引用和可达结尾。
- 只检查入口可达的节点，不扫描 Project 中其他资产。
- 允许失败后重试等回路。存在可达 End 不代表每个分支都必然结束，
  作者仍需检查是否无意配置了无法退出的循环。
- 不写入 PlayerPrefs，不接入现有选项面板。打字机以后可读取
  VisualNovelSettingsData.SecondsPerCharacter，Runner 本身不依赖设置或 UI。

## 验证方法

Unity 菜单：Witch Trial → Story → Run Core Checks。
检查通过时控制台输出 StoryRunner core checks: PASS。
检查仅使用临时内存资产和对象，完成后销毁，不生成示例资产或保存场景。
覆盖对话句序、两条分支、错误答案重试、正确答案结尾、重启、停止、
无效输入、同步重入、校验失败保留进度、错误理由引用及断链。
