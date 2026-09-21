using System.Collections.Concurrent;

namespace Xml.Api.Features.ScheduleUpload.Infrastructure
{
    /// <summary>
    /// Состояние текущего процесса импорта.
    /// </summary>
    public record ProgressState(string Status, int Percent, bool IsCompleted, string? ErrorMessage);

    /// <summary>
    /// Трекер прогресса, хранящий текущие статусы импорта в оперативной памяти сервера.
    /// </summary>
    public class UploadProgressTracker
    {
        private readonly ConcurrentDictionary<string, ProgressState> _states = new();

        /// <summary>
        /// Обновляет или добавляет статус задачи.
        /// </summary>
        public void UpdateProgress(string jobId, string status, int percent, bool isCompleted = false, string? errorMessage = null)
        {
            _states[jobId] = new ProgressState(status, percent, isCompleted, errorMessage);
        }

        /// <summary>
        /// Возвращает текущее состояние задачи. Если задачи нет, возвращает null.
        /// </summary>
        public ProgressState? GetProgress(string jobId)
        {
            return _states.TryGetValue(jobId, out var state) ? state : null;
        }

        /// <summary>
        /// Удаляет задачу из памяти после завершения, чтобы не забивать RAM.
        /// </summary>
        public void RemoveProgress(string jobId)
        {
            _states.TryRemove(jobId, out _);
        }
    }
}
