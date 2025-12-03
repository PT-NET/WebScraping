namespace WebScraping.Domain.ValueObjects
{
    public sealed record EntityName
    {
        public string Value { get; init; }

        private EntityName(string value)
        {
            Value = value;
        }

        public static EntityName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Entity name cannot be empty", nameof(value));

            if (value.Length < 2)
                throw new ArgumentException("Entity name must have at least 2 characters", nameof(value));

            if (value.Length > 200)
                throw new ArgumentException("Entity name cannot exceed 200 characters", nameof(value));

            return new EntityName(value.Trim());
        }

        public override string ToString() => Value;
    }
}
