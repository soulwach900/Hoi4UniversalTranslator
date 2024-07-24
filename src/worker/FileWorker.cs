using System.Text.RegularExpressions;
using System.Text;
using Hoi4UniversalTranslator.translators;

namespace Hoi4UniversalTranslator.worker
{
  class FileWorker
  {
    private const int MaxContentLength = 5000;
    private const int ChunkSize = 1250;

    private static async Task ReadAndTranslateAndSave(string input, string output, string mainLang, string toLang)
    {
      try
      {
        Console.WriteLine($"DEBUG | Reading files from directory: {input}");

        // Get all .yml files in the directory
        var files = Directory.GetFiles(input, "*.yml");
        Console.WriteLine($"DEBUG | Found {files.Length} .yml files");

        // Create tasks for reading, translating, and saving each file
        var translationTasks = files.Select(async filePath =>
        {
          // Read file content asynchronously
          var content = await File.ReadAllTextAsync(filePath);
          Console.WriteLine($"DEBUG | Read content from file: {filePath}");

          // Truncate content to MaxContentLength characters
          if (content.Length > MaxContentLength)
          {
            var chunks = new List<string>();
            for (int i = 0; i < content.Length; i += ChunkSize)
            {
              chunks.Add(content.Substring(i, Math.Min(ChunkSize, content.Length - i)));
            }
            content = string.Join("", chunks);
            Console.WriteLine($"DEBUG | Content truncated to {MaxContentLength} characters");
          }

          // Translate content
          var translatedContent = await TranslateContent(mainLang, toLang, content);
          Console.WriteLine($"DEBUG | Translated content for file: {filePath}");

          // Write translated content to output file immediately
          var outputPath = Path.Combine(output, Path.GetFileName(filePath));
          await File.WriteAllTextAsync(outputPath, translatedContent);
          Console.WriteLine($"DEBUG | Written file: {outputPath}");
        });

        // Wait for all tasks to complete
        await Task.WhenAll(translationTasks);
      }
      catch (IOException e)
      {
        Console.WriteLine($"ERROR | {e.Message}");
      }
    }

    private static async Task<string> TranslateContent(string mainLang, string toLang, string content)
    {
      var stringPattern = "\"(.*?)\"";
      var matches = Regex.Matches(content, stringPattern);

      // Using StringBuilder for efficient string manipulation
      var sb = new StringBuilder(content);
      var translations = new Dictionary<string, string>();

      // Collect all matches and their translations
      foreach (Match match in matches)
      {
        var originalText = match.Groups[1].Value;
        if (!translations.ContainsKey(originalText) && !string.IsNullOrEmpty(originalText))
        {
          try
          {
            var translatedText = await Google.Translate(mainLang, toLang, originalText);
            translations[originalText] = translatedText;
          }
          catch (Exception ex)
          {
            Console.WriteLine($"ERROR | Translation failed for '{originalText}': {ex.Message}");
            translations[originalText] = originalText; // Fallback to original text if translation fails
          }
        }
      }

      // Replace each original text with its translation
      foreach (var pair in translations)
      {
        sb.Replace(pair.Key, pair.Value);
      }

      return sb.ToString();
    }

    public static async Task ReadAndWriteFiles(string output, string input, string mainLang, string toLang)
    {
      // Verify for Folders
      if (!Directory.Exists(input))
      {
        Console.WriteLine($"DEBUG | Input directory does not exist. Creating: {input}");
        Directory.CreateDirectory(input);
      }
      if (!Directory.Exists(output))
      {
        Console.WriteLine($"DEBUG | Output directory does not exist. Creating: {output}");
        Directory.CreateDirectory(output);
      }

      // Read, Translate and Write File
      await ReadAndTranslateAndSave(input, output, mainLang, toLang);
    }
  }
}
