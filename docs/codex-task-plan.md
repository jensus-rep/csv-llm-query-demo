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

Pending.

## Phase 3: GPT query intent integration

Pending.

## Phase 4: Minimal API and frontend

Pending.
