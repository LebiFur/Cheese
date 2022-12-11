using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace Cheese
{
    public static class XmlDeserializer
    {
        public static T Deserialize<T>(string path)
        {
            XmlDocument doc = new();

            doc.Load(path);

            XmlMember member = Memberize(doc.DocumentElement);

            return (T)Classify(member);
        }
        
        private static object Cast(IList elems, Type type, Type elemType)
        {
            if (type.BaseType == typeof(Array))
            {
                Array arr = Array.CreateInstance(elemType, elems.Count);
                elems.CopyTo(arr, 0);
                return arr;
            }
            else if (type.GetInterface("IList") != null) return elems;
            else if (type.GetInterface("IDictionary`2") != null) return Activator.CreateInstance(type, elems);

            return type.GetMethod("Cast", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, new object[] { elems, type, elemType });
        }
        
        private static object Finish(object obj)
        {
            MethodInfo? method = obj.GetType().GetMethod("Serialized", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null) method.Invoke(obj, null);
            return obj;
        }

        private static object Classify(XmlMember member)
        {
            Type type = Type.GetType(member.Name);

            if (type.IsPrimitive()) return Finish(Convert.ChangeType(member.Value, type, CultureInfo.InvariantCulture));

            if (type.BaseType == typeof(Enum)) return Finish(Enum.Parse(type, member.Value));

            if (type.GetInterface("IEnumerable`1") != null)
            {
                Type elemType = type.GetInterface("IEnumerable`1").GetGenericArguments()[0];

                IList elems = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType));

                foreach (XmlMember newMember in member.Members.Values) elems.Add(Classify(newMember));

                return Finish(Cast(elems, type, elemType));
            }

            if (type.Name == "KeyValuePair`2") return Finish(Activator.CreateInstance(type, Classify(member.Members["Key"]), Classify(member.Members["Value"])));

            object obj = Activator.CreateInstance(type);

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (property.SetMethod == null || (!property.IsDefined(typeof(CheeseSerializable), false) && !property.SetMethod.IsPublic) || !member.Members.ContainsKey(property.Name)) continue;
                property.SetValue(obj, Classify(member.Members[property.Name]));
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if ((!field.IsDefined(typeof(CheeseSerializable), false) && !field.IsPublic) || !member.Members.ContainsKey(field.Name)) continue;
                field.SetValue(obj, Classify(member.Members[field.Name]));
            }

            return Finish(obj);
        }

        private static void Print(XmlMember member, string prefix = "")
        {
            if (member.Value != null) Console.WriteLine($"{prefix}{member.Name}: {member.Value}");
            else foreach (XmlMember newMember in member.Members.Values) Print(newMember, $"{prefix}- ");
        }

        private static XmlMember Memberize(XmlNode node)
        {
            if (node.HasChildNodes && node.FirstChild.Name != "#text")
            {
                XmlMember member = new(node.Attributes["type"].Value);

                foreach (XmlNode child in node.ChildNodes) member.Members.Add(child.Name, Memberize(child));

                return member;
            }

            return new(node.Attributes["type"].Value, node.InnerText);
        }
    }
}
