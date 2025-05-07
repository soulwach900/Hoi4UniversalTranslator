using Hoi4Translator.main;

namespace Hoi4Translator;

class Program
{
    private static async Task Main()
    {
        FileWorker worker = new FileWorker();
        await worker.Read();
    }
}