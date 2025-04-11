using System.ComponentModel;

namespace RotationSolver.Data
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            FieldInfo? field = value.GetType().GetField(value.ToString());
            if (field == null)
            {
                return value.ToString();
            }

            DescriptionAttribute? attribute = field.GetCustomAttribute<DescriptionAttribute>();
            if (attribute == null)
            {
                return value.ToString();
            }

            return attribute.Description;
        }
    }
}