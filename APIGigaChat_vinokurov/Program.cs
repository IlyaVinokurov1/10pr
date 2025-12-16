using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace APIGigaChat_vinokurov
{
    internal class Program
    {
        // GigaChat конфигурация
        static string ClientId = "019b287d-4c6f-7695-97bd-095b75ac26a5";
        static string AuthorizationKey = "MDE5YjI4N2QtNGM2Zi03Njk1LTk3YmQtMDk1Yjc1YWMyNmE1OmY5M2M4N2Q0LTJkNTgtNGIwNC05NmMxLTI0YzljNWMzOTM5Yw==";

        // YandexGPT конфигурация
        static string YandexApiKey = "***";
        static string YandexFolderId = "***";
        // История сообщений для каждого провайдера
        static List<GigaMessage> gigaHistory = new List<GigaMessage>();
        static List<YandexMessage> yandexHistory = new List<YandexMessage>();

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("Выберите провайдера AI:");
            Console.WriteLine("1. GigaChat");
            Console.WriteLine("2. YandexGPT");
            Console.Write("Ваш выбор (1 или 2): ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await RunGigaChat();
                    break;
                case "2":
                    await RunYandexGPT();
                    break;
                default:
                    Console.WriteLine("Неверный выбор. Завершение программы.");
                    break;
            }
        }

        static async Task RunGigaChat()
        {
            Console.WriteLine("\nЗапуск GigaChat. Введите 'exit' для выхода.\n");

            // Получаем токен для GigaChat
            string token = await GetGigaChatToken(ClientId, AuthorizationKey);
            if (token == null)
            {
                Console.WriteLine("Не удалось получить токен для GigaChat");
                return;
            }

            // Добавляем системное сообщение
            gigaHistory.Add(new GigaMessage { role = "system", content = "Ты полезный ассистент." });

            while (true)
            {
                Console.Write("Вы: ");
                string input = Console.ReadLine();

                if (input.ToLower() == "exit")
                    break;

                gigaHistory.Add(new GigaMessage { role = "user", content = input });

                string reply = await SendToGigaChat(token, gigaHistory);

                if (reply != null)
                {
                    Console.WriteLine("Бот: " + reply);
                    gigaHistory.Add(new GigaMessage { role = "assistant", content = reply });
                }
                else
                {
                    Console.WriteLine("Ошибка получения ответа от GigaChat.\n");
                }
            }
        }

        static async Task RunYandexGPT()
        {
            Console.WriteLine("\nЗапуск YandexGPT. Введите 'exit' для выхода.\n");

            // Добавляем системное сообщение
            yandexHistory.Add(new YandexMessage { role = "system", text = "Ты полезный ассистент." });

            while (true)
            {
                Console.Write("Вы: ");
                string input = Console.ReadLine();

                if (input.ToLower() == "exit")
                    break;

                yandexHistory.Add(new YandexMessage { role = "user", text = input });

                string reply = await SendToYandexGPT(yandexHistory);

                if (reply != null)
                {
                    Console.WriteLine("Бот: " + reply);
                    yandexHistory.Add(new YandexMessage { role = "assistant", text = reply });
                }
                else
                {
                    Console.WriteLine("Ошибка получения ответа от YandexGPT.\n");
                }
            }
        }

        // Методы для GigaChat
        static async Task<string> GetGigaChatToken(string rqUID, string bearer)
        {
            string returnToken = null;
            string url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyError) => true;
                using (HttpClient client = new HttpClient(handler))
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Add("Accept", "application/json");
                    request.Headers.Add("RqUID", rqUID);
                    request.Headers.Add("Authorization", $"Bearer {bearer}");

                    var data = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
                    };
                    request.Content = new FormUrlEncodedContent(data);

                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        GigaTokenResponse tokenResponse = JsonConvert.DeserializeObject<GigaTokenResponse>(responseContent);
                        returnToken = tokenResponse.access_token;
                    }
                }
            }
            return returnToken;
        }

        static async Task<string> SendToGigaChat(string token, List<GigaMessage> history)
        {
            string url = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
                using (HttpClient client = new HttpClient(handler))
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Add("Accept", "application/json");
                    request.Headers.Add("Authorization", $"Bearer {token}");

                    GigaRequest gigaRequest = new GigaRequest
                    {
                        model = "GigaChat",
                        stream = false,
                        repetition_penalty = 1,
                        messages = history
                    };

                    string json = JsonConvert.SerializeObject(gigaRequest);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    request.Content = content;

                    HttpResponseMessage response = await client.SendAsync(request);
                    string responseText = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Ошибка GigaChat API: {response.StatusCode}");
                        Console.WriteLine(responseText);
                        return null;
                    }

                    GigaResponse result = JsonConvert.DeserializeObject<GigaResponse>(responseText);
                    return result?.choices?[0]?.message?.content;
                }
            }
        }

        // Методы для YandexGPT
        static async Task<string> SendToYandexGPT(List<YandexMessage> history)
        {
            var url = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Api-Key", YandexApiKey);

                var request = new YandexRequest
                {
                    modelUri = $"gpt://{YandexFolderId}/yandexgpt-lite",
                    completionOptions = new CompletionOptions { stream = false },
                    messages = history
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Ошибка YandexGPT API: {response.StatusCode}");
                    Console.WriteLine(responseText);
                    return null;
                }

                var result = JsonConvert.DeserializeObject<YandexResponse>(responseText);
                return result?.result?.alternatives?[0]?.message?.text;
            }
        }
    }

    // Классы для GigaChat
    public class GigaMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class GigaRequest
    {
        public string model { get; set; }
        public List<GigaMessage> messages { get; set; }
        public bool stream { get; set; }
        public int repetition_penalty { get; set; }
    }

    public class GigaResponse
    {
        public List<GigaChoice> choices { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public string @object { get; set; }
        public GigaUsage usage { get; set; }

        public class GigaUsage
        {
            public int completion_tokens { get; set; }
            public int prompt_tokens { get; set; }
            public int total_tokens { get; set; }
        }

        public class GigaChoice
        {
            public string finish_reason { get; set; }
            public int index { get; set; }
            public GigaMessage message { get; set; }
        }
    }

    public class GigaTokenResponse
    {
        public string access_token { get; set; }
        public string expires_at { get; set; }
    }

    // Классы для YandexGPT (уже у вас есть)
    public class YandexMessage
    {
        public string role { get; set; }
        public string text { get; set; }
    }

    public class CompletionOptions
    {
        public bool stream { get; set; }
    }

    public class YandexRequest
    {
        public string modelUri { get; set; }
        public CompletionOptions completionOptions { get; set; }
        public List<YandexMessage> messages { get; set; }
    }

    public class YandexResponse
    {
        public ResponseResult result { get; set; }
    }

    public class ResponseResult
    {
        public List<Alternative> alternatives { get; set; }
    }

    public class Alternative
    {
        public YandexMessage message { get; set; }
    }
}