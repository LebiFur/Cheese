using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace Cheese
{
    public static class XmlSerializer
    {
        public static void Serialize(object target, string saveTo)
        {
            XmlDocument doc = new();
            
            Write(doc, doc, Memberize(target));

            XmlWriter writer = XmlWriter.Create(saveTo, new()
            {
                Indent = true,
                IndentChars = "\t",
                CloseOutput = true
            });

            doc.Save(writer);
        }

        private static void Write(XmlDocument document, XmlNode parent, XmlMember member)
        {
            string name = member.Name;
            string[]? splits = null;
            if (member.Name.Contains('-'))
            {
                splits = member.Name.Split('-');
                name = splits[0];
            }

            XmlElement elem = document.CreateElement(name);

            if (splits != null) elem.SetAttribute("type", splits[1]);

            if (member.Value != null) elem.InnerText = member.Value;
            else foreach (XmlMember newMember in member.Members.Values) Write(document, elem, newMember);

            parent.AppendChild(elem);
        }

        private static XmlMember Memberize(object target, string? name = null)
        {
            PropertyInfo[] propertyInfos = target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo[] fieldInfos = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            name ??= $"UnnamedNode-{target.GetType().GetName()}";

            XmlMember member = new(name);

            foreach (PropertyInfo property in propertyInfos)
            {
                if (property.GetMethod == null || (!property.IsDefined(typeof(CheeseSerializable), false) && (!property.GetMethod.IsPublic || (property.GetMethod.IsPublic && target.GetType().IsDefined(typeof(CheeseSerializable), false))))) continue;

                Evaluate(property.GetValue(target), property.Name, member);
            }

            foreach (FieldInfo field in fieldInfos)
            {
                if ((!field.IsDefined(typeof(CheeseSerializable), false) && (!field.IsPublic || (field.IsPublic && target.GetType().IsDefined(typeof(CheeseSerializable), false))))) continue;

                Evaluate(field.GetValue(target), field.Name, member);
            }

            return member;
        }

        private static void Evaluate(object value, string name, XmlMember member)
        {
            if (value.GetType().IsPrimitive()) member.Members.Add(name, new($"{name}-{value.GetType().GetName()}", Convert.ToString(value, CultureInfo.InvariantCulture)));
            else
            {
                if (value.GetType().GetInterface("IEnumerable") != null)
                {
                    XmlMember newMember = new($"{name}-{value.GetType().GetName()}");

                    int index = 0;
                    foreach (object item in (IEnumerable)value)
                    {
                        Evaluate(item, $"Element_{index}", newMember);
                        index++;
                    }

                    member.Members.Add(name, newMember);
                }
                else if(value.GetType().BaseType == typeof(Enum)) member.Members.Add(name, new($"{name}-{value.GetType().GetName()}", ((Enum)value).ToString()));
                else member.Members.Add(name, Memberize(value, $"{name}-{value.GetType().GetName()}"));
            }
        }
    }
}
