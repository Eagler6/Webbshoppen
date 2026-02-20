using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WindowDemo
{
    public sealed record Customer
    {
        public int Id { get; init; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string Postal { get; set; } = "";
        public string City { get; set; } = "";
        public Customer() { }
        public Customer(int id, string name, string email = "", string address = "", string postal = "", string city = "")
        {
            Id = id;
            Name = name ?? "";
            Email = email ?? "";
            Address = address ?? "";
            Postal = postal ?? "";
            City = city ?? "";
        }

        public override string ToString() => $"{Name} ({Email}) - {Address}, {Postal} {City}";
    }

    internal static partial class ShopService
    {
        private enum CustomerMenuAction { None, Add, Edit, Remove, Back }

        public static List<Customer> Customers { get; private set; } = new();

        private static int NextCustomerId =>
            (Customers.Count > 0 ? Customers.Max(c => c.Id) : 0) + 1;

        private static readonly string AppDataDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WindowDemo");
        public static void InitCustomers()
        {
            Customers = new List<Customer>();

            _ = Task.Run(() =>
            {
                try
                {
                    if (!TryCreateDb(out var db)) return;
                    using (db)
                    {
                        var list = db.Customers.AsNoTracking().ToList();
                        if (list.Count > 0) Customers = list;
                    }
                }
                catch
                {
                  
                }
            });
        }
        private static void SaveCustomers()
        {
            try
            {
                if (!TryCreateDb(out var db)) return;
                using (db)
                {
                    var dbList = db.Customers.AsNoTracking().ToList();
                    var byEmail = new Dictionary<string, Customer>(StringComparer.OrdinalIgnoreCase);
                    var byNameAddress = new Dictionary<(string Name, string Address), Customer>();

                    foreach (var x in dbList)
                    {
                        var email = (x.Email ?? "").Trim();
                        if (!string.IsNullOrWhiteSpace(email) && !byEmail.ContainsKey(email))
                            byEmail[email] = x;

                        var key = (Name: (x.Name ?? "").Trim(), Address: (x.Address ?? "").Trim());
                        if (!byNameAddress.ContainsKey(key))
                            byNameAddress[key] = x;
                    }

                    var toAdd = new List<Customer>();
                    var toUpdate = new List<Customer>();

                    foreach (var c in Customers)
                    {
                        var name = (c.Name ?? "").Trim();
                        var address = (c.Address ?? "").Trim();
                        var email = (c.Email ?? "").Trim();

                        Customer? existing = null;

                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            byEmail.TryGetValue(email, out existing);
                        }
                        else
                        {
                            byNameAddress.TryGetValue((name, address), out existing);
                        }

                        if (existing != null)
                        {
                            toUpdate.Add(new Customer(existing.Id, c.Name, c.Email, c.Address, c.Postal, c.City));
                        }
                        else
                        {
                            toAdd.Add(new Customer(0, c.Name, c.Email, c.Address, c.Postal, c.City));
                        }
                    }

                    foreach (var u in toUpdate)
                    {
                        db.Customers.Attach(u);
                        db.Entry(u).State = EntityState.Modified;
                    }

                    if (toAdd.Count > 0)
                        db.Customers.AddRange(toAdd);

                    db.SaveChanges();

                    Customers = db.Customers.AsNoTracking().ToList();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    if (!Directory.Exists(AppDataDir)) Directory.CreateDirectory(AppDataDir);
                    File.AppendAllText(
                        Path.Combine(AppDataDir, "db_errors.log"),
                        $"{DateTime.UtcNow:O} - SaveCustomers exception: {ex}\n"
                    );
                }
                catch { }
            }
        }

        private static void ManageCustomers()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("ADMIN - Kunder");

                for (int i = 0; i < Customers.Count; i++)
                    Console.WriteLine($"{i + 1}. {Customers[i]}");

                Console.WriteLine();
                Console.WriteLine("E = Redigera, A = Lägg till, R = Ta bort, Esc = Tillbaka");
                Console.Write("Val: ");

                var action = KeyToCustomerMenuAction(Console.ReadKey(true));
                if (action == CustomerMenuAction.Back) break;

                if (action == CustomerMenuAction.Add)
                {
                    var name = Read("Namn: "); if (name == null) continue;
                    var email = Read("Email: ") ?? "";
                    var addr = Read("Adress: ") ?? "";
                    var postal = Read("Postnummer: ") ?? "";
                    var city = Read("Ort: ") ?? "";

                    Customers.Add(new Customer(NextCustomerId, name, email, addr, postal, city));
                    SaveCustomers();
                    Pause("Kund tillagd. Tryck valfri tangent...");
                    continue;
                }

                if (action == CustomerMenuAction.Edit)
                {
                    var input = Read("Ange kundnummer att redigera: ");
                    if (input != null && int.TryParse(input, out int idx) && idx > 0 && idx <= Customers.Count)
                        EditCustomer(Customers[idx - 1]);
                    continue;
                }

                if (action == CustomerMenuAction.Remove)
                {
                    var input = Read("Ange kundnummer att ta bort: ");
                    if (input != null && int.TryParse(input, out int idx) && idx > 0 && idx <= Customers.Count)
                    {
                        Customers.RemoveAt(idx - 1);
                        SaveCustomers();
                        Pause("Kund borttagen. Tryck valfri tangent...");
                    }
                }
            }
        }

        private static CustomerMenuAction KeyToCustomerMenuAction(ConsoleKeyInfo key) => key.Key switch
        {
            ConsoleKey.Escape => CustomerMenuAction.Back,
            ConsoleKey.A => CustomerMenuAction.Add,
            ConsoleKey.E => CustomerMenuAction.Edit,
            ConsoleKey.R => CustomerMenuAction.Remove,
            _ => CustomerMenuAction.None
        };

        private static void EditCustomer(Customer c)
        {
            Console.Clear();
            Console.WriteLine($"Redigera kund Id:{c.Id}");

            var name = Read($"Nytt namn (ENTER för att behålla) [{c.Name}]: "); if (name == null) return;
            var email = Read($"Ny email (ENTER för att behålla) [{c.Email}]: "); if (email == null) return;
            var addr = Read($"Ny adress (ENTER för att behålla) [{c.Address}]: "); if (addr == null) return;
            var postal = Read($"Nytt postnummer (ENTER för att behålla) [{c.Postal}]: "); if (postal == null) return;
            var city = Read($"Ny ort (ENTER för att behålla) [{c.City}]: "); if (city == null) return;

            if (!string.IsNullOrWhiteSpace(name)) c.Name = name;
            if (!string.IsNullOrWhiteSpace(email)) c.Email = email;
            if (!string.IsNullOrWhiteSpace(addr)) c.Address = addr;
            if (!string.IsNullOrWhiteSpace(postal)) c.Postal = postal;
            if (!string.IsNullOrWhiteSpace(city)) c.City = city;

            SaveCustomers();
            Pause("Kund uppdaterad. Tryck valfri tangent...");
        }
    }
}
