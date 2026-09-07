using Xml.Api.Models;
using Xml.Api.Features.ScheduleUpload.Extensions;

namespace Xml.Api.Features.ScheduleUpload
{
    /// <summary>
    /// Модуль маппинга данных.
    /// Отвечает за cоздание справочников из списка DTO.
    /// </summary>
    public class ScheduleMapper
    {
        /// <summary>
        /// Нормализует плоский список занятий(SubjectDto) в справочники сущностей.
        /// Гарантирует уникальность по естественным ключам (имя кафедры, ID группы и т. д.) 
        /// с помощью метода GetOrAdd(он непотокобезопасен, невозможно использовать более чем в одном потоке)
        /// Аудитории формируются по составному ключу «здание_номер», чтобы различать одинаковые номера в разных корпусах.
        /// </summary>
        /// <param name="dtos">Плоский список DTO расписания (каждая запись — отдельное занятие).</param>
        /// <returns>Кортеж: (Chairs, Discs, Groups, Preps, Buildings, Rooms, RawDtos)</returns>
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
            // Временные словари для накопления уникальных записей по их ключам
            var chairsDict = new Dictionary<string, Chair>();
            var discsDict = new Dictionary<string, Disc>();
            var groupsDict = new Dictionary<string, Group>();
            var prepsDict = new Dictionary<string, Prep>();
            var buildingsDict = new Dictionary<string, Building>();
            var roomsDict = new Dictionary<string, BuildingRoom>();
            var dtosList = dtos.ToList();

            foreach (var dto in dtosList)
            {
                // Метод расширения GetOrAdd гарантирует, что сущность создастся только один раз
                chairsDict.GetOrAdd(dto.ChairName,
                    () => new Chair { Name = dto.ChairName });
                discsDict.GetOrAdd(dto.DiscName,
                    () => new Disc { Name = dto.DiscName });

                // Привязываемся к бизнес-ключам из XML(это AisId и Name здания)
                groupsDict.GetOrAdd(dto.GroupId,
                    () => new Group { AisId = dto.GroupId, Name = dto.GroupName });
                prepsDict.GetOrAdd(dto.PrepId,
                    () => new Prep { AisId = dto.PrepId, FullName = dto.PrepName });
                buildingsDict.GetOrAdd(dto.BuildingName,
                    () => new Building { Name = dto.BuildingName });
            }

            // Второй проход: формируем комнаты, когда все здания уже гарантированно уникализированы в памяти
            foreach (var dto in dtosList)
            {
                string roomComboKey = $"{dto.BuildingName}_{dto.RoomNumber}";
                roomsDict.GetOrAdd(roomComboKey, () => new BuildingRoom
                {
                    Name = dto.RoomNumber,
                    Building = buildingsDict[dto.BuildingName] // Связывается объект в памяти через навигационное свойство
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
