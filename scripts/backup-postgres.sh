#!/usr/bin/env bash
# Backup do PostgreSQL da loja, com retenção local.
#
# Uso local (docker compose):
#   ./scripts/backup-postgres.sh
#
# No Coolify: agende este script (ou use o backup nativo do recurso Postgres)
# e envie o arquivo para fora do servidor — backup no mesmo disco não protege
# contra perda do servidor.
#
# Restauração (testada antes do lançamento):
#   gunzip -c backups/iscuissatis-AAAAMMDD-HHMM.sql.gz | \
#     docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DEST="${BACKUP_DIR:-$ROOT/backups}"
RETAIN_DAYS="${RETAIN_DAYS:-14}"
DB_SERVICE="${DB_SERVICE:-db}"
DB_USER="${POSTGRES_USER:-iscuissatis}"
DB_NAME="${POSTGRES_DB:-iscuissatis}"

mkdir -p "$DEST"
STAMP="$(date +%Y%m%d-%H%M)"
FILE="$DEST/iscuissatis-$STAMP.sql.gz"

echo "Dump de $DB_NAME..."
docker compose -f "$ROOT/docker-compose.yml" exec -T "$DB_SERVICE" \
  pg_dump -U "$DB_USER" --no-owner --no-privileges "$DB_NAME" | gzip -9 > "$FILE"

SIZE="$(du -h "$FILE" | cut -f1)"
echo "OK  $FILE  ($SIZE)"

echo "Removendo backups locais com mais de $RETAIN_DAYS dias..."
find "$DEST" -name 'iscuissatis-*.sql.gz' -mtime "+$RETAIN_DAYS" -print -delete || true

echo
echo "Lembrete: copie este arquivo para fora do servidor (S3/R2/outro host) e"
echo "teste a restauração em um banco novo pelo menos uma vez antes de lançar."
