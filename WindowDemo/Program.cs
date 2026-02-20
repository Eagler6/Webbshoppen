using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace WindowDemo
{
    internal class Program
    {
        static void Main()
        {
            var connectionString =
                "Server=tcp:edinsdb.database.windows.net,1433;Initial Catalog=EdinsDB;Persist Security Info=False;User ID=edin;Password=PratesOliPoa479;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            ShopService.SetDbFactory(() => new AppDbContext(options));

            using (var db = new AppDbContext(options))
            {
                db.Database.Migrate();

                if (!db.Products.AsNoTracking().Any())
                {
                    var seed = Product.AllProducts
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

                    db.Products.AddRange(seed);
                    db.SaveChanges();
                }
            }
            ShopService.Init();

            Draw.RenderStartPage();

            while (true)
            {
                var key = Console.ReadKey(true);

                if (key.KeyChar == '1') { Draw.RenderStartPage(); continue; }
                if (key.KeyChar == '2') { Draw.RenderShopPage(); continue; }
                if (key.KeyChar == '3') { Draw.RenderCartPage(); continue; }

                if (key.Key == ConsoleKey.Q)
                    break;

                Draw.HandleKey(key);
            }
        }
    }
}
