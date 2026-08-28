using Microsoft.EntityFrameworkCore;
using System.Xml;
using Xml.Api.Data;
using Xml.Api.Models;

namespace Xml.Api.Services
{
    public class XmlScheduleParserService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        public async Task ParseAndSaveAsync(string filePath)
        {
            var chairs = new Dictionary<string, Chair>();
            var discs = new Dictionary<string, Disc>();
            var groups = new Dictionary<string, Group>();
            var preps = new Dictionary<string, Prep>();
            var buildings = new Dictionary<string, Building>();
            var rooms = new Dictionary<string, BuildingsRoom>();

            var events = new List<Event>();

            var settings = new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit
            };

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            using (var reader = XmlReader.Create(stream, settings))
            {
                while (await reader.ReadAsync())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "subject")
                    {
                        string idSubg = reader.GetAttribute("IDSubg") ?? Guid.NewGuid().ToString();
                        string discName = reader.GetAttribute("disc") ?? "Не указано";
                        string type = reader.GetAttribute("type") ?? "Л";
                        string chairName = reader.GetAttribute("chair") ?? "Не указана";
                        string idPrep = reader.GetAttribute("id_prep") ?? "";
                        string prepName = reader.GetAttribute("prep") ?? "";
                        string idGroup = reader.GetAttribute("id_group") ?? "unknown_group";
                        string groupName = reader.GetAttribute("group") ?? "Не указана";

                        int week = int.TryParse(reader.GetAttribute("week"), out var w) ? w : 0;
                        int day = int.TryParse(reader.GetAttribute("day"), out var d) ? d : 1;
                        int less = int.TryParse(reader.GetAttribute("less"), out var l) ? l : 1;

                        string buildingName = reader.GetAttribute("buildings") ?? "Не указан";
                        string roomNumber = reader.GetAttribute("rooms") ?? "Не указана";

                        if (!chairs.ContainsKey(chairName))
                            chairs[chairName] = new Chair { Name = chairName };

                        if (!discs.ContainsKey(discName))
                            discs[discName] = new Disc { Name = discName };

                        if (!groups.ContainsKey(idGroup))
                            groups[idGroup] = new Group { Id = idGroup, Name = groupName };

                        if (string.IsNullOrWhiteSpace(idPrep))
                        {
                            idPrep = "empty_prep";
                            prepName = "Преподаватель не указан";
                        }
                        if (!preps.ContainsKey(idPrep))
                            preps[idPrep] = new Prep { Id = idPrep, Name = prepName };

                        if (!buildings.ContainsKey(buildingName))
                            buildings[buildingName] = new Building { Name = buildingName };

                        string roomId = $"{buildingName}_{roomNumber}";
                        if (!rooms.ContainsKey(roomId))
                        {
                            rooms[roomId] = new BuildingsRoom
                            {
                                Id = roomId,
                                RoomNumber = roomNumber,
                                BuildingName = buildingName
                            };
                        }

                        var ev = new Event
                        {
                            Id = Guid.NewGuid(),
                            IdSubg = idSubg,
                            Type = type,
                            Week = week,
                            Day = day,
                            Less = less,
                            ChairName = chairName,
                            DiscName = discName,
                            GroupId = idGroup,
                            PrepId = idPrep,
                            RoomId = roomId
                        };

                        events.Add(ev);
                    }
                }
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Events\" CASCADE;");
                    await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Preps\" CASCADE;");
                    await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Groups\" CASCADE;");
                    await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Discs\" CASCADE;");
                    await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Chairs\" CASCADE;");
                    await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Buildings\" CASCADE;");
                    await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"BuildingsRooms\" CASCADE;");

                    await _context.Buildings.AddRangeAsync(buildings.Values);
                    await _context.BuildingsRooms.AddRangeAsync(rooms.Values);
                    await _context.Chairs.AddRangeAsync(chairs.Values);
                    await _context.Discs.AddRangeAsync(discs.Values);
                    await _context.Groups.AddRangeAsync(groups.Values);
                    await _context.Preps.AddRangeAsync(preps.Values);

                    await _context.SaveChangesAsync();

                    await _context.Events.AddRangeAsync(events);

                    string formattedDate = DateTime.Now.ToString("yyyy-0MM-dd HH:mm:ss");

                    var lastUpdateSetting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == "LastUpdateDateTime");
                    if (lastUpdateSetting == null)
                    {
                        await _context.Settings.AddAsync(new Settings { Key = "LastUpdateDateTime", Value = formattedDate });
                    }
                    else
                    {
                        lastUpdateSetting.Value = formattedDate;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }

                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}