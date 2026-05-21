# SCORM Generator

SCORM Generator is a Blazor Server web app for turning structured course content into downloadable SCORM ZIP packages.

The app is intentionally lightweight: it validates course JSON, builds SCORM 2004 or SCORM 1.2 packages, and lets users prepare content with external AI tools without storing API keys or integrating directly with a model provider.

## What the App Does

- Upload a valid course JSON file and generate a SCORM package ZIP.
- Build course JSON manually in the browser with the Course Builder.
- Prepare model-generated content with the Content Prep Utility.
- Validate JSON before packaging.
- Generate one ZIP containing one or more SCORM package folders.
- Support SCORM 2004 3rd Edition by default and SCORM 1.2 when `"format": "scorm_12"` is provided.

## Application Workflows

### 1. Upload Existing JSON

Use this when you already have a course JSON file.

1. Start the app.
2. Open the **Upload** page.
3. Select a `.json` course definition file.
4. Select **Generate SCORM Package**.
5. The browser downloads `scorm_packages.zip`.

The upload page validates the JSON before packaging. If the file is malformed or missing required fields, the page shows the validation error.

### 2. Build JSON Manually

Use this when you want to create the course structure directly in the app.

1. Open the **Builder** page.
2. Enter the course ID, title, version, and SCORM version.
3. Add one or more packages.
4. Add content items such as headings, paragraphs, bullet lists, scenarios, and multiple-choice questions.
5. Select **Export JSON** to save the course definition.
6. Select **Generate SCORM Package** to create the ZIP directly.

### 3. Prepare Markdown Content With an External Model

Use this when you have Markdown lesson content that needs to be shaped into the required JSON.

1. Open the **Content Prep** page.
2. Put the source lesson in Markdown. The Markdown can live in a local `.md` file or be pasted into the prompt builder.
3. Copy the generated prompt.
4. Use the prompt with a local model or agent you already have access to. If the agent can read files, point it at the Markdown file.
5. Tell the model to use the Markdown syntax to infer topic-level package boundaries, headings, paragraphs, lists, scenarios, and questions.
6. Load the model's generated `.json` file in the **Load Generated JSON File** control, or paste the JSON into the **Model Output** field.
7. Select **Clean & Validate JSON**.
8. If valid, download the JSON or generate the SCORM ZIP directly.

The Content Prep Utility does not call an AI model. It provides a Markdown-aware prompt, removes common Markdown code fences from model output, formats the JSON, and validates the result against the generator's schema.

## Markdown Source Syntax

The expected source content for the external model workflow is Markdown. The model should use the Markdown structure to decide how to build the final JSON.

The preferred package granularity is **one SCORM package per topic under each module**. Modules are grouping context. Topics are the default package boundaries. For example, if the Markdown has 6 modules and each module contains 4 topics, the generated JSON should contain 24 packages.

Recommended course frontmatter:

```markdown
---
course_id: SAFETY_101
title: Safety Basics
version: 1.0
---
```

Recommended module and topic structure:

```markdown
## Module 1: Safety Basics

### Topic 1.1: Why Safety Basics Matter

Safety basics help teams recognize common hazards.

### Topic 1.2: Responding to Blocked Exits

**Scenario:** A Blocked Exit
```

This should produce separate packages for `Topic 1.1` and `Topic 1.2`. The module title should be retained in each package title for context.

Optional explicit topic package marker:

```markdown
## Package: SAFETY_101_M1_T1_1 | informational | Module 1: Safety Basics - Topic 1.1: Why Safety Basics Matter
```

The package marker maps to:

- `file_name`: `SAFETY_101_M1_T1_1`
- `content_type`: `informational`
- `title`: `Module 1: Safety Basics - Topic 1.1: Why Safety Basics Matter`

If package markers exist only at the module level, the external model should still split the module into topic-level packages. Supported `content_type` values are `informational`, `ungraded`, and `graded`. For graded packages, add:

```markdown
passing_score: 0.8
```

Recommended informational content:

```markdown
### h3: Why Safety Basics Matter

Safety basics help teams recognize common hazards.

- Recognize common hazards
- Follow local procedures
- Report unsafe conditions promptly
```

Recommended scenario content:

```markdown
**Scenario:** A Blocked Exit

**Situation:** You notice boxes stacked in front of an emergency exit.

- A) Move the boxes yourself without telling anyone.
  **Analysis:** This may fix the immediate issue, but it does not create visibility.
- B) Ignore it because the shift has not started.
  **Analysis:** This leaves a known hazard in place.
- C) Report the blocked exit through the local process.
  **Analysis:** This is the strongest response because it fixes the hazard and creates a record.

Correct: C
Key Insight: Hazards should be reported promptly.
```

Recommended question content:

```markdown
**Question:** What should you do when you notice an unsafe condition?
- A) Ignore it if no one is nearby
- B) Report it through the appropriate local process
- C) Wait until the next team meeting
- D) Only mention it if someone is injured
Correct: B
Explanation: Unsafe conditions should be reported promptly.
```

If the Markdown does not include explicit package markers, the external model should infer topic-level packages from the module/topic heading structure.

## Course JSON Schema

The generator expects a top-level course object:

```json
{
  "course_id": "COURSE_001",
  "title": "Course Title",
  "version": "1.0",
  "format": "scorm_2004",
  "packages": []
}
```

Required top-level fields:

| Field | Description |
| --- | --- |
| `course_id` | Course identifier used for filenames and tracking. |
| `title` | Human-readable course title. |
| `version` | Course version string. |
| `packages` | One or more SCORM package definitions. |

Optional top-level fields:

| Field | Values |
| --- | --- |
| `format` | `scorm_2004` or `scorm_12`. Defaults to SCORM 2004 3rd Edition when omitted. |

### Package Object

Each item in `packages` defines one package:

```json
{
  "file_number": 1,
  "file_name": "COURSE_001_INTRO",
  "content_type": "informational",
  "title": "Introduction",
  "content": []
}
```

Required package fields:

| Field | Description |
| --- | --- |
| `file_number` | Package sequence number. |
| `file_name` | Safe filename stem for generated package files. |
| `content_type` | Must be `informational`, `ungraded`, or `graded`. |
| `title` | Package title shown to learners. |
| `content` | Ordered list of content items. |

Graded packages can also include:

```json
"passing_score": 0.8
```

### Content Items

Heading:

```json
{ "type": "heading", "level": "h3", "text": "Section Title" }
```

Paragraph:

```json
{ "type": "paragraph", "text": "Paragraph text." }
```

Bulleted list:

```json
{ "type": "bulleted_list", "items": ["First point", "Second point"] }
```

Scenario:

```json
{
  "type": "scenario",
  "name": "The First Meeting",
  "situation": "A realistic workplace situation.",
  "options": [
    { "letter": "A", "text": "Option text.", "analysis": "Why this option is weak or strong." },
    { "letter": "B", "text": "Option text.", "analysis": "Why this option is weak or strong." },
    { "letter": "C", "text": "Option text.", "analysis": "Why this option is weak or strong." }
  ],
  "correct_option": "C",
  "key_insight": "The main learning point."
}
```

Multiple choice:

```json
{
  "type": "multiple_choice",
  "question": "Question text?",
  "options": [
    { "letter": "A", "text": "Answer option." },
    { "letter": "B", "text": "Answer option." },
    { "letter": "C", "text": "Answer option." },
    { "letter": "D", "text": "Answer option." }
  ],
  "correct_answer": "B",
  "explanation": "Why the correct answer is correct."
}
```

A complete example is available at `templates/sample_course.json`.

## External Model Prompt Guidance

When using an external model, ask it to:

- Read the Markdown source content first, either from the pasted content or from the local `.md` file you point it at.
- Use Markdown syntax as the source of structure.
- Create one SCORM package for each topic under each module.
- Treat module headings as grouping context, not as package boundaries when topics are present.
- Output only JSON.
- Avoid Markdown code fences.
- Use the exact snake_case fields shown above.
- Use `format: "scorm_2004"` for SCORM 2004 3rd Edition unless SCORM 1.2 is specifically needed.
- Keep `content_type` to `informational`, `ungraded`, or `graded`.
- Keep `type` to `heading`, `paragraph`, `bulleted_list`, `scenario`, or `multiple_choice`.
- Include `passing_score` only for graded packages.
- Make every `correct_answer` or `correct_option` match one of the provided option letters.

The Content Prep page generates this prompt for users and includes the current schema examples.

## Project Structure

```text
src/
  ScormGen.Core/
    Conversion/       Custom Markdown-to-course converter
    Loading/          JSON loading and validation
    Models/           Course, package, and content item models
    Packaging/        SCORM 2004 and SCORM 1.2 package generation
    Templates/        HTML, CSS, and SCORM runtime resources
  ScormGen.Web/
    Components/       Blazor pages, layout, and builder components
    wwwroot/          JavaScript, CSS, favicon, and static assets
tests/
  ScormGen.Tests.Unit/        Core parser, loader, and packager tests
  ScormGen.Tests.Integration/ HTTP endpoint and end-to-end tests
  ScormGen.Tests.Components/  Blazor component tests
templates/
  sample_course.json          Full sample course definition
infra/
  main.bicep                  Azure Container Apps infrastructure
```

## Prerequisites

| Tool | Version | Required for |
| --- | --- | --- |
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ | Building, testing, and running the app |
| Visual Studio 2022 | 17.10+ | Visual Studio workflow |
| Docker or Podman | Recent | Container workflow |

## Run Locally

### Visual Studio

1. Open `ScormGenerator.slnx`.
2. Set **ScormGen.Web** as the startup project.
3. Press **F5** or **Ctrl+F5**.
4. Open `https://localhost:7292`.

### CLI

```bash
dotnet run --project src/ScormGen.Web
```

The app starts at:

- `http://localhost:5034`
- `https://localhost:7292`

### Docker Compose or Podman Compose

```bash
docker compose up
```

or:

```bash
podman compose up
```

The containerized app starts at `http://localhost:8080`.

Stop the app with `Ctrl+C`, then run:

```bash
docker compose down
```

## Run Tests

```bash
dotnet test
```

This runs unit, integration, and component tests.

## HTTP Endpoint

The app exposes a multipart upload endpoint used by integration clients:

```http
POST /generate
Content-Type: multipart/form-data
Field: course=<course.json>
```

Successful responses return `application/zip` with filename `scorm_packages.zip`.

## Validation Notes

The loader validates:

- Course title is present.
- At least one package exists.
- Each package has a `file_name`.
- Each package has a `title`.
- Each package uses a supported `content_type`.
- JSON is syntactically valid and deserializes into the supported content item model.

The current validation is intentionally focused on structural correctness. Content quality, instructional design quality, and answer correctness are still user responsibilities.
