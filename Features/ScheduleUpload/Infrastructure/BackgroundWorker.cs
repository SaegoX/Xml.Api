namespace Xml.Api.Features.ScheduleUpload.Infrastructure
{
    /// <summary>
    /// Фоновая служба
    /// Бесконечно слушает очередь задач в фоновом потоке сервера и поочередно обрабатывает XML-файлы.
    /// </summary>
    public class BackgroundWorker : BackgroundService
    {
        private readonly BackgroundQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BackgroundWorker> _logger;

        public BackgroundWorker(
            BackgroundQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<BackgroundWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Фоновая служба обработки расписания успешно запущена и слушает очередь задач...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    ScheduleJobTask task = await _queue.DequeueAsync(stoppingToken);

                    _logger.LogInformation("Фоновый поток извлек задачу {JobId}. Начинается обработка файла...", task.JobId);

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var parserService = scope.ServiceProvider.GetRequiredService<ImportOrchestrator>();

                        bool isSuccess = await parserService.ParseAndSaveAsync(task.FilePath, task.JobId, stoppingToken);

                        if (isSuccess)
                        {
                            _logger.LogInformation("Фоновая задача {JobId} успешно обработана и закоммичена в БД.", task.JobId);
                        }
                        else
                        {
                            _logger.LogWarning("Фоновая задача {JobId} была прервана из-за ошибки или отмены пользователем.", task.JobId);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Фоновая служба обработки была остановлена рантаймом системы.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Произошел критический сбой в фоновом потоке воркера при обработке задачи.");
                }
            }
        }
    }
}
