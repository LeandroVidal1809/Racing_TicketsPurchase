using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RacingTicketsAgent.Agent;
using RacingTicketsAgent.Skills;
using RacingTicketsAgent.Tools;
using Spectre.Console;

// ── BANNER ────────────────────────────────────────────────────────────────────

AnsiConsole.Write(new FigletText("Racing Agent").Color(Color.DeepSkyBlue1));
AnsiConsole.MarkupLine("[bold cyan]🏟️  Frontend AI Agent - Racing Club de Avellaneda[/]");
AnsiConsole.MarkupLine("[dim]Ollama deepseek-coder:33b · Angular 17 · GitHub[/]\n");

// ── CONFIG ────────────────────────────────────────────────────────────────────

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// ── VALIDATE OLLAMA ───────────────────────────────────────────────────────────

var ollama = new RacingTicketsAgent.Agent.OllamaClient(config);

if (!await ollama.IsAvailableAsync())
{
    AnsiConsole.MarkupLine("[bold red]⚠ Ollama no responde.[/]");
    AnsiConsole.MarkupLine("  1. Ejecutá [bold]ollama serve[/] en otra terminal");
    AnsiConsole.MarkupLine("  2. Precalentá: [bold]ollama run deepseek-coder:33b \"ok\"[/]");
    return;
}

AnsiConsole.MarkupLine("[green]✓ Ollama conectado[/]");

// ── VALIDATE GITHUB TOKEN ─────────────────────────────────────────────────────

var ghToken = config["GitHub:Token"] ?? "";
if (ghToken.StartsWith("YOUR_"))
{
    AnsiConsole.MarkupLine("[bold red]⚠ Configurá GitHub:Token en appsettings.json[/]");
    AnsiConsole.MarkupLine("  Creá tu PAT en: https://github.com/settings/tokens (scope: repo)");
    return;
}

// ── DI ────────────────────────────────────────────────────────────────────────

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddSingleton(ollama);
services.AddSingleton<PromptLoader>();
services.AddSingleton<FileSystemTool>();
services.AddSingleton<GitTool>(sp =>
    new GitTool(config, sp.GetRequiredService<FileSystemTool>().OutputPath));
services.AddSingleton<PromptDrivenSkill>();

var provider = services.BuildServiceProvider();
var skill = provider.GetRequiredService<PromptDrivenSkill>();
var fs = provider.GetRequiredService<FileSystemTool>();

// ── SHOW STATUS ───────────────────────────────────────────────────────────────

var fileCount = fs.ListFiles().Count();
AnsiConsole.MarkupLine($"[dim]📁 Frontend: {fs.OutputPath}[/]");
AnsiConsole.MarkupLine($"[dim]📄 Archivos: {fileCount}[/]");
AnsiConsole.MarkupLine($"[dim]🤖 Modelo:   {config["Ollama:Model"]}[/]\n");

// ── MAIN LOOP ─────────────────────────────────────────────────────────────────

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[bold]¿Qué querés hacer?[/]")
            .AddChoices(
                "💬  Implementar funcionalidad",
                "📁  Ver archivos del proyecto",
                "🚪  Salir"));

    AnsiConsole.WriteLine();

    try
    {
        if (choice.StartsWith("💬"))
        {
            var request = AnsiConsole.Prompt(
                new TextPrompt<string>("[bold yellow]Describí qué querés implementar:[/]")
                    .PromptStyle("yellow"));

            if (!string.IsNullOrWhiteSpace(request))
                await skill.ExecuteAsync(request, cts.Token);
        }
        else if (choice.StartsWith("📁"))
        {
            var files = fs.ListFiles().ToList();

            if (files.Count == 0)
            {
                AnsiConsole.MarkupLine($"[dim]Proyecto vacío en: {fs.OutputPath}[/]");
            }
            else
            {
                var tree = new Tree($"[bold cyan]{fs.OutputPath}[/]");
                foreach (var f in files)
                    tree.AddNode($"[dim]{f}[/]");
                AnsiConsole.Write(tree);
                AnsiConsole.MarkupLine($"\n[dim]Total: {files.Count} archivos[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[bold]¡Hasta la victoria siempre, Academia! 🏆[/]");
            break;
        }
    }
    catch (OperationCanceledException)
    {
        AnsiConsole.MarkupLine("[yellow]Cancelado.[/]");
        cts.TryReset();
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
    }
}