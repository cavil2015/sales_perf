using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace SalesPerf.Backend.Domain.Entities
{
    // Manager.cs Iterations 1-18: Core DDD & EF Core Protections
    // Applied Immutable Keys, HashSet O(1) relationships, IReadOnlyCollection encapsulation,
    // [MaxLength] indexing safety, Transient Equality fixes, Operator overloads, DebuggerDisplay,
    // and PostgreSQL Case-Sensitivity Leak fixes for `TeamOrRole` via Canonical Dictionary.
    [DebuggerDisplay("Manager(Id={Id}, Name={Name}, Team={TeamOrRole}, Active={IsActive})")]
    public sealed class Manager : IComparable<Manager>, IEquatable<Manager>
    {
        private readonly int _id;
        public int Id
        {
            get => _id;
            init => _id = value >= 0 ? value : throw new ArgumentException("Entity ID cannot be negative.");
        }

        //  Two-Phase Bounded Normalization (Unicode Expansion DoS)
        // , we normalized to Form C at the end. But Form D (decomposed) strings 
        // take up more `char` space than their Form C equivalents! If we checked `input.Length > maxLength` 
        // first, a perfectly valid Form D string might be falsely rejected (False-Positive DoS).
        // If we normalize first, we risk a Memory DoS. We MUST use Two-Phase bounds checking.
        private static string SanitizeString(string input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            // Phase 1: Generous bounds check to allow Form D expansion (x2 size)
            if (input.Length > maxLength * 2)
                throw new ArgumentException($"Payload DoS Protection: Input exceeds max generous bounds.");

            Span<char> buffer = stackalloc char[input.Length];
            int pos = 0;

            foreach (char c in input)
            {
                if (c is not ('\r' or '\n' or '\u202E' or '\u202D' or '\u202C' or '\0'))
                {
                    buffer[pos++] = c;
                }
            }

            string normalized = new string(buffer[..pos]).Trim().Normalize(System.Text.NormalizationForm.FormC);

            // Phase 2: Strict bounds check after Canonical Normalization
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

        private static readonly Dictionary<string, string> CanonicalTeams = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Alpha", "Alpha" },
            { "Beta", "Beta" },
            { "Gamma", "Gamma" },
            { "Enterprise", "Enterprise" },
            { "SMB", "SMB" }
        };

        private readonly string _teamOrRole = null!;
        [MaxLength(50)]
        public required string TeamOrRole
        {
            get => _teamOrRole;
            init
            {
                string sanitized = SanitizeString(value, 50);
                if (sanitized.Length == 0) throw new ArgumentException("TeamOrRole cannot be empty.");
                if (!CanonicalTeams.TryGetValue(sanitized, out string? canonical))
                    throw new ArgumentException($"Data Corruption: Invalid Team '{sanitized}'.");
                _teamOrRole = canonical;
            }
        }

        public bool IsActive { get; private set; } = true;

        public void Deactivate()
        {
            if (!IsActive) throw new InvalidOperationException("Manager is already inactive.");
            IsActive = false;
        }

        public void Activate()
        {
            if (IsActive) throw new InvalidOperationException("Manager is already active.");
            IsActive = true;
        }

        private readonly string? _avatarUrl;
        [MaxLength(2048)]
        public string? AvatarUrl
        {
            get => _avatarUrl;
            init
            {
                string sanitized = SanitizeString(value, 2048);
                if (sanitized.Length == 0)
                {
                    _avatarUrl = null;
                    return;
                }

                if (!Uri.TryCreate(sanitized, UriKind.Absolute, out Uri? uriResult)
                    || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                {
                    throw new ArgumentException("AvatarUrl MUST be a valid HTTP/HTTPS absolute URL.");
                }

                if (uriResult.IsLoopback || uriResult.Host == "169.254.169.254")
                {
                    throw new ArgumentException("SSRF Protection: Loopback and Cloud Metadata URIs are strictly forbidden.");
                }

                _avatarUrl = sanitized;
            }
        }

        // If Admin A and Admin B both edit this Manager simultaneously, the last one to click "Save" 
        // will silently overwrite the other's changes without warning (Lost Update Anomaly).
        // By adding `[Timestamp]`, EF Core will inject a concurrency check into the `UPDATE` SQL statement 
        // (`WHERE Id = @Id AND RowVersion = @OldVersion`). If it fails, it throws a DbUpdateConcurrencyException.
        [Timestamp]

        public uint Version { get; set; }

        // If an API controller ever returns this Entity directly to the frontend, the JSON serializer 
        // will traverse `Manager -> Sales -> Sale -> Manager -> ...` causing an infinite loop
        // and crashing the server with a `JsonException: A possible object cycle was detected`.
        // We MUST strictly sever the JSON traversal graph at the navigation property.
        private readonly HashSet<Sale> _sales = new();
        [System.Text.Json.Serialization.JsonIgnore]
        public IReadOnlyCollection<Sale> Sales => _sales;

        public bool Equals(Manager? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Id == 0 || other.Id == 0) return false;
            return Id == other.Id;
        }

        public override bool Equals(object? obj) => Equals(obj as Manager);

        // If a transient Manager (`Id == 0`) is placed into a `HashSet`, its hash is based on `base.GetHashCode()`.
        // When `SaveChanges()` runs, EF Core assigns a real `Id` (e.g. 42). The hash code MAGICALLY CHANGES!
        // The `HashSet` will NEVER find this object again because it is looking in the wrong hash bucket!
        // We MUST cache the HashCode the very first time it is requested, freezing it for the lifetime of the process.
        private int? _cachedHashCode;
        public override int GetHashCode()
        {
            if (_cachedHashCode.HasValue) return _cachedHashCode.Value;

            _cachedHashCode = (Id == 0) ? base.GetHashCode() : HashCode.Combine(typeof(Manager), Id);
            return _cachedHashCode.Value;
        }

        public static bool operator ==(Manager? left, Manager? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Manager? left, Manager? right) => !(left == right);

        public override string ToString() => $"Manager(Id={Id}, Name='{Name}', Team='{TeamOrRole}')";

        //  Zero-Allocation Logging (ISpanFormattable)
        //  Culture-Sensitive Number Fragmentation (The "D" Specifier)
        // If a server is running in a French or Russian locale, the `Id` (e.g. 1234) would be formatted 
        // with a thousands separator: "1 234" or "1.234". If a developer searches the logs for "Id=1234", 
        // they will NEVER find it. Database IDs must NEVER be culture-sensitive!
        // We MUST append the `:D` (Decimal) format specifier to force invariant rendering without allocations.
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return destination.TryWrite(provider, $"Manager(Id={Id:D}, Name='{Name}', Team='{TeamOrRole}')", out charsWritten);
        }

        public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

        public int CompareTo(Manager? other)
        {
            if (other is null) return 1;
            int cmp = string.Compare(TeamOrRole, other.TeamOrRole, StringComparison.OrdinalIgnoreCase);
            if (cmp == 0) return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            return cmp;
        }
    }
}





