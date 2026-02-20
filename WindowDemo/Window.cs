using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowDemo
{
    public class Window
    {
        public string Header { get; set; }
        public string ShortcutLabel { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public List<string> TextRows { get; set; }
        public int? FixedWidth { get; set; }

        public Window(string? header, int left, int top, List<string>? textRows, string? shortcutLabel = null, int? fixedWidth = null)
        {
            Header = header ?? "";
            Left = left;
            Top = top;
            TextRows = textRows ?? new List<string>();
            ShortcutLabel = shortcutLabel ?? "";
            FixedWidth = fixedWidth;
        }

        public void Draw()
        {
            var headerText = Header ?? "";
            var headerWithShortcut = !string.IsNullOrEmpty(ShortcutLabel) ? (string.IsNullOrEmpty(headerText) ? $"[{ShortcutLabel}]" : $"{headerText} [{ShortcutLabel}]") : headerText;

            var contentMax = TextRows.Any() ? TextRows.Max(s => s?.Length ?? 0) : 0;
            var naturalWidth = Math.Max(contentMax, headerWithShortcut.Length + 2);

            int visualWidth = Math.Max(1, Console.WindowWidth);
            int width = FixedWidth.HasValue
                ? Math.Max(10, Math.Min(FixedWidth.Value, Math.Max(10, visualWidth - Left - 3)))
                : Math.Min(naturalWidth, Math.Max(10, visualWidth - Left - 3));

            bool hasHeader = !string.IsNullOrEmpty(headerWithShortcut);

            bool decorativeFirst = TextRows.Count > 0 && !string.IsNullOrEmpty(TextRows[0].Trim()) &&
                TextRows[0].Trim().All(c => "= -─═━▔―".Contains(c));

            List<string> rowsToPrint = decorativeFirst && hasHeader ? TextRows.Skip(1).ToList() : TextRows.ToList();
            int contentStartRow = decorativeFirst && !hasHeader ? Top : Top + 1;
            int winHeight = Math.Max(1, Console.WindowHeight);

            if (!(decorativeFirst && !hasHeader) && Top < winHeight)
            {
                GetSafePosition(Left, Top, out var safeLeft, out var safeTop);
                try
                {
                    Console.SetCursorPosition(safeLeft, safeTop);
                    if (!string.IsNullOrEmpty(headerWithShortcut))
                    {
                        Console.Write('┌' + " ");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        var headerToWrite = headerWithShortcut.Length > width ? headerWithShortcut.Substring(0, width) : headerWithShortcut;
                        Console.Write(headerToWrite);
                        Console.ForegroundColor = ConsoleColor.White;
                        var restCount = Math.Max(0, width - headerToWrite.Length);
                        Console.Write(" " + new string('─', restCount) + '┐');
                    }
                    else Console.Write('┌' + new string('─', width + 2) + '┐');
                }
                catch { }
            }

            for (int j = 0; j < rowsToPrint.Count; j++)
            {
                int row = contentStartRow + j;
                if (row < winHeight)
                {
                    GetSafePosition(Left, row, out var safeLeft, out var safeRow);
                    try
                    {
                        Console.SetCursorPosition(safeLeft, safeRow);
                        var text = rowsToPrint[j] ?? "";
                        if (text.Length > width) text = text.Substring(0, width);
                        var padding = new string(' ', Math.Max(0, width - text.Length + 1));
                        Console.Write('│' + " " + text + padding + '│');
                    }
                    catch { }
                }
            }

            int bottomRow = contentStartRow + rowsToPrint.Count;
            if (bottomRow < winHeight)
            {
                GetSafePosition(Left, bottomRow, out var safeLeft, out var safeBottom);
                try { Console.SetCursorPosition(safeLeft, safeBottom); Console.Write('└' + new string('─', width + 2) + '┘'); } catch { }
            }

            int newLowest = bottomRow + 1;
            Lowest.LowestPosition = Math.Clamp(Lowest.LowestPosition < newLowest ? newLowest : Lowest.LowestPosition, 0, Math.Max(0, Console.WindowHeight - 1));
            GetSafePosition(0, Lowest.LowestPosition, out var safeLeft0, out var safeLowest);
            try { Console.SetCursorPosition(safeLeft0, safeLowest); } catch { }
        }

        private static void GetSafePosition(int requestedLeft, int requestedTop, out int safeLeft, out int safeTop)
        {
            int winW = Math.Max(1, Console.WindowWidth);
            int winH = Math.Max(1, Console.WindowHeight);
            safeLeft = Math.Clamp(requestedLeft, 0, winW - 1);
            safeTop = Math.Clamp(requestedTop, 0, winH - 1);
        }
    }

    public static class Lowest
    {
        public static int LowestPosition { get; set; }
    }
}
