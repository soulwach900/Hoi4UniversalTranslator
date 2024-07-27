using System.Text.RegularExpressions;
using System.Text;
using Hoi4UniversalTranslator.translators;
using System.Collections.Concurrent;

namespace Hoi4UniversalTranslator.worker
{
    class FileWorker
    {
        private const int MaxContentLength = 5000;

        private static async Task ReadAndTranslateAndSave(string input, string output, string mainLang, string toLang)
        {
            try
            {
                var files = Directory.GetFiles(input, "*.yml");
                List<FileStream> fileStreams = new List<FileStream>();

                var translationTasks = files.Select(async filePath =>
                {
                    FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    fileStreams.Add(fs);
                    var ChunkSize = 1250;
                    byte[] buffer = new byte[ChunkSize];
                    StringBuilder contentBuilder = new StringBuilder();

                    int bytesRead;
                    while ((bytesRead = await fs.ReadAsync(buffer, 0, ChunkSize)) > 0)
                    {
                        contentBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                    }

                    fs.Close();
                    string content = contentBuilder.ToString();

                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.WriteLine($"DEBUG | Read content from file: {filePath}");

                    if (content.Length > MaxContentLength)
                    {
                        content = content.Substring(0, MaxContentLength);
                        Console.BackgroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"DEBUG | Content truncated to {MaxContentLength} characters");
                    }

                    // Translate content
                    var translatedContent = await TranslateContent(mainLang, toLang, content);
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"DEBUG | Translated content for file: {filePath}");

                    // Write translated content to output file asynchronously
                    var outputPath = Path.Combine(output, Path.GetFileName(filePath));
                    await File.WriteAllTextAsync(outputPath, translatedContent);
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"DEBUG | Written file: {outputPath}");
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
                        for (int attempt = 0; attempt < maxRetries; attempt++)
                        {
                            try
                            {
                                var translatedText = await Google.Translate(mainLang, toLang, originalText).ConfigureAwait(false);
                                translations[originalText] = translatedText;
                                break; // Exit the retry loop on success
                            }
                            catch (Exception ex)
                            {
                                Console.BackgroundColor = ConsoleColor.DarkRed;
                                Console.WriteLine($"ERROR | Translation failed for '{originalText}' on attempt {attempt + 1}: {ex.Message}");
                                if (attempt == maxRetries - 1)
                                {
                                    translations[originalText] = originalText; // Fallback to original text after max retries
                                }
                            }
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
