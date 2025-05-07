using System.Text.RegularExpressions;
using Hoi4Translator.main.translators;
using Spectre.Console;
using System.Collections.Concurrent;

namespace Hoi4Translator.main;

class FileWorker
{
    private const int MAX_BATCH_LENGTH = 5000;
    private readonly Regex _translationPattern = new Regex(@"(?<!\\|%|\$|@|#)""([^""\\]*(?:\\.[^""\\]*)*)""", RegexOptions.Compiled);
    private readonly Regex _protectedElements = new Regex(@"(\$[^$]+\$|%[^%]+%|#[^#]+#|@[^@]+@|:[0-9]+|\b[A-Z_]+\b)", RegexOptions.Compiled);

    public async Task Read()
    {
        EnsureDirectories();
        
        try
        {
            var files = Directory.GetFiles("input", "*.yml");
            if (files.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]Nenhum arquivo encontrado no diretório 'input'![/]");
                return;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var fileTask = ctx.AddTask("[green]Processando arquivos...[/]", maxValue: files.Length);
                    
                    await Parallel.ForEachAsync(files, async (file, token) =>
                    {
                        await ProcessFileAsync(file);
                        fileTask.Increment(1);
                        fileTask.Description = $"[blue]Processado: {Path.GetFileName(file)}[/]";
                    });
                });

            stopwatch.Stop();
            AnsiConsole.MarkupLine($"[green]✓ Todos os arquivos processados em {stopwatch.Elapsed.TotalSeconds:F2}s[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]ERRO: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private void EnsureDirectories()
    {
        Directory.CreateDirectory("input");
        Directory.CreateDirectory("output");
    }

    private async Task ProcessFileAsync(string filePath)
    {
        string outputPath = Path.Combine("output", Path.GetFileName(filePath));
        
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            var processedLines = new string[lines.Length];
            
            var lineBatches = lines
                .Select((line, index) => new { Line = line, Index = index })
                .GroupBy(x => x.Index / 10)
                .ToList();

            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            
            await Parallel.ForEachAsync(lineBatches, options, async (batch, token) =>
            {
                foreach (var item in batch)
                {
                    if (string.IsNullOrWhiteSpace(item.Line) || item.Line.TrimStart().StartsWith('#'))
                    {
                        processedLines[item.Index] = item.Line;
                        continue;
                    }

                    var translatedLine = await ProcessLine(item.Line);
                    processedLines[item.Index] = translatedLine;
                }
            });

            await File.WriteAllLinesAsync(outputPath, processedLines);
            AnsiConsole.MarkupLine($"[silver]Arquivo gerado: {Path.GetFileName(outputPath)}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]ERRO em {Path.GetFileName(filePath)}: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private async Task<string> ProcessLine(string line)
    {
        var parts = new List<string>();
        var lastPosition = 0;
        
        foreach (Match match in _protectedElements.Matches(line))
        {
            if (match.Index > lastPosition)
            {
                var textToTranslate = line.Substring(lastPosition, match.Index - lastPosition);
                parts.Add(await TranslateText(textToTranslate));
            }

            parts.Add(match.Value);
            lastPosition = match.Index + match.Length;
        }

        if (lastPosition < line.Length)
        {
            var remainingText = line.Substring(lastPosition);
            parts.Add(await TranslateText(remainingText));
        }

        return string.Join("", parts);
    }

    private async Task<string> TranslateText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var batches = new List<string>();
        var currentBatch = new List<string>();
        int currentLength = 0;
        foreach (Match match in _translationPattern.Matches(text))
        {
            var segment = match.Groups[1].Value;
            if (segment.Length + currentLength > MAX_BATCH_LENGTH)
            {
                batches.Add(string.Join(" ", currentBatch));
                currentBatch.Clear();
                currentLength = 0;
            }
            currentBatch.Add(segment);
            currentLength += segment.Length;
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(string.Join(" ", currentBatch));
        }

        var translatedBatches = new List<string>();
        foreach (var batch in batches)
        {
            var translator = new GTranslator();
            var translated = await translator.UseGoogleTranslate(batch);
            translatedBatches.Add(translated);
        }

        return _translationPattern.Replace(text, m => 
            $"\"{translatedBatches.SelectMany(b => b.Split()).FirstOrDefault()?.Trim() ?? m.Value}\"");
    }
}