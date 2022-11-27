using System.ComponentModel;
using System.Reflection;

namespace Support.Shared.Enums
{
    public enum ETicketPriority
    {
        Unknown,
        [Description("Trivial")]
        Trivial,
        [Description("Minor")]
        Minor,
        [Description("Lowest")]
        Lowest,
        [Description("Low")]
        Low,
        [Description("Medium")]
        Medium,
        [Description("High")]
        High,
        [Description("Highest")]
        Highest,
        [Description("Major")]
        Major,
        [Description("Critical")]
        Critical,
        [Description("Blocker")]
        Blocker
    }

    public static class TicketPriority
    {
        public static ETicketPriority FromString(string priority)
        {
            switch (priority.ToLower().Replace(" ", ""))
            {
                case "trivial":
                    return ETicketPriority.Trivial;
                case "minor":
                    return ETicketPriority.Minor;
                case "lowest":
                    return ETicketPriority.Lowest;
                case "low":
                    return ETicketPriority.Low;
                case "medium":
                    return ETicketPriority.Medium;
                case "high":
                    return ETicketPriority.High;
                case "highest":
                    return ETicketPriority.Highest;
                case "major":
                    return ETicketPriority.Major;
                case "critical":
                    return ETicketPriority.Critical;
                case "blocker":
                    return ETicketPriority.Blocker;
            }
            return ETicketPriority.Unknown;
        }

        public static string? AsString(ETicketPriority ticketPriority)
        {
            Type type = ticketPriority.GetType();
            string? name = Enum.GetName(type, ticketPriority);
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
            List<string> list = Enum.GetValues(typeof(ETicketPriority)).Cast<ETicketPriority>().Select(x => AsString(x)).ToList();
            list.RemoveAll(x => x == null);
            return list;
        }
    }
}
