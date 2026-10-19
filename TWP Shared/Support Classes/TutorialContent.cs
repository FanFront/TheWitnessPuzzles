using Microsoft.Xna.Framework;
using TheWitnessPuzzles;
using System;

namespace TWP_Shared
{
    // These panels are authored by hand, not randomly generated.
    // They intentionally use the exact same validation code as normal panels.
    public static class TutorialContent
    {
        public class Lesson
        {
            public readonly string EnglishTitle, ChineseTitle;
            public readonly string EnglishDescription, ChineseDescription;
            public readonly string EnglishHint, ChineseHint;
            public readonly string Icon;
            public Lesson(string enTitle, string zhTitle, string enText, string zhText, string enHint, string zhHint, string icon)
            {
                EnglishTitle = enTitle; ChineseTitle = zhTitle;
                EnglishDescription = enText; ChineseDescription = zhText;
                EnglishHint = enHint; ChineseHint = zhHint; Icon = icon;
            }
            public string Title => Localization.IsChinese ? ChineseTitle : EnglishTitle;
            public string Description => Localization.IsChinese ? ChineseDescription : EnglishDescription;
            public string Hint => Localization.IsChinese ? ChineseHint : EnglishHint;
        }

        public static readonly Lesson[] Lessons =
        {
            new Lesson("1. Draw a line", "1. 连接起点和终点",
                "Touch the round start point and drag along the grid to the exit. Tap again to submit. Lines cannot cross themselves.",
                "按住圆形起点，沿网格滑动到终点。再次点击提交。线条不能与自身相交。",
                "Find any route from the start to the exit.", "从起点画一条线到终点。", "img/circle"),
            new Lesson("2. Broken paths", "2. 断开的路径",
                "Gaps in the grid are broken edges. Your line cannot travel through a missing segment.",
                "网格上的缺口代表断线，画出的线不能经过断开的边。",
                "Go around the gap on the upper edge.", "绕过上方断开的线段。", "img/circle"),
            new Lesson("3. Hexagon dots", "3. 六角形标记",
                "Your line must touch every hexagon on a junction or edge. In symmetric puzzles, colored dots require the matching line.",
                "路线必须经过所有位于交点或边上的六角形标记。对称谜题中的彩色标记必须由对应颜色的线经过。",
                "Pass through the center dot AND the marked edge.", "经过中央标记和右侧边上的标记。", "img/hexagon"),
            new Lesson("4. Colored squares", "4. 彩色方块",
                "Your line divides the board into separate regions. Each region may contain squares of only one color.",
                "画出的线会把棋盘分成多个区域。每个区域内的方块只能有一种颜色。",
                "Split red squares from blue squares.", "将红色与蓝色方块分隔开。", "img/square"),
            new Lesson("5. Suns / stars", "5. 太阳符号",
                "A sun needs EXACTLY two objects of its color in its region, including itself. Its partner can be a sun, square, or another colored rule.",
                "太阳符号所在区域，必须恰好有两个同色符号（包括它自身）。另一个可以是太阳、方块或其他彩色规则。",
                "Keep the red pair together and the blue pair together.", "让两个红色符号同区，两个蓝色符号同区。", "img/sun"),
            new Lesson("6. Triangles", "6. 三角形",
                "The number of triangles in a cell tells you exactly how many of that cell's four edges the line must use.",
                "格子中的三角形数量，表示路线必须经过该格子四条边中的几条。",
                "Touch exactly two edges around the marked cell.", "让路线经过标记格子的恰好两条边。", "img/triangle2"),
            new Lesson("7. Polyominoes", "7. 拼图方块",
                "Yellow Tetris-like shapes must combine to fill their region exactly. Ordinary shapes cannot rotate.",
                "黄色拼图形状必须恰好拼满所在区域。普通形状不能旋转。",
                "Make a vertical region two cells tall.", "围出一个竖直两格高的区域。", "img/tetris"),
            new Lesson("8. Rotating shapes", "8. 可旋转拼图",
                "A tilted polyomino can rotate in 90-degree steps. Its occupied cells must still match the region.",
                "倾斜的拼图形状可以每次旋转90度，但旋转后仍须恰好填满所在区域。",
                "Rotate the horizontal piece to fit a vertical region.", "把横向形状旋转，放入竖直区域。", "img/tetris"),
            new Lesson("9. Subtractive shapes", "9. 蓝色减法拼图",
                "A hollow blue shape subtracts occupied cells from yellow shapes in its region. The remaining cells must exactly match that region.",
                "蓝色空心形状会从同一区域内的黄色拼图中减去对应格子。剩余部分必须恰好填满该区域。",
                "Four yellow cells minus two blue cells leave two cells.", "黄色四格减去蓝色两格，剩下两格。", "img/tetris_sub"),
            new Lesson("10. Erasers", "10. 消除符号",
                "An upside-down Y erases one rule violation in its region. It can cancel a symbol so the remaining rules work. A colored eraser also counts toward suns.",
                "倒Y形消除符号可以消除所在区域的一项规则错误，使剩余规则成立。有颜色的消除符号也参与太阳规则计数。",
                "Separate the right side; let the eraser fix the conflicting squares on the left.", "分出右侧区域，让左侧的消除符号处理冲突方块。", "img/elimination"),
            new Lesson("11. Symmetry", "11. 对称线路",
                "Two lines move together as mirror images. Both must reach exits, and they cannot cross or overlap.",
                "两条线会沿镜像方向同时移动。两条线都必须到达终点，且不能相交或重叠。",
                "Trace a vertical line from the left start; watch its mirror.", "从左侧起点向上画线，观察右侧镜像线路。", "img/circle"),
            new Lesson("12. Invisible mirror", "12. 隐形镜像线路",
                "A mirrored line may be invisible, but still exists and must satisfy colored hexagon dots and other rules.",
                "镜像线路有时不可见，但它仍然存在，必须满足彩色六角形及其他规则。",
                "The hidden yellow line must pass the yellow dot.", "隐藏的黄色线路也必须经过黄色标记。", "img/hexagon")
        };

        private static Puzzle Standard(int w, int h, int start, int exit)
        {
            Puzzle p = new Puzzle(w, h);
            p.Nodes[start].SetState(NodeState.Start);
            p.Nodes[exit].SetState(NodeState.Exit);
            return p;
        }

        public static Puzzle CreatePuzzle(int index)
        {
            Puzzle p;
            switch (index)
            {
                case 0:
                    return Standard(2, 2, 0, 8);
                case 1:
                    p = Standard(2, 2, 0, 8);
                    p.Nodes[0].LinkToNode(p.Nodes[1]).SetState(EdgeState.Broken);
                    return p;
                case 2:
                    p = Standard(2, 2, 0, 8);
                    p.Nodes[4].SetStateAndColor(NodeState.Marked, Color.Black);
                    p.Nodes[4].LinkToNode(p.Nodes[5]).SetStateAndColor(EdgeState.Marked, Color.Black);
                    return p;
                case 3:
                    p = Standard(2, 2, 1, 7);
                    p.Grid[0, 0].Rule = new ColoredSquareRule(Color.Red);
                    p.Grid[1, 0].Rule = new ColoredSquareRule(Color.Blue);
                    return p;
                case 4:
                    p = Standard(2, 2, 1, 7);
                    p.Grid[0, 0].Rule = new SunPairRule(Color.Red);
                    p.Grid[0, 1].Rule = new SunPairRule(Color.Red);
                    p.Grid[1, 0].Rule = new ColoredSquareRule(Color.Blue);
                    p.Grid[1, 1].Rule = new SunPairRule(Color.Blue);
                    return p;
                case 5:
                    p = Standard(2, 2, 0, 8);
                    p.Grid[1, 0].Rule = new TriangleRule(2);
                    return p;
                case 6:
                    p = Standard(2, 2, 1, 7);
                    p.Grid[0, 0].Rule = new TetrisRule(new bool[,] { { true, true } });
                    return p;
                case 7:
                    p = Standard(2, 2, 1, 7);
                    p.Grid[0, 0].Rule = new TetrisRotatableRule(new bool[,] { { true }, { true } });
                    return p;
                case 8:
                    p = Standard(3, 2, 1, 9);
                    p.Grid[0, 0].Rule = new TetrisRule(new bool[,] { { true, true }, { true, true } });
                    p.Grid[0, 1].Rule = new TetrisRule(new bool[,] { { true, true } }, true);
                    return p;
                case 9:
                    p = Standard(3, 2, 2, 10);
                    p.Grid[0, 0].Rule = new ColoredSquareRule(Color.Red);
                    p.Grid[1, 0].Rule = new ColoredSquareRule(Color.Blue);
                    p.Grid[0, 1].Rule = new EliminationRule();
                    return p;
                case 10:
                case 11:
                    var sym = new SymmetryPuzzle(2, 2, false, index == 11, Color.Cyan, Color.Yellow);
                    sym.Nodes[6].SetState(NodeState.Start);
                    sym.Nodes[8].SetState(NodeState.Start);
                    sym.Nodes[0].SetState(NodeState.Exit);
                    sym.Nodes[2].SetState(NodeState.Exit);
                    if (index == 11)
                        sym.Nodes[5].SetStateAndColor(NodeState.Marked, Color.Yellow);
                    return sym;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
