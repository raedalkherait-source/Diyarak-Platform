SOLUTION := Diyarak.Platform.All.sln
CONFIGURATION ?= Release

.PHONY: restore format build test coverage verify pack services-up services-down
restore:
	dotnet restore $(SOLUTION)
format:
	dotnet format $(SOLUTION) --verify-no-changes --no-restore
build:
	dotnet build $(SOLUTION) -c $(CONFIGURATION) --no-restore
test:
	dotnet test $(SOLUTION) -c $(CONFIGURATION) --no-build
coverage:
	./scripts/test-coverage.sh
verify:
	./scripts/verify.sh
pack:
	dotnet pack $(SOLUTION) -c Release --no-build -o artifacts/packages
services-up:
	docker compose --env-file docker/.env up -d
services-down:
	docker compose --env-file docker/.env down
