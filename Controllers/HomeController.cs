using Microsoft.AspNetCore.Mvc;
using System.Xml;
using Xml.Api.Features.ScheduleUpload;

namespace Xml.Api.Controllers
{
    /// <summary>
    /// Контроллер управления веб-интерфейсом загрузки расписания.
    /// </summary>
    /// <param name="env">Окружение веб-хостинга, предоставляет доступ к путям файловой системы (ContentRootPath, WebRootPath)</param>
    /// <param name="parserService">Сервис парсинга XML-расписания. Отвечает за чтение, валидацию и преобразование загруженного XML-файла в структурированные DTO и справочники</param>
    public class HomeController(
		IWebHostEnvironment env,
		XmlScheduleParserService parserService)
		: Controller
	{

		private readonly IWebHostEnvironment _env = env;
		private readonly XmlScheduleParserService _parserService = parserService;


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
		public async Task<IActionResult> Upload(
			IFormFile? xmlFile)
		{
			if (xmlFile == null || xmlFile.Length == 0)
			{
				return BadRequest(new { message = "Файл не выбран или пуст" });
			}

			var extension1 = Path.GetExtension(xmlFile.FileName);
			if (!string.Equals(extension1, ".xml", StringComparison.OrdinalIgnoreCase))
			{
				return BadRequest(new { message = "Разрешён только формат .xml" });
			}

			if (xmlFile.Length > _maxFilesize)
			{
				return BadRequest(new { message = "Файл слишком большой (максимум 10 МБ)" });
			}

			var path1 = _basePath;
			var filename1 = $"rasp_{DateTime.Now:yyyy-0MM-dd_HH-mm-ss}.xml";
			var fullpath1 = Path.Combine(path1, filename1);

			try
			{
				Directory.CreateDirectory(path1);

                // Сохраняем файл на диск
                using (var stream1 = new FileStream(
					fullpath1,
					FileMode.Create,
					FileAccess.Write,
					FileShare.None,
					4096,
					useAsync: true))
				{
					await xmlFile.CopyToAsync(stream1);
				}

                // Запускаем парсинг и сохранение в Postgres
                await _parserService.ParseAndSaveAsync(fullpath1);

			}
			catch (XmlException)
			{
				if (System.IO.File.Exists(fullpath1))
					System.IO.File.Delete(fullpath1);
                return BadRequest(new { message = "Файл не является корректным XML" });
			}
			catch (UnauthorizedAccessException)
			{
                return StatusCode(500, new { message = $"Нет прав на запись в папку {path1}. Настройте права доступа." });
			}
			catch (Exception ex)
			{
                return StatusCode(500, new { message = $"Ошибка при сохранении: {ex.Message}" });
			}

            // Возвращаем успешный JSON-статус
            return Ok(new { message = "Расписание успешно импортировано в базу данных!" });
        }


		private ViewResult ReturnWithError(
			string message)
		{
			ViewBag.Message = message;
			return View("Index");
		}

	}

}
