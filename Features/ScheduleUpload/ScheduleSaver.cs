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
        /// Очищает старые таблицы и сохраняет новые данные в рамках единой транзакции
        /// </summary>
        public async Task SaveAsync(
            List<Chair> chairs,
            List<Disc> discs,
            List<Group> groups,
            List<Prep> preps,
            List<Building> buildings,
            List<BuildingRoom> rooms,
            List<SubjectDto> rawDtos)
        {
            // Открываем транзакцию. Если хоть один шаг упадет — база вернется в исходное состояние
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Каскадно очищаем все таблицы. Последовательность не важна, всё удаляется каскадно(связно). TRUNCATE только очищает(не удаляет) таблицы.
                await _context.Database.ExecuteSqlRawAsync(
                    "TRUNCATE TABLE \"EventToGroupRefs\", \"EventToPrepRefs\", \"EventToBuildingRoomRefs\", \"Events\", \"BuildingRooms\", \"Buildings\", \"Preps\", \"Groups\", \"Discs\", \"Chairs\" RESTART IDENTITY CASCADE;");

                // Заливаем первичные справочники
                await _context.Buildings.AddRangeAsync(buildings);
                await _context.Chairs.AddRangeAsync(chairs);
                await _context.Discs.AddRangeAsync(discs);
                await _context.Groups.AddRangeAsync(groups);
                await _context.Preps.AddRangeAsync(preps);
                await _context.SaveChangesAsync(); // Фиксируем, чтобы получить сгенерированные БД int ID

                // Заливаем комнаты (они связываются по ссылкам на объекты зданий из памяти)
                await _context.BuildingRooms.AddRangeAsync(rooms);
                await _context.SaveChangesAsync();


                // Строим словари быстрого поиска ID в памяти (работают за O(1) обращается сразу к переменной по его конкретному ключу, а не к O(n))
                // Используются для маппинга плоских записей расписания на ID сущностей без обращений к БД.
                var chairMap = chairs.ToDictionary(c => c.Name, c => c.Id);
                var discMap = discs.ToDictionary(d => d.Name, d => d.Id);
                var groupMap = groups.ToDictionary(g => g.AisId!, g => g.Id);
                var prepMap = preps.ToDictionary(p => p.AisId!, p => p.Id);
                var roomMap = rooms.ToDictionary(r => $"{r.Building.Name}_{r.Name}", r => r.Id);

                var events = new List<Event>();
                foreach (var dto in rawDtos)
                {
                    var ev = new Event
                    {
                        SubgId = dto.IdSubg,
                        Type = dto.Type,
                        Week = dto.Week,
                        Day = dto.Day,
                        Less = dto.Less,
                        ChairId = chairMap[dto.ChairName],
                        DiscId = discMap[dto.DiscName]
                    };

                    // Наполняем навигационные свойства с помощью EF Core 
                    ev.GroupRefs.Add(new EventToGroupRef { GroupPtr = groupMap[dto.GroupId] });
                    ev.PrepRefs.Add(new EventToPrepRef { PrepPtr = prepMap[dto.PrepId] });

                    string roomKey = $"{dto.BuildingName}_{dto.RoomNumber}";
                    ev.RoomRefs.Add(new EventToBuildingRoomRef { BuildingRoomPtr = roomMap[roomKey] });

                    events.Add(ev);
                }

                // Сохраняем события вместе со всеми внутренними коллекциями связей
                await _context.Events.AddRangeAsync(events);

                // Записываем время импорта в формате UTC(В postgres жёстко требуется именно UTC формат)
                var lastUpdate = await _context.Settings
                    .OrderBy(s=>s.id)
                    .FirstOrDefaultAsync();
                if (lastUpdate == null)
                    await _context.Settings.AddAsync(new Settings { DateImport = DateTime.UtcNow, DateRelease = DateTime.UtcNow });
                else
                    lastUpdate.DateImport = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); // Подтверждаем транзакцию, если всё успешно
            }
            catch
            {
                await transaction.RollbackAsync(); // Откатываем, если произошёл сбой
                throw;
            }
        }
    }
}
