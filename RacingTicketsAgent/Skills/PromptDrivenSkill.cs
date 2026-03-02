using RacingTicketsAgent.Agent;
using RacingTicketsAgent.Tools;
using Spectre.Console;

namespace RacingTicketsAgent.Skills;

public class PromptDrivenSkill
{
    private readonly OllamaClient _ollama;
    private readonly PromptLoader _loader;
    private readonly FileSystemTool _fs;
    private readonly GitTool _git;

    public PromptDrivenSkill(
        OllamaClient ollama,
        PromptLoader loader,
        FileSystemTool fs,
        GitTool git)
    {
        _ollama = ollama;
        _loader = loader;
        _fs = fs;
        _git = git;
    }

    public async Task ExecuteAsync(string userRequest, CancellationToken ct = default)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Prompt Driven[/]").RuleStyle("cyan"));
        AnsiConsole.MarkupLine($"[dim]Pedido: {userRequest}[/]\n");

        var existingFiles = _fs.ListFiles().ToList();

        // ── STEP 1: identify which files to create/modify ─────────────────
        var targetFiles = await IdentifyFilesAsync(userRequest, existingFiles, ct);

        if (targetFiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No se identificaron archivos para este pedido.[/]");
            AnsiConsole.MarkupLine("[dim]Intentá ser más específico, por ejemplo:[/]");
            AnsiConsole.MarkupLine("[dim]  'Creá la página de lista de partidos con filtros por torneo'[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[cyan]Archivos a generar ({targetFiles.Count}):[/]");
        foreach (var f in targetFiles)
            AnsiConsole.MarkupLine($"  [dim]→ {f}[/]");

        // ── STEP 2: generate each file ────────────────────────────────────
        var generated = new List<string>();

        foreach (var filePath in targetFiles)
        {
            if (ct.IsCancellationRequested) break;

            var ok = await GenerateFileAsync(filePath, userRequest, existingFiles, ct);
            if (ok)
            {
                generated.Add(filePath);
                existingFiles.Add(filePath); // enrich context for next file
            }
        }

        if (generated.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No se pudo generar ningún archivo.[/]");
            return;
        }

        // ── STEP 3: commit & push ─────────────────────────────────────────
        AnsiConsole.WriteLine();
        var commitMsg = $"✨ {userRequest[..Math.Min(60, userRequest.Length)]}";

        string gitResult = string.Empty;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("[cyan]Commiteando a GitHub...[/]", async _ =>
            {
                gitResult = await _git.CommitAndPushAsync(commitMsg, ct);
            });

        AnsiConsole.MarkupLine($"[green]{gitResult}[/]");
        AnsiConsole.MarkupLine($"\n[bold green]✓ Listo. {generated.Count} archivo(s) generado(s) y pusheado(s).[/]");
    }

    // ── PRIVATE ───────────────────────────────────────────────────────────────

    private async Task<List<string>> IdentifyFilesAsync(
        string userRequest, List<string> existing, CancellationToken ct)
    {
        var prompt = _loader.Load("identify_files", new()
        {
            ["user_request"] = userRequest,
            ["existing_files"] = existing.Count > 0
                ? string.Join("\n", existing)
                : "(none)"
        });

        string response = string.Empty;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("[cyan]Identificando archivos...[/]", async _ =>
            {
                response = await _ollama.ChatAsync(prompt, ct);
            });

        AnsiConsole.MarkupLine($"[dim]Respuesta Ollama (identify):[/]");
        AnsiConsole.MarkupLine($"[dim]{response.Trim()[..Math.Min(400, response.Trim().Length)]}[/]\n");

        return response
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim().TrimStart('-', '*', '•', '`', ' '))
            .Where(l => l.Contains('/') && l.Contains('.'))
            .Where(l => !l.StartsWith("//") && !l.StartsWith("#"))
            .Distinct()
            .ToList();
    }

    private async Task<bool> GenerateFileAsync(
        string filePath,
        string userRequest,
        List<string> existingFiles,
        CancellationToken ct)
    {
        AnsiConsole.MarkupLine($"\n[bold yellow]Generando:[/] {filePath}");

        var ext = Path.GetExtension(filePath).ToLower();
        var fileType = ext switch
        {
            ".ts" => "TypeScript Angular 17 standalone component (.ts)",
            ".html" => "Angular HTML template (.html)",
            ".scss" => "SCSS stylesheet (.scss)",
            ".json" => "JSON file (.json)",
            _ => $"{ext} file"
        };

        var prompt = _loader.Load("generate_file", new()
        {
            ["file_path"] = filePath,
            ["file_type"] = fileType,
            ["user_request"] = userRequest,
            ["existing_files"] = string.Join("\n", existingFiles)
        });

        string content = string.Empty;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"[cyan]Escribiendo {Path.GetFileName(filePath)}...[/]", async _ =>
            {
                content = await _ollama.ChatAsync(prompt, ct);
            });

        // Strip markdown fences if model adds them
        content = StripCodeFences(content);

        if (string.IsNullOrWhiteSpace(content))
        {
            AnsiConsole.MarkupLine($"[red]  SKIP: respuesta vacía[/]");
            return false;
        }

        _fs.WriteFile(filePath, content);
        AnsiConsole.MarkupLine($"[green]  ✓ Escrito ({content.Length} chars)[/]");
        return true;
    }

    private static string StripCodeFences(string content)
    {
        var lines = content.Split('\n').ToList();

        if (lines.Count > 0 && lines[0].TrimStart().StartsWith("```"))
            lines.RemoveAt(0);

        if (lines.Count > 0 && lines[^1].Trim().StartsWith("```"))
            lines.RemoveAt(lines.Count - 1);

        return string.Join('\n', lines).Trim();
    }
}