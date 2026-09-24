using System.ComponentModel.DataAnnotations;

using System.Diagnostics;

namespace SalesPerf.Backend.Domain.Entities
{
    // When a developer hovers over a Category in Visual Studio/Rider, the IDE implicitly invokes `ToString()`.
    // This cross-thread evaluation pauses the debugger, can trigger side-effects, and slows down stepping.
    // Microsoft's official best practice is to use `[DebuggerDisplay]` to let the IDE read raw memory instantly.
    [DebuggerDisplay("Category(Id={Id}, Name={Name})")]
    // We overrode `Equals(object)`, which requires a runtime type-check (`obj is Category`) on EVERY comparison.
    // When searching a HashSet of 10,000 categories, this type-casting overhead causes CPU bottlenecks.
    // We MUST implement `IEquatable<Category>` to provide a strongly-typed `Equals` that skips type-checking.
    public sealed class Category : IComparable<Category>, IEquatable<Category>
    {
        // An inexperienced developer might do `new Category { Id = -99 }` for a mock test.
        // In EF Core, NEGATIVE IDs are strictly reserved for internal "Temporary Keys" during disconnected graph attachments.
        // Assigning a negative ID manually causes EF Core to silently merge your entity with its own internal tracker,
        // causing catastrophic cross-wiring of Foreign Keys upon SaveChanges.
        // We MUST block negative IDs at the Domain boundary.
        private readonly int _id;

        public int Id
        {
            get => _id;
            init => _id = value >= 0 ? value : throw new ArgumentException("Entity ID cannot be negative.");
        }

        // Even with the `required` modifier, a developer can instantiate `new Category { Name = "   " }`.
        // Blank strings will bypass EF Core, polluting the database and breaking UI dropdowns with invisible elements.
        // We MUST encapsulate the `init` accessor to strictly enforce Domain rules (fail-fast validation).
        private readonly string _name = null!;

        [MaxLength(100)]
        public required string Name
        {
            get => _name;
            init
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Category Name cannot be empty or whitespace.");
                _name = value.Trim();
            }
        }

        private readonly HashSet<Product> _products = new();
        public IReadOnlyCollection<Product> Products => _products;

        public bool Equals(Category? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Id == 0 || other.Id == 0) return false; // Transient entities are only equal by reference
            return Id == other.Id;
        }

        // Delegate the untyped Equals to the strongly-typed IEquatable implementation
        public override bool Equals(object? obj) => Equals(obj as Category);

        public override int GetHashCode()
        {
            return (Id == 0) ? base.GetHashCode() : HashCode.Combine(typeof(Category), Id);
        }

        // The previous `==` operator checked `if (left is null)` and then invoked `.Equals()`.
        // The `ReferenceEquals` check was buried inside the `.Equals()` method body.
        // By moving `ReferenceEquals(left, right)` to the very first line of the `==` operator, 
        // we completely bypass the method invocation stack overhead when comparing identical references.
        public static bool operator ==(Category? left, Category? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Category? left, Category? right) => !(left == right);

        public override string ToString() => $"Category(Id={Id}, Name='{Name}')";

        public int CompareTo(Category? other)
        {
            if (other is null) return 1;
            return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}





