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
.\scripts\start-api-dev.cmd
```

The Windows `.cmd` launcher invokes the adjacent PowerShell script with
`-NoProfile -ExecutionPolicy Bypass` for that child process only. It forwards
arguments and the script exit code; no persistent execution policy is changed.
This also works when direct `.ps1` execution is disabled.

The script locates the repository from its own path, requires the existing
`docker/.env`, checks Docker/Compose and the selected .NET SDK, starts only the
`postgres` service, waits up to 120 seconds for its configured health check,
then runs the API's `http` launch profile. The frontend is available at
`http://localhost:5227/app/` after the API reports it is listening.

To start/check PostgreSQL without launching a second API process:

```powershell
.\scripts\start-api-dev.cmd -DatabaseOnly
```

For a slower initial startup, override the bounded wait:

```powershell
.\scripts\start-api-dev.cmd -DatabaseOnly -DatabaseWaitSeconds 240
```

A missing environment file, unavailable Docker engine, failed Compose startup,
stopped/unhealthy container, or wait timeout stops the script before API launch.
It does not create or modify `.env`, display its contents, change credentials,
run migrations, or delete volumes. Existing PostgreSQL data is retained.
Stopping the API with Ctrl+C leaves PostgreSQL running for the next session.
If an API process is already running on port 5227, stop it first or use
`-DatabaseOnly`; the script does not stop another process.

## Read-only API readiness check

Once the Development API is listening, use a second PowerShell window:

```powershell
.\scripts\check-api-dev.cmd
$LASTEXITCODE
```

The check issues anonymous GET requests to the local frontend HTML, JavaScript,
CSS and public listing collection (`page=1&pageSize=1`). It reports HTTP status
and duration, verifies collection pagination metadata and, if an item exists,
checks its detail endpoint and matching identifier. An empty collection is a
successful check and skips the detail request. It prints no response bodies,
credentials or environment-file contents.

The check does not start/stop services, build the application, run migrations,
or change data. Success returns 0; HTTP, connection, timeout or invalid-response
errors return a nonzero exit code. It is not a replacement for browser/keyboard
checks or the full automated test suite. As with the startup launcher, the
Windows wrapper uses a child-process-only execution policy.

Optional local port and request timeout:

```powershell
.\scripts\check-api-dev.cmd -Port 5227 -TimeoutSeconds 30
```

## Full Windows verification

With the selected .NET SDK and Node.js (including `node --test`) installed:

```powershell
.\scripts\verify.cmd
$LASTEXITCODE
```

The Windows verifier resolves the repository from the script path, checks
required commands, then runs frontend behavior tests, .NET restore, formatting
verification, Release build and Release .NET tests. Every native command's exit
code is checked; the first failure stops later steps and yields a nonzero exit
code. The final success message is printed only after every step passes.
Node.js is required; frontend tests are not silently skipped. No npm install
or additional frontend dependencies are needed for these tests.

The existing generated `artifacts` cleanup is scoped to the repository. The
caller's working directory is restored afterward. Stop a running Development
API before the Release build to avoid file locking. The `.cmd` wrapper uses the
same process-only PowerShell execution policy as the other Windows launchers.
This verifier does not start services or invoke the live readiness check.
