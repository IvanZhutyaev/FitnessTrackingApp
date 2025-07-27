using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class OpenRouterChatBot
{
    public readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private const string ApiUrl = "https://openrouter.ai/api/v1/chat/completions";

    public OpenRouterChatBot(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/IvanZhutyaev/FitnessTrackingApp"); 
        _httpClient.DefaultRequestHeaders.Add("X-Title", "FitnessTrackingAI");
    }

    public async Task<string> GetResponseAsync(string prompt)
    {
        var requestData = new
        {
            model = "mistralai/mistral-7b-instruct",
            messages = new[]
            {
            new
            {
                role = "system",
                content = "Вы — квалифицированный фитнес-тренер. Ваша задача — давать чёткие, профессиональные и обоснованные ответы на вопросы, касающиеся сна, питания, физических тренировок, восстановления, режима дня, образа жизни и мотивации. Вы не отвечаете на вопросы, не связанные с данной тематикой. В таких случаях следует строго и сдержанно сообщить, что ваша компетенция ограничена вопросами, касающимися фитнеса, здоровья и тренировочного процесса."
            },
            new { role = "user", content = prompt }
        },
            max_tokens = 500,
            temperature = 0.7
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(ApiUrl, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var responseObject = JsonSerializer.Deserialize<ChatResponse>(responseJson, options);

            return responseObject?.Choices?[0]?.Message?.Content?.Trim() ?? "Пустой ответ от модели.";
        }
        catch (Exception ex)
        {
            return $"Ошибка: {ex.Message}";
        }
    }



    public class ChatResponse
    {
        public Choice[] Choices { get; set; }
    }

    public class Choice
    {
        public Message Message { get; set; }
    }

    public class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        
        var apiKey = ""; //Артём, сюда API key от https://openrouter.ai/keys

        var chatBot = new OpenRouterChatBot(apiKey);

        while (true)
        {
            Console.Write("\nВы: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput)) continue;
            if (userInput.Equals("выход", StringComparison.OrdinalIgnoreCase)) break;

            Console.WriteLine("\nБот:");
            var response = await chatBot.GetResponseAsync(userInput);
            Console.WriteLine(response);
        }
    }
}
