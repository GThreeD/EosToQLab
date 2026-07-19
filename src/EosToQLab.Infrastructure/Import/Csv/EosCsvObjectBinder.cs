using System.Globalization;
using System.Reflection;
using EosToQLab.Core.Exceptions;

namespace EosToQLab.Infrastructure.Import.Csv;

internal sealed class EosCsvObjectBinder<T> where T : new()
{
    private static readonly List<PropertyBinding> Bindings = CreateBindings();

    public static List<string> FindMissingRequiredColumns(
        Dictionary<string, int> columns)
    {
        return Bindings
            .Where(binding => binding.Attribute.Required
                              && ResolveColumnName(binding.Attribute, columns) is null)
            .Select(binding => binding.Attribute.Name)
            .ToList();
    }

    public static T Bind(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> columns,
        int rowNumber)
    {
        var instance = new T();
        foreach (var binding in Bindings)
        {
            var columnName = ResolveColumnName(binding.Attribute, columns);
            if (columnName is null) continue;

            var index = columns[columnName];
            var rawValue = index < row.Count ? row[index] : string.Empty;
            var value = binding.Attribute.Trim ? rawValue.Trim() : rawValue;

            try
            {
                binding.Property.SetValue(instance, ConvertValue(value, binding.Property.PropertyType));
            }
            catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentException
                                                  or TargetInvocationException)
            {
                throw new CsvValueConversionException(
                    binding.Attribute.Name,
                    rowNumber,
                    value,
                    binding.Property.PropertyType,
                    exception);
            }
        }

        return instance;
    }

    private static string? ResolveColumnName(
        EosCsvColumnAttribute attribute,
        IReadOnlyDictionary<string, int> columns)
    {
        if (columns.ContainsKey(attribute.Name)) return attribute.Name;

        return attribute.Aliases.FirstOrDefault(columns.ContainsKey);
    }

    private static object? ConvertValue(string value, Type propertyType)
    {
        var nullableType = Nullable.GetUnderlyingType(propertyType);
        var targetType = nullableType ?? propertyType;
        if (string.IsNullOrWhiteSpace(value))
        {
            if (targetType == typeof(string)) return null;

            if (nullableType is not null) return null;

            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        if (targetType == typeof(string)) return value;

        if (targetType == typeof(int)) return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

        if (targetType == typeof(decimal))
            return decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

        if (targetType.IsEnum) return Enum.Parse(targetType, value, true);

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static List<PropertyBinding> CreateBindings()
    {
        var bindings = new List<PropertyBinding>();
        foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var attribute = property.GetCustomAttribute<EosCsvColumnAttribute>();
            if (attribute is null) continue;

            if (property.SetMethod is null) throw new CsvColumnBindingException(property.Name);

            bindings.Add(new PropertyBinding(property, attribute));
        }

        return bindings;
    }

    private sealed record PropertyBinding(PropertyInfo Property, EosCsvColumnAttribute Attribute);
}