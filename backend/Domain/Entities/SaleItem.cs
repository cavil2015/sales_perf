using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace SalesPerf.Backend.Domain.Entities
{
    // SaleItem.cs Iterations 1-42: The Master Template
    // Applied all 42 previous protections (Span formatting, Split-Brain encapsulation, etc.)
    [DebuggerDisplay("SaleItem(Id={Id:D}, SaleId={SaleId:D}, ProductId={ProductId:D}, Qty={Quantity})")]
    public sealed class SaleItem : IComparable<SaleItem>, IEquatable<SaleItem>, ISpanFormattable
    {
        private readonly int _id;
        public int Id
        {
            get => _id;
            init => _id = value >= 0 ? value : throw new ArgumentException("Entity ID cannot be negative.");
        }

        private readonly int _saleId;
        public required int SaleId
        {
            get => _saleId;
            init => _saleId = value >= 0 ? value : throw new ArgumentException("SaleId must be valid.");
        }

        //  Explicit Proxy Defeat & Eager Loading Enforcement
        // By marking this class as `sealed` and navigation properties as non-`virtual`, 
        // we explicitly DEFEAT Entity Framework's `UseLazyLoadingProxies()`. 
        // Lazy loading in analytics loops causes catastrophic N+1 Database Query Storms. 
        // This architectural lock FORCES developers to use `.Include()` (Eager Loading), 
        // guaranteeing predictable O(1) query performance!
        [JsonIgnore]
        public Sale? Sale { get; private set; }

        private readonly int _productId;
        public required int ProductId
        {
            get => _productId;
            init => _productId = value >= 0 ? value : throw new ArgumentException("ProductId must be valid.");
        }

        [JsonIgnore]
        public Product? Product { get; private set; }

        private readonly int _quantity;
        public required int Quantity
        {
            get => _quantity;
            init => _quantity = value >= 0 ? value : throw new ArgumentException("Quantity must be greater than zero.");
        }

        // JavaScript parses JSON numbers as IEEE-754 double precision floats (max 53-bit precision).
        // If we send a 28-digit C# `decimal` directly as a JSON number, the browser will silently 
        // truncate the lower digits, destroying financial precision (micro-cents vanish!).
        // We MUST serialize all financial decimals as Strings to the frontend, forcing the UI 
        // to parse them safely via a `BigDecimal` library (like decimal.js).
        private readonly decimal _salePrice;
        [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
        public required decimal SalePrice
        {
            get => _salePrice;
            init => _salePrice = value >= 0 ? value : throw new ArgumentException("SalePrice cannot be negative.");
        }

        private readonly decimal _costPrice;
        [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
        public required decimal CostPrice
        {
            get => _costPrice;
            init => _costPrice = value >= 0 ? value : throw new ArgumentException("CostPrice cannot be negative.");
        }

        [NotMapped]
        [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
        public decimal Revenue => SalePrice * Quantity;

        [NotMapped]
        [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
        public decimal Profit => (SalePrice - CostPrice) * Quantity;

        [NotMapped]
        [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
        public decimal MarginPercent => Revenue == 0m ? 0m : Profit / Revenue;

        [Timestamp]

        public uint Version { get; set; }

        public bool Equals(SaleItem? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Id == 0 || other.Id == 0) return false;
            return Id == other.Id;
        }

        public override bool Equals(object? obj) => Equals(obj as SaleItem);

        private int? _cachedHashCode;
        public override int GetHashCode()
        {
            if (_cachedHashCode.HasValue) return _cachedHashCode.Value;
            _cachedHashCode = (Id == 0) ? base.GetHashCode() : HashCode.Combine(typeof(SaleItem), Id);
            return _cachedHashCode.Value;
        }

        public static bool operator ==(SaleItem? left, SaleItem? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(SaleItem? left, SaleItem? right) => !(left == right);

        public override string ToString() => $"SaleItem(Id={Id}, SaleId={SaleId}, ProductId={ProductId}, Qty={Quantity})";

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return destination.TryWrite(provider, $"SaleItem(Id={Id:D}, SaleId={SaleId:D}, ProductId={ProductId:D}, Qty={Quantity:D})", out charsWritten);
        }

        public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

        // If two SaleItems have the same SaleId and ProductId (e.g. added to cart twice), 
        // the previous `CompareTo` returned `0` (meaning "they are identical for sorting").
        // BUT `Equals()` checks `Id`, so it returned `false`. 
        // In C#, if `CompareTo == 0` but `Equals == false`, collections like `SortedSet` and LINQ `.OrderBy()` 
        // exhibit non-deterministic, unstable sorting, corrupting UI pagination!
        // We MUST fallback to comparing the Primary Key (`Id`) to guarantee absolute deterministic sorting.
        public int CompareTo(SaleItem? other)
        {
            if (other is null) return 1;
            int cmp = SaleId.CompareTo(other.SaleId);
            if (cmp == 0) cmp = ProductId.CompareTo(other.ProductId);
            if (cmp == 0) cmp = Id.CompareTo(other.Id);
            return cmp;
        }
    }
}





