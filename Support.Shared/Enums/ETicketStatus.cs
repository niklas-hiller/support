using System.ComponentModel;
using System.Reflection;

namespace Support.Shared.Enums
{
    public enum ETicketStatus
    {
        Unknown,
        [Description("Open")]
        Open,
        [Description("In Progress")]
        In_Progress,
        [Description("Done")]
        Done,
        [Description("Declined")]
        Declined,
    }

    public static class TicketStatus
    {
        public static ETicketStatus FromString(string status)
        {
            switch (status.ToLower())
            {
                case "open":
                    return ETicketStatus.Open;
                case "in_progress":
                    return ETicketStatus.In_Progress;
                case "done":
                    return ETicketStatus.Done;
                case "declined":
                    return ETicketStatus.Declined;
            }
            return ETicketStatus.Unknown;
        }

        public static string? AsString(ETicketStatus ticketStatus)
        {
            Type type = ticketStatus.GetType();
            string? name = Enum.GetName(type, ticketStatus);
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
            List<string> list = Enum.GetValues(typeof(ETicketStatus)).Cast<ETicketStatus>().Select(x => AsString(x)).ToList();
            list.RemoveAll(x => x == null);
            return list;
        }
    }
}