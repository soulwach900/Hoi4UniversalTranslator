using System.Text.Json;
using Hoi4UniversalTranslator.worker;

namespace Hoi4UniversalTranslator
{
  class Program
  {
    static async Task Main(string[] args)
    {
      string json = await File.ReadAllTextAsync("config.json");
      Config config = JsonSerializer.Deserialize<Config>(json);

      await worker(config.Output, config.Input, config.MainLang, config.ToLang);
    }

    static async Task worker(string output, string input, string mainLang, string toLang)
    {
      await FileWorker.ReadAndWriteFiles(output, input, mainLang, toLang);
      Console.WriteLine("LOG | " + "Translation completed.");
    }
  }

  public class Config
  {
    public required string Input { get; set; }
    public required string Output { get; set; }
    public required string MainLang { get; set; }
    public required string ToLang { get; set; }
  }
}
