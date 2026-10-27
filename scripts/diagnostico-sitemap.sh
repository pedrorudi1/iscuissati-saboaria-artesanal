#!/usr/bin/env bash
# Diagnóstico do sitemap no ambiente local (docker compose).
# Uso: ./scripts/diagnostico-sitemap.sh [http://localhost:8080]
set -euo pipefail

BASE="${1:-http://localhost:8080}"
OUT="/tmp/sitemap-$(date +%Y%m%d-%H%M%S).xml"

echo "== Subindo a aplicação (se ainda não estiver no ar)"
docker compose up -d --build

echo "== Aguardando /health responder 200"
for attempt in $(seq 1 60); do
  code=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/health" || true)
  if [ "$code" = "200" ]; then break; fi
  sleep 2
done
echo "/health -> $code"

echo "== Baixando o sitemap"
status=$(curl -s -o "$OUT" -w "%{http_code}" "$BASE/sitemap.xml")
echo "HTTP $status · arquivo: $OUT · bytes: $(wc -c < "$OUT")"
head -c 400 "$OUT"
echo

echo "== Contagens"
echo "urls no sitemap:      $(grep -c '<loc>' "$OUT" || true)"
echo "rotas /produto/:      $(grep -c '/produto/' "$OUT" || true)"
echo "rotas /sabonetes?:    $(grep -c '/sabonetes' "$OUT" || true)"

echo "== Produtos publicados no banco"
docker compose exec -T db psql -U "${POSTGRES_USER:-iscuissatis}" -d "${POSTGRES_DB:-iscuissatis}" -c \
  "select count(*) as publicados from \"Products\" where \"Status\" = 1;"
docker compose exec -T db psql -U "${POSTGRES_USER:-iscuissatis}" -d "${POSTGRES_DB:-iscuissatis}" -c \
  "select \"Name\", \"Slug\" from \"Products\" where \"Status\" = 1 order by \"Name\" limit 5;"

echo "== Categorias ativas"
docker compose exec -T db psql -U "${POSTGRES_USER:-iscuissatis}" -d "${POSTGRES_DB:-iscuissatis}" -c \
  "select count(*) as ativas from \"Categories\" where \"IsActive\" = true;"

echo
echo "Se 'publicados' > 0 e o sitemap tiver menos '/produto/' que isso, o problema está na consulta."
echo "Se 'publicados' = 0, cadastre/publique um produto pelo painel antes de testar."
