using Microsoft.EntityFrameworkCore;
using Xml.Api.Data;
using Xml.Api.Models;

namespace Xml.Api.Services.Modules
{
    public class ScheduleSaver(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        public async Task SaveAsync(
            List<Chair> chairs,
            List<Disc> discs,
            List<Group> groups,
            List<Prep> preps,
            List<Building> buildings,
            List<BuildingRoom> rooms,
            List<SubjectDto> rawDtos)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "TRUNCATE TABLE \"Events\", \"Preps\", \"Groups\", \"Discs\", \"Chairs\", \"Buildings\", \"BuildingsRooms\", \"EventToGroupRefs\", \"EventToPrepRefs\", \"EventToBuildingRoomRefs\" CASCADE;");

                await _context.Buildings.AddRangeAsync(buildings);
                await _context.Chairs.AddRangeAsync(chairs);
                await _context.Discs.AddRangeAsync(discs);
                await _context.Groups.AddRangeAsync(groups);
                await _context.Preps.AddRangeAsync(preps);
                await _context.SaveChangesAsync();

                await _context.BuildingsRooms.AddRangeAsync(rooms);
                await _context.SaveChangesAsync();

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

                    ev.GroupRefs.Add(new EventToGroupRef { GroupPtr = groupMap[dto.GroupId] });
                    ev.PrepRefs.Add(new EventToPrepRef { PrepPtr = prepMap[dto.PrepId] });

                    string roomKey = $"{dto.BuildingName}_{dto.RoomNumber}";
                    ev.RoomRefs.Add(new EventToBuildingRoomRef { BuildingRoomPtr = roomMap[roomKey] });

                    events.Add(ev);
                }

                await _context.Events.AddRangeAsync(events);

                var lastUpdate = await _context.Settings.FirstOrDefaultAsync();
                if (lastUpdate == null)
                    await _context.Settings.AddAsync(new Settings { DateImport = DateTime.Now, DateRelease = DateTime.Now });
                else
                    lastUpdate.DateImport = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
