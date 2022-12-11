namespace Cheese
{
    internal struct XmlMember
    {
        internal string Name { get; }
        internal string? Value { get; }
        internal Dictionary<string, XmlMember> Members { get; } = new();

        internal XmlMember(string name, string? value = null)
        {
            Name = name;
            Value = value;
        }
    }
}
