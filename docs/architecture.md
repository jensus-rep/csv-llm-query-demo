# Architecture

The demo uses a small deterministic pipeline:

CSV
→ `CsvLoader`
→ `DatasetProfiler`
→ `DatasetProfile`
→ LLM creates `QueryIntent`
→ `QueryEngine` executes locally
→ `QueryResult`
→ Frontend

The CSV file is loaded from `data/demodaten.csv` into application memory. `CsvLoader` parses UTF-8 semicolon-separated data with a header row. `DatasetProfiler` creates metadata and profiling information such as row count, column count, inferred types, examples and top values.

The LLM receives only the user's question, the `DatasetProfile` JSON and the allowed `QueryIntent` schema. It does not receive the full CSV and does not calculate the answer.

`QueryEngine` validates the operation and column names, then executes `count`, `filter`, `distinct`, `top_values` or `group_by_count` in C#. The result is saved as `output/query-result.json` and returned to the frontend.
