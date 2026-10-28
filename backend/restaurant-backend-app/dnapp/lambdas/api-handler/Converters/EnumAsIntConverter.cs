using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using System;

namespace APIHandler.Converters;

public class EnumAsIntConverter<T> : IPropertyConverter where T : Enum
{
    public object FromEntry(DynamoDBEntry entry)
    {
        int value = entry.AsInt();
        if (!Enum.IsDefined(typeof(T), value))
        {
            throw new ArgumentOutOfRangeException(nameof(entry), $"Value {value} is not defined in enum {typeof(T).Name}");
        }

        return (T)Enum.ToObject(typeof(T), value);
    }

    public DynamoDBEntry ToEntry(object value)
    {
        if (value is not T enumValue)
        {
            throw new ArgumentException($"Value must be of type {typeof(T).Name}", nameof(value));
        }

        return Convert.ToInt32(enumValue);
    }
}