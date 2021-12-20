using System;

namespace PlayerTags.Data
{
    // FirstName LastName
    // FirstName LastName:Id
    public struct Identity : IComparable<Identity>, IEquatable<Identity>
    {
        public string Name;
        public string? Id;

        public Identity(string name)
        {
            Name = name;
            Id = null;
        }

        public Identity(string name, string id)
        {
            Name = name;
            Id = id;
        }

        public override string ToString()
        {
            string str = Name;

            if (Id != null)
            {
                Name += $":{Id}";
            }

            return str;

        }

        public override bool Equals(object? obj)
        {
            return obj is Identity identity && Equals(identity);
        }

        public bool Equals(Identity obj)
        {
            return this == obj;
        }

        public static bool operator ==(Identity first, Identity second)
        {
            if (first.Id != null || second.Id != null)
            {
                return first.Id == second.Id;
            }

            return first.Name.ToLower().Trim() == second.Name.ToLower().Trim();
        }

        public static bool operator !=(Identity first, Identity second)
        {
            return !(first == second);
        }

        public override int GetHashCode()
        {
            var hashCode = Name.GetHashCode();

            if (Id != null)
            {
                hashCode *= 17 ^ Id.GetHashCode();
            }

            return hashCode;
        }

        public int CompareTo(Identity other)
        {
            return Name.CompareTo(other.Name);
        }
    }
}
