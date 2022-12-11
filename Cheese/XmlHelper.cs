namespace Cheese
{
    internal static class XmlHelper
    {
        internal static bool IsPrimitive(this Type type) => type.IsPrimitive || type == typeof(string);

        private static string CheckName(string name)
        {
            int start = name.IndexOf(", Version");
            int end = -1;
            for (int i = start; i < name.Length; i++)
            {
                if (name[i] == ']')
                {
                    end = i;
                    break;
                }
            }

            if (end == -1) return name[..start];

            return CheckName(name.Remove(start, end - start));
        }

        internal static string GetName(this Type type) => CheckName(type.AssemblyQualifiedName);
    }
}
