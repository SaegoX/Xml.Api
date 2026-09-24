namespace Xml.Api.Features.ScheduleUpload.Options
{
    /// <summary>
    /// Кастомный метод расширения, который либо возвращает существующее значение, либо если его нет создаёт.(аналог ConcurrentDictionary.GetOrAdd, но для словаря)
    /// Убирает дубликаты сущностей(позоляет не создавать один и тот же объект много раз)
    /// Подходит только для однопоточных сценариев
    /// </summary>
    public static class GetOrAddExtension
    {
        /// <summary>
        /// Реализуется паттерн Get or create
        /// Типичное применение: дедупликация справочников при обработке плоских списков (DTO, XML, CSV).
        /// Производительность: O(1) в среднем за счёт хеш‑таблицы.
        /// </summary>
        /// <typeparam name="TKey">Тип ключа словаря</typeparam>
        /// <typeparam name="TValue">Тип значения словаря</typeparam>
        /// <param name="dictionary">Словарь, для которого реализуется метод</param>
        /// <param name="key">Ключ, по которому производится поиск или добавление</param>
        /// <param name="valueFactory">Делегат, который создаёт новое значение, если ключа в словаре нет</param>
        /// <returns>Значение, связанное с ключом. Если ключ отсутствовал — созданное через фабрику значение</returns>
        /// <remarks>
        /// Метод **не является потокобезопасным**. Нужно использовать только в однопоточных сценариях. 
        /// При вызове из нескольких потоков одновременно возможны исключения при добавлении ключа или некорректные результаты. 
        /// Для многопоточных сценариев рекомендуется использовать `ConcurrentDictionary<TKey, TValue>.GetOrAdd`.
        /// </remarks>
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

