using System;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(JiangHuEventsDisplayMod), "江湖事件显示", "2.0.0", "Author")]
[assembly: MelonGame("TppStudio", "LongYinLiZhiZhuan")]

public class JiangHuEventsDisplayMod : MelonMod
{
    private bool _enabled = false;
    private float _nextRevealAt = 0f;
    private const float RevealIntervalSeconds = 2.5f;
    private int _lastBigMapCount = int.MinValue;
    private int _lastAreaCount = int.MinValue;
    private bool _showSettings = false;
    private string _debugInfo = "";

    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("江湖事件显示MOD已加载！F7显示全部隐藏事件 F8设置");
    }

    public override void OnLateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F7))
        {
            _enabled = !_enabled;
            LoggerInstance.Msg(_enabled ? "江湖事件显示：已开启，显示全部隐藏事件" : "江湖事件显示：已关闭");
            if (_enabled)
            {
                _lastBigMapCount = int.MinValue;
                _lastAreaCount = int.MinValue;
                TryApply(true);
            }
            else
            {
                _lastBigMapCount = int.MinValue;
                _lastAreaCount = int.MinValue;
            }
        }

        if (Input.GetKeyDown(KeyCode.F8))
            _showSettings = !_showSettings;

        if (!_enabled) return;

        if (Time.unscaledTime < _nextRevealAt) return;
        _nextRevealAt = Time.unscaledTime + RevealIntervalSeconds;
        TryApply(false);
    }

    public override void OnGUI()
    {
        if (_showSettings)
        {
            GUILayout.BeginArea(new Rect(10, 10, 320, 200));
            GUILayout.BeginVertical("box");
            GUILayout.Label("=== 江湖事件显示 ===");
            GUILayout.Label($"状态: {(_enabled?"已开启 [F7关闭]":"已关闭 [F7开启]")}");
            GUILayout.Label($"刷新间隔: {RevealIntervalSeconds}秒");
            GUILayout.Space(5);
            GUILayout.Label("开启后自动:");
            GUILayout.Label("• 标记所有事件为已发现");
            GUILayout.Label("• 定期刷新大地图图标");
            GUILayout.Label("• 显示隐藏的江湖事件位置");
            GUILayout.Space(5);
            GUILayout.Label($"[调试] {_debugInfo}");
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }

    private void TryApply(bool forceRecreate)
    {
        try
        {
            var gc = GameController.Instance;
            if (gc == null)
            {
                _debugInfo = "GameController=null";
                return;
            }

            var wd = gc.worldData;
            if (wd == null)
            {
                _debugInfo = "worldData=null";
                return;
            }

            var big = wd.BigMapRandomEventDatas;
            var area = wd.AreaMapRandomEventDatas;
            int bigCount = big == null ? 0 : big.Count;
            int areaCount = area == null ? 0 : area.Count;

            RevealAll(big);
            RevealAll(area);

            bool countChanged = bigCount != _lastBigMapCount || areaCount != _lastAreaCount;
            _lastBigMapCount = bigCount;
            _lastAreaCount = areaCount;

            if (!forceRecreate && !countChanged) return;

            var bmp = BigMapController.Instance;
            if (bmp != null)
                bmp.RecreatAllBigMapRandomEvent();

            _debugInfo = $"大地图:{bigCount}个 区域:{areaCount}个{(forceRecreate?" [强制刷新]":"")}";
        }
        catch (Exception ex)
        {
            _debugInfo = $"错误: {ex.Message}";
            LoggerInstance.Error(ex.ToString());
            _enabled = false;
        }
    }

    private static void RevealAll(Il2CppSystem.Collections.Generic.List<EventData> list)
    {
        if (list == null) return;
        int n = list.Count;
        for (int i = 0; i < n; i++)
        {
            var e = list[i];
            if (e == null) continue;
            e.seen = true;
            e.noticed = true;
        }
    }
}
