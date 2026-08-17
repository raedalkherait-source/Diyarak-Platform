# Diyarak Foundation Enterprise v1.0.1 Patch

Fixes strict analyzer failures discovered during the first Windows .NET 10 Release build:

- CA1000: moved generic API response factories to a non-generic factory type.
- IDE0040: made the interface member accessibility explicit.
- CA1512: adopted modern ArgumentOutOfRangeException throw helpers.
- CA1036: completed Money relational operators for IComparable semantics.
- xUnit2009: replaced Assert.True string-suffix check with Assert.EndsWith.
- IDE0005 build configuration: enabled documentation generation in architecture tests.
