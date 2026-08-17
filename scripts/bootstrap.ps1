$ErrorActionPreference = "Stop"
dotnet --version
dotnet restore Diyarak.Platform.All.sln
dotnet build Diyarak.Platform.All.sln -c Debug --no-restore
dotnet test Diyarak.Platform.All.sln -c Debug --no-build
git config core.hooksPath .githooks
