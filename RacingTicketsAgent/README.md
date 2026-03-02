# 🏟️ Racing Tickets AI Agent

Agente de IA en .NET 9 que desarrolla y mantiene el frontend Angular del sitio de venta de tickets de **Racing Club de Avellaneda**, usando Ollama (deepseek-coder) como LLM local.

## Stack

| Componente | Tecnología |
|---|---|
| Runtime | .NET 9 Console App |
| LLM | Ollama + `deepseek-coder:latest` (local) |
| Frontend generado | Angular 17+ (standalone components, SCSS) |
| Control de versiones | LibGit2Sharp + Octokit (GitHub API) |
| UI de consola | Spectre.Console |
| Patrón de agente | ReAct (Reason → Act → Observe) |
| Prompts | Archivos `.txt` externos en `/Prompts` |

## Arquitectura

```
RacingTicketsAgent/
├── Agent/
│   ├── AgentOrchestrator.cs   ← Loop ReAct principal
│   ├── OllamaClient.cs        ← HTTP client para Ollama
│   └── PromptLoader.cs        ← Carga y reemplaza variables en .txt
├── Skills/
│   ├── AutoImproveSkill.cs    ← Skill 1: el agente decide qué mejorar
│   └── PromptDrivenSkill.cs   ← Skill 2: vos le decís qué implementar
├── Tools/
│   ├── FileSystemTool.cs      ← Lee/escribe archivos Angular
│   └── GitHubTool.cs          ← Crea repo, init, commit, push
└── Prompts/                   ← ⭐ Prompts fuera del código
    ├── system.txt             ← Personalidad y tools del agente
    ├── auto_improve.txt       ← Prompt Skill 1
    ├── prompt_driven.txt      ← Prompt Skill 2
    └── frontend_context.txt   ← Contexto del dominio Racing Club
```

## Setup

### 1. Prerrequisitos

```bash
# .NET 9 SDK
dotnet --version   # debe ser 9.x

# Ollama corriendo con deepseek-coder
ollama run deepseek-coder:latest
ollama ps   # verificar que está activo
```

### 2. Configurar credenciales

Editá `appsettings.json`:

```json
{
  "GitHub": {
    "Token": "ghp_XXXXXXXXXXXXXXXXXX",   ← tu PAT con scope 'repo'
    "Username": "tu-usuario-github",
    "RepoName": "racing-tickets-frontend"
  },
  "Agent": {
    "FrontendOutputPath": "./frontend-output"   ← carpeta donde se genera el Angular
  }
}
```

**Crear GitHub PAT:**  
→ https://github.com/settings/tokens → `Generate new token (classic)` → scope: `repo`

### 3. Ejecutar

```bash
cd RacingTicketsAgent
dotnet run
```

## Uso

Al ejecutar, aparece un menú:

```
? ¿Qué querés hacer?
> 🤖  Skill 1 - Auto-mejora (el agente decide qué hacer)
  💬  Skill 2 - Prompt libre (vos le decís qué implementar)
  📁  Ver archivos del proyecto
  🚪  Salir
```

### Skill 1 - Auto-mejora

El agente lee el estado actual del proyecto y decide autónomamente qué construir o mejorar.  
Primera ejecución → genera la estructura completa de Angular.  
Ejecuciones siguientes → agrega funcionalidades que faltan.

### Skill 2 - Prompt libre

Ejemplos de pedidos:

```
Agregá un componente de countdown al próximo partido en el hero
Implementá el selector de sectores del estadio con SVG
Agregá animaciones de entrada con @angular/animations en la lista de partidos
Creá el servicio de autenticación con JWT mock
Mejorar el responsive del header para mobile
```

## Cómo funciona el loop ReAct

```
[Vos] → Skill elegida
  ↓
[Agente] THOUGHT: razona qué hacer
  ↓
[Agente] ACTION: nombre_herramienta
  ↓
[Tool]   Ejecuta (escribe archivo / llama Git / etc.)
  ↓
[Agente] OBSERVATION: resultado del tool
  ↓
[Agente] THOUGHT: qué hacer ahora...
  ↓ (repite hasta DONE o max iterations)
[Agente] ACTION: DONE → commit pusheado a GitHub
```

## Personalizar prompts

Los prompts viven en `Prompts/*.txt` y podés editarlos sin recompilar:

- `system.txt` → personalidad del agente, herramientas disponibles, reglas
- `auto_improve.txt` → lógica de decisión de la Skill 1
- `prompt_driven.txt` → cómo interpreta tus pedidos (Skill 2)
- `frontend_context.txt` → contexto del dominio (colores, modelos, secciones)

## Troubleshooting

| Problema | Solución |
|---|---|
| `Ollama no responde` | `ollama serve` en otra terminal |
| `GitHub 401` | Verificar PAT con scope `repo` |
| `LibGit2Sharp error` | Asegurate que `frontend-output` existe y tiene permisos |
| `El agente loopeá sin terminar` | Aumentar `MaxReActIterations` en appsettings.json |
