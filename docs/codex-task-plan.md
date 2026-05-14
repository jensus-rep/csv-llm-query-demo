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

Pending.

## Phase 4: Minimal API and frontend

Pending.
