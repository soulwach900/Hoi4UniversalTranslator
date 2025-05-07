using Newtonsoft.Json;

namespace Hoi4Translator.main.translators;

class GTranslator
{
    private static readonly HttpClient Client = new();

    public async Task<string> UseGoogleTranslate(string content)
    {
        try
        {
            return await Translate(content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
            return $"Erro ao traduzir: {ex.Message}";
        }
    }

    private async Task<string> Translate(string content)
    {
        string mainLang = "en";
        string toLang = "pt";

        string url =
            $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={mainLang}&tl={toLang}&dt=t&q={Uri.EscapeDataString(content)}";

        try
        {
            HttpResponseMessage msg = await Client.GetAsync(url);

            if (msg.IsSuccessStatusCode)
            {
                string mainContent = await msg.Content.ReadAsStringAsync();

                var response = JsonConvert.DeserializeObject<dynamic>(mainContent);

                string result = response![0][0][0];

                return result;
            }

            return $"Erro --> Código HTTP: {msg.StatusCode}, Motivo: {msg.ReasonPhrase}";
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}