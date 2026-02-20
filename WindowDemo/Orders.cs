using System;
using System.Collections.Generic;

namespace WindowDemo
{
    public enum ShippingMethod
    {
        Unknown = 0,
        Standard = 1,
        Express = 2
    }

    public enum OrderStatus
    {
        Unknown = 0,
        Created = 1,
        Paid = 2,
        Shipped = 3,
        Cancelled = 4
    }

    public class Order
    {
        public int Id { get; private set; }
        public int CustomerId { get; private set; }
        public List<OrderLine> Items { get; private set; } = new();
        public DateTime Date { get; private set; }
        public decimal Subtotal { get; private set; }
        public decimal Shipping { get; private set; }
        public decimal Vat { get; private set; }
        public decimal Total { get; private set; }
        public string ShippingType { get; private set; } = "Unknown";
        public string RecipientName { get; private set; } = "";
        public string Address { get; private set; } = "";
        public string Postal { get; private set; } = "";
        public string City { get; private set; } = "";
        public OrderStatus Status { get; private set; } = OrderStatus.Unknown;

        protected Order() { }
        public Order(
            int id,
            int customerId,
            List<OrderLine> items,
            DateTime date,
            decimal subtotal,
            decimal shipping,
            decimal vat,
            decimal total,
            string shippingType,
            string recipientName,
            string address,
            string postal,
            string city)
        {
            Id = id;
            CustomerId = customerId;

            Items = items ?? new List<OrderLine>();

            Date = date.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
                : date.ToUniversalTime();

            Subtotal = subtotal;
            Shipping = shipping;
            Vat = vat;
            Total = total;

            ShippingType = string.IsNullOrWhiteSpace(shippingType)
                ? "Unknown"
                : shippingType;

            RecipientName = recipientName ?? "";
            Address = address ?? "";
            Postal = postal ?? "";
            City = city ?? "";

            Status = OrderStatus.Created;
        }
        public ShippingMethod ShippingMethod => ParseShippingMethod(ShippingType);

        private static ShippingMethod ParseShippingMethod(string? shippingType)
        {
            if (string.IsNullOrWhiteSpace(shippingType))
                return ShippingMethod.Unknown;

            var s = shippingType.Trim().ToLowerInvariant();

            if (s.Contains("standard")) return ShippingMethod.Standard;
            if (s.Contains("express")) return ShippingMethod.Express;

            return Enum.TryParse<ShippingMethod>(shippingType, true, out var parsed)
                ? parsed
                : ShippingMethod.Unknown;
        }
        public override string ToString()
        {
            var dateStr = Date.ToLocalTime().ToString("yyyy-MM-dd");
            var count = Items?.Count ?? 0;
            var label = count == 1 ? "produkt" : "produkter";

            return $"Order {Id} | {dateStr} | {RecipientName} | {count} {label} | Totalt: {Total:0.00} kr";
        }
    }
}
