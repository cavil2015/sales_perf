using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace SalesPerf.Backend.Domain.Entities
{
    public enum SaleStatus
    {
        Paid,
        Cancelled,
        Refunded
    }

    [DebuggerDisplay("Sale(Id={Id:D}, Status={Status}, Date={Date})")]
    public sealed class Sale : IComparable<Sale>, IEquatable<Sale>, ISpanFormattable
    {
        private readonly int _id;
        public int Id
        {
            get => _id;
            init => _id = value >= 0 ? value : throw new ArgumentException("Entity ID cannot be negative.");
        }

        // Same as Product.CategoryId, we MUST encapsulate ManagerId and CustomerId.
        private readonly int _managerId;
        public required int ManagerId
        {
            get => _managerId;
            init => _managerId = value > 0 ? value : throw new ArgumentException("ManagerId must be valid.");
        }

        [JsonIgnore]
        public Manager? Manager { get; private set; }

        private readonly int _customerId;
        public required int CustomerId
        {
            get => _customerId;
            init => _customerId = value > 0 ? value : throw new ArgumentException("CustomerId must be valid.");
        }

        [JsonIgnore]
        public Customer? Customer { get; private set; }

        //  Temporal Boundaries (Analytics Chart DoS)
        // If a developer accidentally inserts `DateTimeOffset.MaxValue`, Postgres saves it fine.
        // But when the Frontend Analytics Dashboard groups sales by Year, it will try to allocate 
        // 8000+ years of empty chart columns, instantly crashing the browser with OutOfMemory!
        // We MUST enforce strict Temporal Boundaries at the domain level.
        private readonly DateTimeOffset _date;
        public required DateTimeOffset Date
        {
            get => _date;
            init
            {
                var utc = value.ToUniversalTime();
                if (utc > DateTimeOffset.UtcNow.AddHours(1))
                    throw new ArgumentException("Temporal Anomaly: Sale cannot be in the future.");
                if (utc.Year < 2000)
                    throw new ArgumentException("Temporal Anomaly: Sale cannot be before company founding.");
                _date = utc;
            }
        }

        public SaleStatus Status { get; private set; } = SaleStatus.Paid;

        public void Cancel()
        {
            if (Status != SaleStatus.Paid) throw new InvalidOperationException("Only Paid sales can be cancelled.");
            Status = SaleStatus.Cancelled;
        }

        public void Refund()
        {
            if (Status != SaleStatus.Paid) throw new InvalidOperationException("Only Paid sales can be refunded.");
            Status = SaleStatus.Refunded;
        }

        [Timestamp]

        public uint Version { get; set; }

        private readonly HashSet<SaleItem> _items = new();
        [JsonIgnore]
        public IReadOnlyCollection<SaleItem> Items => _items;

        //  Aggregate Root Parent Hijacking
        // If a developer accidentally passes a `SaleItem` that belongs to Sale #5 into `sale1.AddItem()`,
        // EF Core will silently mutate `SaleItem.SaleId` to 1 behind the scenes, stealing the item!
        // We MUST enforce Aggregate Root ownership integrity.
        public void AddItem(SaleItem item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            // If the item already belongs to another persistent Sale, reject the hijack attempt.
            // Reflection check: we assume item has a SaleId property.
            // Since SaleItem is not refactored yet, we just check if it's initialized.
            // We will fully protect SaleItem in the next step, but the parent must defend itself!
            if (item.SaleId != 0 && item.SaleId != this.Id && this.Id != 0)
            {
                throw new InvalidOperationException("Parent Hijacking: Item already belongs to another Sale.");
            }

            _items.Add(item);
        }

        public bool Equals(Sale? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Id == 0 || other.Id == 0) return false;
            return Id == other.Id;
        }

        public override bool Equals(object? obj) => Equals(obj as Sale);

        private int? _cachedHashCode;
        public override int GetHashCode()
        {
            if (_cachedHashCode.HasValue) return _cachedHashCode.Value;
            _cachedHashCode = (Id == 0) ? base.GetHashCode() : HashCode.Combine(typeof(Sale), Id);
            return _cachedHashCode.Value;
        }

        public static bool operator ==(Sale? left, Sale? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Sale? left, Sale? right) => !(left == right);

        public override string ToString() => $"Sale(Id={Id}, Status={Status})";

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return destination.TryWrite(provider, $"Sale(Id={Id:D}, Status={Status})", out charsWritten);
        }

        public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

        public int CompareTo(Sale? other)
        {
            if (other is null) return 1;
            return Date.CompareTo(other.Date);
        }
    }
}





