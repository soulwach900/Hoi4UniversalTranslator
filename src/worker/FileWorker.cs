using System.Text.RegularExpressions;
using System.Text;
using Hoi4UniversalTranslator.translators;
using System.Collections.Concurrent;

namespace Hoi4UniversalTranslator.worker
{
    class FileWorker
    {
        private static async Task ReadAndTranslateAndSave(string input, string output, string mainLang, string toLang)
        {
            try
            {
                var files = Directory.GetFiles(input, "*.yml");

                var translationTasks = files.Select(async filePath =>
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        const int ChunkSize = 1250;
                        var buffer = new byte[ChunkSize];
                        var contentBuilder = new StringBuilder();

                        int bytesRead;
                        while ((bytesRead = await fs.ReadAsync(buffer, 0, ChunkSize)) > 0)
                        {
                            contentBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                        }

                        string content = contentBuilder.ToString();

                        Console.BackgroundColor = ConsoleColor.DarkBlue;
                        Console.WriteLine($"DEBUG | Read content from file: {filePath}");

                        if (content.Length > 5000)
                        {
                            Console.BackgroundColor = ConsoleColor.DarkYellow;
                            Console.WriteLine($"DEBUG | Content length exceeds 5000 characters. Splitting for translation.");

                            var chunkTasks = new List<Task<string>>();
                            for (int i = 0; i < content.Length; i += ChunkSize)
                            {
                                var chunkLength = Math.Min(ChunkSize, content.Length - i);
                                var chunk = content.Substring(i, chunkLength);
                                chunkTasks.Add(TranslateContent(mainLang, toLang, chunk));
                            }

                            var translatedChunks = await Task.WhenAll(chunkTasks);
                            var translatedContent = string.Join("", translatedChunks);

                            Console.BackgroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine($"DEBUG | Translated content for file: {filePath}");

                            var outputPath = Path.Combine(output, Path.GetFileName(filePath));
                            await File.WriteAllTextAsync(outputPath, translatedContent);
                            Console.BackgroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine($"DEBUG | Written file: {outputPath}");
                        }
                        else
                        {
                            // If content is within the 5000 character limit
                            var translatedContent = await TranslateContent(mainLang, toLang, content);

                            Console.BackgroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine($"DEBUG | Translated content for file: {filePath}");

                            var outputPath = Path.Combine(output, Path.GetFileName(filePath));
                            await File.WriteAllTextAsync(outputPath, translatedContent);
                            Console.BackgroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine($"DEBUG | Written file: {outputPath}");
                        }
                    }
                });

                await Task.WhenAll(translationTasks);
            }
            catch (Exception ex)
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"ERROR | Failed to read, translate or save files: {ex.Message}");
            }
        }

        private static async Task<string> TranslateContent(string mainLang, string toLang, string content)
        {
            var stringPattern = "\"(.*?)\"";
            var matches = Regex.Matches(content, stringPattern);

            // Using StringBuilder for efficient string manipulation
            var sb = new StringBuilder(content);
            var translations = new ConcurrentDictionary<string, string>();

            // Define the maximum number of retry attempts
            int maxRetries = 5;

            // Collect all matches and their translations in parallel
            var translationTasks = matches
                .Cast<Match>()
                .Select(async match =>
                {
                    var originalText = match.Groups[1].Value;
                    if (!translations.ContainsKey(originalText) && !string.IsNullOrEmpty(originalText))
                    {
                        int attempt = 0;
                        while (attempt < maxRetries)
                        {
                            try
                            {
                                var translatedText = await Google.Translate(mainLang, toLang, originalText).ConfigureAwait(false);
                                translations[originalText] = translatedText;
                                break;  // Exit the loop on success
                            }
                            catch (Exception ex)
                            {
                                Console.Write(ex.Message);
                            }
                            attempt++;
                        }
                    }
                });

            await Task.WhenAll(translationTasks).ConfigureAwait(false);

            // Replace each original text with its translation
            foreach (var pair in translations)
            {
                sb.Replace(pair.Key, pair.Value);
            }

            return sb.ToString();
        }

        public static async Task ReadAndWriteFiles(string output, string input, string mainLang, string toLang)
        {
            try
            {
                // Verify and create directories if they don't exist
                if (!Directory.Exists(input))
                {
                    Console.BackgroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"DEBUG | Input directory does not exist. Creating: {input}");
                    Directory.CreateDirectory(input);
                }
                if (!Directory.Exists(output))
                {
                    Console.BackgroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"DEBUG | Output directory does not exist. Creating: {output}");
                    Directory.CreateDirectory(output);
                }

                // Read, Translate and Write Files
                await ReadAndTranslateAndSave(input, output, mainLang, toLang);
            }
            catch (Exception ex)
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"ERROR | Failed to process directories or files: {ex.Message}");
            }
        }
    }
}
