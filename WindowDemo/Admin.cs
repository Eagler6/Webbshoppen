using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace WindowDemo
{
    public record Supplier
    {
        public int Id { get; init; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Email { get; set; }

        public Supplier(int id, string name, string contact = "", string email = "")
        {
            Id = id;
            Name = name;
            Contact = contact;
            Email = email;
        }

        public override string ToString() => $"{Name} ({Contact}) - {Email}";
    }

    internal static partial class ShopService
    {
        private enum AdminMainChoice { None, Products = 1, Categories, Customers, Orders, Suppliers }
        private enum ListAction { None, Add, Edit, Remove, Back }

        public static void AdminMenu()
        {
            Console.Clear();
            Console.WriteLine("Admin - Logga in för att fortsätta.");
            if (!AuthenticateAdmin())
            {
                Console.WriteLine("Ogiltiga inloggningsuppgifter.");
                Console.WriteLine("Tryck ESC för att återgå.");
                while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }
                return;
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine("ADMIN - Huvudmeny");
                Console.WriteLine("1. Produkter");
                Console.WriteLine("2. Produktkategorier");
                Console.WriteLine("3. Kunder");
                Console.WriteLine("4. Beställningshistorik");
                Console.WriteLine("5. Leverantörer");
                Console.WriteLine("Esc. Logga ut");
                Console.Write("Val: ");

                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape) break;

                switch (KeyToAdminMainChoice(key))
                {
                    case AdminMainChoice.Products: ManageProducts(); break;
                    case AdminMainChoice.Categories: ManageCategories(); break;
                    case AdminMainChoice.Customers: ManageCustomers(); break; // finns i Customers.cs (partial)
                    case AdminMainChoice.Orders: ShowOrders(); break;
                    case AdminMainChoice.Suppliers: ManageSuppliers(); break;
                }
            }
        }

        private static AdminMainChoice KeyToAdminMainChoice(ConsoleKeyInfo key) => key.KeyChar switch
        {
            '1' => AdminMainChoice.Products,
            '2' => AdminMainChoice.Categories,
            '3' => AdminMainChoice.Customers,
            '4' => AdminMainChoice.Orders,
            '5' => AdminMainChoice.Suppliers,
            _ => AdminMainChoice.None
        };

        private static bool AuthenticateAdmin()
        {
            var user = Read("Användarnamn: ");
            var pass = Read("Lösenord: ");
            return user != null
                && pass != null
                && string.Equals(user, "Admin", StringComparison.OrdinalIgnoreCase)
                && pass == "admin123";
        }

        private static ListAction KeyToListAction(ConsoleKeyInfo key) => key.Key switch
        {
            ConsoleKey.Escape => ListAction.Back,
            ConsoleKey.A => ListAction.Add,
            ConsoleKey.E => ListAction.Edit,
            ConsoleKey.R => ListAction.Remove,
            _ => ListAction.None
        };

        // PRODUKTER
        private static void ManageProducts()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("ADMIN - Produkter");
                PrintProducts();

                Console.WriteLine();
                Console.WriteLine("A = Lägg till, E = Redigera, R = Ta bort, Esc = Tillbaka");
                Console.Write("Val: ");

                var action = KeyToListAction(Console.ReadKey(true));
                if (action == ListAction.Back) break;

                switch (action)
                {
                    case ListAction.Add:
                        AddProduct();
                        break;

                    case ListAction.Edit:
                        if (TryPickIndex("Ange produktnummer att redigera: ", Products.Count, out var eIdx))
                            EditProduct(Products[eIdx]);
                        break;

                    case ListAction.Remove:
                        if (TryPickIndex("Ange produktnummer att ta bort: ", Products.Count, out var rIdx))
                            RemoveProduct(Products[rIdx]);
                        break;
                }
            }
        }

        private static void PrintProducts()
        {
            for (int i = 0; i < Products.Count; i++)
            {
                var p = Products[i];
                var supplierName = Suppliers.FirstOrDefault(s => s.Id == p.SupplierId)?.Name ?? "Ingen";
                Console.WriteLine($"{i + 1}. {p.Name} - {p.Price} kr (Id:{p.Id}) Kategori:{p.CategoryName} Lager:{p.Stock} Leverantör:{supplierName}");
            }
        }

        private static void AddProduct()
        {
            Console.Clear();
            Console.WriteLine("Lägg till produkt:");

            var name = Read("Namn: "); if (name == null) return;
            var desc = Read("Infotext: "); if (desc == null) return;
            var price = ReadDecimal("Pris (decimalt): "); if (price == null) { Pause("Ogiltigt pris."); return; }

            var categoryName = Read("Kategori (ny eller befintlig): ");
            if (categoryName == null) return;

            categoryName = categoryName.Trim();
            if (string.IsNullOrWhiteSpace(categoryName)) { Pause("Ogiltig kategori."); return; }

            var stock = ReadInt("Lagersaldo (heltal): "); if (stock == null) { Pause("Ogiltigt lagersaldo."); return; }

            var supplierId = PickSupplierId();

            var savedToDb = TryDbWrite(db =>
            {
                var category = db.Categories.FirstOrDefault(c => c.Name.ToLower() == categoryName.ToLower());
                if (category == null)
                {
                    category = new Category { Name = categoryName };
                    db.Categories.Add(category);
                    db.SaveChanges();
                }

                var newProduct = new Product(0, name, category.Id, desc, price.Value, stock.Value, supplierId);
                db.Products.Add(newProduct);
                db.SaveChanges();

                Products = db.Products.Include(p => p.Category).AsNoTracking().ToList();
                ProductCategories = db.Categories.AsNoTracking().Select(c => c.Name).OrderBy(x => x).ToList();
            }, onErrorMessage: "DB-fel vid sparning. Produkten lades inte till.");

            if (!savedToDb) return;

            Pause("Produkt tillagd.");
        }

        // REDIGERA PRODUCTS
        private static void EditProduct(Product p)
        {
            Console.Clear();
            Console.WriteLine($"Redigera produkt Id:{p.Id}");

            var name = Read($"Nytt namn (ENTER för att behålla) [{p.Name}]: "); if (name == null) return;
            var desc = Read($"Ny infotext (ENTER för att behålla) [{p.Description}]: "); if (desc == null) return;
            var priceStr = Read($"Nytt pris (ENTER för att behålla) [{p.Price}]: "); if (priceStr == null) return;
            var categoryInput = Read($"Ny kategori (ENTER för att behålla) [{p.CategoryName}]: "); if (categoryInput == null) return;
            var stockStr = Read($"Nytt lagersaldo (ENTER för att behålla) [{p.Stock}]: "); if (stockStr == null) return;

            var newName = string.IsNullOrWhiteSpace(name) ? p.Name : name;
            var newDesc = string.IsNullOrWhiteSpace(desc) ? p.Description : desc;
            var newPrice = decimal.TryParse(priceStr, out var pr) ? pr : p.Price;
            var newStock = int.TryParse(stockStr, out var st) ? st : p.Stock;
            var newSupplierId = PickSupplierId(allowKeep: true, current: p.SupplierId);

            var savedToDb = TryDbWrite(db =>
            {
                var categoryName = string.IsNullOrWhiteSpace(categoryInput) ? p.CategoryName : categoryInput.Trim();

                var category = db.Categories.FirstOrDefault(c => c.Name.ToLower() == categoryName.ToLower());
                if (category == null)
                {
                    category = new Category { Name = categoryName };
                    db.Categories.Add(category);
                    db.SaveChanges();
                }

                var updated = new Product(p.Id, newName, category.Id, newDesc, newPrice, newStock, newSupplierId);

                db.Products.Attach(updated);
                db.Entry(updated).State = EntityState.Modified;
                db.SaveChanges();

                Products = db.Products.Include(x => x.Category).AsNoTracking().ToList();
                ProductCategories = db.Categories.AsNoTracking().Select(c => c.Name).OrderBy(x => x).ToList();
            }, onErrorMessage: "DB-fel vid uppdatering.");

            if (!savedToDb) return;

            Pause("Produkten uppdaterad.");
        }

        private static void RemoveProduct(Product toRemove)
        {
            TryDbWrite(db =>
            {
                var dbEntity = db.Products.Find(toRemove.Id);
                if (dbEntity != null)
                {
                    db.Products.Remove(dbEntity);
                    db.SaveChanges();
                }
                Products = db.Products.AsNoTracking().ToList();
            }, onErrorMessage: "DB-fel vid borttagning. Produkten tas bort lokalt.");

            Products.RemoveAll(p => p.Id == toRemove.Id);
            FeaturedIds.Remove(toRemove.Id);

            Pause("Produkten togs bort.");
        }

        private static int PickSupplierId(bool allowKeep = false, int current = 0)
        {
            if (!Suppliers.Any()) return 0;

            Console.WriteLine();
            if (allowKeep)
            {
                var currentName = Suppliers.FirstOrDefault(s => s.Id == current)?.Name ?? "Ingen";
                Console.WriteLine($"Nuvarande leverantör: {currentName}");
                Console.WriteLine("Välj ny leverantör (nummer) eller tryck Enter för att behålla:");
            }
            else
            {
                Console.WriteLine("Välj leverantör (nummer) eller tryck Enter för ingen:");
            }

            for (int i = 0; i < Suppliers.Count; i++)
                Console.WriteLine($"{i + 1}. {Suppliers[i].Name}");

            var supSel = Read("Val: ");
            if (supSel == null) return allowKeep ? current : 0;
            if (string.IsNullOrWhiteSpace(supSel)) return allowKeep ? current : 0;

            return (int.TryParse(supSel, out int sIdx) && sIdx > 0 && sIdx <= Suppliers.Count)
                ? Suppliers[sIdx - 1].Id
                : (allowKeep ? current : 0);
        }


        //KATEGORIER
        private static void ManageCategories()
        {
            while (true)
            {
                // Ladda från DB
                TryDbRead(db =>
                {
                    return db.Categories.AsNoTracking().OrderBy(c => c.Name).Select(c => c.Name).ToList();
                }, out List<string>? dbCats);

                if (dbCats != null)
                    ProductCategories = dbCats;

                Console.Clear();
                Console.WriteLine("ADMIN - Produktkategorier");

                for (int i = 0; i < ProductCategories.Count; i++)
                    Console.WriteLine($"{i + 1}. {ProductCategories[i]}");

                Console.WriteLine();
                Console.WriteLine("A = Lägg till, R = Ta bort, Esc = Tillbaka");
                Console.Write("Val: ");

                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape) break;

                if (key.Key == ConsoleKey.A)
                {
                    var cat = Read("Ny kategori: ");
                    if (string.IsNullOrWhiteSpace(cat))
                    {
                        Pause("Ogiltig kategori.");
                        continue;
                    }

                    cat = cat.Trim();

                    TryDbWrite(db =>
                    {
                        var exists = db.Categories.Any(c => c.Name.ToLower() == cat.ToLower());
                        if (!exists)
                        {
                            db.Categories.Add(new Category { Name = cat });
                            db.SaveChanges();
                        }

                        ProductCategories = db.Categories.AsNoTracking()
                            .OrderBy(c => c.Name)
                            .Select(c => c.Name)
                            .ToList();
                    }, "DB-fel vid tillägg av kategori.");

                    Pause("Kategori tillagd.");
                    continue;
                }

                if (key.Key == ConsoleKey.R)
                {
                    if (TryPickIndex("Ange nummer att ta bort: ", ProductCategories.Count, out var idx))
                    {
                        var catName = ProductCategories[idx];

                        var removed = TryDbWrite(db =>
                        {
                            var cat = db.Categories.FirstOrDefault(c => c.Name == catName);
                            if (cat == null) return;

                            var usedByProducts = db.Products.Any(p => p.CategoryId == cat.Id);
                            if (usedByProducts)
                                throw new Exception("Kan inte ta bort kategori som används av produkter.");

                            db.Categories.Remove(cat);
                            db.SaveChanges();

                            ProductCategories = db.Categories.AsNoTracking()
                                .OrderBy(c => c.Name)
                                .Select(c => c.Name)
                                .ToList();
                        }, "DB-fel vid borttagning av kategori.");

                        if (removed)
                            Pause($"Kategori '{catName}' borttagen.");
                    }
                }
            }
        }
        private static void ShowOrders()
        {
            Console.Clear();
            Console.WriteLine("ADMIN - Beställningshistorik");
            if (!Orders.Any()) { Pause("Inga beställningar ännu."); return; }

            foreach (var o in Orders.OrderByDescending(x => x.Date))
                Console.WriteLine(o);

            Console.WriteLine();
            Console.WriteLine("Ange order-id för detaljer eller tryck valfri tangent för att återgå:");
            var input = ReadLineAllowEscape();
            if (string.IsNullOrWhiteSpace(input)) return;

            if (!int.TryParse(input, out int id)) return;
            var ord = Orders.FirstOrDefault(x => x.Id == id);
            if (ord == null) return;

            Console.Clear();
            Console.WriteLine($"Order {ord.Id} - {ord.Date}");
            Console.WriteLine($"Mottagare: {ord.RecipientName}");
            Console.WriteLine($"{ord.Address}");
            Console.WriteLine($"{ord.Postal} {ord.City}");
            Console.WriteLine();
            Console.WriteLine("Produkter:");
            foreach (var l in ord.Items) Console.WriteLine($"{l.Quantity} x {l.ProductName} - {l.UnitPrice} kr");
            Console.WriteLine();
            Console.WriteLine($"Delsumma: {ord.Subtotal} kr");
            Console.WriteLine($"Frakt: {ord.Shipping} kr");
            Console.WriteLine($"Moms: {ord.Vat} kr");
            Console.WriteLine($"Totalt: {ord.Total} kr");
            Pause();
        }
        public static List<Supplier> Suppliers { get; private set; } = new();
        private static int NextSupplierId => (Suppliers.Any() ? Suppliers.Max(s => s.Id) : 0) + 1;

        public static void InitSuppliers()
        {
            Suppliers = new List<Supplier>
            {
                new Supplier(1, "Nordic Textiles", "Erik Svensson", "erik@nordictext.se"),
                new Supplier(2, "ShoeCo", "Anna Nilsson", "anna@shoeco.se"),
                new Supplier(3, "Outdoor Goods", "Lars Karlsson", "lars@outdoorgoods.se")
            };
        }

        private static void ManageSuppliers()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("ADMIN - Leverantörer");

                for (int i = 0; i < Suppliers.Count; i++)
                    Console.WriteLine($"{i + 1}. {Suppliers[i]} (Id:{Suppliers[i].Id})");

                Console.WriteLine();
                Console.WriteLine("A = Lägg till, E = Redigera, R = Ta bort, Esc = Tillbaka");
                Console.Write("Val: ");

                var action = KeyToListAction(Console.ReadKey(true));
                if (action == ListAction.Back) break;

                switch (action)
                {
                    case ListAction.Add:
                        AddSupplier();
                        break;

                    case ListAction.Edit:
                        if (TryPickIndex("Ange leverantörsnummer att redigera: ", Suppliers.Count, out var eIdx))
                            EditSupplier(Suppliers[eIdx]);
                        break;

                    case ListAction.Remove:
                        if (TryPickIndex("Ange leverantörsnummer att ta bort: ", Suppliers.Count, out var rIdx))
                        {
                            Suppliers.RemoveAt(rIdx);
                            Pause("Leverantör borttagen.");
                        }
                        break;
                }
            }
        }

        private static void AddSupplier()
        {
            var name = Read("Namn: "); if (name == null) return;
            var contact = Read("Kontakt: ") ?? "";
            var email = Read("Email: ") ?? "";
            Suppliers.Add(new Supplier(NextSupplierId, name, contact, email));
            Pause("Leverantör tillagd.");
        }

        private static void EditSupplier(Supplier s)
        {
            Console.Clear();
            Console.WriteLine($"Redigera leverantör Id:{s.Id}");

            var name = Read($"Nytt namn (ENTER för att behålla) [{s.Name}]: "); if (name == null) return;
            var contact = Read($"Ny kontakt (ENTER för att behålla) [{s.Contact}]: "); if (contact == null) return;
            var email = Read($"Ny email (ENTER för att behålla) [{s.Email}]: "); if (email == null) return;

            if (!string.IsNullOrWhiteSpace(name)) s.Name = name;
            if (!string.IsNullOrWhiteSpace(contact)) s.Contact = contact;
            if (!string.IsNullOrWhiteSpace(email)) s.Email = email;

            Pause("Leverantör uppdaterad.");
        }
        private static bool TryPickIndex(string prompt, int count, out int zeroBasedIndex)
        {
            zeroBasedIndex = -1;
            var input = Read(prompt);
            if (input == null) return false;

            if (!int.TryParse(input, out int idx)) return false;
            if (idx <= 0 || idx > count) return false;

            zeroBasedIndex = idx - 1;
            return true;
        }

        private static bool TryDbWrite(Action<AppDbContext> write, string? onErrorMessage = null)
        {
            try
            {
                if (!TryCreateDb(out var db)) return false;
                using (db)
                {
                    write(db);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB ERROR: {ex.Message}");
                if (!string.IsNullOrWhiteSpace(onErrorMessage))
                    Pause(onErrorMessage);
                return false;
            }
        }
    }
}
