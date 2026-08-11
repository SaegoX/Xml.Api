using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

namespace Xml.Api.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Upload(IFormFile xmlFile)
        {
            try
            {
                if (xmlFile == null || xmlFile.Length == 0)
                {
                    ViewBag.Message = "Файл не выбран";
                    return View("Index");
                }

                var extension = Path.GetExtension(xmlFile.FileName).ToLowerInvariant();
                if (extension != ".xml")
                {
                    ViewBag.Message = "Разрешён только формат .xml";
                    return View("Index");
                }

                try
                {
                    using var stream = xmlFile.OpenReadStream();
                    XDocument.Load(stream);
                }
                catch
                {
                    ViewBag.Message = "Файл не является корректным XML";
                    return View("Index");
                }

                var folderPath = @"C:\TEMP\rasp";
                Directory.CreateDirectory(folderPath);

                DateTime now = DateTime.Now;
                string year = now.ToString("yyyy");
                string month = now.Month.ToString("D3");
                string day = now.ToString("dd");
                string time = now.ToString("HH-mm-ss");

                string fileName = $"rasp_{year}-{month}-{day}_{time}.xml";
                string fullPath = Path.Combine(folderPath, fileName);

                try
                {
                    using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
                    xmlFile.CopyTo(fileStream);
                }
                catch (UnauthorizedAccessException)
                {
                    ViewBag.Message = "Нет прав на запись в C:\\TEMP\\rasp. Настройте права в ОС или выберите другую папку.";
                    return View("Index");
                }
                catch (Exception ex)
                {
                    ViewBag.Message = $"Ошибка при сохранении файла: {ex.Message}";
                    return View("Index");
                }

                ViewBag.Message = "Файл успешно сохранён";
                return View("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Произошла ошибка: {ex.GetType().Name} — {ex.Message}";
                return View("Index");

            }
        }
    }
}
