using APIGigaChat_vinokurov.Models.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace APIGigaChat_vinokurov
{
    internal class Program
    {
    
        static string ClientId = "019b287d-4c6f-7695-97bd-095b75ac26a5";
        static string AuthorizationKey = "MDE5YjI4N2QtNGM2Zi03Njk1LTk3YmQtMDk1Yjc1YWMyNmE1OmY5M2M4N2Q0LTJkNTgtNGIwNC05NmMxLTI0YzljNWMzOTM5Yw==";
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
        }
        public static async Task<string> GetToken(string rqUID, string bearer)
        {
            string ReturnToken = null;
            string Url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                Handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyError) => true;
                using (HttpClient client = new HttpClient(Handler))
                {
                    HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Url);
                    Request.Headers.Add("Accept", "application/json");
                    Request.Headers.Add("RqUID", rqUID);
                    Request.Headers.Add("Authorization", $"Bearer {bearer}");
                    var Data = new List<KeyValuePair<string, string>>
                    {
                       new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
                    };
                    Request.Content = new FormUrlEncodedContent(Data);
                    HttpResponseMessage Response = await client.SendAsync(Request);
                    if (Response.IsSuccessStatusCode)
                    {
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        ResponseToken Token = JsonConvert.DeserializeObject<ResponseToken>(ResponseContent);
                        ReturnToken = Token.access_token;
                    }

                }
            }
            return ReturnToken;
        }
        public static async Task<ResponseMessage> GetAnswer(string token, string message)
        {
            ResponseMessage responseMessage = null;
            string Url = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";
            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                Handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
                using (HttpClient client = new HttpClient(Handler))
                {
                    HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Url);
                    Request.Headers.Add("Accept", "application/json");
                    Request.Headers.Add("Authorization", $"Bearer {token}");
                    Models.Request DataRequest = new Models.Request()
                    {
                        model = "GigaChat",
                        stream = false,
                        repetition_penalty = 1,
                        messages = new List<Models.Request.Message>()
                        {
                            new Models.Request.Message()
                            {

                                role="user",
                                content=message
                            }
                        }
                    };
                    string JsonContent = JsonConvert.SerializeObject(DataRequest);
                    Request.Content = new StringContent(JsonContent, Encoding.UTF8, "application/json");
                    HttpResponseMessage Response = await client.SendAsync(Request);
                    if (Response.IsSuccessStatusCode)
                    {
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(ResponseContent);
                    }
                }
            }
            return responseMessage;

        }
    }
}

