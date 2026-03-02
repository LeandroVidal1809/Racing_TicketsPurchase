using Microsoft.Extensions.Configuration;

namespace RacingTicketsAgent.Agent;

public class PromptLoader
{
    private readonly string _promptsPath;

    public PromptLoader(IConfiguration config)
    {
        _promptsPath = config["Agent:PromptsPath"] ?? "./Prompts";
    }

    public string Load(string name, Dictionary<string, string>? vars = null)
    {
        var path = Path.Combine(_promptsPath, $"{name}.txt");

        if (!File.Exists(path))
            throw new FileNotFoundException($"Prompt not found: {path}");

        var template = File.ReadAllText(path);

        if (vars != null)
            foreach (var (key, value) in vars)
                template = template.Replace($"{{{{{key}}}}}", value);

        return template;
    }
}