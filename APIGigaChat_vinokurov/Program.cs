using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using APIGigaChat_vinokurov.Models.Response;

namespace APIGigaChat_vinokurov
{
    internal class Program
    {
        static string ClientId = "019b287d-4c6f-7695-97bd-095b75ac26a5";
        static string AuthorizationKey = "MDE5YjI4N2QtNGM2Zi03Njk1LTk3YmQtMDk1Yjc1YWMyNmE1OmY5M2M4N2Q0LTJkNTgtNGIwNC05NmMxLTI0YzljNWMzOTM5Yw==";

        static string YandexApiKey = "***";
        static string YandexFolderId = "***";

        static List<GigaMessage> gigaHistory = new List<GigaMessage>();
        static List<YandexMessage> yandexHistory = new List<YandexMessage>();

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("1. GigaChat");
            Console.WriteLine("2. YandexGPT");

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
                    break;
            }
        }

        static async Task RunGigaChat()
        {

            string token = await GetGigaChatToken(ClientId, AuthorizationKey);
            if (token == null)
            {
                Console.WriteLine("Не удалось получить токен для GigaChat");
                return;
            }

            gigaHistory.Add(new GigaMessage { role = "system", content = "Ты полезный ассистент." });

            while (true)
            {
                Console.Write("Вы: ");
                string input = Console.ReadLine();

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

            yandexHistory.Add(new YandexMessage { role = "system", text = "Ты полезный ассистент." });

            while (true)
            {
                Console.Write("Вы: ");
                string input = Console.ReadLine();
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
                        ResponseToken tokenResponse = JsonConvert.DeserializeObject<ResponseToken>(responseContent);
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

                    var gigaRequest = new
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

                    ResponseMessage result = JsonConvert.DeserializeObject<ResponseMessage>(responseText);
                    return result?.choices?[0]?.message?.content;
                }
            }
        }

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

    public class GigaMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }

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