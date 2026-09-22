using System.Threading.Channels;

namespace Xml.Api.Features.ScheduleUpload.Infrastructure
{
    /// <summary>
    /// Модель задачи для передачи в фоновую очередь.
    /// </summary>
    public record ScheduleJobTask(string JobId, string FilePath);

    /// <summary>
    /// Потокобезопасная очередь фоновых задач на основе каналов
    /// Обработка файлов происходит в порядке очереди
    /// </summary>
    public class BackgroundQueue
    {
        // Создаем неограниченный канал(очередь) для задач
        private readonly Channel<ScheduleJobTask> _channel = Channel.CreateUnbounded<ScheduleJobTask>(new UnboundedChannelOptions
        {
            SingleReader = true, // Одиночное чтение от воркера(однопоточный)
            SingleWriter = false // Записываться в очередь может больше одной задачи
        });

        /// <summary>
        /// Ставит задачу на импорт файла в фоновую очередь.
        /// </summary>
        public async ValueTask EnqueueAsync(ScheduleJobTask task)
        {
            ArgumentNullException.ThrowIfNull(task);
            await _channel.Writer.WriteAsync(task);
        }

        /// <summary>
        /// Извлекает задачу из очереди. Если очередь пуста, поток асинхронно засыпает в ожидании.
        /// </summary>
        public async ValueTask<ScheduleJobTask> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
