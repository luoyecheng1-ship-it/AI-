# Il2Cpp/MelonLoader MOD 开发指南

> 整理自实际开发经验（江湖立志转 LongYinLiZhiZhuan）
> 最后更新: 2026-04-06

---

## 一行代码解决一切

```csharp
using Il2Cpp;  // ← 就是这一行！没有它，一切反射都是徒劳。
```

---

## 核心原则

### ❌ 绝对不要做的事

1. **不要用 `System.Reflection` / `Traverse.Create()` 访问游戏类型**
2. **不要用 `Il2CppType.From(systemType)` 动态转换类型**
3. **不要在运行时通过 `AppDomain.CurrentDomain.GetAssemblies()` 查找类型**
4. **不要引用 Cpp2IL 生成的 DLL 来编译**
5. **不要假设 `OnInitializeMelon` 时游戏程序集已加载**

### ✅ 正确做法

```csharp
using Il2Cpp;  // 必须有这行

// 然后直接用，像普通C#一样：
var gc = GameController.Instance;
var wd = gc.worldData;
var events = wd.BigMapRandomEventDatas;
string name = events[i].eventName;
bool seen = events[i].seen;
float x = events[i].bigMapPos.x;
```

---

## 项目配置 (csproj)

### DLL 引用来源（只引这些）

| DLL | 路径 |
|-----|------|
| MelonLoader 核心 | `MelonLoader/net6/0Harmony.dll` |
| MelonLoader | `MelonLoader/net6/MelonLoader.dll` |
| Il2CppInterop.Runtime | `MelonLoader/net6/Il2CppInterop.Runtime.dll` |
| 游戏程序集 | `MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll` |
| Unity 模块 | `MelonLoader/Il2CppAssemblies/UnityEngine.*.dll` |

⚠️ **关键**: 只引用 `Il2CppAssemblies/` 下的，不引用 `Cpp2Il/cpp2il_out/`

---

## 已验证的数据访问链路（江湖立志转）

```
GameController.Instance
  → .worldData
    → .BigMapRandomEventDatas (List<EventData>)    ← 大地图随机事件
      → [i].eventName (string)
      → [i].spriteName (string)
      → [i].seen (bool)        ← 改为true显示事件
      → [i].noticed (bool)     ← 改为true标记已发现
      → [i].bigMapPos.x / .y   ← 地图坐标
    → .AreaMapRandomEventDatas (List<EventData>)   ← 区域事件

BigMapController.Instance
  → .RecreatAllBigMapRandomEvent()  ← 调用后刷新大地图图标
```

---

## 完整可用 MOD 模板

```csharp
using System;
using Il2Cpp;           // 关键！
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(MyMod), "名称", "1.0", "作者")]
[assembly: MelonGame("开发商名", "游戏名")]

public class MyMod : MelonMod
{
    private bool _enabled = false;

    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("MOD已加载");
    }

    public override void OnLateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F7))
        {
            _enabled = !_enabled;
            if (_enabled) Apply();
            LoggerInstance.Msg(_enabled ? "已开启" : "已关闭");
        }
    }

    private void Apply()
    {
        var gc = GameController.Instance;
        if (gc == null) return;

        var big = gc.worldData.BigMapRandomEventDatas;
        for (int i = 0; i < big.Count; i++)
        {
            var e = big[i];
            if (e == null) continue;
            e.seen = true;
            e.noticed = true;
        }

        BigMapController.Instance?.RecreatAllBigMapRandomEvent();
    }
}
```

---

## 常见错误与解决

| 错误 | 原因 | 解决 |
|------|------|------|
| TypeLoadException | 没写 `using Il2Cpp;` | 加上 using Il2Cpp; |
| GameController=null | 不在游戏中 | 加 null 检查 |
| MOD没效果 | 开发商名拼错 | 检查 MelonGame 属性 |
| 编译找不到类型 | 引用了错误的DLL | 用 Il2CppAssemblies 版本 |
| OnInitialize类型null | 太早了，程序集未加载 | 移到 OnGUI/LateUpdate 中使用 |

---

## MelonMod 生命周期

```
OnInitializeMelon()  → 加载时调用（游戏可能未就绪）
OnLateUpdate()       → 每帧调用（按键检测）
OnGUI()              → 每帧调用（UI绘制）
```
