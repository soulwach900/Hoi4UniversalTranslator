using System.Text.Json;

namespace Hoi4UniversalTranslator.translators
{
    class Google
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<string> Translate(string mainLang, string toLang, string context)
        {
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={mainLang}&tl={toLang}&dt=t&q={Uri.EscapeDataString(context)}";

            try
            {
                // Get Google Response
                HttpResponseMessage httpResponse = await client.GetAsync(url);

                if (httpResponse.IsSuccessStatusCode)
                {
                    // Success
                    string mainContent = await httpResponse.Content.ReadAsStringAsync();

                    // Parse Response
                    string result = ParseResult(mainContent);

                    // Return result
                    return result;
                }
                else
                {
                    // Failed
                    return $"ERROR | HTTP Status Code: {httpResponse.StatusCode}, Reason: {httpResponse.ReasonPhrase}";
                }
            }
            catch (HttpRequestException e)
            {
                // Failed to Connect with Google
                return $"ERROR | HTTP Request Exception: {e.Message}";
            }
            catch (Exception e)
            {
                // Catch any other unexpected exceptions
                return $"ERROR | Unexpected Exception: {e.Message}";
            }
        }

        private static string ParseResult(string inputJson)
        {
            try
            {
                JsonDocument document = JsonDocument.Parse(inputJson);
                JsonElement root = document.RootElement;

                if (root[0].ValueKind == JsonValueKind.Array)
                {
                    JsonElement firstArray = root[0];
                    string result = "";

                    foreach (JsonElement item in firstArray.EnumerateArray())
                    {
                        if (item[0].ValueKind == JsonValueKind.String)
                        {
                            result += item[0].GetString();
                        }
                    }

                    return result;
                }
                else
                {
                    return "No content found in response";
                }
            }
            catch (JsonException e)
            {
                return $"ERROR | JSON Parsing Exception: {e.Message}";
            }
        }

        public static async Task PrintTranslation(string mainLang, string toLang, string context)
        {
            string translation = await Translate(mainLang, toLang, context);
            Console.WriteLine(translation);
        }
    }
}
