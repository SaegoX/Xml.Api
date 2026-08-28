using Xml.Api.Models;

namespace Xml.Api.Services.Modules
{
    public class ScheduleMapper
    {
        public (
            List<Chair> Chairs, 
            List<Disc> Discs, 
            List<Group> Groups,
            List<Prep> Preps, 
            List<Building> Buildings, 
            List<BuildingsRoom> Rooms,
            List<Event> Events)
                Map(IEnumerable<XmlSubjectDto> dtos)
        {
            var chairsDict = new Dictionary<string, Chair>();
            var discsDict = new Dictionary<string, Disc>();
            var groupsDict = new Dictionary<string, Group>();
            var prepsDict = new Dictionary<string, Prep>();
            var buildingsDict = new Dictionary<string, Building>();
            var roomsDict = new Dictionary<string, BuildingsRoom>();
            var events = new List<Event>();

            foreach (var dto in dtos)
            {
                chairsDict.GetOrAdd(dto.ChairName, 
                    () => new Chair { Name = dto.ChairName });
                discsDict.GetOrAdd(dto.DiscName, 
                    () => new Disc { Name = dto.DiscName });
                groupsDict.GetOrAdd(dto.GroupId, 
                    () => new Group { Id = dto.GroupId, Name = dto.GroupName });
                prepsDict.GetOrAdd(dto.PrepId, 
                    () => new Prep { Id = dto.PrepId, Name = dto.PrepName });
                buildingsDict.GetOrAdd(dto.BuildingName, 
                    () => new Building { Name = dto.BuildingName });

                string roomId = $"{dto.BuildingName}_{dto.RoomNumber}";
                if (!roomsDict.ContainsKey(roomId))
                {

                    roomsDict[roomId] = new BuildingsRoom
                    {
                        Id = roomId,
                        RoomNumber = dto.RoomNumber,
                        BuildingName = dto.BuildingName
                    };

                }

                events.Add(new Event
                {

                    Id = Guid.NewGuid(),
                    IdSubg = dto.IdSubg,
                    Type = dto.Type,
                    Week = dto.Week,
                    Day = dto.Day,
                    Less = dto.Less,
                    ChairName = dto.ChairName,
                    DiscName = dto.DiscName,
                    GroupId = dto.GroupId,
                    PrepId = dto.PrepId,
                    RoomId = roomId

                });
            }

            return (
                chairsDict.Values.ToList(),
                discsDict.Values.ToList(),
                groupsDict.Values.ToList(),
                prepsDict.Values.ToList(),
                buildingsDict.Values.ToList(),
                roomsDict.Values.ToList(),
                events
            );
        }
    }
}
