#!/usr/bin/env bash
# Compila o Tailwind (CLI standalone, sem Node) para wwwroot/css/site.css.
# Uso: ./scripts/build-css.sh [--watch]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TOOLS="$ROOT/.tools"
BIN="$TOOLS/tailwindcss"
CSS_IN="$ROOT/src/Iscuissatis.Web/Styles/site.css"
CSS_OUT="$ROOT/src/Iscuissatis.Web/wwwroot/css/site.css"

if [[ ! -x "$BIN" ]]; then
  echo "Baixando Tailwind standalone..."
  mkdir -p "$TOOLS"
  curl -sSL -o "$BIN" \
    "https://github.com/tailwindlabs/tailwindcss/releases/latest/download/tailwindcss-linux-x64"
  chmod +x "$BIN"
fi

if [[ "${1:-}" == "--watch" ]]; then
  exec "$BIN" --input "$CSS_IN" --output "$CSS_OUT" --watch
fi

"$BIN" --input "$CSS_IN" --output "$CSS_OUT" --minify
echo "CSS gerado em src/Iscuissatis.Web/wwwroot/css/site.css"
