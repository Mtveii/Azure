using AzureP33.Models;
using AzureP33.Models.Cosmos;
using AzureP33.Models.Home;
using AzureP33.Models.Orm;
using AzureP33.Services.CosmosDb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AzureP33.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICosmosBbService _cosmosDbService;
        private static LanguagesResponse? languagesResponse;


        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, ICosmosBbService cosmosDbService)
        {
            _logger = logger;
            _configuration = configuration;
            _cosmosDbService = cosmosDbService;
        }

        public async Task<IActionResult> IndexAsync(HomeIndexFormModel? formModel)
        {
            Task<LanguagesResponse> respTask = GetLanguagesAsync();

            HomeIndexViewModel viewModel = new()
            {
                PageTitle = "Перекладач",
                FormModel = formModel?.Action == null ? null : formModel,
                // LanguagesResponse = resp
            };

            if(formModel?.Action == "translate")
            {
                // Передано дані для перекладу               
                string query = $"from={formModel.LangFrom}&to={formModel.LangTo}";
                string textToTranslate = formModel.OriginalText;
                object[] body = new object[] { new { Text = textToTranslate } };
                var requestBody = JsonSerializer.Serialize(body);

                string result = await RequestApi(query, requestBody, ApiMode.Translate);
                if (result[0] == '[')
                {
                    viewModel.Items = JsonSerializer.Deserialize<List<TranslatorResponseItem>>(result);
                }
                else
                {
                    viewModel.ErrorResponse = JsonSerializer.Deserialize<TranslatorErrorResponse>(result);
                }
                // ViewData["result"] = result;   
                // [{"translations":[{"text":"Greetings","to":"en"}]}]
                // {"error":{"code":400036,"message":"The target language is not valid."}}
                // {"error":{"code":401001,"message":"The request is not authorized because credentials are missing or invalid."}}
            }

            var resp = await respTask;

            // if (formModel?.Action == "transliterate")
            if(viewModel.Items != null)   // ознака успішно виконаного перекладу
            {
                // Знаходимо мову у resp.Transliterations і беремо перший (0) скрипт
                LangData langData;
                try { 
                    langData = resp.Transliterations[formModel!.LangFrom];
                    String fromScript = langData.Scripts![0].Code!;
                    String toScript = langData.Scripts![0].ToScripts![0].Code!;

                    string query = $"language={formModel.LangFrom}&fromScript={fromScript}&toScript={toScript}";
                    var requestBody = JsonSerializer.Serialize(new object[] { 
                        new { Text = formModel.OriginalText } 
                    });
                    viewModel.FromTransliteration = JsonSerializer.Deserialize<List<TransliteratorResponseItem>>(
                        await RequestApi(query, requestBody, ApiMode.Transliterate)
                    )![0];
                    // ViewData["result"] = await RequestApi(query, requestBody, ApiMode.Transliterate);
                }
                catch { }

                try
                {
                    langData = resp.Transliterations[formModel!.LangTo];
                    String fromScript = langData.Scripts![0].Code!;
                    String toScript = langData.Scripts![0].ToScripts![0].Code!;

                    string query = $"language={formModel.LangTo}&fromScript={fromScript}&toScript={toScript}";
                    var requestBody = JsonSerializer.Serialize(new object[] {
                        new { Text = viewModel.Items[0].Translations[0].Text }
                    });
                    viewModel.ToTransliteration = JsonSerializer.Deserialize<List<TransliteratorResponseItem>>(
                        await RequestApi(query, requestBody, ApiMode.Transliterate)
                    )![0];

                }
                catch { }

            }

            viewModel.LanguagesResponse = await respTask;
            return View(viewModel);
        }

        private async Task<LanguagesResponse> GetLanguagesAsync()
        {
            if(languagesResponse == null)
            {
                using HttpClient client = new();

                languagesResponse = JsonSerializer.Deserialize<LanguagesResponse>(
                    await client.GetStringAsync(
                        @"https://api.cognitive.microsofttranslator.com/languages?api-version=3.0"
                    )
                );
                if (languagesResponse == null)
                {
                    throw new Exception("LanguagesResponse got NULL result");
                }
            }
            return languagesResponse;
        }

        private async Task<String> RequestTranslationAsync(HomeIndexFormModel formModel)
        {           
            string query = $"from={formModel.LangFrom}&to={formModel.LangTo}";
            string textToTranslate = formModel.OriginalText;
            object[] body = new object[] { new { Text = textToTranslate } };
            var requestBody = JsonSerializer.Serialize(body);

            String result = await RequestApi(query, requestBody, ApiMode.Translate);

            if (result[0] == '[')
            {
                return JsonSerializer.Deserialize<List<TranslatorResponseItem>>(result)![0].Translations[0].Text;
            }
            else
            {
                throw new Exception( JsonSerializer.Deserialize<TranslatorErrorResponse>(result)!.Error.Message );
            }
        }

        private async Task<String> RequestApi(String query, String body, ApiMode apiMode)
        {
            var sec = _configuration.GetSection("Azure")?.GetSection("Translator") ?? throw new Exception("Configuration error: Azure.Translator is null");
            String key = sec.GetValue<String>("Key") ?? throw new Exception("Configuration error: 'Key' is null");
            String endpoint = sec.GetValue<String>("Endpoint") ?? throw new Exception("Configuration error: 'Endpoint' is null");
            String location = sec.GetValue<String>("Location") ?? throw new Exception("Configuration error: 'Location' is null");
            String apiVersion = sec.GetValue<String>("ApiVersion") ?? throw new Exception("Configuration error: 'ApiVersion' is null");
            String apiPath = apiMode switch { 
                ApiMode.Translate => sec.GetValue<String>("TranslatorPath"),
                ApiMode.Transliterate => sec.GetValue<String>("TransliteratorPath"),
                _ => null
            } ?? throw new Exception("Configuration error: 'apiPath' is null");

            using (var client2 = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri($"{endpoint}{apiPath}?api-version={apiVersion}&{query}");
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                request.Headers.Add("Ocp-Apim-Subscription-Region", location);
                HttpResponseMessage response = await client2.SendAsync(request).ConfigureAwait(false);
                string result = await response.Content.ReadAsStringAsync();
                return result;
            }
        }

        [HttpGet]
        public async Task<JsonResult> FetchTranslationAsync(HomeIndexFormModel formModel)
        {
            // Перевірити, що модель правильно заповнена:
            // - мови є у переліку languagesResponse
            // - action зазначений як fetch
            // - текст є і має довжину більше нуля (після видалення пробілів)
            // у разі неправильних даних формуємо відповідь з текстом помилки та статусом 400
            LanguagesResponse resp = await GetLanguagesAsync();
            if( ! resp.Translations.ContainsKey(formModel.LangFrom))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return Json($"LangFrom '{formModel.LangFrom}' unsupported");
            }
            if( ! resp.Translations.ContainsKey(formModel.LangTo))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return Json($"LangTo '{formModel.LangTo}' unsupported");
            }
            if(formModel.Action != "fetch")
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return Json($"Action '{formModel.Action}' unsupported");
            }
            if(String.IsNullOrEmpty(formModel.OriginalText))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return Json($"Text must not be empty");
            }
            
            try
            {
                return Json(await RequestTranslationAsync(formModel));
            }
            catch (Exception ex)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Json(ex.Message);
            }
        }

        public async Task<IActionResult> CosmosAddAsync([FromForm] HomeCosmosAddFormModel? formModel)
        {
            if(formModel?.Action == "Create")
            {
                if(String.IsNullOrEmpty(formModel.Name) 
                || String.IsNullOrEmpty(formModel.Email))
                {
                    ViewData["result"] = "Заповніть усі поля";
                }
                else
                {
                    Container container = await _cosmosDbService.GetContainerAsync();
                    Models.Cosmos.User user = new()
                    {
                        Id = Guid.NewGuid(),
                        Name = formModel.Name,
                        Email = formModel.Email,
                    };

                    ItemResponse<Models.Cosmos.User> response = await container.UpsertItemAsync<Models.Cosmos.User>(
                        item: user,
                        partitionKey: new PartitionKey(Models.Cosmos.User.PartitionKey)
                    );

                    ViewData["result"] = $"Upserted item: {response.Resource}, Status code: {response.StatusCode}, Request charge: {response.RequestCharge:0.00}";
                }
            }
            return View();
        }

        /* Завдання
         * Збереження у БД історії перекладів (використання перекладача)
         * - Зробити запобіжник (чекбокс) на сторінці з перекладачем, який 
         *    включає (або виключає) переклад виділенням. [за замовчанням - вимкнений]
         * - Розробити моделі для збереження даних у БД з урахуванням
         *    дата-час, ід користувача (опціонально), оригінальний текст та мова,
         *    перекладений текст та мова, обидві транслітерації (за наявності) з позначками
         *    скриптів, вжитих для транслітерації
         * - Реалізувати передачу до БД усіх успішних звернень до перекладача
         * - Реалізувати завантаження історії перекладів (на вибір)
         *    = або при завантаженні сторінки (перекладача)
         *    = або за натисканням кнопки
         */

        public async Task<IActionResult> CosmosAsync()
        {            
            Container container = await _cosmosDbService.GetContainerAsync();

            var query = new QueryDefinition(
                query: "SELECT * FROM c WHERE c.categoryId = @category"
            ).WithParameter("@category", "26C74104-40BC-4541-8EF5-9892F7F03D72");

            using FeedIterator<Product> feed = container.GetItemQueryIterator<Product>(
                queryDefinition: query
            );

            List<Product> items = new();
            double requestCharge = 0d;
            while (feed.HasMoreResults)
            {
                FeedResponse<Product> response = await feed.ReadNextAsync();
                foreach (Product item in response)
                {
                    items.Add(item);
                }
                requestCharge += response.RequestCharge;
            }

            return View(new HomeCosmosViewModel
            {
                Products = items,
                RequestCharge = requestCharge,
            });
            /*
             * Д.З. Виконати код підключення до БД з домашніх ПК,
             * дослідити можливість встановлення з'єднання та 
             * вибірки даних.
             * Прикласти посилання на сторінку вашого сайту.
             */
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


    enum ApiMode   // https://learn.microsoft.com/en-us/azure/ai-services/translator/text-translation/reference/rest-api-guide?WT.mc_id=Portal-Microsoft_Azure_ProjectOxford
    {
        Translate,
        Transliterate,
    }
}
/* Д.З. Вилучити результат транслітерації з JSON відповіді, вивести його у полі для перекладу
 * Здійснити публікацію (оновлення) проєкту 
 * Прикласти посилання на сайт (***.azurewebsites.net)
 */