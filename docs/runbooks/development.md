# Development Runbook

1. Install the SDK specified by `global.json`.
2. Copy `docker/.env.example` to `docker/.env` and change local passwords.
3. Run `./scripts/bootstrap.sh`.
4. Start dependencies with `docker compose --env-file docker/.env up -d` when needed.
5. Run `./scripts/verify.sh` before every pull request.
