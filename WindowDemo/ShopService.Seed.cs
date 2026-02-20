using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace WindowDemo
{
    internal static partial class ShopService
    {
        public static void SeedDatabase()
        {
            try
            {
                if (!TryCreateDb(out var db))
                    return;

                using (db)
                {
                    var existingProductNames = db.Products
                        .AsNoTracking()
                        .Select(p => p.Name)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var productsToAdd = Product.AllProducts
                        .Where(p => !existingProductNames.Contains(p.Name))
                        .Select(p => new Product(
                            0,
                            p.Name,
                            p.CategoryId,
                            p.Description,
                            p.Price,
                            p.Stock,
                            p.SupplierId
                        ))
                        .ToList();

                    if (productsToAdd.Count > 0)
                    {
                        db.Products.AddRange(productsToAdd);
                        db.SaveChanges();
                    }
                    var seedCustomers = LoadSeedCustomers();
                    if (seedCustomers.Count == 0)
                        return;

                    var existingCustomers = db.Customers
                        .AsNoTracking()
                        .Select(c => new { c.Email, c.Name, c.Address })
                        .ToList();

                    var existingEmails = existingCustomers
                        .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                        .Select(x => x.Email!)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var existingNameAddress = existingCustomers
                        .Select(x => (
                            Name: (x.Name ?? "").Trim().ToLowerInvariant(),
                            Address: (x.Address ?? "").Trim().ToLowerInvariant()
                        ))
                        .ToHashSet();

                    var customersToAdd = new List<Customer>();

                    foreach (var c in seedCustomers)
                    {
                        var email = (c.Email ?? "").Trim();

                        bool exists = !string.IsNullOrWhiteSpace(email)
                            ? existingEmails.Contains(email)
                            : existingNameAddress.Contains((
                                (c.Name ?? "").Trim().ToLowerInvariant(),
                                (c.Address ?? "").Trim().ToLowerInvariant()
                            ));

                        if (!exists)
                        {
                            customersToAdd.Add(new Customer(
                                0,
                                c.Name ?? "",
                                c.Email ?? "",
                                c.Address ?? "",
                                c.Postal ?? "",
                                c.City ?? ""
                            ));
                        }
                    }

                    if (customersToAdd.Count > 0)
                    {
                        db.Customers.AddRange(customersToAdd);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Seed] DB seeding failed: {ex.Message}");
            }
        }
        private static List<Customer> LoadSeedCustomers()
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "WindowDemo");

                var path = Path.Combine(dir, "customers.json");

                if (!File.Exists(path))
                    return new List<Customer>();

                var json = File.ReadAllText(path);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                return JsonSerializer.Deserialize<List<Customer>>(json, opts)
                       ?? new List<Customer>();
            }
            catch
            {
                return new List<Customer>();
            }
        }
    }
}
