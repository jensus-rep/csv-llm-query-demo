# Architecture

CsvAiQueryDemo uses a small deterministic pipeline with an LLM only at the interpretation boundary.

```text
CSV
-> CsvLoader
-> DatasetProfiler
-> DatasetProfile
-> QueryIntentService
-> LLM provider
-> QueryIntent
-> QueryEngine
-> QueryResult
-> ResultExplanationService
-> Frontend
```

## Runtime Flow

At startup, `Program.cs` loads `data/demodaten.csv` with `CsvLoader`, creates a `DatasetProfile` with `DatasetProfiler`, and writes `output/dataset-profile.json`.

The CSV is UTF-8, semicolon separated and has a header row. The current demo dataset contains 2,000 rows and 4 columns: `Rufnummer`, `Vorname`, `Nachname` and `Mail`.

For each chat request:

1. `POST /api/chat` receives a natural-language question.
2. `QueryIntentService` builds a prompt from the user question, `DatasetProfile` JSON and the allowed `QueryIntent` schema.
3. The configured provider returns a structured `QueryIntent`.
4. `QueryEngine` validates the operation, column and operator.
5. `QueryEngine` executes the query against the in-memory CSV rows.
6. `QueryResult` is written to `output/query-result.json`.
7. `ResultExplanationService` creates a short answer from a safe summary.
8. The frontend displays the answer, pipeline status, full `DatasetProfile`, model prompt details, `QueryIntent` and `QueryResult`.

## Providers

`OpenAiProviderOptions` selects the provider from environment variables:

- OpenAI Responses API when `OPENAI_PROVIDER=openai` or no Azure endpoint is configured.
- Azure OpenAI Chat Completions when `OPENAI_PROVIDER=azure`, `AI_PROVIDER=azure_openai` or `AZURE_OPENAI_ENDPOINT` is configured.

Supported aliases:

- `AI_PROVIDER` for `OPENAI_PROVIDER`
- `AI_MODEL` for `OPENAI_MODEL`
- `AI_API_KEY` for provider API keys
- `AI_RESPONSES_ENDPOINT` for `OPENAI_RESPONSES_ENDPOINT`
- `AI_API_VERSION` for `OPENAI_API_VERSION`

`OPENAI_USE_PROXY=true` enables proxy use on the underlying `SocketsHttpHandler`.

## QueryIntent Contract

Supported operations:

- `count`
- `filter`
- `distinct`
- `top_values`
- `group_by_count`

Supported filter operators:

- `equals`
- `contains`
- `starts_with`
- `ends_with`

`QueryEngine` rejects unsupported operations, missing required columns, unknown columns and unsupported operators with a deterministic error `QueryResult`.

## Data Boundary

The full CSV row collection is held only in application memory and passed to `QueryEngine`.

The intent-generation LLM call receives:

- the user question
- the generated `DatasetProfile`
- the allowed `QueryIntent` JSON schema

The result-explanation LLM call receives:

- the user question
- operation, success flag, scalar result, row count, message and source

It does not receive full result rows. This keeps counting, filtering, grouping and aggregation deterministic and auditable in C#.
