using Microsoft.AspNetCore.SignalR;

namespace Xml.Api.Features.ScheduleUpload.Infrastructure
{
    /// <summary>
    /// SignalR-хаб для трансляции статуса фонового импорта расписания в реальном времени
    /// </summary>
    public class ScheduleHub : Hub
    {
        /// <summary>
        /// Подключение клиента к группе отслеживания конкретной фоновой задачи
        /// </summary>
        /// <param name="jobId">Уникальный идентификатор фоновой задачи импорта</param>
        public async Task SubscribeToJob(string jobId)
        {
            if (!string.IsNullOrEmpty(jobId))
            {
                // Фича signalR, теперь сеанс привязывается к id и по нему можно вернуться
                await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
            }
        }
    }
}
