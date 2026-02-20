using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WindowDemo
{
    public enum ProductCategory
    {
        Unknown = 0,
        Tröjor = 1,
        Byxor = 2,
        Skor = 3
    }

    public sealed record Product
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;

        // Rätt DB-lösning
        public int CategoryId { get; init; }
        public Category? Category { get; init; }

        public string Description { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int Stock { get; set; }
        public int SupplierId { get; init; }

        public Product() { }

        public Product(int id, string name, int categoryId, string description, decimal price, int stock = 0, int supplierId = 0)
        {
            Id = id;
            Name = name ?? string.Empty;
            CategoryId = categoryId;
            Description = description ?? string.Empty;
            Price = price;
            Stock = stock;
            SupplierId = supplierId;
        }

        // Hjälp-egenskap för UI (inte lagrad i DB)
        [NotMapped]
        public string CategoryName => Category?.Name ?? CategoryIdToName(CategoryId);

        [NotMapped]
        public ProductCategory CategoryEnum => ParseCategory(CategoryName);

        private static ProductCategory ParseCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return ProductCategory.Unknown;

            var s = category.Trim();

            if (string.Equals(s, "Tröjor", StringComparison.OrdinalIgnoreCase)) return ProductCategory.Tröjor;
            if (string.Equals(s, "Byxor", StringComparison.OrdinalIgnoreCase)) return ProductCategory.Byxor;
            if (string.Equals(s, "Skor", StringComparison.OrdinalIgnoreCase)) return ProductCategory.Skor;

            if (Enum.TryParse<ProductCategory>(s, ignoreCase: true, out var parsed))
                return parsed;

            return ProductCategory.Unknown;
        }

        private static string CategoryIdToName(int categoryId) => categoryId switch
        {
            1 => "Tröjor",
            2 => "Byxor",
            3 => "Skor",
            _ => "Unknown"
        };

        public static IReadOnlyList<Product> AllProducts { get; } = CreateProducts();

        private static List<Product> CreateProducts()
        {
            // 1 = Tröjor, 2 = Byxor, 3 = Skor
            var seed = new (string Name, int CategoryId, string Desc, decimal Price, int Stock)[]
            {
                ("Blå tröja",1,"Mjuk och varm ulltröja",299m, 12),
                ("Röd tröja",1,"Tunn merinotröja",249m, 20),
                ("Grå hoodie",1,"Bekväm hoodie i bomull",399m, 15),
                ("Vit t-shirt",1,"Bas-t-shirt i ekologisk bomull",149m, 30),
                ("Randig tröja",1,"Stilren randig tröja",199m, 10),

                ("Jeans Slim",2,"Slim fit jeans i blått",499m, 5),
                ("Chinos",2,"Casual chinos",449m, 8),
                ("Svarta byxor",2,"Klassiska svarta byxor",399m, 10),
                ("Joggers",2,"Mjukare vardagsbyxa",299m, 20),
                ("Shorts",2,"Sommarshorts",199m, 25),

                ("Vita sneakers",3,"Klassiska sneakers",599m, 10),
                ("Läderskor",3,"Finskor i läder",899m, 5),
                ("Sandaler",3,"Bekväma sandaler",249m, 15),
                ("Vandringskängor",3,"Robusta kängor",1299m, 8),
                ("Gym-skor",3,"Lätta träningsskor",699m, 12)
            };

            var list = new List<Product>(seed.Length);
            int id = 1;

            foreach (var (name, categoryId, desc, price, stock) in seed)
                list.Add(new Product(id++, name, categoryId, desc, price, stock));

            return list;
        }
    }
}