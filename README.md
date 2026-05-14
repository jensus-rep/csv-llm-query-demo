# CsvAiQueryDemo

CsvAiQueryDemo is a .NET 8 MVP for querying structured CSV data with AI assistance while keeping the actual data processing deterministic and local.

The LLM is not used as a database or calculator. It translates a natural-language question into a structured `QueryIntent`; `QueryEngine` executes the query in C# against the locally loaded CSV.

## What The Demo Shows

- Loads `data/demodaten.csv` locally.
- Creates a compact `DatasetProfile` with metadata, examples and top values.
- Sends only the user question, `DatasetProfile` and allowed `QueryIntent` schema to the LLM.
- Executes `count`, `filter`, `distinct`, `top_values` and `group_by_count` deterministically in C#.
- Writes technical artifacts to `output/`.
- Shows the profile, pipeline, answer, prompt details, `QueryIntent` and `QueryResult` in a small web frontend.

The full CSV is never sent to the LLM.

## Project Structure

```text
src/CsvAiQueryDemo/          ASP.NET Core Minimal API and static frontend
tests/CsvAiQueryDemo.Tests/  xUnit tests
data/demodaten.csv           Demo contact dataset, semicolon separated
output/                      Generated profile, intent and result JSON
docs/                        Architecture and best-practice notes
```

## Setup

Prerequisite:

- .NET 8 SDK

Create a local environment file:

```powershell
Copy-Item .env.example .env
```

Then edit `.env`. The file is ignored by Git.

### OpenAI Responses API

```env
OPENAI_USE_PROXY=false
OPENAI_ENABLE_FALLBACK=false

OPENAI_PROVIDER=openai
# AI_PROVIDER=openai is also accepted
OPENAI_API_KEY=your-api-key
OPENAI_MODEL=gpt-5.1
# AI_MODEL=gpt-5.1 is also accepted
OPENAI_RESPONSES_ENDPOINT=https://api.openai.com/v1/responses
```

### Azure OpenAI Chat Completions

```env
OPENAI_USE_PROXY=false
OPENAI_ENABLE_FALLBACK=false

OPENAI_PROVIDER=azure
# AI_PROVIDER=azure_openai is also accepted
AZURE_OPENAI_ENDPOINT=https://your-resource.cognitiveservices.azure.com/
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_DEPLOYMENT=gpt-5.1
AZURE_OPENAI_API_VERSION=2024-12-01-preview
```

For Azure, `AZURE_OPENAI_DEPLOYMENT` wins. If it is missing, the app falls back to `OPENAI_MODEL` and then `AI_MODEL`.

Set `OPENAI_ENABLE_FALLBACK=true` only when you explicitly want the local demo fallback for supported example questions. Keep it `false` when validating the real OpenAI or Azure OpenAI integration.

## Run

```powershell
dotnet build
dotnet test
dotnet run --project src/CsvAiQueryDemo
```

Open the URL printed by ASP.NET Core, usually `http://localhost:5000` or `https://localhost:5001`.

Useful API endpoints:

- `GET /api/dataset/profile`
- `POST /api/chat`
- `GET /api/output/query-intent`
- `GET /api/output/query-result`

## Example Questions

- Wie oft kommt der Vorname Max vor?
- Wie viele unterschiedliche Vornamen gibt es?
- Welche Vornamen kommen am häufigsten vor?
- Zeige mir alle Einträge, bei denen der Nachname Müller ist.
- Wie viele Mail-Adressen enthalten example.com?

## Pipeline

1. CSV is loaded from `data/demodaten.csv`.
2. `DatasetProfiler` creates `output/dataset-profile.json`.
3. The API receives the user question.
4. `QueryIntentService` asks the configured LLM provider for structured JSON.
5. `QueryIntentService` writes `output/query-intent.json`.
6. `QueryEngine` executes the intent locally in C#.
7. `QueryEngine` writes `output/query-result.json`.
8. `ResultExplanationService` creates a short answer from a safe result summary.
9. The frontend displays the answer, pipeline, full `DatasetProfile`, model prompt details and technical JSON details.

## Data Boundary

`QueryIntentService` sends only:

- user question
- `DatasetProfile` JSON
- allowed `QueryIntent` schema

`ResultExplanationService` sends only:

- user question
- operation, success flag, scalar result, row count, message and source

It does not send returned CSV rows to the LLM. Rows are displayed in the frontend for the user when relevant, but the LLM only receives a row count in the explanation step.
