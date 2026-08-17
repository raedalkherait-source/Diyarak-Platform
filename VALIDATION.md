# Validation Report

The generated repository passed these artifact-generation checks:

- JSON parsing.
- MSBuild/XML parsing.
- Project-reference target existence.
- Solution project-path existence.
- Basic C# delimiter integrity.
- SHA-256 manifest generation.

A full `dotnet restore`, build, test, Docker pull, and GitHub Actions execution could not be run in the artifact-generation environment because the .NET SDK and Docker engine were not installed there. Run `./scripts/verify.sh` in the provided .NET 10 dev container or a machine with the SDK from `global.json` before accepting the release.
