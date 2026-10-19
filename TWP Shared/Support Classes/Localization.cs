using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework.Graphics;

namespace TWP_Shared
{
    /// <summary>Small, dependency-free English / Simplified Chinese UI catalog.</summary>
    public static class Localization
    {
        public static SpriteFont ChineseFont { get; set; }

        public static bool IsChinese =>
            SettingsManager.Language == "zh" ||
            (SettingsManager.Language == "auto" &&
             CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh");

        // Keep original English labels as keys, so unrecognized text is still legible.
        private static readonly Dictionary<string, string> Chinese = new Dictionary<string, string>
        {
            { "Start", "开始游戏" },
            { "Library", "题库" },
            { "Settings", "设置" },
            { "About", "关于" },
            { "Tutorial", "新手教程" },
            { "Rules", "规则说明" },
            { "Back", "返回" },
            { "Next", "下一关" },
            { "Previous", "上一关" },
            { "Play example", "试玩本关" },
            { "Rule guide", "规则说明" },
            { "Learn to play", "新手教程" },
            { "Extra settings", "高级设置" },
            { "Reset seed", "重置种子" },
            { "Sound", "声音" },
            { "Volume", "音量" },
            { "Sensitivity", "灵敏度" },
            { "Fullscreen", "全屏" },
            { "Bloom FX", "泛光效果" },
            { "Lock orientation", "锁定屏幕方向" },
            { "Sequential mode", "顺序模式" },
            { "Reset progress for", "确定要重置" },
            { "sequential mode?", "顺序模式进度吗？" },
            { "DISCLAIMER", "声明" },
            { "Language: English", "Language: English" },
            { "Language: Chinese", "语言：简体中文" },
            { "Solved!", "完成！" },
            { "Puzzle solved!", "谜题完成！" },
            { "Next puzzle", "下一题" },
            { "Next lesson", "下一课" },
            { "Back to lessons", "返回教程" },
            { "Back to library", "返回题库" },
            { "Tap a start circle, trace to an exit, then tap to submit.", "点击起点圆圈，滑动至终点，再点击提交。" },
            { "Close the panel to return to this lesson.", "关闭谜题即可返回当前课程。" },
            { "This game is inspired by the amazing game The Witness, ", "本游戏的灵感来自精彩的《见证者》，" },
            { "which was created by Jonathan Blow.", "该游戏由 Jonathan Blow 创作。" },
            { "If you have not played his games yet ", "如果你还没玩过他的游戏，" },
            { "(The Witness and Braid), you should definitely do it.", "也欢迎体验《见证者》和《时空幻境》。" },
            { "All rights to The Witness and its sound assets ", "《见证者》及相关音效的所有权利" },
            { "belong to Jonathan Blow and Thekla Inc.", "归 Jonathan Blow 和 Thekla Inc. 所有。" }
        };

        public static string T(string english)
        {
            if (!IsChinese || english == null) return english;
            string translated;
            return Chinese.TryGetValue(english, out translated) ? translated : english;
        }

        public static SpriteFont Font(SpriteFont original) =>
            IsChinese && ChineseFont != null ? ChineseFont : original;
    }
}
