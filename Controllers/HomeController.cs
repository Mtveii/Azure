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
            if (viewModel.Items != null && formModel.Action == "translate")
            {
                // Тут можна додати перевірку чекбокса (запобіжника)
                await SaveTranslationToHistoryAsync(
                    formModel,
                    viewModel.Items[0].Translations[0].Text,
                    viewModel.FromTransliteration,
                    viewModel.ToTransliteration
                );
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

        public async Task<IActionResult> CosmosAsync([FromQuery] Guid[]? selectedCategoryIds)
        {            
            Container container = await _cosmosDbService.GetContainerAsync();

            // --- Получаем доступные категории (categoryId + categoryName) ---
            var catQuery = new QueryDefinition("SELECT c.categoryId, c.categoryName FROM c");
            using FeedIterator<Category> catFeed = container.GetItemQueryIterator<Category>(catQuery);

            List<Category> categories = new();
            while (catFeed.HasMoreResults)
            {
                FeedResponse<Category> resp = await catFeed.ReadNextAsync();
                foreach (var c in resp)
                {
                    categories.Add(c);
                }
            }
            // Унікальні категорії по categoryId
            categories = categories
                .GroupBy(c => c.categoryId)
                .Select(g => g.First())
                .OrderBy(c => c.categoryName)
                .ToList();

            // --- Формируем запрос продуктов в зависимости от выбранных категорий ---
            QueryDefinition prodQuery;
            if (selectedCategoryIds != null && selectedCategoryIds.Length > 0)
            {
                var sb = new StringBuilder();
                sb.Append("SELECT * FROM c WHERE ");
                for (int i = 0; i < selectedCategoryIds.Length; i++)
                {
                    if (i > 0) sb.Append(" OR ");
                    sb.Append($"c.categoryId = @id{i}");
                }

                prodQuery = new QueryDefinition(sb.ToString());
                for (int i = 0; i < selectedCategoryIds.Length; i++)
                {
                    // сохраняем как строку, т.к. в БД поле хранится как строковий GUID
                    prodQuery = prodQuery.WithParameter($"@id{i}", selectedCategoryIds[i].ToString());
                }
            }
            else
            {
                prodQuery = new QueryDefinition("SELECT * FROM c");
            }

            using FeedIterator<Product> feed = container.GetItemQueryIterator<Product>(prodQuery);

            List<Product> items = new();
            double requestCharge = 0d;
            while (feed.HasMoreResults)
            {
                FeedResponse<Product> response = await feed.ReadNextAsync();
                items.AddRange(response);
                requestCharge += response.RequestCharge;
            }

            return View(new HomeCosmosViewModel
            {
                Products = items,
                RequestCharge = requestCharge,
                AvailableCategories = categories,
                SelectedCategoryIds = selectedCategoryIds ?? Array.Empty<Guid>()
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


        private async Task SaveTranslationToHistoryAsync(
            HomeIndexFormModel formModel,
            string translatedText,
            TransliteratorResponseItem? fromTrans,
            TransliteratorResponseItem? toTrans)
        {
            // 1. Використовуємо модель User (наприклад, анонімний або системний користувач)
            var currentUser = new AzureP33.Models.Cosmos.User { Id = Guid.NewGuid(), Name = "Guest" };

            // 2. Використовуємо модель Category для позначення типу запису в БД
            var historyCategory = new Category
            {
                categoryId = Guid.NewGuid(),
                categoryName = "TranslationHistory"
            };

            // 3. Формуємо об'єкти транслітерації (модель Trasliteration)
            Trasliteration? fromT = fromTrans == null ? null : new Trasliteration
            {
                fromSrcipt = "Latn", // Приклад. У реальному коді брати з LangData
                toSrcipt = "Cyrl",
                result = fromTrans.Text
            };

            Trasliteration? toT = toTrans == null ? null : new Trasliteration
            {
                fromSrcipt = "Cyrl",
                toSrcipt = "Latn",
                result = toTrans.Text
            };

            // 4. Формуємо моделі trans_from та trans_to
            var fromData = new trans_from
            {
                lang = formModel.LangFrom,
                text = formModel.OriginalText,
                trasliteration = fromT!
            };

            var toData = new trans_to
            {
                lang = formModel.LangTo,
                text = translatedText,
                trasliteration = toT!
            };

            // 5. Створюємо головний об'єкт історії (HistoryTranslate)
            var historyEntry = new HistoryTranslate
            {
                categoryId = historyCategory.categoryId.ToString(), // Використовуємо ID категорії
                userId = currentUser.Id, // Використовуємо ID користувача
                time = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                from = fromData,
                to = toData
            };

            // Додатково: приклад використання моделі Product (якщо переклад стосується товару)
            // Можна додати метадані, що цей переклад зроблено для перегляду списку Product
            var contextProduct = new Product { name = "Translation Context" };

            // 6. Запис у Cosmos DB
            Container container = await _cosmosDbService.GetContainerAsync();
            await container.CreateItemAsync(historyEntry, new PartitionKey(historyEntry.categoryId));
        }


        public async Task<List<HistoryTranslate>> GetTranslationHistoryAsync()
        {
            Container container = await _cosmosDbService.GetContainerAsync();

            // Шукаємо записи за категорією історії
            var query = new QueryDefinition("SELECT * FROM c WHERE IS_DEFINED(c.from) AND IS_DEFINED(c.to)");
            using FeedIterator<HistoryTranslate> iterator = container.GetItemQueryIterator<HistoryTranslate>(query);

            List<HistoryTranslate> history = new();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                history.AddRange(response);
            }
            return history;
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