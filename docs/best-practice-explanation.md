# Best Practice Explanation

An LLM should not be used as a database or calculator. Counting, filtering, grouping and aggregation are deterministic tasks. They belong in code where the behavior is repeatable, testable and auditable.

Sending a full CSV to an LLM creates avoidable problems:

- The model may miscount or overlook rows.
- Large files can exceed context limits or become expensive.
- Sensitive data may be exposed unnecessarily.
- The result is harder to verify than a deterministic query.

This demo sends only a `DatasetProfile` to the LLM. The profile contains metadata and profiling information: columns, inferred types, row count, examples and top values. It is enough context for translating a question into a structured `QueryIntent`, but it is not the dataset itself.

The useful role for the LLM is interpretation. It maps a natural language question such as "Wie oft kommt der Vorname Max vor?" to a JSON intent such as `count` on column `Vorname` with operator `equals` and value `Max`.

The actual answer is produced by C# in `QueryEngine`. That keeps the calculation deterministic and ensures the full CSV is not sent to the LLM.
