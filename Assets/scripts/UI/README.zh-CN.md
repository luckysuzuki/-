# 视觉小说 UI 框架接入

这套代码使用现有的 uGUI（Canvas、Button、Slider、Toggle、TMP），分为两个独立层：

- `FullscreenRoot`：全屏页栈。打开下一页会隐藏上一页，`Back()` 会恢复上一页。
- `PopupRoot`：弹窗栈。弹窗盖在当前全屏页上，共用一个拦截点击的半透明遮罩。

业务代码只调用 `UIRouter`。页面的创建、缓存、父节点、遮罩和前后顺序都由 `UIManager` 处理。

## 当前 startscene 的接入方式

1. 在现有 `UI/MainCanvas` 下新建一个拉伸铺满的 `MainPage`。
2. 将现在分散的 `bk`、`icon`、底部的 `Panel` 全部移入 `MainPage`。现在场景里的 `Panel` 只有底部按钮，不能直接把它当成完整页面。
3. 给 `MainPage` 添加 `MainPanel`。把五个按钮拖到对应字段；Load/New Game/Gallery 的实际业务入口放到三个 UnityEvent 中。
4. 给场景根对象 `UI` 添加 `UIManager`。它会找到子级的 `MainCanvas`，并在运行时补齐 `FullscreenRoot`、`PopupRoot` 和 `PopupMask`。
5. 把 `MainPage` 同时拖入 `UIManager/Scene Panels` 和 `Initial Page`。

`UIManager` 应放在顶层对象上，因为默认会 `DontDestroyOnLoad`。不要把它放到会被隐藏的某个 `UIPanel` 里面。

## Options 弹窗

1. 建立一个铺满 Canvas 的 `OptionsPanel` 根对象并挂 `OptionsPanel` 脚本。
2. 按图二搭建内容。根对象本身不需要再做黑色遮罩；管理器会自动创建全屏遮罩。
3. 绑定两个 Slider、两个数值文本、两个 Toggle、语言 TMP_Dropdown、关闭按钮和恢复默认按钮。
4. Slider 的范围会由脚本设成 1–10 且只取整数。Dropdown 顺序应为：简体中文、繁体中文、日文、英文。
5. 将根对象做成 Prefab。在 Project 窗口执行 `Create > Witch Trial > UI > UI Registry`，把 Options Prefab 加入列表，再把 Registry 拖给 `UIManager`。

主菜单的 Options 按钮可以任选一种连接方法：

- 简单方式：拖到 `MainPanel/optionsButton`，脚本会调用 `UIRouter.Open<OptionsPanel>()`。
- 数据驱动方式：给按钮添加 `UIActionButton`，Action 选 `Open`，Destination 拖入 Options Prefab；这时不要再填写 `MainPanel/optionsButton`，避免重复监听。

## 调用 API

```csharp
// 打开页面或弹窗；框架根据 UIPanel.Layer 自动选择层。
UIRouter.Open<OptionsPanel>();

// 打开一个全屏页，并保留上一页以便返回。
UIRouter.Open<GalleryPanel>();

// 清空旧的全屏历史，再进入指定页（常用于开始新游戏）。
UIRouter.Replace<GamePanel>(newGameContext);

// 优先关闭最上层弹窗，否则返回上一张全屏页。
UIRouter.Back();

// 页面自己关闭。
RequestClose();
```

所有自定义页面都继承 `UIPanel`，并明确声明层：

```csharp
public sealed class GalleryPanel : UIPanel
{
    public override UILayer Layer => UILayer.Fullscreen;
}
```

Options 数据保存在 `PlayerPrefs`。对话系统不需要引用 Options UI，可以监听设置变化：

```csharp
VisualNovelSettings.Changed += ApplySettings;
var settings = VisualNovelSettings.Load();
float secondsPerCharacter = settings.SecondsPerCharacter;
```

## 生命周期

- `OnOpening(context)`：首次显示或关闭后再次显示。
- `OnRefreshed(context)`：对已经打开的同一页面再次执行 Open。
- `OnCovered()` / `OnRevealed()`：全屏页被下一页覆盖或返回恢复。
- `OnClosing()` / `OnClosed()`：页面退出。

默认面板关闭后会缓存实例。对体积很大且不常用的页面，可在 `UIPanel` Inspector 关闭 `Keep Alive`，退出时会销毁实例。
