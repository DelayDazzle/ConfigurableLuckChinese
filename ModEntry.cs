using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;

namespace ConfigurableLuckChinese
{
    public class ModEntry : Mod
    {
        private Harmony? _harmony;

        private static readonly Dictionary<string, string> Translations = new()
        {
            // ===== 选项和描述 =====
            ["Enabled"] = "启用",
            ["If this option is selected, your daily luck will be overridden with the selected value."] = "如果选中此选项，您的每日运气将被所选的数值覆盖。",
            ["Note: Vanilla luck goes from Very Bad to Very Good. This mod lets you set your luck outside those values (everything labeled Worst or Best). Unintended behavior may occur."] = "注意：原版运气从“非常差”到“非常好”。\n本模组允许您将运气设置在这些范围之外（标记为“最差”或“最好”）。\n可能会出现意外行为。",
            ["Luck Value"] = "运气值",
            ["The value to override your luck with."] = "用于覆盖您运气的数值。",

            // ===== 滑块标签 =====
            ["Worst\n(Beyond\nVanilla)"] = "最差\n（超越\n原版）",
            ["Worst"] = "最差",
            ["Very Bad"] = "非常差",
            ["Bad"] = "差",
            ["Neutral"] = "中性",
            ["Good"] = "好",
            ["Very Good"] = "非常好",
            ["Best"] = "最好",
            ["Best\n(Beyond\nVanilla)"] = "最好\n（超越\n原版）",
            ["Unknown"] = "未知"
        };

        public override void Entry(IModHelper helper)
        {
            _harmony = new Harmony(ModManifest.UniqueID);

            // 匹配 ConfigurableLuck 程序集
            Assembly? targetMod = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name?.Contains("ConfigurableLuck", StringComparison.OrdinalIgnoreCase) == true);

            if (targetMod == null)
            {
                Monitor.Log("未找到 ConfigurableLuck 程序集，跳过汉化。", LogLevel.Warn);
                return;
            }

            MethodInfo transpiler = typeof(ModEntry).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.NonPublic)!;
            var harmonyTranspiler = new HarmonyMethod(transpiler);
            int patchedCount = 0;

            foreach (Type type in targetMod.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (!ShouldPatch(method)) continue;
                    try
                    {
                        _harmony.Patch(method, transpiler: harmonyTranspiler);
                        patchedCount++;
                    }
                    catch { }
                }
            }
            Monitor.Log($"ConfigurableLuck 汉化补丁已加载，共修补 {patchedCount} 个方法。", LogLevel.Info);
        }

        private static bool ShouldPatch(MethodBase method)
        {
            string name = method.Name;
            // 瞄准 BuildConfigMenu 和注册 GMCM 相关的方法
            if (!name.Contains("BuildConfigMenu") && 
                !name.Contains("RegisterConfig") && !name.Contains("AddBoolOption") && 
                !name.Contains("AddNumberOption") && !name.Contains("AddParagraph")) return false;
            try { return method.GetMethodBody() != null; } catch { return false; }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string original && Translations.TryGetValue(original, out string? translated))
                {
                    instruction.operand = translated;
                }
                yield return instruction;
            }
        }
    }
}
