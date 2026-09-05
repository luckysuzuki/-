# UI 故障修复记录与 UIPanel 框架使用指南

> 记录日期：2026-09-04  
> 涉及场景：`Assets/Scenes/startscene.unity`  
> 涉及模块：`UIManager`、`UIPanel`、`UIRouter`、`UIRegistry`、`UIPointEnter`

## 1. 本次故障概述

进入 Play Mode 后出现了两个表面现象：

1. 主菜单 `MainPage` 的背景、标题和按钮没有显示。
2. 主菜单按钮上的 `UIPointEnter` 看起来没有响应鼠标进入和移出。

这两个现象并不是同一个脚本单独失效造成的，而是 UI 初始化、页面注册和射线遮挡共同造成的。

## 2. 根因分析

### 2.1 空页面栈被错误判断为“目标页面已在栈顶”

`UIManager.OpenFullscreen` 原来的判断如下：

```csharp
var existingIndex = _fullscreenStack.IndexOf(panel);
if (existingIndex == _fullscreenStack.Count - 1)
{
    panel.RefreshInternal(context);
    return;
}
```

首次打开页面时：

- `_fullscreenStack` 为空，因此 `_fullscreenStack.Count - 1` 等于 `-1`。
- 目标页面不在栈中，因此 `IndexOf(panel)` 也返回 `-1`。
- 两个 `-1` 相等，代码误以为目标页面已经位于栈顶并提前返回。

结果是页面只执行了刷新逻辑，没有执行 `OpenInternal`，所以 `MainPage` 仍处于隐藏状态。

修复后的判断为：

```csharp
if (existingIndex >= 0 && existingIndex == _fullscreenStack.Count - 1)
{
    panel.RefreshInternal(context);
    return;
}
```

只有页面确实存在于栈中时，才允许把它判断为当前栈顶页面。

### 2.2 场景中的 UIManager 没有形成完整的启动配置

场景检查时发现，`UIManager` 没有处于完整可用状态：

- 组件需要保持启用。
- `MainPanel` 和 `OptionsPanel` 需要注册到 `Scene Panels`。
- `Initial Page` 需要指向 `MainPanel`。
- `UIRegistry` 可以作为后续 Prefab 面板的来源，但空 Registry 不会自动发现 Prefab。

如果 `UIManager` 未启用，`UIRouter` 不会完成绑定，页面也不会被注册或打开。如果面板既不在 `Scene Panels` 中，也不在 `UIRegistry` 中，调用 `Open<T>()` 会抛出“未注册面板”的异常。

### 2.3 Options UI 在启动时覆盖主菜单并拦截射线

`OptionCanvas` 原本在场景启动时处于活动状态，而且它拥有自己的 Canvas、GraphicRaycaster 或可接收射线的 Graphic。即使它的部分图像是透明的，也仍然可能位于主菜单上方并拦截鼠标事件。

这会造成两个误导性结果：

- 视觉上以为 `MainPage` 没有显示。
- 鼠标射线没有命中主菜单按钮，于是以为 `UIPointEnter` 没有执行。

实际测试表明 `UIPointEnter` 本身能够正常工作。遮挡移除后，`Button_LoadGame` 成为射线检测的第一命中对象，鼠标进入时图片从 `loadgame_default` 切换到 `loadgame_focus`，移出后能够恢复。

### 2.4 Play Mode 中的脚本重载会丢失非序列化运行时状态

`UIManager` 的页面字典和页面栈都是运行时集合，不会被 Unity 序列化。Play Mode 中发生程序集或 Domain Reload 后，这些集合可能被清空，但场景对象仍然存在。

为此，初始化逻辑被集中到 `Initialize()`，并由 `Awake()` 与 `OnEnable()` 调用。初始化函数带有状态检查，正常情况下不会重复注册；重载后则会重新建立页面缓存、绑定 `UIRouter` 并打开初始页面。

### 2.5 Toggle 与 Dropdown Item 的 Background 收不到完整悬停事件

Toggle 和 TMP_Dropdown Item 的结构通常是 `Selectable 根对象/Background`，Label 与 Background 是同级或分支不同的子对象。旧版 `UIPointEnter` 挂在 Background 上，并只操作当前 GameObject 的 Image。

当射线首先命中 Label 或 Checkmark 时，Unity 只沿“命中对象 → 父对象”查找事件处理器，不会把事件横向发送给同级的 Background。因此 Background 上的 `OnPointerEnter` 不会执行。TMP_Dropdown 还会在展开时动态克隆 Item；旧脚本到 `Start()` 才缓存 Image，新实例有机会先收到事件并产生空引用。

修复后，脚本在 `Awake()` 中初始化。若组件位于 `Selectable.targetGraphic` 子对象上，会自动在 Toggle 或 Dropdown Item 根对象安装运行时代理，并继续修改原来的 Background Image。这样鼠标命中 Label、Checkmark 或 Background 都属于同一个稳定的悬停范围。

## 3. 本次修复内容

### 代码修复

- 修复 `OpenFullscreen` 对空栈的错误判断。
- 将 UI 初始化流程集中到 `Initialize()`。
- 在 `Awake()` 和 `OnEnable()` 中恢复必要的运行时状态。
- 使用 `OpenInitialPageIfNeeded()` 保证初始页面只在页面栈为空时打开。
- 将 `UIPointEnter` 初始化提前到 `Awake()`，增加目标 Image 和 Sprite 的空引用保护。
- 为 Toggle 与 Dropdown Item 的 `targetGraphic` 自动建立根对象事件代理。

### 场景修复

当前 `startscene` 的启动配置为：

- `UIManager`：启用。
- `Scene Panels`：包含 `MainPanel` 和 `OptionsPanel`。
- `Initial Page`：指向 `MainPanel`。
- `OptionsPanel`：由 `UIManager` 管理初始隐藏、打开、关闭和射线状态，不再作为独立覆盖层常驻显示。

### 验证结果

- Play Mode 启动后 `MainPage` 正常显示。
- 全屏页面栈包含 `MainPanel`。
- `OptionsPanel` 可以打开并返回主菜单。
- `UIPointEnter` 的进入与退出换图均正常。
- 4 个普通 Toggle、Dropdown 模板项和 4 个运行时 Dropdown Item 的悬停测试共 9/9 通过。
- EventSystem 射线能够命中主菜单按钮。
- Unity Console 无错误。

## 4. 框架组成

### UIManager

UI 的唯一运行时管理器，负责：

- 绑定 `UIRouter`。
- 注册场景面板。
- 从 `UIRegistry` 创建 Prefab 面板。
- 管理全屏页面栈和弹窗栈。
- 控制页面的显示、隐藏、缓存、返回顺序和弹窗遮罩。

项目中只能存在一个有效的 `UIManager`。默认开启 `persistentAcrossScenes` 时，它应放在启动场景的顶层常驻对象上，不能放在会被某个页面隐藏的子对象中。

### UIRouter

业务代码访问 UI 的统一入口。业务脚本不要自行查找 `UIManager`，也不要直接调用其他页面对象的 `SetActive`。

常用调用：

```csharp
// 打开页面或弹窗。
UIRouter.Open<OptionsPanel>();

// 打开页面并传入上下文。
UIRouter.Open<SavePanel>(saveData);

// 清空之前的全屏页面历史并进入新页面。
UIRouter.Replace<GamePanel>(newGameContext);

// 关闭指定类型的页面。
UIRouter.Close<OptionsPanel>();

// 优先关闭最上层弹窗，否则返回上一张全屏页面。
UIRouter.Back();

// 查询或取得已经注册的页面。
bool open = UIRouter.IsOpen<OptionsPanel>();
OptionsPanel panel = UIRouter.Get<OptionsPanel>();
```

调用前必须保证启动场景中的 `UIManager` 已经执行初始化。不要从另一个对象的 `Awake()` 中抢先调用 `UIRouter`；通常应从 `Start()`、按钮回调或更晚的业务流程中调用。

### UIPanel

所有受框架管理的 UI 页面都必须继承 `UIPanel`。不要在页面业务代码中直接控制根对象的激活状态，应该通过 `UIRouter` 切换页面。

```csharp
using WitchTrial.UI;

public sealed class GalleryPanel : UIPanel
{
    public override UILayer Layer => UILayer.Fullscreen;

    protected override void OnOpening(object context)
    {
        // 首次打开或关闭后再次打开时刷新数据。
    }
}
```

`UIPanel` 根对象最好拥有自己独立的 `CanvasGroup`。可以不手动赋值，让框架自动在页面根对象上取得或添加；如果手动拖拽，必须拖入同一个页面根对象上的 `CanvasGroup`，不要引用父 Canvas 或其他页面的 CanvasGroup，否则隐藏一个页面时可能连带影响整个 UI。

### UIRegistry

`UIRegistry` 保存可以按需实例化的页面 Prefab。调用 `UIRouter.Open<T>()` 时，如果场景中没有该类型的实例，`UIManager` 会在 Registry 中寻找完全相同类型的 Prefab并创建它。

注意：

- 每种具体 `UIPanel` 类型只能注册一个 Prefab。
- Registry 中必须拖入带有对应 `UIPanel` 组件的 Prefab，而不是普通 GameObject。
- Registry 不会扫描整个 Assets 目录，忘记添加 Prefab 就等于没有注册。
- 同一类型不要在 `Scene Panels` 中放入两个实例。

### UILayer

框架有两个导航层：

- `Fullscreen`：全屏页面。打开新页面时会覆盖并隐藏上一页，`Back()` 会恢复上一页。
- `Popup`：弹窗。显示在当前全屏页面上方，关闭后下面的页面保持不变。

页面应明确声明自己的层：

```csharp
public override UILayer Layer => UILayer.Popup;
```

## 5. 新增 UIPanel 的标准流程

### 5.1 创建页面脚本

新建继承 `UIPanel` 的具体类型，明确它是全屏页面还是弹窗，并使用生命周期回调处理页面数据。

```csharp
using WitchTrial.UI;

public sealed class SavePanel : UIPanel
{
    public override UILayer Layer => UILayer.Popup;
    public override bool CloseOnMaskClick => true;

    protected override void OnOpening(object context)
    {
        var saveData = context as SaveData;
        RefreshView(saveData);
    }

    protected override void OnRefreshed(object context)
    {
        var saveData = context as SaveData;
        RefreshView(saveData);
    }

    private void RefreshView(SaveData saveData)
    {
        // 更新显示。
    }
}
```

### 5.2 搭建页面根对象

页面根对象应满足：

- 挂载唯一的具体 `UIPanel` 派生组件。
- RectTransform 按设计正确铺满或定位。
- CanvasGroup 位于本页面根对象，或留空让框架自动添加。
- 初始激活状态不是业务逻辑依据；运行时由 `UIManager` 接管。
- 页面内部的按钮、图片和文字引用都在 Inspector 中完成绑定。

### 5.3 选择一种注册方式

方式 A：场景实例

适合启动时就存在的固定页面，例如 `MainPanel`。

1. 把页面对象放入启动场景。
2. 把页面组件拖入 `UIManager/Scene Panels`。
3. 如果它是启动页，同时拖入 `Initial Page`。

方式 B：Prefab + UIRegistry

适合按需创建的页面和弹窗。

1. 将页面制作成 Prefab。
2. 把 Prefab 上的 `UIPanel` 派生组件添加到 `UIRegistry/Panel Prefabs`。
3. 把该 Registry 赋给 `UIManager/Registry`。

通常一个页面选择一种来源即可。使用场景实例时，Registry 中即使保留同类型 Prefab也不会替换现有实例，但这种重复配置容易让维护者误判实际来源，应尽量避免。

### 5.4 连接导航按钮

代码方式：

```csharp
private void OnEnable()
{
    optionsButton.onClick.AddListener(OpenOptions);
}

private void OnDisable()
{
    optionsButton.onClick.RemoveListener(OpenOptions);
}

private void OpenOptions()
{
    UIRouter.Open<OptionsPanel>();
}
```

Inspector 方式：给按钮添加 `UIActionButton`，选择 `Open`、`Replace`、`CloseCurrent` 或 `Back`。`Open` 和 `Replace` 还需要把目标页面 Prefab 或组件拖入 `Destination`。

同一个按钮不要同时使用代码监听和 `UIActionButton` 执行同一次导航，否则一次点击可能触发两次。

## 6. UIPanel 生命周期

- `OnOpening(context)`：页面从关闭状态进入打开状态。
- `OnOpened()`：页面已经激活并完成 CanvasGroup 显示设置。
- `OnRefreshed(context)`：对当前已打开的同一页面再次调用 `Open`。
- `OnCovered()`：全屏页被新全屏页覆盖。
- `OnRevealed()`：上层全屏页关闭后，本页恢复。
- `OnClosing()`：页面即将关闭。
- `OnClosed()`：页面已经关闭。

事件监听建议放在 `OnEnable()`，并在 `OnDisable()` 中成对移除。数据加载或界面刷新放在 `OnOpening()` 和 `OnRefreshed()`，不要只依赖 `Start()`，因为缓存页面再次打开时不会重新执行 `Start()`。

`Keep Alive` 开启时，关闭页面只会隐藏并缓存实例；关闭后再次打开仍是同一个对象。关闭 `Keep Alive` 后，页面退出时会被销毁，下次打开会从 Registry 重新实例化。

## 7. UIPointEnter 使用与排查

当前 `UIPointEnter` 用于把按钮图片在默认 Sprite 和悬停 Sprite 之间切换。组件会在 `Awake()` 中完成初始化，因此也适用于 TMP_Dropdown 在运行时动态生成的 Item。

正确配置要求：

1. `Target Image` 指向需要换图的 Image；留空时使用组件所在 GameObject 上的 Image。
2. `Enter Sprite` 已赋值。
3. 普通按钮可以把组件直接挂在按钮 Image 上。
4. Toggle 和 Dropdown Item 可以继续把组件挂在各自的 `targetGraphic`（Background）上；运行时会自动在 Selectable 根对象安装事件代理，使鼠标位于 Label、Checkmark 或 Background 时都能换图。
5. 场景中存在启用的 `EventSystem` 和正确的 UI Input Module。
6. 所在 Canvas 存在 `GraphicRaycaster`。
7. 按钮 Image 的 `Raycast Target` 已开启。
8. 页面 CanvasGroup 的 `Interactable` 和 `Blocks Raycasts` 在显示时为开启状态。
9. 按钮上方不存在透明 Image、全屏 Canvas 或其他启用 `Raycast Target` 的遮挡对象。

如果悬停没有反应，优先检查 EventSystem 的射线命中对象，而不是立刻判断脚本失效。透明 UI 仍然可以拦截射线；不需要交互的装饰 Image 应关闭 `Raycast Target`。

Dropdown 的模板对象通常处于禁用状态，展开时才会克隆并激活。不要依赖 `Start()` 才缓存组件，因为新 Item 有机会在首帧收到指针事件；当前实现使用 `Awake()`，并对缺失的 Image 和 Enter Sprite 做了保护。

## 8. 重要注意事项

- 不要直接对受管理页面调用 `SetActive`，否则页面对象状态与 UIManager 的栈状态会不一致。
- 不要把 `UIManager` 放在某个 `UIPanel` 内部。
- 不要在场景中保留一个独立、全屏且持续活动的 Canvas 覆盖受管理页面。
- 每个具体页面类型只保留一个运行时实例来源。
- `Initial Page` 必须是具体、非抽象的 `UIPanel`，并能被场景列表注册。
- Popup 自己不要再制作一层重复的全屏遮罩；统一使用 `UIManager` 的 `PopupMask`。
- 页面关闭按钮优先调用 `RequestClose()` 或 `UIRouter.Back()`，不要自行隐藏根对象。
- `Replace<T>()` 只适用于 `Fullscreen` 页面，不能用来替换成 Popup。
- `Close()` 不会关闭全屏栈中仅剩的最后一个页面；切换主流程应使用 `Replace<T>()`。
- `UIRouter.Get<T>()` 只返回已经注册或创建的实例，不负责自动打开页面。
- 业务对象若跨场景调用 UI，应保证常驻 UIManager 没有产生重复实例。

## 9. 提交前回归检查清单

每次新增或修改页面后至少验证：

- [ ] 进入 Play Mode 后初始页面可见。
- [ ] Console 没有未注册页面、重复类型或空引用错误。
- [ ] 打开全屏页时上一页被隐藏，返回后能够恢复。
- [ ] 打开 Popup 时底层页面仍可见，但不会穿透点击。
- [ ] 点击遮罩或关闭按钮后 Popup 正常关闭。
- [ ] 每个按钮只执行一次导航。
- [ ] Hover、点击和键盘/手柄导航都能命中正确对象。
- [ ] 页面关闭再打开时，数据与事件监听没有重复。
- [ ] Domain Reload 或脚本重新编译后，初始页面和路由仍然可用。
- [ ] 切换场景后只存在一个 `UIManager`。

## 10. 本次故障的核心经验

这次问题最容易被误判成“图片资源丢失”或“PointerEnter 脚本坏了”，但真正的排查顺序应该是：

1. `UIManager` 是否启用并成功绑定路由。
2. 页面是否已注册，以及初始页面是否真的进入页面栈。
3. 页面根对象、CanvasGroup 和父节点是否处于可见状态。
4. 是否有更高层 Canvas 或透明 Graphic 拦截射线。
5. 最后再检查按钮自己的事件脚本和 Sprite 引用。

先验证管理状态和射线命中，再检查单个交互脚本，可以显著减少 UI 问题的误判时间。
