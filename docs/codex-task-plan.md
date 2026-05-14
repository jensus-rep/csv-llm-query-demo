# Codex Task Plan

## Phase 1: Project structure and dataset profiling

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
- This task plan is updated.

### Implementation Notes
- Created solution `CsvAiQueryDemo.sln`.
- Added ASP.NET Core Minimal API project in `src/CsvAiQueryDemo`.
- Added xUnit test project in `tests/CsvAiQueryDemo.Tests`.
- Implemented `CsvLoader` with UTF-8 reading, header parsing and semicolon delimiter support.
- Implemented `DatasetProfiler` with row/column counts, inferred types, examples and top values.
- Added `data/demodaten.csv`.
- Startup writes `output/dataset-profile.json`.

### Test Evidence
- `dotnet build` succeeded with 0 warnings and 0 errors.
- `dotnet test` succeeded: 3 tests passed.
- Application startup generated `output/dataset-profile.json` with 12 rows and 4 columns.

### GPT Data Boundary Review
- Phase 1 has no GPT integration. No CSV data is sent to any LLM.

## Phase 2: Deterministic query engine

### Plan
- Add `QueryIntent` and `QueryResult` models.
- Implement local deterministic `QueryEngine`.
- Support `count`, `filter`, `distinct`, `top_values` and `group_by_count`.
- Validate unsupported operations, missing columns and unknown columns.
- Persist query results as JSON.
- Add unit tests for all supported operations and error handling.

### Acceptance Criteria
- `count`, `filter`, `distinct`, `top_values`, `group_by_count` work.
- Invalid columns and operations return understandable errors.
- `output/query-result.json` is written.
- Tests for `QueryEngine` run.
- This task plan is updated.

### Implementation Notes
- Added `QueryIntent` and `QueryResult` models.
- Implemented `QueryEngine` with supported operations `count`, `filter`, `distinct`, `top_values` and `group_by_count`.
- Added validation for unsupported operations, missing columns, unknown columns and unsupported operators.
- Added JSON persistence helper for query results.
- Added `output/query-result.json` as a deterministic example output.

### Test Evidence
- `dotnet build` succeeded with 0 warnings and 0 errors.
- `dotnet test` succeeded: 11 tests passed.

### GPT Data Boundary Review
- Phase 2 has no GPT integration. Query execution is deterministic C# code only.

## Phase 3: GPT query intent integration

### Plan
- Add prompt files for QueryIntent generation and result explanation.
- Add OpenAI integration for QueryIntent generation using only DatasetProfile, user question and schema.
- Add optional natural language result explanation using only user question and QueryResult.
- Add local fallback for supported demo questions when `OPENAI_API_KEY` is not configured.
- Persist generated QueryIntent as JSON.
- Add tests proving that QueryIntent prompts do not include full CSV rows.

### Acceptance Criteria
- GPT gets only DatasetProfile, user question and QueryIntent schema.
- GPT never gets the full CSV.
- QueryIntent is generated as JSON.
- `output/query-intent.json` is written.
- Result explanation works optionally.
- Missing API key errors are understandable.
- This task plan is updated.

### Implementation Notes
- Added `Prompts/query-intent-system-prompt.txt` with explicit instructions that the model receives no full CSV and must return JSON only.
- Added `Prompts/result-explanation-system-prompt.txt` for short factual answers based only on `QueryResult`.
- Implemented `QueryIntentService` using the OpenAI Responses API and JSON schema output.
- `QueryIntentService.BuildUserPrompt` includes only DatasetProfile JSON, the user question and the allowed QueryIntent schema.
- Implemented local fallback intents for the demo questions when `OPENAI_API_KEY` is missing.
- Implemented `ResultExplanationService` with optional OpenAI use and local fallback explanation.
- Added `.env.example`, removed `appsettings.example.json`, and kept local `.env` ignored.
- Added `output/query-intent.json` as an example output.

### Test Evidence
- `dotnet build` succeeded with 0 warnings and 0 errors.
- `dotnet test` succeeded: 13 tests passed.
- Added test verifying that the QueryIntent prompt does not include full CSV row strings.

### GPT Data Boundary Review
- Verified by code review and test: `QueryIntentService` receives `DatasetProfile`, user question and schema only.
- No service passes the in-memory row collection to OpenAI.
- No full CSV rows are written into prompt files.

## Phase 4: Minimal API and frontend

### Plan
- Add Minimal API endpoints.
- Add static frontend in `wwwroot`.
- Display DatasetProfile, pipeline steps, chat, examples and technical JSON details.
- Add documentation files and update README.
- Run final build, tests and data-boundary review.

### Acceptance Criteria
- `GET /api/dataset/profile` works.
- `POST /api/chat` works.
- Frontend loads DatasetProfile.
- Chat works.
- Pipeline steps are displayed.
- QueryIntent and QueryResult are visible in the frontend.
- Notice is visible: the full CSV was not sent to the LLM.
- `dotnet build` and `dotnet test` succeed.
- This task plan is updated.

### Implementation Notes
- Added Minimal API endpoints:
  - `GET /api/dataset/profile`
  - `POST /api/chat`
  - `GET /api/output/query-intent`
  - `GET /api/output/query-result`
- Added static frontend in `wwwroot` with dataset panel, pipeline panel, chat panel, examples and technical details.
- Added visible frontend notice that the full CSV was not sent to the LLM.
- Added `ChatRequest`, `ChatResponse` and `PipelineStep` models.
- Added `PipelineSteps` helper for transparent step status output.
- Updated `ResultExplanationService` so optional LLM explanation receives a safe QueryResult summary with row count, not full result rows.
- Added architecture and best practice documentation.
- Expanded README with goal, setup, environment variables, pipeline and example questions.

### Test Evidence
- `dotnet build` succeeded with 0 warnings and 0 errors.
- `dotnet test` succeeded: 14 tests passed.
- Local API smoke test on `http://localhost:5086`:
  - `GET /api/dataset/profile` returned 12 rows and 4 columns.
  - `POST /api/chat` for "Wie oft kommt der Vorname Max vor?" returned a fallback QueryIntent `count` on `Vorname` and deterministic result `4`.

### GPT Data Boundary Review
- `QueryIntentService` sends only DatasetProfile, user question and QueryIntent schema.
- `QueryEngine` receives the in-memory rows and executes deterministically in C#.
- `ResultExplanationService` sends only a safe summary of QueryResult and row count, not full rows.
- Frontend displays QueryResult rows to the user when relevant, but those rows are not sent to the LLM.
