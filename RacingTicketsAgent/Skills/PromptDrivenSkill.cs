using RacingAgentClaude.Agent;
using RacingAgentClaude.Tools;
using RacingTicketsAgent.Agent;
using Spectre.Console;

namespace RacingAgentClaude.Skills;

public class PromptDrivenSkill
{
    private readonly ILlmClient _llm;
    private readonly PromptLoader _loader;
    private readonly FileSystemTool _fs;
    private readonly GitTool _git;

    public PromptDrivenSkill(ILlmClient llm, PromptLoader loader, FileSystemTool fs, GitTool git)
    {
        _llm = llm;
        _loader = loader;
        _fs = fs;
        _git = git;
    }

    public async Task ExecuteAsync(string userRequest, CancellationToken ct = default)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Prompt Driven[/]").RuleStyle("cyan"));
        AnsiConsole.MarkupLine($"[dim]Pedido: {userRequest}[/]\n");

        var existingFiles = _fs.ListFiles().ToList();

        // ── STEP 1: identify files ────────────────────────────────────────
        var targetFiles = await IdentifyFilesAsync(userRequest, existingFiles, ct);

        if (targetFiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No se identificaron archivos para este pedido.[/]");
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
                existingFiles.Add(filePath);
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
        AnsiConsole.MarkupLine($"\n[bold green]✓ {generated.Count} archivo(s) generado(s) y pusheado(s).[/]");
    }

    private async Task<List<string>> IdentifyFilesAsync(
        string userRequest, List<string> existing, CancellationToken ct)
    {
        var prompt = _loader.Load("identify_files", new()
        {
            ["user_request"] = userRequest,
            ["existing_files"] = existing.Count > 0 ? string.Join("\n", existing) : "(none)"
        });

        string response = string.Empty;
        await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
            .StartAsync("[cyan]Identificando archivos...[/]", async _ =>
            {
                response = await _llm.ChatAsync(prompt, ct);
            });

        AnsiConsole.MarkupLine($"[dim]Claude identify → {response.Trim()[..Math.Min(200, response.Trim().Length)]}[/]\n");

        var validExtensions = new HashSet<string> { ".ts", ".html", ".scss", ".css", ".json" };

        return response
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Select(l => System.Text.RegularExpressions.Regex.Replace(l, @"^[\-\*\•\`\d\.\s]+", ""))
            .Select(l => l.Contains('#') ? l[..l.IndexOf('#')].Trim() : l)
            .Select(l => l.Contains("//") ? l[..l.IndexOf("//")].Trim() : l)
            .Select(l => l.Replace('\\', '/').Trim())
            .Where(l => l.StartsWith("src/"))
            .Where(l => validExtensions.Contains(Path.GetExtension(l).ToLower()))
            .Where(l => !l.Contains(' '))
            .Distinct()
            .ToList();
    }

    private async Task<bool> GenerateFileAsync(
        string filePath, string userRequest,
        List<string> existingFiles, CancellationToken ct)
    {
        AnsiConsole.MarkupLine($"\n[bold yellow]Generando:[/] {filePath}");

        var ext = Path.GetExtension(filePath).ToLower();
        var fileType = ext switch
        {
            ".ts" => "TypeScript Angular 17 standalone component",
            ".html" => "Angular HTML template",
            ".scss" => "SCSS stylesheet",
            ".json" => "JSON file",
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
        await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
            .StartAsync($"[cyan]  Escribiendo {Path.GetFileName(filePath)}...[/]", async _ =>
            {
                content = await _llm.ChatAsync(prompt, ct);
            });

        content = StripCodeFences(content);

        if (string.IsNullOrWhiteSpace(content))
        {
            AnsiConsole.MarkupLine("[red]  SKIP: respuesta vacía[/]");
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