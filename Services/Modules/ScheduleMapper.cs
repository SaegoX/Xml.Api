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
            List<BuildingRoom> Rooms,
            List<SubjectDto> RawDtos)
                Map(IEnumerable<SubjectDto> dtos)
        {
            var chairsDict = new Dictionary<string, Chair>();
            var discsDict = new Dictionary<string, Disc>();
            var groupsDict = new Dictionary<string, Group>();
            var prepsDict = new Dictionary<string, Prep>();
            var buildingsDict = new Dictionary<string, Building>();
            var roomsDict = new Dictionary<string, BuildingRoom>();
            var dtosList = dtos.ToList();

            foreach (var dto in dtosList)
            {
                chairsDict.GetOrAdd(dto.ChairName,
                    () => new Chair { Name = dto.ChairName });
                discsDict.GetOrAdd(dto.DiscName,
                    () => new Disc { Name = dto.DiscName });
                groupsDict.GetOrAdd(dto.GroupId,
                    () => new Group { AisId = dto.GroupId, Name = dto.GroupName });
                prepsDict.GetOrAdd(dto.PrepId,
                    () => new Prep { AisId = dto.PrepId, FullName = dto.PrepName });
                buildingsDict.GetOrAdd(dto.BuildingName,
                    () => new Building { Name = dto.BuildingName });
            }

            foreach (var dto in dtosList)
            {
                string roomComboKey = $"{dto.BuildingName}_{dto.RoomNumber}";
                roomsDict.GetOrAdd(roomComboKey, () => new BuildingRoom
                {
                    Name = dto.RoomNumber,
                    Building = buildingsDict[dto.BuildingName]
                });
            }

            return (
                chairsDict.Values.ToList(),
                discsDict.Values.ToList(),
                groupsDict.Values.ToList(),
                prepsDict.Values.ToList(),
                buildingsDict.Values.ToList(),
                roomsDict.Values.ToList(),
                dtosList
            );
        }
    }
}
