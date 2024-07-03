using System.Collections;

namespace KafkaFlow.CryptoShredding.Avro;

internal static class ReflectionUtils
{
    public static object MapFunctionToList(object list, Func<object, object?> func)
    {
        var listType = list.GetType();
        if (!listType.IsGenericType || listType.GetGenericTypeDefinition() != typeof(List<>))
            return list;

        var elementType = listType.GetGenericArguments()[0];

        var resultListType = typeof(List<>).MakeGenericType(elementType);
        if (Activator.CreateInstance(resultListType) is not IList resultList) return list;

        foreach (var item in (IEnumerable)list) resultList.Add(func(item));

        return resultList;
    }

    public static object MapFunctionToDictionary(object dictionary, Func<object?, object?> func)
    {
        var dictionaryType = dictionary.GetType();
        if (!dictionaryType.IsGenericType || dictionaryType.GetGenericTypeDefinition() != typeof(Dictionary<,>))
            return dictionary;

        var keyType = dictionaryType.GetGenericArguments()[0];
        var valueType = dictionaryType.GetGenericArguments()[1];

        var resultDictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        var kvType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);

        var keyProperty = kvType.GetProperty("Key")!;
        var valueProperty = kvType.GetProperty("Value")!;

        if (Activator.CreateInstance(resultDictionaryType) is not IDictionary resultDictionary) return dictionary;

        foreach (var entry in (IEnumerable)dictionary)
        {
            var key = keyProperty.GetValue(entry);
            var value = valueProperty.GetValue(entry);
            resultDictionary.Add(key!, func(value));
        }

        return resultDictionary;
    }
}
