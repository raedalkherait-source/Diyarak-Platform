# Secure Engineering Baseline

- No secrets in source control.
- NuGet auditing is enabled at moderate severity.
- CodeQL and dependency review run in GitHub Actions.
- External inputs are validated at system boundaries.
- Personally identifiable information must be classified before persistence or logging.
- Local Docker credentials are non-production and must be changed.
- Production images must use immutable digests and undergo vulnerability scanning.
