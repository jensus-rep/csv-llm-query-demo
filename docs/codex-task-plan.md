# Codex Task Plan

This file records the implementation phases that produced the current MVP and the current documentation status.

## Current Status

- .NET 8 solution with ASP.NET Core Minimal API and xUnit tests.
- Static frontend in `src/CsvAiQueryDemo/wwwroot`.
- Demo dataset in `data/demodaten.csv` with 2,000 rows and 4 columns.
- Generated artifacts in `output/`:
  - `dataset-profile.json`
  - `query-intent.json`
  - `query-result.json`
- LLM provider support:
  - OpenAI Responses API
  - Azure OpenAI Chat Completions
- Optional local fallback for supported demo questions when `OPENAI_ENABLE_FALLBACK=true`.
- Documentation updated for the current provider configuration, endpoints, data boundary and generated artifacts.

## Phase 1: Project Structure And Dataset Profiling

### Plan

- Create .NET solution with ASP.NET Core Minimal API project and test project.
- Add dataset profiling models.
- Implement UTF-8 semicolon CSV loading with header handling.
- Generate and persist `output/dataset-profile.json` at application startup.
- Add unit tests for `CsvLoader` and `DatasetProfiler`.

### Acceptance Criteria

- `dotnet build` succeeds.
- CSV is read.
- `DatasetProfile` is generated.
- `output/dataset-profile.json` is written.
- Tests for `CsvLoader` and `DatasetProfiler` run.

### Implementation Notes

- Created solution `CsvAiQueryDemo.sln`.
- Added ASP.NET Core Minimal API project in `src/CsvAiQueryDemo`.
- Added xUnit test project in `tests/CsvAiQueryDemo.Tests`.
- Implemented `CsvLoader` with UTF-8 reading, header parsing and semicolon delimiter support.
- Implemented `DatasetProfiler` with row/column counts, inferred types, examples and top values.
- Added `data/demodaten.csv`.
- Startup writes `output/dataset-profile.json`.

### Data Boundary Review

Phase 1 has no LLM integration. No CSV data is sent to any model.

## Phase 2: Deterministic Query Engine

### Plan

- Add `QueryIntent` and `QueryResult` models.
- Implement local deterministic `QueryEngine`.
- Support `count`, `filter`, `distinct`, `top_values` and `group_by_count`.
- Validate unsupported operations, missing columns and unknown columns.
- Persist query results as JSON.
- Add unit tests for supported operations and error handling.

### Acceptance Criteria

- `count`, `filter`, `distinct`, `top_values` and `group_by_count` work.
- Invalid columns and operations return understandable errors.
- `output/query-result.json` is written.
- Tests for `QueryEngine` run.

### Implementation Notes

- Added `QueryIntent` and `QueryResult` models.
- Implemented `QueryEngine` with operation and operator validation.
- Added JSON persistence helper for query results.
- Added `output/query-result.json` as an example output.

### Data Boundary Review

Phase 2 has no LLM integration. Query execution is deterministic C# code only.

## Phase 3: LLM Query Intent Integration

### Plan

- Add prompt files for QueryIntent generation and result explanation.
- Add OpenAI integration for QueryIntent generation using only `DatasetProfile`, user question and schema.
- Add optional natural-language result explanation using only user question and safe `QueryResult` summary.
- Add local fallback for supported demo questions when explicitly enabled.
- Persist generated `QueryIntent` as JSON.
- Add tests proving that QueryIntent prompts do not include full CSV rows.

### Acceptance Criteria

- The LLM gets only `DatasetProfile`, user question and `QueryIntent` schema for intent generation.
- The LLM never gets the full CSV.
- `QueryIntent` is generated as JSON.
- `output/query-intent.json` is written.
- Missing API key errors are understandable unless fallback is explicitly enabled.

### Implementation Notes

- Added `Prompts/query-intent-system-prompt.txt`.
- Added `Prompts/result-explanation-system-prompt.txt`.
- Implemented `QueryIntentService` with JSON schema output.
- Added OpenAI Responses API support.
- Added Azure OpenAI Chat Completions support.
- Implemented environment aliases for `AI_PROVIDER`, `AI_MODEL`, `AI_API_KEY`, `AI_RESPONSES_ENDPOINT` and `AI_API_VERSION`.
- Implemented local fallback intents only when `OPENAI_ENABLE_FALLBACK=true`.
- Implemented `ResultExplanationService` with local explanation fallback.

### Data Boundary Review

- `QueryIntentService` receives `DatasetProfile`, user question and schema only.
- No service passes the in-memory row collection to the LLM provider.
- `ResultExplanationService` sends only a safe summary with row count, not full rows.

## Phase 4: Minimal API And Frontend

### Plan

- Add Minimal API endpoints.
- Add static frontend in `wwwroot`.
- Display `DatasetProfile`, pipeline steps, chat, examples and technical JSON details.
- Add documentation files and update README.
- Run final build, tests and data-boundary review.

### Acceptance Criteria

- `GET /api/dataset/profile` works.
- `POST /api/chat` works.
- `GET /api/output/query-intent` works after an intent has been generated.
- `GET /api/output/query-result` works after a query has been executed.
- Frontend loads `DatasetProfile`.
- Chat works.
- Pipeline steps are displayed.
- `QueryIntent` and `QueryResult` are visible in the frontend.
- A notice is visible that the full CSV was not sent to the LLM.
- `dotnet build` and `dotnet test` succeed.

### Implementation Notes

- Added Minimal API endpoints:
  - `GET /api/dataset/profile`
  - `POST /api/chat`
  - `GET /api/output/query-intent`
  - `GET /api/output/query-result`
- Added static frontend in `wwwroot` with dataset panel, pipeline panel, chat panel, examples and technical details.
- Added `ChatRequest`, `ChatResponse` and `PipelineStep` models.
- Added `PipelineSteps` helper for transparent step status output.
- Updated `ResultExplanationService` so optional LLM explanation receives a safe summary with row count, not full result rows.
- Added and refreshed architecture, README and best-practice documentation.

### Data Boundary Review

- `QueryIntentService` sends only `DatasetProfile`, user question and `QueryIntent` schema.
- `QueryEngine` receives the in-memory rows and executes deterministically in C#.
- `ResultExplanationService` sends only a safe `QueryResult` summary and row count.
- Frontend displays `QueryResult` rows to the user when relevant, but those rows are not sent to the LLM.
