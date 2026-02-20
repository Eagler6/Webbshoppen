using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowDemo
{
    internal static partial class ShopService
    {
        public static List<Product> Products { get; private set; } = new();
        public static List<int> FeaturedIds { get; private set; } = new();
        public static List<Product> Cart { get; private set; } = new();
        public static List<string> ProductCategories { get; private set; } = new();
        public static List<Order> Orders { get; private set; } = new();
        public static void Init() => Init(null);
        public static void Init(AppDbContext? _ignored)
        {
            LoadDefaults();

            if (TryDbRead(db =>
            {
                var dbProducts = db.Products
                    .Include(p => p.Category)
                    .AsNoTracking()
                    .ToList();

                var dbOrders = db.Orders.AsNoTracking()
                    .Include(o => o.Items)
                    .OrderByDescending(o => o.Date)
                    .ToList();

                return (dbProducts, dbOrders);
            }, out var loaded))
            {
                if (loaded.dbProducts.Count > 0)
                {
                    Products = loaded.dbProducts;
                    FeaturedIds = Products.Take(3).Select(p => p.Id).ToList();
                    ProductCategories = Products.Select(p => p.CategoryName).Distinct().ToList();
                }

                Orders = loaded.dbOrders;
            }
        }

        private static void LoadDefaults()
        {
            Products = new List<Product>(Product.AllProducts);
            FeaturedIds = Products.Take(3).Select(p => p.Id).ToList();
            ProductCategories = Products.Select(p => p.CategoryName).Distinct().ToList();
            Orders = new List<Order>();
        }
        public static List<Product> GetFeaturedProducts() =>
            FeaturedIds
                .Select(id => Products.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .ToList()!;
        public static List<Product> GetBestSellingProducts(int take = 5)
        {
            try
            {
                if (!TryDbRead(db =>
                {
                    var best = (
                        from ol in db.OrderLines.AsNoTracking()
                        join p in db.Products.AsNoTracking() on ol.ProductId equals p.Id
                        group new { ol, p } by p.Id into g
                        orderby g.Sum(x => x.ol.Quantity) descending
                        select g.Select(x => x.p).FirstOrDefault()
                    )
                    .Take(take)
                    .ToList();

                    return best.Where(p => p != null).ToList()!;
                }, out var bestProducts))
                {
                    return Products.Take(take).ToList();
                }

                if (bestProducts.Count == 0)
                    return Products.Take(take).ToList();

                return bestProducts;
            }
            catch
            {
                return Products.Take(take).ToList();
            }
        }

        // SHOPPENS UI
        public static void ShowShopInteractive()
        {
            while (true)
            {
                Console.Clear();
                var categories = Products.Select(p => p.CategoryName).Distinct().ToList();

                Console.WriteLine("Kategorier:");
                for (int i = 0; i < categories.Count; i++)
                    Console.WriteLine($"{i + 1}. {categories[i]}");

                Console.WriteLine();
                Console.WriteLine("S=Fritextsök, B=Visa utvalda, Esc=Åter");
                Console.Write("Välj kategori (nummer) eller kommando: ");

                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape) break;
                if (key.Key == ConsoleKey.S) { SearchProducts(); continue; }
                if (key.Key == ConsoleKey.B) { ShowFeaturedInteractive(); continue; }

                if (char.IsDigit(key.KeyChar))
                {
                    Console.Write(key.KeyChar);
                    var rest = ReadLineAllowEscape();
                    if (rest == null) { Console.WriteLine(); continue; }

                    if (int.TryParse(key.KeyChar + rest, out int catIndex) &&
                        catIndex > 0 && catIndex <= categories.Count)
                    {
                        ShowCategoryProducts(categories[catIndex - 1]);
                    }
                }
            }
        }

        public static void ShowProductDetail(Product p)
        {
            Console.Clear();
            var rows = new List<string>
            {
                p.Name,
                p.Description,
                $"Pris: {p.Price} kr",
                "",
                "Tryck K för att köpa, Esc för att gå tillbaka.",
                $"Produkt-id: {p.Id}"
            };

            new Window(p.Name, 4, 4, rows).Draw();
            Draw.SafeSetCursorToLowest();

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape) break;
                if (key.Key == ConsoleKey.K)
                {
                    Cart.Add(p);
                    Draw.SafeSetCursorToLowest(2);
                    Console.WriteLine($"{p.Name} lades till i kundvagnen. Tryck valfri tangent för att fortsätta...");
                    Console.ReadKey(true);
                    break;
                }
            }
        }

        public static void ShowFeaturedInteractive()
        {
            Console.Clear();
            var featured = GetFeaturedProducts();
            if (!featured.Any())
            {
                Console.WriteLine("Inga utvalda produkter är valda. Öppna Admin för att välja.");
                Pause();
                return;
            }

            int left = 2;
            foreach (var p in featured)
            {
                var rows = new List<string>
                {
                    p.Name,
                    p.Description,
                    $"Pris: {p.Price} kr",
                    "Tryck K för att köpa",
                    $"Id: {p.Id}"
                };
                new Window(p.Name, left, 4, rows).Draw();
                left += 28;
            }

            Draw.SafeSetCursorToLowest();
            Console.WriteLine();
            Console.WriteLine("Visar utvalda produkter. Tryck Esc för att återgå, eller välj produkt-id för mer info.");

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape) break;

                if (char.IsDigit(key.KeyChar))
                {
                    Draw.SafeSetCursorToLowest(3);
                    Console.Write("Ange produkt-id och tryck Enter: ");
                    var input = ReadLineAllowEscape();
                    if (input == null) { Console.WriteLine(); continue; }

                    if (int.TryParse(input, out int pid))
                    {
                        var prod = Products.FirstOrDefault(p => p.Id == pid);
                        if (prod != null) ShowProductDetail(prod);
                        Console.Clear();
                        ShowFeaturedInteractive();
                        return;
                    }

                    Console.WriteLine("Ogiltigt id.");
                }
            }
        }

        public static void SearchProducts()
        {
            Console.Clear();
            Console.Write("Sökterm: ");
            var term = ReadLineAllowEscape();
            if (term == null) return;

            var results = Products
                .Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            p.Description.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!results.Any())
            {
                Console.WriteLine();
                Pause("Inga träffar. Tryck valfri tangent för att återgå.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Träffar:");
            for (int i = 0; i < results.Count; i++)
                Console.WriteLine($"{i + 1}. {results[i].Name} - {results[i].Price} kr (Id:{results[i].Id})");

            Console.WriteLine();
            Console.WriteLine("Ange nummer för att visa produkt, eller tryck Enter för att återgå:");
            var input = ReadLineAllowEscape();
            if (input == null) return;

            if (int.TryParse(input, out int idx) && idx > 0 && idx <= results.Count)
                ShowProductDetail(results[idx - 1]);
        }

        private static void ShowCategoryProducts(string category)
        {
            Console.Clear();
            var prods = Products
                .Where(p => string.Equals(p.CategoryName, category, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!prods.Any())
            {
                Pause("Inga produkter i denna kategori. Tryck valfri tangent...");
                return;
            }

            Console.WriteLine($"Kategori: {category}");
            for (int i = 0; i < prods.Count; i++)
                Console.WriteLine($"{i + 1}. {prods[i].Name} - {prods[i].Price} kr (Id:{prods[i].Id})");

            Console.WriteLine();
            Console.WriteLine("Ange nummer för att visa produkt, eller tryck Esc för att återgå:");

            var input = ReadLineAllowEscape();
            if (input == null) return;

            if (int.TryParse(input, out int idx) && idx > 0 && idx <= prods.Count)
                ShowProductDetail(prods[idx - 1]);
        }
        public static Window GetCartWindow()
        {
            var cartText = Cart.Any()
                ? Cart.GroupBy(p => p.Id)
                      .Select(g => $"{g.Count()} st {g.First().Name} - {g.First().Price} kr")
                      .ToList()
                : new List<string> { "Varukorgen är tom" };

            var total = Cart.Sum(p => p.Price);
            cartText.Add($"Totalt: {total} kr");
            cartText.Add("Tryck ESC för att gå tillbaka, ENTER för att checka ut.");
            return new Window("Din varukorg", 30, 6, cartText, "3");
        }

        public static List<(Product Product, int Quantity)> GetCartDisplayItems() =>
            Cart.GroupBy(p => p.Id).Select(g => (Product: g.First(), Quantity: g.Count())).ToList();

        public static void UpdateCartQuantity(int productId, int quantity)
        {
            quantity = Math.Clamp(quantity, 0, 5);

            var product = Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return;

            int current = Cart.Count(p => p.Id == productId);
            if (quantity == current) return;

            if (quantity == 0)
            {
                Cart.RemoveAll(p => p.Id == productId);
                return;
            }

            if (quantity > current)
            {
                for (int i = 0; i < quantity - current; i++) Cart.Add(product);
                return;
            }

            for (int i = 0; i < current - quantity; i++)
            {
                var existing = Cart.FirstOrDefault(p => p.Id == productId);
                if (existing != null) Cart.Remove(existing);
            }
        }

        public static void PromptSetCartQuantityByIndex(int index)
        {
            var items = GetCartDisplayItems();
            if (index < 0 || index >= items.Count)
            {
                Pause("Ogiltigt val. Tryck valfri tangent...");
                return;
            }

            var entry = items[index];
            Console.Clear();
            Console.WriteLine($"Ändra antal för: {entry.Product.Name}");
            Console.WriteLine($"Nuvarande antal: {entry.Quantity}");
            Console.WriteLine("Ange nytt antal (0 för att ta bort) - max 5. Tryck ESC för att avbryta.");

            var newQty = ReadInt("Antal (0-5): ");
            if (newQty == null) { Pause("Avbrutet."); return; }

            UpdateCartQuantity(entry.Product.Id, newQty.Value);
            Pause("Antal uppdaterat. Tryck valfri tangent...");
        }


        // CHECKOUT
        public static void ShowShippingAndPaymentFlow()
        {
            if (!Cart.Any())
            {
                Console.Clear();
                Pause("Varukorgen är tom. Tryck valfri tangent för att återgå.");
                return;
            }

            Console.Clear();
            Console.WriteLine("=== Fraktuppgifter ===");

            var name = Read("Namn: "); if (name == null) { Pause("Avbrutet."); return; }
            var email = Read("Email: "); if (email == null) { Pause("Avbrutet."); return; }
            var address = Read("Adress: "); if (address == null) { Pause("Avbrutet."); return; }
            var postal = Read("Postnummer: "); if (postal == null) { Pause("Avbrutet."); return; }
            var city = Read("Ort: "); if (city == null) { Pause("Avbrutet."); return; }

            Console.WriteLine();
            Console.WriteLine("Välj frakttyp:");
            var shippingOptions = new List<(string Name, decimal Price)>
            {
                ("Standard (2-5 arbetsdagar)", 49m),
                ("Express (1-2 arbetsdagar)", 99m)
            };
            for (int i = 0; i < shippingOptions.Count; i++)
                Console.WriteLine($"{i + 1}. {shippingOptions[i].Name} - {shippingOptions[i].Price} kr");

            var shipSel = Read("Val (nummer): "); if (shipSel == null) { Pause("Avbrutet."); return; }
            if (!int.TryParse(shipSel, out int shipIdx) || shipIdx < 1 || shipIdx > shippingOptions.Count)
            {
                Pause("Ogiltigt val. Avbryter.");
                return;
            }
            var chosenShipping = shippingOptions[shipIdx - 1];

            Console.Clear();
            Console.WriteLine("=== Betala ===");

            var grouped = Cart.GroupBy(p => p.Id)
                              .Select(g => new { Product = g.First(), Quantity = g.Count() })
                              .ToList();

            decimal subtotal = grouped.Sum(g => g.Quantity * g.Product.Price);

            Console.WriteLine("Produkter:");
            foreach (var g in grouped)
                Console.WriteLine($"{g.Quantity} x {g.Product.Name} - {g.Product.Price} kr  = {g.Quantity * g.Product.Price} kr");

            Console.WriteLine();
            Console.WriteLine($"Delsumma: {subtotal} kr");
            Console.WriteLine($"Frakt ({chosenShipping.Name}): {chosenShipping.Price} kr");

            const decimal vatRate = 0.25m;
            decimal vat = grouped.Sum(g => Math.Round(g.Quantity * g.Product.Price * vatRate, 2, MidpointRounding.AwayFromZero));
            decimal total = subtotal + chosenShipping.Price;

            Console.WriteLine($"Moms ({vatRate * 100}%): {vat} kr");
            Console.WriteLine($"Totalt att betala: {total} kr");
            Console.WriteLine();

            Console.WriteLine("Välj betalningsmetod:");
            var paymentMethods = new List<string> { "Kort", "Swish" };
            for (int i = 0; i < paymentMethods.Count; i++)
                Console.WriteLine($"{i + 1}. {paymentMethods[i]}");

            var paySel = Read("Val (nummer): "); if (paySel == null) { Pause("Avbrutet."); return; }
            if (!int.TryParse(paySel, out int payIdx) || payIdx < 1 || payIdx > paymentMethods.Count)
            {
                Pause("Ogiltigt val. Avbryter.");
                return;
            }

            var confirm = Read("Bekräfta betalning och skicka order? (J/N): ");
            if (confirm == null || !(confirm.Equals("J", StringComparison.OrdinalIgnoreCase) || confirm.Equals("Y", StringComparison.OrdinalIgnoreCase)))
            {
                Pause("Betalning avbruten.");
                return;
            }

            var orderLines = grouped
                .Select(g => new OrderLine(g.Product.Id, g.Product.Name, g.Product.Price, g.Quantity))
                .ToList();

            int customerId = 0;

            var savedToDb = TryDbWrite(db =>
            {
                Customer? cust = null;

                if (!string.IsNullOrWhiteSpace(email))
                    cust = db.Customers.FirstOrDefault(c => c.Email == email);

                cust ??= db.Customers.FirstOrDefault(c =>
                    c.Name == name && c.Address == address && c.Postal == postal);

                if (cust == null)
                {
                    cust = new Customer(0, name, email, address, postal, city);
                    db.Customers.Add(cust);
                    db.SaveChanges();
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(email) && cust.Email != email)
                    {
                        cust.Email = email;
                        db.SaveChanges();
                    }
                }

                customerId = cust.Id;

                var order = new Order(
                    0,
                    customerId,
                    orderLines,
                    DateTime.UtcNow,
                    subtotal,
                    chosenShipping.Price,
                    vat,
                    total,
                    chosenShipping.Name,
                    name,
                    address,
                    postal,
                    city
                );

                db.Orders.Add(order);
                db.SaveChanges();

                Orders = db.Orders.AsNoTracking()
                    .Include(o => o.Items)
                    .OrderByDescending(o => o.Date)
                    .ToList();

                Customers = db.Customers.AsNoTracking().ToList();
            });

            if (!savedToDb)
            {
                Pause("Kunde inte spara order i databasen (DB offline).");
                return;
            }

            Cart.Clear();

            Console.Clear();
            Console.WriteLine("=== Orderbekräftelse ===\n");
            Console.WriteLine($"Mottagare: {name}");
            Console.WriteLine($"{address}");
            Console.WriteLine($"{postal} {city}\n");
            Console.WriteLine("Beställda produkter:");
            foreach (var l in orderLines)
                Console.WriteLine($"{l.Quantity} x {l.ProductName} - {l.UnitPrice} kr");

            Console.WriteLine();
            Console.WriteLine($"Delsumma: {subtotal} kr");
            Console.WriteLine($"Frakt: {chosenShipping.Price} kr");
            Console.WriteLine($"Moms: {vat} kr");
            Console.WriteLine($"Totalt: {total} kr\n");

            Pause("Betalning genomförd. Tack för din beställning!");
        }
        private static string? ReadLineAllowEscape()
        {
            var sb = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape) return null;
                if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return sb.ToString(); }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0) { sb.Length--; Console.Write("\b \b"); }
                    continue;
                }

                if (char.IsControl(key.KeyChar)) continue;
                sb.Append(key.KeyChar);
                Console.Write(key.KeyChar);
            }
        }

        private static string? Read(string prompt)
        {
            Console.Write(prompt);
            return ReadLineAllowEscape();
        }

        private static void Pause(string message = "Tryck valfri tangent...")
        {
            Console.WriteLine(message);
            Console.ReadKey(true);
        }

        private static decimal? ReadDecimal(string prompt)
        {
            var s = Read(prompt);
            if (s == null) return null;
            return decimal.TryParse(s, out var d) ? d : (decimal?)null;
        }

        private static int? ReadInt(string prompt)
        {
            var s = Read(prompt);
            if (s == null) return null;
            return int.TryParse(s, out var v) ? v : (int?)null;
        }
    }
}
