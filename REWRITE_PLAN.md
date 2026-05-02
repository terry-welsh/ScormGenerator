# SCORM Generator — .NET 10 Rewrite Plan

## Context

The current SCORM Generator is a Python/Flask application that converts a structured JSON (or Markdown) course definition into SCORM 2004 3rd Edition-compliant ZIP packages. The goal of this rewrite is a strict 1:1 port to **.NET 10 / C#**, preserving all functionality, interfaces, and behavior while gaining the benefits of a compiled, typed runtime, a single-binary CLI, and the modern .NET ecosystem. DevEx features (Podman Compose, hot-reload, volume mounts) must be at parity or better than the current stack.

---

## Target Tech Stack

| Concern | Choice |
|---|---|
| Language | C# 13 / .NET 10 |
| Web framework | ASP.NET Core Minimal APIs |
| Frontend | Vanilla HTML + JS + TailwindCSS (via CDN script — no build step) |
| ZIP generation | `System.IO.Compression` (stdlib) |
| JSON parsing | `System.Text.Json` (stdlib) |
| XML generation | `System.Xml.Linq` (stdlib) |
| Markdown parsing | `Markdig` NuGet package |
| CLI framework | `System.CommandLine` NuGet package |
| Testing | xUnit + `Microsoft.AspNetCore.Mvc.Testing` |
| Containerization | Dockerfile (multi-stage) + `docker-compose.yml` (Podman-compatible) |

---

## Repository Structure

```
ScormGenerator/
├── ScormGenerator.sln
├── src/
│   ├── ScormGen.Core/                    # Core library — all SCORM logic
│   │   ├── ScormGen.Core.csproj
│   │   ├── Models/
│   │   │   ├── Course.cs                 # Course, SCORMPackage records
│   │   │   └── ContentItems.cs          # Heading, Paragraph, BulletedList,
│   │   │                                #   Scenario, MultipleChoice (+ options)
│   │   ├── Loading/
│   │   │   └── CourseLoader.cs          # JSON deserialization + validation
│   │   │                                #   (mirrors blueprints.py)
│   │   ├── Packaging/
│   │   │   └── ScormPackager.cs         # ZIP assembly, imsmanifest.xml gen
│   │   │                                #   (mirrors packager.py)
│   │   ├── Templates/
│   │   │   └── HtmlTemplates.cs         # All HTML/CSS/JS template strings
│   │   │                                #   (mirrors templates.py)
│   │   └── Conversion/
│   │       └── MarkdownConverter.cs     # .scorm.md → Course object
│   │                                    #   (mirrors md_converter.py)
│   │
│   ├── ScormGen.Web/                     # ASP.NET Core web app
│   │   ├── ScormGen.Web.csproj
│   │   ├── Program.cs                   # Minimal API routes (GET /, GET /builder,
│   │   │                                #   POST /generate), 16 MB upload limit
│   │   └── wwwroot/
│   │       ├── index.html               # Main upload UI (Tailwind via CDN)
│   │       └── builder.html            # Builder page (Tailwind via CDN)
│   │
│   └── ScormGen.Cli/                    # Dotnet global tool / executable
│       ├── ScormGen.Cli.csproj
│       └── Program.cs                  # Commands: build, md-to-json
│                                       #   (System.CommandLine)
│
├── tests/
│   ├── ScormGen.Tests.Unit/
│   │   ├── ScormGen.Tests.Unit.csproj
│   │   ├── LoadingTests.cs             # mirrors test_blueprints.py
│   │   ├── PackagingTests.cs          # mirrors test_packager.py
│   │   ├── MarkdownConverterTests.cs  # mirrors test_md_converter.py
│   │   └── CliTests.cs               # mirrors test_cli.py
│   └── ScormGen.Tests.Integration/
│       ├── ScormGen.Tests.Integration.csproj
│       └── EndToEndTests.cs          # mirrors test_integration.py
│
├── templates/
│   └── sample_course.json            # Copied as-is from current repo
│
├── Dockerfile                        # Multi-stage: sdk build → aspnet runtime
├── docker-compose.yml               # dev service with hot-reload + volumes
├── .dockerignore
├── .gitignore
├── FORMAT.md                        # Copied as-is (Markdown input spec)
└── README.md
```

---

## Models (ScormGen.Core/Models/)

### Course.cs
```csharp
record Course(string Title, string Description, List<ScormPackage> Packages);

record ScormPackage(
    string Id,
    string Title,
    string Type,           // "informational" | "ungraded" | "graded"
    string? Objective,
    int? PassingScore,     // graded only
    List<IContentItem> Content
);
```

### ContentItems.cs
```csharp
interface IContentItem { string Type { get; } }

record Heading(string Level, string Text) : IContentItem;
record Paragraph(string Text) : IContentItem;
record BulletedList(List<string> Items) : IContentItem;

record ScenarioOption(string Letter, string Text, string Analysis);
record Scenario(string Question, List<ScenarioOption> Options,
    string CorrectAnswer, string KeyInsight) : IContentItem;

record MultipleChoiceOption(string Letter, string Text);
record MultipleChoice(string Question, List<MultipleChoiceOption> Options,
    string CorrectAnswer, string Explanation) : IContentItem;
```

JSON discriminator: use `System.Text.Json` polymorphic deserialization with `type` field as discriminator (matching the current JSON schema's `"type"` key on each content item).

---

## Key Logic Mapping (Python → C#)

| Python file | C# equivalent | Notes |
|---|---|---|
| `models.py` | `Models/Course.cs`, `Models/ContentItems.cs` | Use C# records |
| `blueprints.py` | `Loading/CourseLoader.cs` | `System.Text.Json` with custom converter for polymorphic content items |
| `packager.py` | `Packaging/ScormPackager.cs` | `ZipArchive` from `System.IO.Compression` |
| `templates.py` | `Templates/HtmlTemplates.cs` | C# interpolated strings / raw string literals |
| `md_converter.py` | `Conversion/MarkdownConverter.cs` | `Markdig` for parsing; custom visitor for .scorm.md syntax |
| `web/app.py` | `ScormGen.Web/Program.cs` | ASP.NET Core Minimal API |
| `cli.py` | `ScormGen.Cli/Program.cs` | `System.CommandLine` |

---

## Web API Routes (Program.cs)

```
GET  /           → serve wwwroot/index.html
GET  /builder    → serve wwwroot/builder.html
POST /generate   → accept multipart/form-data field "course" (JSON file)
                   → validate → generate packages → return application/zip
                   → response header X-Package-Count: N
                   → 400 on bad input, 500 on generation error
```

Upload size limit: 16 MB (set via `KestrelServerOptions` or `RequestSizeLimitAttribute`).

---

## CLI Commands (System.CommandLine)

```
scorm-gen build --input <file.json> [--output <dir>] [--single <N>]
scorm-gen md-to-json <input.scorm.md> [--output <course.json>]
```

Publish as a self-contained executable or dotnet global tool (`dotnet tool install`).

---

## Frontend (wwwroot/)

- Vanilla HTML with TailwindCSS loaded via CDN script tag:
  ```html
  <script src="https://cdn.tailwindcss.com"></script>
  ```
- Tailwind config block inline (for custom brand colors):
  ```html
  <script>
    tailwind.config = {
      theme: {
        extend: {
          colors: {
            primary: '#98c93d',   // LE Green
            accent:  '#49c6e5',   // LE Blue
            success: '#6ca437',
            danger:  '#da7552',
          }
        }
      }
    }
  </script>
  ```
- No npm, no build step — mirrors the "vanilla" spirit of the current stack.
- Replicate the existing upload UX, progress display, and download button using Tailwind utility classes instead of the current hand-written CSS.

---

## Dockerfile (Multi-stage)

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ScormGenerator.sln .
COPY src/ src/
RUN dotnet restore
RUN dotnet publish src/ScormGen.Web/ScormGen.Web.csproj \
    -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "ScormGen.Web.dll"]
```

---

## docker-compose.yml (Dev — Podman-compatible)

```yaml
services:
  scorm-gen:
    build: .
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:5000
    volumes:
      - ./src:/src/src           # hot-reload source
      - ./wwwroot:/app/wwwroot   # live frontend edits
    command: dotnet watch run --project /src/src/ScormGen.Web/ScormGen.Web.csproj
```

`dotnet watch run` is the .NET equivalent of Flask debug mode — it watches for source changes and rebuilds automatically.

---

## .gitignore additions for .NET

```
bin/
obj/
*.user
.vs/
*.suo
TestResults/
```

---

## NuGet Dependencies

| Package | Version | Purpose |
|---|---|---|
| `Markdig` | latest stable | Markdown parsing for .scorm.md |
| `System.CommandLine` | latest stable | CLI argument parsing |

All other dependencies use .NET stdlib (`System.IO.Compression`, `System.Text.Json`, `System.Xml.Linq`).

---

## Testing Strategy

Mirror the existing Python test suite in xUnit:

| Test class | Mirrors | What to verify |
|---|---|---|
| `LoadingTests` | `test_blueprints.py` | Valid JSON loads correctly; missing fields throw; invalid content type throws |
| `PackagingTests` | `test_packager.py` | ZIP contains `index.html`, `imsmanifest.xml`, `scorm_api.js`; manifest validates against SCORM 2004 schema |
| `MarkdownConverterTests` | `test_md_converter.py` | .scorm.md → Course round-trips correctly |
| `CliTests` | `test_cli.py` | `build` and `md-to-json` exit codes and output paths |
| `EndToEndTests` | `test_integration.py` | POST /generate with `sample_course.json` returns 200, ZIP with 4 packages, correct `X-Package-Count` header |

Run tests: `dotnet test`

---

## Critical Files from Current Repo to Reference During Rewrite

The agent doing the rewrite should read these in full before starting:

1. `src/scorm_gen/models.py` — data structures to replicate as C# records
2. `src/scorm_gen/blueprints.py` — validation rules and JSON schema
3. `src/scorm_gen/packager.py` — ZIP assembly and manifest generation (most critical)
4. `src/scorm_gen/templates.py` — all HTML/CSS/JS strings to translate
5. `src/scorm_gen/md_converter.py` — Markdown parsing logic
6. `web/app.py` — Flask routes to replicate as Minimal API endpoints
7. `templates/sample_course.json` — golden test fixture (copy as-is)
8. `FORMAT.md` — .scorm.md spec (copy as-is)
9. `README.md` — JSON schema documentation (copy and update tooling references)

---

## Handoff Notes for the Implementing Agent

- The new repo is a **new directory** — do not modify the Python repo at all.
- SCORM 2004 3rd Edition compliance is non-negotiable. The `imsmanifest.xml` structure, sequencing rules, and JavaScript SCORM API wrapper must be byte-for-byte equivalent in behavior to the current templates.
- The JavaScript SCORM API wrapper (`scorm_api.js` content in `templates.py`) should be copied verbatim into `HtmlTemplates.cs` as a raw string literal — it is already correct and tested.
- Brand colors must be preserved (defined in the Tailwind config block, not in external CSS).
- The generated HTML inside ZIP packages does NOT use Tailwind — it uses the existing embedded CSS from `templates.py` (translated as raw string literals in `HtmlTemplates.cs`). Tailwind is only for the web app UI (`index.html`, `builder.html`).
- For polymorphic JSON deserialization of content items, use `[JsonPolymorphic]` and `[JsonDerivedType]` attributes (available in .NET 7+, fully supported in .NET 10).
- The `POST /generate` endpoint must clean up temp files even on error (use `try/finally` with `Path.GetTempPath()` directories).
- The output ZIP from `/generate` wraps all package ZIPs in a single archive — preserve this behavior exactly.
