using Microsoft.AspNetCore.Mvc;
using System.Xml;

namespace Xml.Api.Controllers
{

	public class HomeController(
		IWebHostEnvironment env)
		: Controller
	{

		private readonly IWebHostEnvironment _env = env;
		private const long _maxFilesize = 10 * 1024 * 1024; // 10 МБ для защиты от DOS
		private readonly string _basePath = "C:\\TEMP\\rasp";


		[HttpGet]
		public IActionResult Index()
		{
			return View();
		}


		[HttpPost]
		public async Task<IActionResult> Upload(
			IFormFile? xmlFile)
		{
			if (xmlFile == null || xmlFile.Length == 0)
			{
				return ReturnWithError("Файл не выбран");
			}

			var extension1 = Path.GetExtension(xmlFile.FileName);
			if (!string.Equals(extension1, ".xml", StringComparison.OrdinalIgnoreCase))
			{
				return ReturnWithError("Разрешён только формат .xml");
			}

			if (xmlFile.Length > _maxFilesize)
			{
				return ReturnWithError("Файл слишком большой (макс. 10 МБ)");
			}

			var path1 = _basePath;
			var filename1 = $"rasp_{DateTime.Now:yyyy-0MM-dd_HH-mm-ss}.xml";
			var fullpath1 = Path.Combine(path1, filename1);

			try
			{
				Directory.CreateDirectory(path1);

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

				using (var stream1 = new FileStream(
					fullpath1,
					FileMode.Open,
					FileAccess.Read,
					FileShare.Read,
					4096,
					useAsync: true))
				{
					var settings1 = new XmlReaderSettings
					{
						Async = true,
						DtdProcessing = DtdProcessing.Prohibit
					};
					using var reader1 = XmlReader.Create(
						stream1, settings1);
					while (await reader1.ReadAsync())
					{
					}
				}
			}
			catch (XmlException)
			{
				if (System.IO.File.Exists(fullpath1))
					System.IO.File.Delete(fullpath1);
				return ReturnWithError("Файл не является корректным XML");
			}
			catch (UnauthorizedAccessException)
			{
				return ReturnWithError($"Нет прав на запись в папку {path1}. Настройте права доступа.");
			}
			catch (Exception ex)
			{
				return ReturnWithError($"Ошибка при сохранении: {ex.Message}");
			}

			ViewBag.Message = "Файл успешно сохранён";
			return View("Index");
		}


		private ViewResult ReturnWithError(
			string message)
		{
			ViewBag.Message = message;
			return View("Index");
		}

	}

}
