using Xml.Api.Data;
using Xml.Api.Services.Modules;

namespace Xml.Api.Services
{
    public class XmlScheduleParserService
    {
        private readonly ScheduleReader _reader;
        private readonly ScheduleMapper _mapper;
        private readonly ScheduleSaver _saver;

        public XmlScheduleParserService(AppDbContext context)
        {
            _reader = new ScheduleReader();
            _mapper = new ScheduleMapper();
            _saver = new ScheduleSaver(context);
        }

        public async Task ParseAndSaveAsync(string filePath)
        {
            var dtos = await _reader.ReadAsync(filePath);
            var (chairs, discs, groups, preps, buildings, rooms, rawDtos) = _mapper.Map(dtos);
            await _saver.SaveAsync(chairs, discs, groups, preps, buildings, rooms, rawDtos);
        }
    }

}