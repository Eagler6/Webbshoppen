using Microsoft.EntityFrameworkCore;
using System;

namespace WindowDemo
{
    public enum OrderLineType
    {
        Product = 0,
        Shipping = 1,
        Discount = 2
    }

    public class OrderLine
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public OrderLineType LineType { get; set; } = OrderLineType.Product;
        public decimal LineTotal => UnitPrice * Quantity;
        public OrderLine() { }
        public OrderLine(int productId, string productName, decimal unitPrice, int quantity)
        {
            ProductId = productId;
            ProductName = productName ?? string.Empty;
            UnitPrice = unitPrice;
            Quantity = quantity;
        }
        public OrderLine(int orderId, int productId, string productName, decimal unitPrice, int quantity, OrderLineType lineType = OrderLineType.Product)
            : this(productId, productName, unitPrice, quantity)
        {
            OrderId = orderId;
            LineType = lineType;
        }

    }

}
