namespace Xml.Api.Services.Modules
{
    public static class ExistSubjectExtension
    {
        public static TValue GetOrAdd<TKey, TValue>(
          this Dictionary<TKey, TValue> dictionary,
          TKey key,
          Func<TValue> valueFactory)
          where TKey : notnull
        {
            if (dictionary.TryGetValue(key, out var value))
                return value;

            value = valueFactory();
            dictionary.Add(key, value);
            return value;
        }
    }
}

