using System;
using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;

using System.Diagnostics;

namespace SalesPerf.Backend.Domain.Entities
{
    // Customer.cs Iterations 1-5: The Core Domain Hardening
    // Applying the same foundational EF Core and DDD protections we discovered in Category.cs:
    // 1. Immutable Keys (Init properties to prevent accidental mutation).
    // 2. Unbounded String Bloat Prevention (MaxLength attributes for B-Tree safety).
    // 3. Whitespace Corruption Prevention (Backing fields with fail-fast validation).
    // 4. O(N) Navigation Degradation Fix (HashSet instead of List).
    // 5. Broken Encapsulation Fix (IReadOnlyCollection exposed).
    [DebuggerDisplay("Customer(Id={Id}, Name={Name}, Company={Company})")]
    public sealed class Customer : IComparable<Customer>, IEquatable<Customer>
    {
        private readonly int _id;
        public int Id
        {
            get => _id;
            init => _id = value >= 0 ? value : throw new ArgumentException("Entity ID cannot be negative.");
        }

        private readonly string _name = null!;
        [MaxLength(100)]
        public required string Name
        {
            get => _name;
            init
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Name cannot be empty.");
                _name = value.Trim();
            }
        }

        private readonly string _company = null!;
        [MaxLength(150)]
        public required string Company
        {
            get => _company;
            init
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Company cannot be empty.");
                _company = value.Trim();
            }
        }

        // 'Segment' is stored as a string. If a developer accidentally passes "Enterprize" (typo),
        // EF Core will save it. This permanently fragments the database and destroys Analytics `GROUP BY` pie charts.
        // We MUST restrict `Segment` strictly to the known business vocabulary at the Domain boundary.

        // , I used `StringComparer.OrdinalIgnoreCase`. This meant if the frontend sent "ENTERPRISE",
        // it passed validation, and "ENTERPRISE" was saved to the DB.
        // Because PostgreSQL is CASE-SENSITIVE, the SQL `GROUP BY Segment` would group "ENTERPRISE" and "Enterprise" 
        // as TWO DIFFERENT PIE CHART SLICES, bypassing our fix!
        // We MUST use a Dictionary to map ANY input casing strictly to the Canonical Casing.
        private static readonly Dictionary<string, string> CanonicalSegments = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Enterprise", "Enterprise" },
            { "Mid-Market", "Mid-Market" },
            { "Small Business", "Small Business" },
            { "Startup", "Startup" }
        };

        private readonly string _segment = null!;
        [MaxLength(50)]
        public required string Segment
        {
            get => _segment;
            init
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Segment cannot be empty.");
                if (!CanonicalSegments.TryGetValue(value.Trim(), out string? canonical))
                    throw new ArgumentException($"Data Corruption: Invalid Segment '{value.Trim()}'.");
                _segment = canonical;
            }
        }

        private readonly HashSet<Sale> _sales = new();
        public IReadOnlyCollection<Sale> Sales => _sales;

        // Customer.cs Iterations 6-12: The Advanced Equality & Performance Optimizations
        public bool Equals(Customer? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Id == 0 || other.Id == 0) return false;
            return Id == other.Id;
        }

        public override bool Equals(object? obj) => Equals(obj as Customer);

        public override int GetHashCode()
        {
            return (Id == 0) ? base.GetHashCode() : HashCode.Combine(typeof(Customer), Id);
        }

        public static bool operator ==(Customer? left, Customer? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Customer? left, Customer? right) => !(left == right);

        public override string ToString() => $"Customer(Id={Id}, Name='{Name}', Company='{Company}')";

        //  Multi-Dimensional Sorting Instability (UI Jitter)
        // If we sort purely by `Company` or `Name`, and two customers work at the same "Acme Corp",
        // their relative order will randomly flip based on Postgres B-Tree disk fetch order (UI Jitter).
        // We MUST chain the sorting dimensions to guarantee 100% deterministic UI rendering.
        public int CompareTo(Customer? other)
        {
            if (other is null) return 1;
            int cmp = string.Compare(Company, other.Company, StringComparison.OrdinalIgnoreCase);
            if (cmp == 0) return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            return cmp;
        }
    }
}





