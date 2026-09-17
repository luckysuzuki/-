using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WitchTrial.Story;

namespace WitchTrial.Presentation
{
    /// <summary>
    /// 定义转场期间预加载下一个对话段落资源的异步接口。
    /// </summary>
    public interface IStorySegmentLoader
    {
        // Yield until ready; throw on failure. Use iterator finally to release loading resources on cancellation.
        IEnumerator PrepareSegment(DialogueNode next);
    }

    /// <summary>
    /// 响应剧情转场请求，协调淡出、资源准备、黑屏停留和淡入。
    /// </summary>
    public sealed class StoryTransitionController : MonoBehaviour
    {
        public StoryRunner runner;
        [Tooltip("顶层黑色全屏 Image 的 CanvasGroup；不指定时没有视觉淡入淡出。")]
        public CanvasGroup overlay;
        public TMP_Text loadingLabel;
        [Tooltip("可选，实现 IStorySegmentLoader 的组件。直接引用资源时留空。")]
        public MonoBehaviour[] loaders = Array.Empty<MonoBehaviour>();
        public string LastError { get; private set; }
        public event Action<string> Failed;
        private StoryRunner subscribedRunner;
        private Coroutine routine;
        private void OnEnable()
        {
            subscribedRunner = runner;
            if (subscribedRunner == null) return;
            subscribedRunner.TransitionRequested += Begin;
            subscribedRunner.Stopped += Cancel;
            subscribedRunner.PlaybackStarted += Cancel;
            if (subscribedRunner.CurrentTransition != null) Begin(subscribedRunner.CurrentTransition);
            else SetOverlay(0);
        }
        private void OnDisable()
        {
            if (subscribedRunner != null)
            {
                subscribedRunner.TransitionRequested -= Begin;
                subscribedRunner.Stopped -= Cancel;
                subscribedRunner.PlaybackStarted -= Cancel;
            }
            Cancel();
            subscribedRunner = null;
        }
        public void Retry()
        { if (subscribedRunner != null && subscribedRunner.CurrentTransition != null) Begin(subscribedRunner.CurrentTransition); }
        private void Begin(StoryTransition request)
        {
            if (routine != null) StopCoroutine(routine);
            LastError = null;
            routine = StartCoroutine(Guard(Run(request), request));
        }
        private bool IsCurrent(StoryTransition request) => subscribedRunner != null && subscribedRunner.CurrentTransition == request;
        private IEnumerator Run(StoryTransition request)
        {
            // Leave the runner's event dispatch before calling it again.
            yield return null;
            if (loadingLabel != null) loadingLabel.text = request.Node.loadingText;
            yield return Fade(1, request.Node.fadeOutSeconds);
            var start = Time.realtimeSinceStartup;
            if (!request.IsPrepared)
            {
                foreach (var component in loaders)
                {
                    if (!(component is IStorySegmentLoader loader)) throw new InvalidOperationException("Loader 必须实现 IStorySegmentLoader。");
                    yield return loader.PrepareSegment(request.Node.next);
                }
                if (!IsCurrent(request)) yield break;
                if (!subscribedRunner.PrepareTransition(request)) throw new InvalidOperationException(subscribedRunner.LastError);
            }
            while (Time.realtimeSinceStartup - start < request.Node.minimumBlackSeconds) yield return null;
            yield return Fade(0, request.Node.fadeInSeconds);
            if (IsCurrent(request) && !subscribedRunner.CompleteTransition(request)) throw new InvalidOperationException(subscribedRunner.LastError);
        }
        private IEnumerator Guard(IEnumerator task, StoryTransition request)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(task);
            try
            {
                while (stack.Count > 0 && IsCurrent(request))
                {
                    bool moved = false;
                    object value = null;
                    Exception error = null;
                    try { moved = stack.Peek().MoveNext(); if (moved) value = stack.Peek().Current; }
                    catch (Exception exception) { error = exception; }
                    if (error != null)
                    {
                        LastError = error.Message;
                        SetOverlay(1);
                        if (loadingLabel != null) loadingLabel.text = LastError;
                        Debug.LogException(error, this);
                        Failed?.Invoke(LastError);
                        yield break;
                    }
                    if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (value is IEnumerator child) stack.Push(child); else yield return value;
                }
            }
            finally
            {
                while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
                routine = null;
            }
        }
        private IEnumerator Fade(float target, float seconds)
        {
            float from = overlay != null ? overlay.alpha : 0;
            for (float elapsed = 0; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            { SetOverlay(Mathf.Lerp(from, target, elapsed / seconds)); yield return null; }
            SetOverlay(target);
        }
        private void SetOverlay(float alpha)
        {
            if (overlay == null) return;
            overlay.alpha = alpha;
            overlay.blocksRaycasts = alpha > 0;
            overlay.interactable = false;
        }
        private void Cancel()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            LastError = null;
            SetOverlay(0);
            if (loadingLabel != null) loadingLabel.text = "";
        }
    }
}
