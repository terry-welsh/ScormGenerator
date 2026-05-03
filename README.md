# SCORM Generator

A web app that converts course definitions (JSON or Markdown) into SCORM 2004 3rd Edition packages ready for upload to any LMS.

## Prerequisites

| Tool | Version | Required for |
|------|---------|-------------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0+ | CLI and Visual Studio |
| [Visual Studio 2022](https://visualstudio.microsoft.com/) | 17.10+ | Visual Studio workflow |
| Docker or [Podman](https://podman.io/) | any recent | Container workflow |

## Run via Visual Studio

1. Open `ScormGenerator.slnx`
2. Set **ScormGen.Web** as the startup project (right-click → Set as Startup Project)
3. Press **F5** (or Ctrl+F5 to run without debugging)
4. The browser opens at `https://localhost:7292`

## Run via CLI

```bash
dotnet run --project src/ScormGen.Web
```

App starts at **http://localhost:5034** (HTTP) or **https://localhost:7292** (HTTPS).

## Run via Docker Compose / Podman Compose

```bash
docker compose up
# or
podman compose up
```

App starts at **http://localhost:8080** with hot-reload enabled (source changes rebuild automatically).

To stop: `Ctrl+C`, then `docker compose down` / `podman compose down`.

## Run Tests

```bash
dotnet test
```

Runs unit, integration, and component tests across all three test projects.

## Project Structure

```
src/
  ScormGen.Core/    — Business logic: JSON/Markdown loading, SCORM packaging, HTML templates
  ScormGen.Web/     — Blazor Server UI: course builder, file upload endpoint, download endpoint
tests/
  ScormGen.Tests.Unit/        — Unit tests for parsing and packaging
  ScormGen.Tests.Integration/ — End-to-end HTTP endpoint tests
  ScormGen.Tests.Components/  — Blazor component rendering tests (bUnit)
infra/
  main.bicep        — Azure Container Apps infrastructure (used by CI/CD)
```
