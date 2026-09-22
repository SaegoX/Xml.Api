using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using Xml.Api.Data;
using Xml.Api.Models;
using Xml.Api.Features.ScheduleApi;
using static Xml.Api.Features.ScheduleApi.ScheduleViewModels;

namespace Xml.Api.Features.ScheduleApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly RaspOptions _options;

        // Стандартные названия дней недели для вывода в JSON
        private readonly string[] _dows = ["Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье"];

        /// <summary>
        /// Конструктор контроллера API расписания.
        /// Внедряем контекст БД и наши строго типизированные опции расписания звонков.
        /// </summary>
        public ScheduleApiController(AppDbContext db, IOptions<RaspOptions> options)
        {
            _db = db;
            _options = options.Value;
        }

        /// <summary>
        /// Возвращает информацию о текущем состоянии расписания и метаданных семестра.
        /// </summary>		
        [HttpGet("get-info")]
        public async Task<IActionResult> GetInfo()
        {
            var info = await _getInfoModelAsync();
            return info == null
                ? BadRequest(new { status = "error", message = "Метаданные расписания не найдены в СУБД." })
                : Ok(info);
        }

        /// <summary>
        /// Эндпоинт: Возвращает реестр всех кафедр, отсортированных по названию.
        /// </summary>
        [HttpGet("get-chairs")]
        public async Task<IActionResult> GetChairs()
        {
            var chairs = await _db.Chairs
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new ChairViewModel
                {
                    Id = x.Id,
                    Title = x.Name // Маппим наше поле Name в свойство Title для API
                })
                .ToListAsync();

            return Ok(chairs);
        }

        /// <summary>
        /// Эндпоинт: Возвращает реестр аудиторий, сгруппированных по зданиям (корпусам).
        /// </summary>
        [HttpGet("get-buildings")]
        public async Task<IActionResult> GetBuildings()
        {
            // Вытягиваем здания и жадно (Eager Loading) подтягиваем связанные комнаты через Include
            var buildings = await _db.Buildings
                .AsNoTracking()
                .Include(b => b.Rooms)
                .OrderBy(b => b.Name)
                .ToListAsync();

            // Преобразуем реляционную структуру в список моделей ответа API
            var model = buildings.Select(b => new BuildingViewModel
            {
                Id = b.Id,
                Title = b.Name,
                // Извлекаем комнаты, принадлежащие конкретно этому зданию
                Rooms = b.Rooms
                    .OrderBy(r => r.Name)
                    .Select(r => new RoomInnerModel
                    {
                        Id = r.Id,
                        Title = r.Name
                    })
                    .ToList()
            });

            return Ok(model);
        }

        /// <summary>
        /// Эндпоинт: Возвращает полное расписание занятий с фильтрацией по параметрам.
        /// </summary>
        [HttpGet("get-rasp-full")]
        public async Task<IActionResult> GetRaspFull(
            [FromQuery] int groupAisId = 0,
            [FromQuery] int prepAisId = 0,
            [FromQuery] int chairId = 0,
            [FromQuery] int roomId = 0)
        {
            var days = await _getRaspFullAsync(groupAisId, prepAisId, chairId, roomId);
            return Ok(new { days });
        }

        /// <summary>
        /// Эндпоинт: Возвращает умное расписание на текущую неделю (исключая архивные или неактуальные недели).
        /// </summary>
        [HttpGet("get-rasp-smart")]
        public async Task<IActionResult> GetRaspSmart(
            [FromQuery] int groupAisId = 0,
            [FromQuery] int prepAisId = 0,
            [FromQuery] int chairId = 0,
            [FromQuery] int roomId = 0)
        {
            var days = await _getRaspSmartAsync(groupAisId, prepAisId, chairId, roomId);
            return Ok(new { days });
        }

        /// <summary>
        /// Внутренний метод: Вычисляет параметры текущего семестра, четность недель и номера пар.
        /// </summary>
        private async Task<InfoModel?> _getInfoModelAsync()
        {
            //Извлечение строки системных настроек из импортированных данных
            var settingsEntity = await _db.Settings
                .AsNoTracking()
                .OrderBy(s => s.id)
                .FirstOrDefaultAsync();

            if (settingsEntity == null) return null;

            var now = DateTime.Now;

            //Осенний или весенний семестр
            bool isAutumn = now.Month > 7;

            //Вычисление года
            int y1 = isAutumn ? now.Year : now.Year - 1;
            int y2 = (isAutumn ? now.Year + 1 : now.Year) - 2000;

            int calendarWeek = CultureInfo.InvariantCulture.Calendar
                .GetWeekOfYear(now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);

            int currentWeek = calendarWeek + _options.WeekOffset;

            bool isWeekOdd = currentWeek % 2 != 0;

            int currentDay = now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;

            var model = new InfoModel
            {
                Years = $"{y1}/{y2}",
                IsAutumn = isAutumn,
                DateRelease = settingsEntity.DateRelease?.ToLocalTime() ?? now,
                DateImport = settingsEntity.DateImport?.ToLocalTime() ?? now,
                CurrentWeek = currentWeek,
                IsWeekOdd = isWeekOdd,
                CurrentDay = currentDay
            };

            _calcLess(model);

            return model;
        }

        /// <summary>
        /// Вспомогательный метод: Сопоставляет текущее время сервера с массивом звонков Lessons.
        /// </summary>
        private void _calcLess(InfoModel model)
        {
            var currentTimeStr = DateTime.Now.ToString("HH:mm:ss");
            model.Time = currentTimeStr;

            if (!TimeOnly.TryParse(currentTimeStr, out var time1)) return;

            var i1 = 1;

            // Сканируем массив расписания пар из конфигурации appsettings.json
            foreach (var lessonConfig in _options.Lessons)
            {
                if (model.CurrentLess == 0)
                {
                    if (TimeOnly.TryParse(lessonConfig.Begin, out var beginTime) &&
                        TimeOnly.TryParse(lessonConfig.End, out var endTime))
                    {
                        // Если время сервера внутри интервала пары — это текущая пара
                        if (beginTime <= time1 && endTime > time1)
                        {
                            model.CurrentLess = i1;
                            model.PastLess = 0;
                            break;
                        }

                        // Если пара уже закончилась — фиксируем её как прошедшую
                        if (endTime <= time1)
                        {
                            model.PastLess = i1;
                        }

                        // Если пара еще впереди — фиксируем как предстоящую
                        if (model.UpcomingLess == 0 && beginTime > time1)
                        {
                            model.UpcomingLess = i1;
                        }
                    }
                    ++i1;
                }
            }
        }

        /// <summary>
        /// Внутренний метод сборки расписания (Полная версия).
        /// Выполняет жадную загрузку (Include) графа связей и группирует данные по дням и номерам пар.
        /// </summary>
        private async Task<IEnumerable<DayModel>> _getRaspFullAsync(int groupAisId, int prepAisId, int chairId, int roomId)
        {
            if (groupAisId < 1 && prepAisId < 1 && chairId < 1 && roomId < 1)
                return Array.Empty<DayModel>();

            // Базовый запрос к центральному звену БД - таблице Events
            var query = _db.Events.AsNoTracking();

            // Применяем цепочку динамических LINQ-фильтров
            query = _applyEventsFilter(query, groupAisId, prepAisId, chairId, roomId);

            // Подтягиваем все справочники через наши чистые навигационные свойства Refs (Многие-ко-многим)
            var events = await query
                .Include(x => x.Disc)
                .Include(x => x.Chair)
                .Include(x => x.GroupRefs).ThenInclude(g => g.Group)
                .Include(x => x.PrepRefs).ThenInclude(p => p.Prep)
                .Include(x => x.RoomRefs).ThenInclude(r => r.Room).ThenInclude(rm => rm.Building)
                .ToListAsync();

            // Переводим данные в память и группируем: День недели -> Номер пары -> Список занятий
            return events
                .GroupBy(e => e.Day)
                .OrderBy(g => g.Key)
                .Select(gDays => new DayModel
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
        /// Внутренний метод сборки расписания (Умная версия). Отсекает архивные недели (например, Week == 1).
        /// </summary>
        private async Task<IEnumerable<DayModel>> _getRaspSmartAsync(int groupAisId, int prepAisId, int chairId, int roomId)
        {
            if (groupAisId < 1 && prepAisId < 1 && chairId < 1 && roomId < 1)
                return Array.Empty<DayModel>();

            var query = _db.Events.AsNoTracking().Where(x => x.Week != 1); // Бизнес-логика фильтрации недель

            query = _applyEventsFilter(query, groupAisId, prepAisId, chairId, roomId);

            var events = await query
                .Include(x => x.Disc)
                .Include(x => x.Chair)
                .Include(x => x.GroupRefs).ThenInclude(g => g.Group)
                .Include(x => x.PrepRefs).ThenInclude(p => p.Prep)
                .Include(x => x.RoomRefs).ThenInclude(r => r.Room).ThenInclude(rm => rm.Building)
                .ToListAsync();

            return events
                .GroupBy(e => e.Day)
                .OrderBy(g => g.Key)
                .Select(gDays => new DayModel
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
        /// Метод применения фильтров
        /// </summary>
        private IQueryable<Event> _applyEventsFilter(IQueryable<Event> query, int groupAisId, int prepAisId, int chairId, int roomId)
        {
            if (groupAisId > 0)
            {
                // Фильтр по группе через таблицу-линк Nhiều-ко-многим
                query = query.Where(x => x.GroupRefs.Any(g => g.Group.AisId == groupAisId.ToString()));
            }
            if (prepAisId > 0)
            {
                // Фильтр по преподавателю
                query = query.Where(x => x.PrepRefs.Any(p => p.Prep.AisId == prepAisId.ToString()));
            }
            if (chairId > 0)
            {
                // Прямой фильтр по ID кафедры
                query = query.Where(x => x.ChairId == chairId);
            }
            if (roomId > 0)
            {
                // Фильтр по ID аудитории
                query = query.Where(x => x.RoomRefs.Any(r => r.BuildingRoomPtr == roomId));
            }
            return query;
        }

        /// <summary>
        /// Распределяет пары по разным спискам четности (Четная, Нечетная, Обе недели) и подтягивает расписание звонков.
        /// </summary>
        private LessModel _getLessModel(IGrouping<int, Event> group)
        {
            var times = group.Key >= 1 && group.Key <= _options.Lessons.Count
                ? _options.Lessons[group.Key - 1]
                : null;

            var lessModel = new LessModel
            {
                Less = group.Key,
                Begin = times?.Begin,
                End = times?.End
            };

            foreach (var ev in group)
            {
                var eventModel = new EventModel
                {
                    Id = ev.Id,
                    SubgId = ev.SubgId,
                    Type = ev.Type,
                    Week = ev.Week,
                    DiscName = ev.Disc?.Name ?? "Дисциплина не указана",
                    ChairName = ev.Chair?.Name ?? "Кафедра не указана",
                    // Собираем списки групп, преподов и аудиторий в одну строку (через запятую) для фронтенда
                    Groups = string.Join(", ", ev.GroupRefs.Select(g => g.Group.Name)),
                    Preps = string.Join(", ", ev.PrepRefs.Select(p => p.Prep.FullName)),
                    Rooms = string.Join(", ", ev.RoomRefs.Select(r => $"{r.Room.Name} ({r.Room.Building.Name})"))
                };

                switch (ev.Week)
                {
                    case 1:
                        lessModel.Week1.Add(eventModel);
                        break;
                    case 2:
                        lessModel.Week2.Add(eventModel);
                        break;
                    default:
                        lessModel.WeekAll.Add(eventModel);
                        break;
                }
            }
            return lessModel;
        }
    }
}