using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using Xml.Api.Data;
using Xml.Api.Features.ScheduleUpload.Options;
using Xml.Api.Models;

namespace Xml.Api.Controllers
{
    /// <summary>
    /// Контроллер API для выдачи расписания занятий и списков
    /// </summary>
    [ApiController]
    [Route("v1")]
    public class ScheduleApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly RaspOptions _options;
        private readonly string[] _dows = ["Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье"];

        public ScheduleApiController(AppDbContext db, IOptions<RaspOptions> options)
        {
            _db = db;
            _options = options.Value;
        }

        /* functions */

        /// <summary>
		/// Возвращает информацию о расписании
		/// </summary>
        [HttpGet("get-info")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EntitiesAndServiceModel.InfoModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(EntitiesAndServiceModel.ApiResultModel))]
        public async Task<IActionResult> GetInfo()
        {
            var info = await _getInfoModelAsync();
            return info == null
                ? BadRequest(new EntitiesAndServiceModel.ApiResultModel{Result = "error" })
                : Ok(info);
        }

        /// <summary>
		/// Возвращает расписание (полный универсальный метод)
		/// </summary>
		/// <param name="groupAisId">id группы</param>
		/// <param name="prepAisId">id преподавателя</param>
		/// <param name="chairId">id кафедры</param>
		/// <param name="roomId">id аудитории</param>
        [HttpGet("get-rasp-full")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EntitiesAndServiceModel.RaspFullModel))]
        public async Task<IActionResult> GetRaspFull(
            [FromQuery] int groupAisId = 0,
            [FromQuery] int prepAisId = 0,
            [FromQuery] int chairId = 0,
            [FromQuery] int roomId = 0)
        {
            var rawGroups = await _db.Groups.AsNoTracking().OrderBy(g => g.Name).ToListAsync();
            var rawPreps = await _db.Preps.AsNoTracking().OrderBy(p => p.FullName).ToListAsync();
            var rawChairs = await _db.Chairs.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            var rawBuildings = await _db.Buildings.AsNoTracking().Include(b => b.Rooms).OrderBy(b => b.Name).ToListAsync();

            var regGroups = _getRegGroups(groupAisId, rawGroups);
            var regPreps = _getRegPreps(prepAisId, rawPreps);
            var regChairs = _getRegChairs(chairId, rawChairs);
            var regRooms = _getRegRooms(roomId, rawBuildings);

            var filterParts = new List<string>();

            if (regGroups?.Selected != null)
                filterParts.Add($"гр. {regGroups.Selected.Inner}");

            if (regPreps?.Selected != null)
                filterParts.Add(regPreps.Selected.Inner);

            if (regChairs?.Selected != null)
                filterParts.Add(regChairs.Selected.Inner);

            if (regRooms?.Selected != null)
                filterParts.Add($"ауд. {regRooms.Selected.Inner}");

            string filterText = filterParts.Count > 0
                ? string.Join(", ", filterParts)
                : "Полное расписание занятий";

            var model = new EntitiesAndServiceModel.RaspFullModel
            {
                Info = await _getInfoModelAsync(),
                Days = await _getRaspFullAsync(groupAisId, prepAisId, chairId, roomId),
                RegGroups = regGroups,
                RegPreps = regPreps,
                RegChairs = regChairs,
                RegRooms = regRooms,
                Filter = new EntitiesAndServiceModel.FilterModel
                {
                    GroupAisId = groupAisId,
                    PrepAisId = prepAisId,
                    ChairId = chairId,
                    RoomId = roomId,
                    Text = filterText
                }
            };

            return Ok(model);
        }

        /// <summary>
		/// Возвращает актуальное расписание на неделю (оптимизированный универсальный метод)
		/// </summary>
		/// <param name="groupAisId">id группы</param>
		/// <param name="prepAisId">id преподавателя</param>
		/// <param name="chairId">id кафедры</param>
		/// <param name="roomId">id аудитории</param>
        [HttpGet("get-rasp-smart")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EntitiesAndServiceModel.RaspSmartModel))]
        public async Task<IActionResult> GetRaspSmart(
            [FromQuery] int groupAisId = 0,
            [FromQuery] int prepAisId = 0,
            [FromQuery] int chairId = 0,
            [FromQuery] int roomId = 0)
        {
            var rawGroups = await _db.Groups.AsNoTracking().OrderBy(g => g.Name).ToListAsync();
            var rawPreps = await _db.Preps.AsNoTracking().OrderBy(p => p.FullName).ToListAsync();
            var rawChairs = await _db.Chairs.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            var rawBuildings = await _db.Buildings.AsNoTracking().Include(b => b.Rooms).OrderBy(b => b.Name).ToListAsync();

            var regGroups = _getRegGroups(groupAisId, rawGroups);
            var regPreps = _getRegPreps(prepAisId, rawPreps);
            var regChairs = _getRegChairs(chairId, rawChairs);
            var regRooms = _getRegRooms(roomId, rawBuildings);

            var filterParts = new List<string>();
            if (regGroups?.Selected != null) filterParts.Add($"гр. {regGroups.Selected.Inner}");
            if (regPreps?.Selected != null) filterParts.Add(regPreps.Selected.Inner);
            if (regChairs?.Selected != null) filterParts.Add(regChairs.Selected.Inner);
            if (regRooms?.Selected != null) filterParts.Add($"ауд. {regRooms.Selected.Inner}");

            var infoModel = await _getInfoModelAsync();
            string weekStatus = infoModel != null ? (infoModel.IsWeekOdd ? "Нечетная неделя" : "Четная неделя") : "Текущая неделя";

            string filterText = filterParts.Count > 0
                ? $"{string.Join(", ", filterParts)} ({weekStatus})"
                : $"Расписание на семестр ({weekStatus})";

            var model = new EntitiesAndServiceModel.RaspSmartModel
            {
                Info = infoModel,
                Days = await _getRaspSmartAsync(groupAisId, prepAisId, chairId, roomId),
                Filter = new EntitiesAndServiceModel.FilterModel
                {
                    GroupAisId = groupAisId,
                    PrepAisId = prepAisId,
                    ChairId = chairId,
                    RoomId = roomId,
                    Text = filterText
                }
            };

            return Ok(model);
        }

        /// <summary>
		/// Возвращает реестр преподавателей
		/// </summary>
        [HttpGet("get-preps")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Prep>))]
        public async Task<IActionResult> GetPreps()
        {
            var preps = await _db.Preps
                .AsNoTracking()
                .OrderBy(x => x.FullName)
                .Select(x => new Prep
                {
                    Id = x.Id,
                    AisId = x.AisId,
                    FullName = x.FullName,
                    Degree = x.Degree ?? "",
                    Data = x.Data,
                    Prop = x.Prop
                })
                .ToListAsync();

            return Ok(preps);
        }

        /// <summary>
        /// Возвращает реестр учебных групп
        /// </summary>
        [HttpGet("get-groups")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Group>))]
        public async Task<IActionResult> GetGroups()
        {
            var groups = await _db.Groups
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new Group
                {
                    Id = x.Id,
                    AisId = x.AisId,
                    Name = x.Name
                })
                .ToListAsync();

            return Ok(groups);
        }

        /// <summary>
        /// Возвращает реестр аудиторий по зданиям
        /// </summary>
        [HttpGet("get-buildings")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Building>))]
        public async Task<IActionResult> GetBuildings()
        {
            var buildings = await _db.Buildings
                .AsNoTracking()
                .Include(b => b.Rooms)
                .OrderBy(b => b.Name)
                .ToListAsync();

            var model = buildings.Select(b => new Building
            {
                Id = b.Id,
                Name = b.Name,
                Rooms = b.Rooms
                    .OrderBy(r => r.Name)
                    .Select(r => new BuildingRoom
                    {
                        Id = r.Id,
                        Name = r.Name,
                        MasterPtr = r.MasterPtr
                    }).ToList()
            }).ToList();

            return Ok(model);
        }

        /* privates */

        private async Task<IEnumerable<EntitiesAndServiceModel.DayModel>> _getRaspFullAsync(int groupAisId, int prepAisId, int chairId, int roomId)
        {
            if (groupAisId < 1 && prepAisId < 1 && chairId < 1 && roomId < 1)
                return Array.Empty<EntitiesAndServiceModel.DayModel>();

            var query = _db.Events.AsNoTracking();
            query = _applyEventsFilter(query, groupAisId, prepAisId, chairId, roomId);

            var events = await query
                .Include(x => x.Disc)
                .Include(x => x.GroupRefs).ThenInclude(g => g.Group)
                .Include(x => x.PrepRefs).ThenInclude(p => p.Prep)
                .Include(x => x.RoomRefs).ThenInclude(r => r.Room).ThenInclude(rm => rm.Building)
                .ToListAsync();

            return events
                .GroupBy(e => e.Day)
                .OrderBy(g => g.Key)
                .Select(gDays => new EntitiesAndServiceModel.DayModel
                {
                    Day = gDays.Key,
                    Title = gDays.Key >= 1 && gDays.Key <= _dows.Length ? _dows[gDays.Key - 1] : "Неизвестный день",
                    Lessons = gDays
                        .GroupBy(e => e.Less)
                        .OrderBy(g => g.Key)
                        .Select(gLess => _getLessModel(gLess))
                        .ToList()
                });
        }

        /// <summary>
        /// Применение фильтра с накоплением условий
        /// </summary>
        private IQueryable<Xml.Api.Models.Event> _applyEventsFilter(IQueryable<Xml.Api.Models.Event> query, int groupAisId, int prepAisId, int chairId, int roomId)
        {
            if (groupAisId > 0)
                query = query.Where(x => x.GroupRefs.Any(g => g.Group.AisId == groupAisId.ToString()));
            if (prepAisId > 0)
                query = query.Where(x => x.PrepRefs.Any(p => p.Prep.AisId == prepAisId.ToString()));
            if (chairId > 0)
                query = query.Where(x => x.ChairId == chairId);
            if (roomId > 0)
                query = query.Where(x => x.RoomRefs.Any(r => r.BuildingRoomPtr == roomId));
            return query;
        }
        private async Task<IEnumerable<EntitiesAndServiceModel.DayModel>> _getRaspSmartAsync(int groupAisId, int prepAisId, int chairId, int roomId)
        {
            if (groupAisId < 1 && prepAisId < 1 && chairId < 1 && roomId < 1)
                return Array.Empty<EntitiesAndServiceModel.DayModel>();

            var query = _db.Events.AsNoTracking().Where(x => x.Week != 1);
            query = _applyEventsFilter(query, groupAisId, prepAisId, chairId, roomId);

            var events = await query
                .Include(x => x.Disc)
                .Include(x => x.GroupRefs).ThenInclude(g => g.Group)
                .Include(x => x.PrepRefs).ThenInclude(p => p.Prep)
                .Include(x => x.RoomRefs).ThenInclude(r => r.Room).ThenInclude(rm => rm.Building)
                .ToListAsync();

            return events
                .GroupBy(e => e.Day)
                .OrderBy(g => g.Key)
                .Select(gDays => new EntitiesAndServiceModel.DayModel
                {
                    Day = gDays.Key,
                    Title = gDays.Key >= 1 && gDays.Key <= _dows.Length ? _dows[gDays.Key - 1] : "Неизвестный день",
                    Lessons = gDays
                        .GroupBy(e => e.Less)
                        .OrderBy(g => g.Key)
                        .Select(gLess => _getLessModel(gLess))
                        .ToList()
                });
        }

        private EntitiesAndServiceModel.LessModel _getLessModel(IGrouping<int, Xml.Api.Models.Event> group)
        {
            var times = group.Key >= 1 && group.Key <= _options.Lessons.Count
                ? _options.Lessons[group.Key - 1]
                : null;

            var lessModel = new EntitiesAndServiceModel.LessModel
            {
                Less = group.Key,
                Begin = times != null ? TimeOnly.Parse(times.Begin) : null,
                End = times != null ? TimeOnly.Parse(times.End) : null,
                WeekAll = new List<EntitiesAndServiceModel.EventModel>(),
                Week1 = new List<EntitiesAndServiceModel.EventModel>(),
                Week2 = new List<EntitiesAndServiceModel.EventModel>()
            };

            foreach (var ev in group)
            {
                var eventModel = new EntitiesAndServiceModel.EventModel
                {
                    Id = ev.Id,
                    SubgId = ev.SubgId,
                    Week = ev.Week,
                    Type = ev.Type,
                    Dics = ev.Disc?.Name ?? "Дисциплина не указана",
                    ChairId = ev.ChairId ?? 0,
                    GroupsAisIds = ev.GroupRefs.Select(g => int.TryParse(g.Group.AisId, out var id) ? id : 0).ToArray(),
                    PrepsAisIds = ev.PrepRefs.Select(p => int.TryParse(p.Prep.AisId, out var id) ? id : 0).ToArray(),
                    RoomsIds = ev.RoomRefs.Select(r => r.BuildingRoomPtr).ToArray()
                };

                switch (ev.Week)
                {
                    case 1:
                        ((List<EntitiesAndServiceModel.EventModel>)lessModel.Week1).Add(eventModel);
                        break;
                    case 2:
                        ((List<EntitiesAndServiceModel.EventModel>)lessModel.Week2).Add(eventModel);
                        break;
                    default:
                        ((List<EntitiesAndServiceModel.EventModel>)lessModel.WeekAll).Add(eventModel);
                        break;
                }
            }
            return lessModel;
        }

        private async Task<EntitiesAndServiceModel.InfoModel?> _getInfoModelAsync()
        {
            var settingsEntity = await _db.Settings.AsNoTracking().OrderBy(s => s.id).FirstOrDefaultAsync();
            if (settingsEntity == null) return null;

            var now = DateTime.Now;
            bool isAutumn = now.Month > 7;
            int y1 = isAutumn ? now.Year : now.Year - 1;
            int y2 = (isAutumn ? now.Year + 1 : now.Year) - 2000;

            int calendarWeek = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            int currentWeek = calendarWeek + _options.WeekOffset;
            bool isWeekOdd = currentWeek % 2 != 0;
            int currentDay = now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;

            var model = new EntitiesAndServiceModel.InfoModel
            {
                Years = $"{y1}/{y2}",
                IsAutumn = isAutumn,
                DateRelease = settingsEntity.DateRelease ?? now,
                DateImport = settingsEntity.DateImport ?? now,
                CurrentWeek = currentWeek,
                IsWeekOdd = isWeekOdd,
                CurrentDay = currentDay,
                Time = TimeOnly.FromDateTime(now)
            };

            var currentTimeStr = now.ToString("HH:mm:ss");
            if (TimeOnly.TryParse(currentTimeStr, out var time1))
            {
                var i1 = 1;
                foreach (var lessonConfig in _options.Lessons)
                {
                    if (model.CurrentLess == 0)
                    {
                        if (TimeOnly.TryParse(lessonConfig.Begin, out var beginTime) && TimeOnly.TryParse(lessonConfig.End, out var endTime))
                        {
                            if (beginTime <= time1 && endTime > time1)
                            {
                                model.CurrentLess = i1;
                                model.PastLess = 0;
                                break;
                            }
                            if (endTime <= time1) model.PastLess = i1;
                            if (model.UpcomingLess == 0 && beginTime > time1) model.UpcomingLess = i1;
                        }
                        ++i1;
                    }
                }
            }

            return model;
        }

        private static EntitiesAndServiceModel.TagSelectModel _getRegGroups(int currentGroupAisId, IEnumerable<Group> items)
        {
            var options = items.Select(x => new EntitiesAndServiceModel.TagOptionModel
            {
                Value = x.AisId ?? string.Empty,
                Inner = x.Name,
                IsSelected = x.AisId == currentGroupAisId.ToString()
            }).ToList();

            return new EntitiesAndServiceModel.TagSelectModel { Options = options, Selected = options.FirstOrDefault(x => x.IsSelected)! };
        }

        private static EntitiesAndServiceModel.TagSelectModel _getRegPreps(int currentPrepAisId, IEnumerable<Prep> items)
        {
            var options = items.Select(x => new EntitiesAndServiceModel.TagOptionModel
            {
                Value = x.AisId ?? string.Empty,
                Inner = string.IsNullOrWhiteSpace(x.Degree) ? x.FullName : $"{x.FullName}, {x.Degree}",
                IsSelected = x.AisId == currentPrepAisId.ToString()
            }).ToList();

            return new EntitiesAndServiceModel.TagSelectModel { Options = options, Selected = options.FirstOrDefault(x => x.IsSelected)! };
        }

        private static EntitiesAndServiceModel.TagSelectModel _getRegChairs(int id, IEnumerable<Chair> items)
        {
            var options = items.Select(x => new EntitiesAndServiceModel.TagOptionModel
            {
                Value = x.Id.ToString(),
                Inner = x.Name,
                IsSelected = x.Id == id
            }).ToList();

            return new EntitiesAndServiceModel.TagSelectModel { Options = options, Selected = options.FirstOrDefault(x => x.IsSelected)! };
        }

        private static EntitiesAndServiceModel.TagSelectModel _getRegRooms(int id, IEnumerable<Building> items)
        {
            var options = new List<EntitiesAndServiceModel.TagOptionModel>();
            foreach (var building in items.OrderBy(x => x.Name))
            {
                foreach (var room in building.Rooms.OrderBy(x => x.Name))
                {
                    options.Add(new EntitiesAndServiceModel.TagOptionModel
                    {
                        Value = room.Id.ToString(),
                        Inner = $"{room.Name}&nbsp;({building.Name})",
                        IsSelected = room.Id == id
                    });
                }
            }

            return new EntitiesAndServiceModel.TagSelectModel { Options = options, Selected = options.FirstOrDefault(x => x.IsSelected)! };
        }
    }
} 