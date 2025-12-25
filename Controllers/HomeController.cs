using Azure.Models.Orm;
using AzureP33.Models;
using AzureP33.Models.Home;
using AzureP33.Models.Orm;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AzureP33.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IActionResult> IndexAsync(HomeIndexFormModel? formModel)
        {
            using HttpClient client = new();

            var resp = JsonSerializer.Deserialize<languagesRespons>(
                await client.GetStringAsync(
                    @"https://api.translator.azure.cn/languages?api-version=3.0"
                )
            );
            if (resp == null)
            {
                throw new Exception("LanguagesResponse got NULL result");
            }
            HomeIndexViewModel viewModel = new()
            {
                PageTitle = "Перекладач",
                FormModel = formModel?.Action == null ? null : formModel,
                Languages = resp
            };

            if (formModel?.Action == "translate")
            {
                // Якщо дані не валідні — не виконувати переклад, повернути помилки в форму
                if (!ModelState.IsValid)
                {
                    // FormModel вже встановлено раніше, але гарантуємо що передамо його у ViewModel
                    viewModel.FormModel = formModel;
                    return View(viewModel);
                }

                // Передано дані для перекладу
                var sec = _configuration.GetSection("Azure")?.GetSection("Translator");
                if (sec == null)
                {
                    throw new Exception("Configuration error");
                }
                String key = sec.GetValue<String>("Key");
                String endpoint = sec.GetValue<String>("Endpoint");
                String location = sec.GetValue<String>("Location");
                String translatorPath = sec.GetValue<String>("TranslatorPath");
                String apiVersion = sec.GetValue<String>("ApiVersion");

                string route = $"{translatorPath}?api-version={apiVersion}&from={formModel.LangFrom}&to={formModel.LangTo}";
                string textToTranslate = formModel.OriginalText;
                object[] body = new object[] { new { Text = textToTranslate } };
                var requestBody = JsonSerializer.Serialize(body);

                using (var client2 = new HttpClient())
                using (var request = new HttpRequestMessage())
                {
                    // Build the request.
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(endpoint + route);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                    // location required if you're using a multi-service or regional (not global) resource.
                    request.Headers.Add("Ocp-Apim-Subscription-Region", location);

                    // Send the request and get response.
                    HttpResponseMessage response = await client2.SendAsync(request).ConfigureAwait(false);
                    // Read response as a string.
                    string result = await response.Content.ReadAsStringAsync();
                    if (result[0] == '[')
                    {
                        viewModel.Item = JsonSerializer.Deserialize<List<TranslatorResponseltem>>(result);
                    }
                    else
                    {
                        viewModel.ErrorResponse = JsonSerializer.Deserialize<translatorErrorResponse>(result);
                    }
                    ViewData["result"] = result;
                }
            }

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
/* Реалізувати валідацію даних форми перекладача:
 * Якщо є передача даних, але немає введеного тексту - видавати повідомлення про помилку
 * Якщо немає передачі даних - нічого не виводити, початкова форма вважається валідною
 */