using Microsoft.EntityFrameworkCore;
using Xml.Api.Data;
using Xml.Api.Models;

namespace Xml.Api.Features.ScheduleUpload
{
    /// <summary>
    /// Модуль сохранения данных в СУБД, через транзакцию
    /// Отвечает за полную очистку старого расписания и атомарную запись новых сущностей
    /// </summary>
    public class ScheduleSaver(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        /// <summary>
        /// Интервал процентов для модуля сохранения
        /// </summary>
        private const int StartSavePercent = 81;
        private const int MaxSavePercent = 99;

        /// <summary>
        /// Очищает старые таблицы и сохраняет новые данные в рамках единой транзакции
        /// </summary>
        public async Task SaveAsync(
            List<Chair> chairs,
            List<Disc> discs,
            List<Group> groups,
            List<Prep> preps,
            List<Building> buildings,
            List<BuildingRoom> rooms,
            List<SubjectDto> rawDtos,
            Func<int, string, Task> onProgress,
            CancellationToken cancellationToken)
        {
            // Открываем транзакцию. Если хоть один шаг упадет — база вернется в исходное состояние
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await onProgress.Invoke(StartSavePercent, $"Очистка старых данных в PostgreSQL... ({StartSavePercent}%)");

                // Каскадно очищаем все таблицы. Последовательность не важна, всё удаляется каскадно(связно). TRUNCATE только очищает(не удаляет) таблицы.
                await _context.Database.ExecuteSqlRawAsync(
                    "TRUNCATE TABLE \"EventToGroupRefs\", \"EventToPrepRefs\", \"EventToBuildingRoomRefs\", \"Events\", \"BuildingRooms\", \"Buildings\", \"Preps\", \"Groups\", \"Discs\", \"Chairs\" RESTART IDENTITY CASCADE;", cancellationToken);


                // Заливаем первичные справочники
                await _context.Buildings.AddRangeAsync(buildings, cancellationToken);
                await _context.Chairs.AddRangeAsync(chairs, cancellationToken);
                await _context.Discs.AddRangeAsync(discs, cancellationToken);
                await _context.Groups.AddRangeAsync(groups, cancellationToken);
                await _context.Preps.AddRangeAsync(preps, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken); // Фиксируем, чтобы получить сгенерированные БД int ID

                // Заливаем комнаты (они связываются по ссылкам на объекты зданий из памяти)
                await _context.BuildingRooms.AddRangeAsync(rooms, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);


                // Строим словари быстрого поиска ID в памяти (работают за O(1) обращается сразу к переменной по его конкретному ключу, а не к O(n))
                // Используются для маппинга плоских записей расписания на ID сущностей без обращений к БД.
                var chairMap = chairs.ToDictionary(c => c.Name, c => c.Id);
                var discMap = discs.ToDictionary(d => d.Name, d => d.Id);
                var groupMap = groups.ToDictionary(g => g.AisId!, g => g.Id);
                var prepMap = preps.ToDictionary(p => p.AisId!, p => p.Id);
                var roomMap = rooms.ToDictionary(r => $"{r.Building.Name}_{r.Name}", r => r.Id);

                var eventsBatch = new List<Event>();
                const int batchSize = 1000;

                for (int i = 0; i < rawDtos.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var dto = rawDtos[i];

                    //Защита на мелкие ошибки в файле
                    if (!chairMap.TryGetValue(dto.ChairName, out int chairId))
                        throw new InvalidOperationException($"Ошибка валидации XML: Кафедра '{dto.ChairName}' не найдена в реестре. Проверьте опечатки и пробелы. Пара IDSubg={dto.IdSubg}, Предмет='{dto.DiscName}'.");

                    if (!discMap.TryGetValue(dto.DiscName, out int discId))
                        throw new InvalidOperationException($"Ошибка валидации XML: Предмет '{dto.DiscName}' не найден в реестре. Пара IDSubg={dto.IdSubg}.");

                    if (!groupMap.TryGetValue(dto.GroupId, out int groupId))
                        throw new InvalidOperationException($"Ошибка валидации XML: Группа ID='{dto.GroupId}' ({dto.GroupName}) не найдена. Пара IDSubg={dto.IdSubg}.");

                    if (!prepMap.TryGetValue(dto.PrepId, out int prepId))
                        throw new InvalidOperationException($"Ошибка валидации XML: Преподаватель ID='{dto.PrepId}' ({dto.PrepName}) не найден. Пара IDSubg={dto.IdSubg}.");

                    string roomKey = $"{dto.BuildingName}_{dto.RoomNumber}";
                    if (!roomMap.TryGetValue(roomKey, out int roomId))
                        throw new InvalidOperationException($"Ошибка валидации XML: Аудитория '{dto.RoomNumber}' в корпусе '{dto.BuildingName}' не найдена. Пара IDSubg={dto.IdSubg}.");

                    var ev = new Event
                    {
                        SubgId = dto.IdSubg,
                        Type = dto.Type,
                        Week = dto.Week,
                        Day = dto.Day,
                        Less = dto.Less,
                        ChairId = chairId,
                        DiscId = discId
                    };

                    // Наполняем навигационные свойства с помощью EF Core 
                    ev.GroupRefs.Add(new EventToGroupRef { GroupPtr = groupMap[dto.GroupId] });
                    ev.PrepRefs.Add(new EventToPrepRef { PrepPtr = prepMap[dto.PrepId] });
                    ev.RoomRefs.Add(new EventToBuildingRoomRef { BuildingRoomPtr = roomMap[roomKey] });

                    eventsBatch.Add(ev);
                    if (eventsBatch.Count >= batchSize || i == rawDtos.Count - 1)
                    {
                        await _context.Events.AddRangeAsync(eventsBatch, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);
                        _context.ChangeTracker.Clear();
                        eventsBatch.Clear();

                        int currentSavePercent = (i * 100) / rawDtos.Count;
                        int scaledSavePercent = StartSavePercent + ((currentSavePercent * (MaxSavePercent - StartSavePercent)) / 100);

                        await onProgress.Invoke(scaledSavePercent, $"Сохранение записей расписания в PostgreSQL... ({scaledSavePercent}%)");
                    }
                }

                var lastUpdate = await _context.Settings
                    .OrderBy(s => s.id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (lastUpdate == null)
                    await _context.Settings.AddAsync(new Settings { DateImport = DateTime.UtcNow, DateRelease = DateTime.UtcNow }, cancellationToken);
                else
                {
                    _context.Entry(lastUpdate).State = EntityState.Modified;
                    lastUpdate.DateImport = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(CancellationToken.None);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None); 
                throw;
            }
        }
    }
}
