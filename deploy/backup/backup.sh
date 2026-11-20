#!/bin/sh
# Periodic PostgreSQL dump and media archive with retention. Runs inside postgres:17-alpine.
set -eu

: "${PGHOST:=postgres}"
: "${BACKUP_INTERVAL_HOURS:=24}"
: "${BACKUP_RETENTION_DAYS:=14}"
BACKUP_DIR=/backups

mkdir -p "$BACKUP_DIR/db" "$BACKUP_DIR/media"

while true; do
  stamp=$(date -u +%Y%m%dT%H%M%SZ)

  if pg_dump --format=custom --no-owner --file="$BACKUP_DIR/db/diyarak-$stamp.dump.tmp"; then
    mv "$BACKUP_DIR/db/diyarak-$stamp.dump.tmp" "$BACKUP_DIR/db/diyarak-$stamp.dump"
    echo "$(date -u) database backup diyarak-$stamp.dump written"
  else
    rm -f "$BACKUP_DIR/db/diyarak-$stamp.dump.tmp"
    echo "$(date -u) database backup FAILED" >&2
  fi

  if [ -d /media ]; then
    tar -czf "$BACKUP_DIR/media/media-$stamp.tar.gz.tmp" -C /media . \
      && mv "$BACKUP_DIR/media/media-$stamp.tar.gz.tmp" "$BACKUP_DIR/media/media-$stamp.tar.gz" \
      && echo "$(date -u) media backup media-$stamp.tar.gz written"
  fi

  find "$BACKUP_DIR" -type f -mtime +"$BACKUP_RETENTION_DAYS" -delete
  sleep $((BACKUP_INTERVAL_HOURS * 3600))
done
