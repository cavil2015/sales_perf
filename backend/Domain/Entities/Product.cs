using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace SalesPerf.Backend.Domain.Entities
{
    [DebuggerDisplay("Product(Id={Id:D}, Name={Name}, CategoryId={CategoryId:D})")]
    public sealed class Product : IComparable<Product>, IEquatable<Product>, ISpanFormattable
    {
        private readonly int _id;
        public int Id
        {
            get => _id;
            init => _id = value >= 0 ? value : throw new ArgumentException("Entity ID cannot be negative.");
        }

        private static string SanitizeString(string input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            if (input.Length > maxLength * 2)
                throw new ArgumentException($"Payload DoS Protection: Input exceeds max bounds.");

            Span<char> buffer = stackalloc char[input.Length];
            int pos = 0;
            foreach (char c in input)
            {
                if (c is not ('\r' or '\n' or '\u202E' or '\u202D' or '\u202C' or '\0'))
                    buffer[pos++] = c;
            }

            string normalized = new string(buffer[..pos]).Trim().Normalize(System.Text.NormalizationForm.FormC);
            if (normalized.Length > maxLength)
                throw new ArgumentException($"Payload DoS Protection: Normalized input exceeds {maxLength} chars.");

            return normalized;
        }

        private readonly string _name = null!;
        [MaxLength(100)]
        public required string Name
        {
            get => _name;
            init
            {
                string sanitized = SanitizeString(value, 100);
                if (sanitized.Length == 0) throw new ArgumentException("Name cannot be empty.");
                _name = sanitized;
            }
        }

        // If a developer sets `product.CategoryId = 5`, but `product.Category` still points to 
        // the Category object with `Id = 1`, EF Core's ChangeTracker suffers a Split-Brain paradox.
        // During `SaveChanges()`, EF Core will overwrite one with the other based on random tracker state!
        // We MUST encapsulate the relationship so you can only set the ID (scalar tracking), 
        // and let EF Core exclusively manage the navigation object.
        private readonly int _categoryId;
        public required int CategoryId
        {
            get => _categoryId;
            init => _categoryId = value > 0 ? value : throw new ArgumentException("CategoryId must be valid.");
        }

        [JsonIgnore]
        public Category? Category { get; private set; } // Read-only for developers. EF Core can map to it natively.

        [Timestamp]

        public uint Version { get; set; }

        private readonly HashSet<SaleItem> _saleItems = new();
        [JsonIgnore]
        public IReadOnlyCollection<SaleItem> SaleItems => _saleItems;

        public bool Equals(Product? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Id == 0 || other.Id == 0) return false;
            return Id == other.Id;
        }

        public override bool Equals(object? obj) => Equals(obj as Product);

        private int? _cachedHashCode;
        public override int GetHashCode()
        {
            if (_cachedHashCode.HasValue) return _cachedHashCode.Value;
            _cachedHashCode = (Id == 0) ? base.GetHashCode() : HashCode.Combine(typeof(Product), Id);
            return _cachedHashCode.Value;
        }

        public static bool operator ==(Product? left, Product? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Product? left, Product? right) => !(left == right);

        public override string ToString() => $"Product(Id={Id}, Name='{Name}', CategoryId={CategoryId})";

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return destination.TryWrite(provider, $"Product(Id={Id:D}, Name='{Name}', CategoryId={CategoryId:D})", out charsWritten);
        }

        public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

        public int CompareTo(Product? other)
        {
            if (other is null) return 1;
            int cmp = CategoryId.CompareTo(other.CategoryId);
            if (cmp == 0) return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            return cmp;
        }
    }
}





