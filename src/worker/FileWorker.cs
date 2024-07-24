using System.Text.RegularExpressions;
using Hoi4UniversalTranslator.translators;

namespace Hoi4UniversalTranslator.worker
{
  class FileWorker
  {
    private const int MaxContentLength = 5000;
    private const int ChunkSize = 1250;

    private static async Task<IEnumerable<(string fileName, string content)>> Read(string input, string mainLang, string toLang)
    {
      var translatedContents = new List<(string fileName, string content)>();

      try
      {
        Console.WriteLine($"DEBUG | Reading files from directory: {input}");

        // Get all .yml files in the directory
        var files = Directory.GetFiles(input, "*.yml");
        Console.WriteLine($"DEBUG | Found {files.Length} .yml files");

        // Create tasks for reading and translating each file
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
          var translatedContent = TranslateContent(mainLang, toLang, content);
          Console.WriteLine($"DEBUG | Translated content for file: {filePath}");

          // Return translated content along with the original file name
          return (fileName: Path.GetFileName(filePath), content: translatedContent);
        });

        // Wait for all translation tasks to complete
        translatedContents.AddRange(await Task.WhenAll(translationTasks));
      }
      catch (IOException e)
      {
        Console.WriteLine($"ERROR | {e.Message}");
        translatedContents.Add(("ERROR", e.Message));
      }

      return translatedContents;
    }

    private static string TranslateContent(string mainLang, string toLang, string content)
    {
      var stringPattern = "\"(.*?)\"";
      var matches = Regex.Matches(content, stringPattern);

      Parallel.ForEach(matches.Cast<Match>(), match =>
      {
        var originalText = match.Groups[1].Value;
        var translatedText = Google.Translate(mainLang, toLang, originalText).Result;
        content = content.Replace(originalText, translatedText);
      });

      return content;
    }

    private static async Task Write(string output, IEnumerable<(string fileName, string content)> translatedContents)
    {
      try
      {
        Console.WriteLine($"DEBUG | Writing files to directory: {output}");
        foreach (var (fileName, content) in translatedContents)
        {
          var outputPath = Path.Combine(output, fileName);
          await File.WriteAllTextAsync(outputPath, content);
          Console.WriteLine($"DEBUG | Written file: {outputPath}");
        }
      }
      catch (IOException e)
      {
        Console.WriteLine($"ERROR | {e.Message}");
      }
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

      // Read and Write File
      try
      {
        var translatedContents = await Read(input, mainLang, toLang);
        await Write(output, translatedContents);
      }
      catch (IOException e)
      {
        Console.WriteLine($"ERROR | {e.Message}");
      }
    }
  }
}
