using Microsoft.AspNetCore.Mvc;
using Xml.Api.Features.ScheduleUpload.Infrastructure;

namespace Xml.Api.Controllers
{
    /// <summary>
    /// Контроллер управления веб-интерфейсом загрузки расписания.
    /// </summary>
    /// <param name="env">Окружение веб-хостинга, предоставляет доступ к путям файловой системы (ContentRootPath, WebRootPath)</param>
    /// <param name="queue">Системная фоновая очередь для постановки задач на импорт</param>
    /// <param name="progressTracker">Потокобезопасный трекер для проверки статусов из оперативной памяти</param>
    public class HomeController(
		IWebHostEnvironment env,
        BackgroundQueue queue,
        UploadProgressTracker progressTracker)
		: Controller
	{

		private readonly IWebHostEnvironment _env = env;
        private readonly BackgroundQueue _queue = queue;
        private readonly UploadProgressTracker _progressTracker = progressTracker;


        private const long _maxFilesize = 10 * 1024 * 1024; // 10 МБ
        private readonly string _basePath = "C:\\TEMP\\rasp";

		/// <summary>
		/// Заглушка, для базового отображения стартовой страницы(без неё невозможен переход на наше приложение)
		/// </summary>
		[HttpGet]
		public IActionResult Index()
		{
			return View();
		}

        /// <summary>
        /// Асинхронный эндпоинт для загрузки и обработки файла.
        /// Возвращает JSON-результат, для возможности отображения прогресса загрузки
        /// </summary>
        /// <param name="xmlFile"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile? xmlFile)
        {
            if (xmlFile == null || xmlFile.Length == 0)
            {
                return BadRequest(new { message = "Файл не выбран или пуст" });
            }

            var extension = Path.GetExtension(xmlFile.FileName);
            if (!string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Разрешён только формат .xml" });
            }

            if (xmlFile.Length > _maxFilesize)
            {
                return BadRequest(new { message = "Файл слишком большой (максимум 10 МБ)" });
            }

            string jobId = Guid.NewGuid().ToString();

            var path1 = _basePath;
			var filename1 = $"rasp_{DateTime.Now:yyyy-0MM-dd_HH-mm-ss}.xml";
			var fullpath1 = Path.Combine(path1, filename1);

			try
			{
				Directory.CreateDirectory(path1);

                using (var stream = new FileStream(
                    fullpath1, 
                    FileMode.Create, 
                    FileAccess.Write, 
                    FileShare.None, 
                    4096, 
                    useAsync: true))
                {
                    await xmlFile.CopyToAsync(stream);
                }

                // Инициализируем статус в оперативной памяти сервера
                _progressTracker.UpdateProgress(jobId, "Файл успешно загружен. Задача поставлена в фоновую очередь...", 0);

                // Ставим задачу в асинхронную шину воркера
                await _queue.EnqueueAsync(new ScheduleJobTask(jobId, fullpath1));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(500, new { message = $"Нет прав на запись в папку {path1}. Настройте права доступа." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ошибка при подготовке импорта: {ex.Message}" });
            }

            // МГНОВЕННЫЙ ОТВЕТ: Возвращаем токен задачи, освобождая поток контроллера!
            return Ok(new { jobId });

        }

        /// <summary>
        /// Эндпоинт опроса текущего статуса задачи (для восстановления интерфейса при обновлении страницы).
        /// </summary>
        /// <param name="jobId">Уникальный ID задачи.</param>
        [HttpGet("get-upload-status")]
        public IActionResult GetStatus(string jobId)
        {
            var progress = _progressTracker.GetProgress(jobId);
            if (progress == null)
            {
                return NotFound(new { message = "Задача не найдена или уже удалена из памяти" });
            }

            return Ok(progress);
        }
    }

}
