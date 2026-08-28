using Microsoft.EntityFrameworkCore;
using Xml.Api.Data;
using Xml.Api.Models;

namespace Xml.Api.Services.Modules
{
    public class ScheduleSaver
    {
        private readonly AppDbContext _context;

        public ScheduleSaver(AppDbContext context) => _context = context;

        public async Task SaveAsync(
            IEnumerable<Chair> chairs,
            IEnumerable<Disc> discs,
            IEnumerable<Group> groups,
            IEnumerable<Prep> preps,
            IEnumerable<Building> buildings,
            IEnumerable<BuildingsRoom> rooms,
            IEnumerable<Event> events)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "TRUNCATE TABLE \"Events\", \"Preps\", \"Groups\", \"Discs\", \"Chairs\", \"Buildings\", \"BuildingsRooms\" CASCADE;");

                await _context.Buildings.AddRangeAsync(buildings);
                await _context.BuildingsRooms.AddRangeAsync(rooms);
                await _context.Chairs.AddRangeAsync(chairs);
                await _context.Discs.AddRangeAsync(discs);
                await _context.Groups.AddRangeAsync(groups);
                await _context.Preps.AddRangeAsync(preps);
                await _context.SaveChangesAsync();

                await _context.Events.AddRangeAsync(events);

                string formattedDate = DateTime.Now.ToString("yyyy-0MM-dd HH:mm:ss");
                var lastUpdate = await _context.Settings.FirstOrDefaultAsync(s => s.Key == "LastUpdateDateTime");
                if (lastUpdate == null)
                    await _context.Settings.AddAsync(new Settings { Key = "LastUpdateDateTime", Value = formattedDate });
                else
                    lastUpdate.Value = formattedDate;

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
