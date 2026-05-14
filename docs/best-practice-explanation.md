# Why The LLM Is Not The Query Engine

An LLM should not be used as a database or calculator. Counting, filtering, grouping and aggregation are deterministic tasks. They belong in code where behavior is repeatable, testable and auditable.

Sending a full CSV to an LLM creates avoidable problems:

- The model may miscount or overlook rows.
- Large files can exceed context limits or become expensive.
- Sensitive data may be exposed unnecessarily.
- The result is harder to verify than a deterministic query.
- Small prompt or model changes can change answers that should be exact.

This demo uses the LLM only for interpretation. It maps a question such as "Wie oft kommt der Vorname Max vor?" to a structured JSON intent, for example `count` on column `Vorname` with operator `equals` and value `Max`.

The actual answer is produced by `QueryEngine` in C#. That keeps the calculation deterministic and ensures the full CSV is not sent to the LLM.

## What Is Sent To The LLM

For query intent generation, the model receives:

- the user question
- a compact `DatasetProfile`
- the allowed `QueryIntent` schema

The `DatasetProfile` contains metadata and profiling information such as columns, inferred types, row count, examples and top values. It is enough context for translating a question into a structured query, but it is not the dataset itself.

For result explanation, the model receives only a safe `QueryResult` summary:

- operation
- success flag
- scalar result
- row count
- message
- source

Returned result rows are not sent back to the LLM for explanation.

## Why This Split Matters

The LLM handles language ambiguity. The application handles data correctness.

That split keeps the system easier to test:

- Prompt tests can verify that full CSV rows are not included.
- Unit tests can verify every supported query operation.
- API smoke tests can verify the end-to-end pipeline.
- Generated JSON files in `output/` make the intermediate state inspectable.

## Practical Review Checklist

When extending this project, keep these checks intact:

- New operations should be added to `QueryIntentService` schema and `QueryEngine` together.
- Every operation needs deterministic tests in `tests/CsvAiQueryDemo.Tests`.
- Prompt-building tests should keep proving that full CSV rows are not sent to the LLM.
- Result explanation should continue to use only scalar result data and row counts.
