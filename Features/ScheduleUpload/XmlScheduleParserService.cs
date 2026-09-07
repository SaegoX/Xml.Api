using Xml.Api.Data;

namespace Xml.Api.Features.ScheduleUpload
{
    /// <summary>
    /// Фасад(Оркестратор) процесса импорта расписания
    /// Внедряет и координирует работу всех наших модулей
    /// </summary>
    public class XmlScheduleParserService
    {
        // Внедрение через конструктор
        private readonly ScheduleReader _reader;
        private readonly ScheduleMapper _mapper;
        private readonly ScheduleSaver _saver;

        // Регистрация модулей
        public XmlScheduleParserService(
            ScheduleReader reader,
            ScheduleMapper mapper,
            ScheduleSaver saver)
        {
            _reader = reader;
            _mapper = mapper;
            _saver = saver;
        }
        
        /// <summary>
        /// Запускает полный цикл импорта XML файла в базу данных.
        /// </summary>
        /// <param name="filePath">Путь к сохранённому XML файлу</param>
        /// <returns></returns>
        public async Task ParseAndSaveAsync(string filePath)
        {
            // 1. Читаем файл в плоские структуры
            var dtos = await _reader.ReadAsync(filePath);
            // 2. Выделяем уникальные справочники
            var (chairs, discs, groups, preps, buildings, rooms, rawDtos) = _mapper.Map(dtos);
            // 3. Сохраняем реляционную структуру в базу данных
            await _saver.SaveAsync(chairs, discs, groups, preps, buildings, rooms, rawDtos);
        }
    }

}