# Development Runbook

1. Install the SDK specified by `global.json`.
2. Copy `docker/.env.example` to `docker/.env` and change local passwords.
3. Run `./scripts/bootstrap.sh`.
4. Start dependencies with `docker compose --env-file docker/.env up -d` when needed.
5. Run `./scripts/verify.sh` before every pull request.

## Windows API startup

From PowerShell, start the existing PostgreSQL service and the Development API
with one command (Docker Desktop must already be running):

```powershell
.\scripts\start-api-dev.ps1
```

The script locates the repository from its own path, requires the existing
`docker/.env`, checks Docker/Compose and the selected .NET SDK, starts only the
`postgres` service, waits up to 120 seconds for its configured health check,
then runs the API's `http` launch profile. The frontend is available at
`http://localhost:5227/app/` after the API reports it is listening.

To start/check PostgreSQL without launching a second API process:

```powershell
.\scripts\start-api-dev.ps1 -DatabaseOnly
```

For a slower initial startup, override the bounded wait:

```powershell
.\scripts\start-api-dev.ps1 -DatabaseOnly -DatabaseWaitSeconds 240
```

A missing environment file, unavailable Docker engine, failed Compose startup,
stopped/unhealthy container, or wait timeout stops the script before API launch.
It does not create or modify `.env`, display its contents, change credentials,
run migrations, or delete volumes. Existing PostgreSQL data is retained.
Stopping the API with Ctrl+C leaves PostgreSQL running for the next session.
If an API process is already running on port 5227, stop it first or use
`-DatabaseOnly`; the script does not stop another process.
