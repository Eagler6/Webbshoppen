using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowDemo
{
    internal static class Draw
    {
        private enum View
        {
            Start,
            Shop,
            Cart
        }

        private static View _currentView = View.Start;
        private static List<Product> _startFeatured = new();
        private static List<Product> _shopHotkeys = new();
        private static List<(Product Product, int Quantity)> _cartHotkeys = new();
        public static void SafeSetCursorToLowest(int extraLines = 0)
        {
            int pos = Lowest.LowestPosition + extraLines;
            pos = Math.Clamp(pos, 0, Math.Max(0, Console.WindowHeight - 1));
            try { Console.SetCursorPosition(0, pos); } catch { }
        }

        public static void PrintShortcuts()
        {
            int pos = Math.Clamp(Lowest.LowestPosition, 0, Math.Max(0, Console.WindowHeight - 1));
            int winW = Math.Max(0, Console.WindowWidth);

            string context = _currentView switch
            {
                View.Start => " | F1-F3=Erbjudanden | A=Admin",
                View.Shop  => " | F1-F10=Produkter | C=Kategorier | A=Admin",
                View.Cart  => " | ENTER=Checkout | ESC=Tillbaka",
                _ => ""
            };

            var text = "Genvägar: 1=Start, 2=Shop, 3=Varukorg, Q=Avsluta" + context;

            try
            {
                Console.SetCursorPosition(0, pos);
                Console.Write(text.PadRight(winW));
            }
            catch { }

            Lowest.LowestPosition = Math.Clamp(pos + 1, 0, Math.Max(0, Console.WindowHeight - 1));
        }

        private static int? FunctionKeyIndex(ConsoleKey key) => key switch
        {
            ConsoleKey.F1  => 1,
            ConsoleKey.F2  => 2,
            ConsoleKey.F3  => 3,
            ConsoleKey.F4  => 4,
            ConsoleKey.F5  => 5,
            ConsoleKey.F6  => 6,
            ConsoleKey.F7  => 7,
            ConsoleKey.F8  => 8,
            ConsoleKey.F9  => 9,
            ConsoleKey.F10 => 10,
            _ => null
        };
        public static void HandleKey(ConsoleKeyInfo key)
        {
            if (key.Key == ConsoleKey.A)
            {
                OpenAdmin();
                return;
            }

            switch (_currentView)
            {
                case View.Start:
                    HandleStartKeys(key);
                    break;

                case View.Shop:
                    HandleShopKeys(key);
                    break;

                case View.Cart:
                    HandleCartKeys(key);
                    break;
            }
        }

        private static void HandleStartKeys(ConsoleKeyInfo key)
        {
            var idx = FunctionKeyIndex(key.Key);
            if (idx is null) return;

            if (idx.Value >= 1 && idx.Value <= 3 && _startFeatured.Count >= idx.Value)
            {
                var p = _startFeatured[idx.Value - 1];
                ShopService.ShowProductDetail(p);
                RenderStartPage();
            }
        }

        private static void HandleShopKeys(ConsoleKeyInfo key)
        {
            if (key.Key == ConsoleKey.C)
            {
                ShopService.ShowShopInteractive();
                RenderShopPage();
                return;
            }

            var idx = FunctionKeyIndex(key.Key);
            if (idx is null) return;

            if (idx.Value >= 1 && idx.Value <= 10 && _shopHotkeys.Count >= idx.Value)
            {
                var p = _shopHotkeys[idx.Value - 1];
                ShopService.ShowProductDetail(p);
                RenderShopPage();
            }
        }

        private static void HandleCartKeys(ConsoleKeyInfo key)
        {
            if (key.Key == ConsoleKey.Enter)
            {
                ShopService.ShowShippingAndPaymentFlow();
                RenderCartPage();
                return;
            }

            if (key.Key == ConsoleKey.Escape)
            {
                RenderCartPage();
                return;
            }

            var idx = FunctionKeyIndex(key.Key);
            if (idx is null) return;

            if (idx.Value >= 1 && idx.Value <= 10 && _cartHotkeys.Count >= idx.Value)
            {
                ShopService.PromptSetCartQuantityByIndex(idx.Value - 1);
                RenderCartPage();
            }
        }

        private static void OpenAdmin()
        {
            ShopService.AdminMenu();

            if (_currentView == View.Shop) RenderShopPage();
            else if (_currentView == View.Cart) RenderCartPage();
            else RenderStartPage();
        }

        public static void ShowWindow(Window w, string hint)
        {
            ResetBufferToWindow();
            try { Console.Clear(); } catch { }

            int origLeft = w.Left; int origTop = w.Top;
            w.Left = Math.Max(0, (Console.WindowWidth / 2) - 20);
            w.Top = Math.Max(0, (Console.WindowHeight / 2) - (w.TextRows.Count / 2));

            w.Draw();
            SafeSetCursorToLowest();
            try
            {
                Console.SetCursorPosition(0, Math.Clamp(Lowest.LowestPosition, 0, Console.WindowHeight - 1));
                Console.Write(hint.PadRight(Console.WindowWidth));
            }
            catch { }

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape) break;
                if (key.Key == ConsoleKey.O)
                {
                    SafeSetCursorToLowest(2);
                    try
                    {
                        Console.SetCursorPosition(0, Math.Clamp(Lowest.LowestPosition + 2, 0, Console.WindowHeight - 1));
                        Console.Write("Checkout genomförs. Tryck valfri tangent...".PadRight(Console.WindowWidth));
                    }
                    catch { }

                    ShopService.ShowShippingAndPaymentFlow();
                    Console.ReadKey(true);
                    break;
                }
            }

            w.Left = origLeft; w.Top = origTop;
            ResetBufferToWindow();
            try { Console.Clear(); } catch { }
        }
        public static void RenderStartPage()
        {
            _currentView = View.Start;
            _startFeatured = ShopService.GetFeaturedProducts().Take(3).ToList();

            try
            {
                ResetBufferToWindow();
                try { Console.Clear(); } catch { }
                ClearVisibleWindow();
                Console.SetCursorPosition(0, 0);
                Lowest.LowestPosition = 0;

                var welcomeText = new List<string>
                {
                    "========================================",
                    "     Välkommen till Edins klädbutik!    ",
                    " Kvalitet, Stil och Fantastiska priser! ",
                    "========================================",
                    ""
                };

                int winW = Math.Max(0, Console.WindowWidth);
                int welcomeTop = 0;
                for (int i = 0; i < welcomeText.Count; i++)
                {
                    var line = welcomeText[i] ?? "";
                    int left = Math.Clamp((Console.WindowWidth / 2) - (line.Length / 2), 0, Math.Max(0, Console.WindowWidth - 1));
                    int top = welcomeTop + i;
                    if (top >= 0 && top < Console.WindowHeight)
                    {
                        try { Console.SetCursorPosition(0, top); Console.Write(new string(' ', winW)); } catch { }
                        Console.SetCursorPosition(left, top);
                        Console.Write(line);
                    }
                }

                Lowest.LowestPosition = Math.Clamp(welcomeTop + welcomeText.Count + 1, 0, Console.WindowHeight - 1);

                int offersTop = Math.Clamp(Math.Max(14, Lowest.LowestPosition + 2), 0, Math.Max(0, Console.WindowHeight - 6));
                var featured = _startFeatured;

                int count = Math.Min(3, featured.Count);
                if (count <= 0) count = 0;

                if (count > 0)
                {
                    int center = Console.WindowWidth / 2;
                    int spacing = 28;
                    var lefts = new int[count];
                    if (count == 1) lefts[0] = center - 12;
                    else if (count == 2) { lefts[0] = center - spacing / 2 - 12; lefts[1] = center + spacing / 2 - 12; }
                    else { lefts[0] = center - spacing - 12; lefts[1] = center - 12; lefts[2] = center + spacing - 12; }

                    string title = "Erbjudanden";
                    int tileWidth = 28;
                    int gridLeft = lefts[0];
                    int gridRight = lefts[count - 1] + tileWidth;
                    int titleLeft = count == 1
                        ? Math.Max(0, (Console.WindowWidth - title.Length) / 2)
                        : Math.Clamp((gridLeft + gridRight - title.Length) / 2, 0, Math.Max(0, Console.WindowWidth - title.Length));
                    int titleRow = Math.Clamp(offersTop - 3, 0, Console.WindowHeight - 1);

                    var prevColor = Console.ForegroundColor;
                    try
                    {
                        try { Console.SetCursorPosition(0, titleRow); Console.Write(new string(' ', winW)); } catch { }
                        Console.SetCursorPosition(titleLeft, titleRow);
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write(title);
                    }
                    finally { Console.ForegroundColor = prevColor; }

                    for (int i = 0; i < count; i++)
                    {
                        var p = featured[i];
                        var rows = new List<string> { p.Name, p.Description, $"Pris: {p.Price} kr", "", $"Id: {p.Id}" };
                        var shortcut = i == 0 ? "F1" : i == 1 ? "F2" : "F3";
                        new Window(p.Name, Math.Max(2, lefts[i]), offersTop, rows, shortcut, fixedWidth: tileWidth).Draw();
                    }
                }

                var adminText = new List<string> { "1. Administrera produkter", "2. Administrera kategorier", "3. Administrera kunder", "4. Se statistik(Queries)" };
                var adminLeft = Math.Max(0, Console.WindowWidth - 34);
                var adminTop = Math.Max(0, Console.WindowHeight - 8);
                new Window("Admin", adminLeft, adminTop, adminText, "A", fixedWidth: 30).Draw();

                PrintShortcuts();
            }
            catch { return; }
        }
        public static void RenderShopPage()
        {
            _currentView = View.Shop;

            try
            {
                ResetBufferToWindow();
                try { Console.Clear(); } catch { }
                ClearVisibleWindow();
                Console.SetCursorPosition(0, 0);
                Lowest.LowestPosition = 0;

                var derivedCategories = ShopService.Products.Select(p => p.CategoryName).Distinct().ToList();
                var categories = (ShopService.ProductCategories != null && ShopService.ProductCategories.Any())
                    ? ShopService.ProductCategories.ToList()
                    : derivedCategories;

                var categoriesRows = categories.Select((c, idx) => $"{idx + 1}. {c}").ToList();
                new Window("Kategorier", 2, 1, categoriesRows, "C", fixedWidth: 20).Draw();

                int catTop = 1;
                int catContentStart = catTop + 1;
                int catBottomRow = catContentStart + categoriesRows.Count;

                var best = ShopService.GetBestSellingProducts(10);
                var hot = best.Any() ? best : ShopService.Products.Take(10).ToList();

                int tileWidth = 20;
                int gutter = 4;
                int contentLines = 3;
                int tileHeight = contentLines + 2;

                int availableWidth = Math.Max(20, Console.WindowWidth - 4);
                int cols = (availableWidth + gutter) / (tileWidth + gutter);
                cols = Math.Clamp(cols, 1, 4);
                cols = Math.Min(cols, Math.Max(1, hot.Count));

                int totalGridWidth = cols * tileWidth + (cols - 1) * gutter;
                int startLeft = Math.Max(2, (Console.WindowWidth / 2) - (totalGridWidth / 2));

                int titleRow = Math.Clamp(catBottomRow + 1, 0, Console.WindowHeight - 1);
                int sampleStartTop = Math.Clamp(catBottomRow + 3, 0, Console.WindowHeight - 1);

                int availableHeight = Console.WindowHeight - sampleStartTop - 2;
                int rowsThatFit = Math.Max(1, availableHeight / (tileHeight + 1));
                int maxItemsThatFit = Math.Max(1, rowsThatFit * cols);

                var sample = hot.Take(Math.Min(10, maxItemsThatFit)).ToList();
                _shopHotkeys = sample;

                if (sample.Any())
                {
                    string title = "Bästsäljare";
                    int titleLeft = Math.Clamp((Console.WindowWidth - title.Length) / 2, 0, Math.Max(0, Console.WindowWidth - title.Length));

                    var prevColor = Console.ForegroundColor;
                    try
                    {
                        Console.SetCursorPosition(titleLeft, titleRow);
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write(title);
                    }
                    catch { }
                    finally
                    {
                        Console.ForegroundColor = prevColor;
                    }

                    for (int i = 0; i < sample.Count; i++)
                    {
                        var p = sample[i];
                        int col = i % cols;
                        int row = i / cols;
                        int left = startLeft + col * (tileWidth + gutter);
                        int top = sampleStartTop + row * (tileHeight + 1);

                        if (top >= 0 && top < Console.WindowHeight)
                        {
                            new Window(
                                p.Name,
                                left,
                                top,
                                new List<string> { p.Name, $"Pris: {p.Price} kr", $"Id: {p.Id}" },
                                $"F{Math.Min(i + 1, 10)}",
                                fixedWidth: tileWidth
                            ).Draw();
                        }
                    }
                }

                PrintShortcuts();
            }
            catch { return; }
        }

        public static void RenderCartPage()
        {
            _currentView = View.Cart;

            try
            {
                ResetBufferToWindow();
                try { Console.Clear(); } catch { }
                ClearVisibleWindow();
                Console.SetCursorPosition(0, 0);
                Lowest.LowestPosition = 0;

                var items = ShopService.GetCartDisplayItems();
                _cartHotkeys = items;

                if (!items.Any())
                {
                    var cartWindow = ShopService.GetCartWindow();
                    cartWindow.Draw();

                    SafeSetCursorToLowest();
                    try
                    {
                        Console.SetCursorPosition(0, Math.Clamp(Lowest.LowestPosition, 0, Console.WindowHeight - 1));
                        Console.Write("ENTER=checka ut, ESC=återgå.".PadRight(Console.WindowWidth));
                    }
                    catch { }

                    Lowest.LowestPosition = Math.Clamp(Lowest.LowestPosition + 1, 0, Math.Max(0, Console.WindowHeight - 1));
                    PrintShortcuts();
                    return;
                }
                int count = Math.Min(items.Count, 10);
                int designWidth = Math.Min(100, Console.WindowWidth);
                int tileWidth = 28;
                int gutter = 4;
                int contentLines = 3;
                int tileHeight = contentLines + 2;
                int availableWidth = Math.Max(20, designWidth - 4);
                int cols = Math.Min(count, Math.Max(1, (availableWidth + gutter) / (tileWidth + gutter)));
                while (cols > 1 && (cols * tileWidth + (cols - 1) * gutter) > availableWidth) cols--;

                int totalGridWidth = cols * tileWidth + (cols - 1) * gutter;
                int startLeft = Math.Max(2, (Console.WindowWidth / 2) - (totalGridWidth / 2));
                int startTop = 2;

                int winW2 = Math.Max(0, Console.WindowWidth);
                string title2 = "Din varukorg";
                int gridLeft2 = startLeft;
                int gridRight2 = startLeft + totalGridWidth;
                int titleLeft2 = Math.Clamp((gridLeft2 + gridRight2 - title2.Length) / 2, 0, Math.Max(0, Console.WindowWidth - title2.Length));
                int titleRow2 = Math.Clamp(startTop - 2, 0, Console.WindowHeight - 1);

                var prevColor2 = Console.ForegroundColor;
                try
                {
                    try { Console.SetCursorPosition(0, titleRow2); Console.Write(new string(' ', winW2)); } catch { }
                    Console.SetCursorPosition(titleLeft2, titleRow2);
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write(title2);
                }
                finally { Console.ForegroundColor = prevColor2; }

                for (int i = 0; i < count; i++)
                {
                    var entry = items[i];
                    int col = i % cols; int row = i / cols;
                    int left = startLeft + col * (tileWidth + gutter);
                    int top = startTop + row * (tileHeight + 1);

                    var rows = new List<string>
                    {
                        $"{entry.Quantity} st {entry.Product.Name}",
                        $"Pris: {entry.Product.Price} kr",
                        $"Id: {entry.Product.Id}"
                    };

                    if (top >= 0 && top < Console.WindowHeight)
                        new Window(entry.Product.Name, left, top, rows, $"F{Math.Min(i + 1, 10)}", fixedWidth: tileWidth).Draw();
                }

                decimal total = items.Sum(x => x.Product.Price * x.Quantity);
                int instrRow = startTop + ((count + cols - 1) / cols) * (tileHeight + 1) + 1;
                instrRow = Math.Clamp(instrRow, 0, Console.WindowHeight - 1);
                try
                {
                    Console.SetCursorPosition(0, instrRow);
                    Console.Write($"Totalt: {total} kr".PadRight(Console.WindowWidth));
                }
                catch { }

                int hintRow = Math.Clamp(instrRow + 1, 0, Console.WindowHeight - 1);
                try
                {
                    Console.SetCursorPosition(0, hintRow);
                    Console.Write("ENTER=checka ut, ESC=återgå.".PadRight(Console.WindowWidth));
                }
                catch { }

                Lowest.LowestPosition = Math.Clamp(hintRow + 1, 0, Math.Max(0, Console.WindowHeight - 1));
                PrintShortcuts();
            }
            catch { return; }
        }
        private static void ResetBufferToWindow()
        {
            try
            {
                var winW = Math.Max(1, Console.WindowWidth);
                var winH = Math.Max(1, Console.WindowHeight);
                if (Console.BufferWidth != winW || Console.BufferHeight != winH)
                    Console.SetBufferSize(winW, winH);
            }
            catch { }
        }

        public static void ClearVisibleWindow()
        {
            int winW = Math.Max(0, Console.WindowWidth);
            int winH = Math.Max(0, Console.WindowHeight);
            var blank = new string(' ', winW);
            for (int r = 0; r < winH; r++)
            {
                try { Console.SetCursorPosition(0, r); Console.Write(blank); }
                catch { }
            }
            try { Console.SetCursorPosition(0, 0); } catch { }
        }
    }
}
