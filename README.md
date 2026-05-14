# CsvAiQueryDemo

The LLM is not used as a database or calculator. It only translates a natural language question into a structured query intent. The actual query is executed deterministically in C#.

## Goal

This .NET 8 MVP demonstrates how structured CSV data can be queried with AI assistance without sending the full CSV to an LLM.

The application loads `data/demodaten.csv`, creates a `DatasetProfile`, asks the LLM only for a structured `QueryIntent`, and executes the actual query locally in C#.

## Architecture Principle

The LLM receives only:

- User question
- `DatasetProfile` JSON
- Allowed `QueryIntent` schema

The LLM never receives the full CSV. It does not count, filter or aggregate data. `QueryEngine` performs those operations deterministically in C#.

## Setup

Prerequisites:

- .NET 8 SDK

OpenAI configuration:

```powershell
Copy-Item .env.example .env
```

Then edit `.env`:

```env
OPENAI_API_KEY=your-api-key
OPENAI_MODEL=gpt-5.1
OPENAI_RESPONSES_ENDPOINT=https://api.openai.com/v1/responses
OPENAI_USE_PROXY=false
OPENAI_ENABLE_FALLBACK=false
```

`.env` is ignored by Git; `.env.example` is safe to commit.

Set `OPENAI_ENABLE_FALLBACK=true` only when you explicitly want the local demo fallback for example questions. Keep it `false` when you want to test the real OpenAI integration.

## Run

```powershell
dotnet build
dotnet test
dotnet run --project src/CsvAiQueryDemo
```

Open the URL printed by ASP.NET Core, usually `http://localhost:5000` or `https://localhost:5001`.

## Example Questions

- Wie oft kommt der Vorname Max vor?
- Wie viele unterschiedliche Vornamen gibt es?
- Welche Vornamen kommen am häufigsten vor?
- Zeige mir alle Einträge, bei denen der Nachname Müller ist.
- Wie viele Mail Adressen enthalten example.com?

## Pipeline

1. CSV is loaded locally.
2. CSV is technically parsed.
3. `DatasetProfile` is written to `output/dataset-profile.json`.
4. LLM receives only question, profile and schema.
5. LLM returns `QueryIntent` JSON.
6. `QueryEngine` executes locally in C#.
7. `QueryResult` is written to `output/query-result.json`.
8. The frontend displays answer, pipeline, QueryIntent and QueryResult.

The full CSV is not sent to the LLM.
