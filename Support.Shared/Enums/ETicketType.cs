using System.ComponentModel;
using System.Reflection;

namespace Support.Shared.Enums
{
    public enum ETicketType
    {
        [Description("Unknown")]
        Unknown,
        [Description("Bug")]
        Bug,
        [Description("Request")]
        Request,
    }

    public static class TicketType
    {
        public static ETicketType FromString(string type)
        {
            switch (type.ToLower().Replace(" ", ""))
            {
                case "bug":
                    return ETicketType.Bug;
                case "request":
                    return ETicketType.Request;
            }
            return ETicketType.Unknown;
        }

        public static string? AsString(ETicketType ticketType)
        {
            Type type = ticketType.GetType();
            string? name = Enum.GetName(type, ticketType);
            if (name != null)
            {
                FieldInfo? field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute? attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return null;
        }

        public static List<string> AsList()
        {
            List<string> list = Enum.GetValues(typeof(ETicketType)).Cast<ETicketType>().Select(x => AsString(x)).ToList();
            list.RemoveAll(x => x == null);
            return list;
        }
    }
}
