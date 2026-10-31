#!/usr/bin/env bash
#
# Dump diario do banco do Sherlock.
#
# Existe porque ate 31/10/2026 nao havia copia nenhuma: os 541 MB viviam num
# unico volume Docker, num unico NVMe. O que doi perder e o catalog_items -- 54
# mil linhas que sao a colheita de crawlear as 67 lojas. As queries sao cache
# com TTL de 60 min, mas vao junto: o dump inteiro comprime para ~23 MB, e
# separar daria mais trabalho do que o espaco economizado vale.
#
# ATENCAO -- isto grava em ~/backups/sherlock, no MESMO disco do banco. Protege
# contra DELETE errado, bug e migration ruim; NAO protege contra o SSD morrer,
# a maquina ser roubada ou um incendio. Para isso a copia tem de sair daqui
# (pendrive, outra maquina, bucket). Enquanto essa segunda camada nao existir,
# o risco de perder a maquina segue descoberto -- e consciente.
#
# Uso:
#   scripts/backup-db.sh            # dump + validacao + expurgo
#   DIAS_RETENCAO=30 scripts/backup-db.sh
#
# Cron (diario as 3h):
#   0 3 * * * /home/lucas/Desktop/Projects/Sherlock/scripts/backup-db.sh >> ~/backups/sherlock/backup.log 2>&1

set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DESTINO="${DESTINO:-$HOME/backups/sherlock}"
DIAS_RETENCAO="${DIAS_RETENCAO:-14}"
CONTAINER="${CONTAINER:-sherlock-postgres}"

log() { printf '%s  %s\n' "$(date '+%Y-%m-%d %H:%M:%S')" "$*"; }
falhar() { log "ERRO: $*"; exit 1; }

# O .env tem os segredos; nao e versionado.
[[ -f "$RAIZ/.env" ]] || falhar "nao achei $RAIZ/.env"
# shellcheck disable=SC1091
set -a; source "$RAIZ/.env"; set +a
: "${POSTGRES_USER:?POSTGRES_USER ausente no .env}"
: "${POSTGRES_DB:?POSTGRES_DB ausente no .env}"

docker inspect -f '{{.State.Running}}' "$CONTAINER" 2>/dev/null | grep -q true \
  || falhar "container $CONTAINER nao esta rodando"

mkdir -p "$DESTINO"
ARQUIVO="$DESTINO/sherlock-$(date +%Y%m%d-%H%M).dump"

# Escreve em .parcial e so renomeia no fim: dump interrompido nunca se parece
# com um backup bom, e o expurgo mais abaixo nunca o confunde com um.
log "iniciando dump de $POSTGRES_DB"
if ! docker exec "$CONTAINER" pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc > "$ARQUIVO.parcial"; then
  rm -f "$ARQUIVO.parcial"
  falhar "pg_dump falhou"
fi

# Um dump que nao abre nao e backup. Conferir agora custa um segundo; descobrir
# no dia do restore custa o banco.
if ! docker exec -i "$CONTAINER" pg_restore --list < "$ARQUIVO.parcial" > /dev/null 2>&1; then
  rm -f "$ARQUIVO.parcial"
  falhar "dump gerado nao passou na validacao, descartado"
fi

mv "$ARQUIVO.parcial" "$ARQUIVO"
log "ok: $ARQUIVO ($(du -h "$ARQUIVO" | cut -f1))"

# Expurgo so depois do dump bom existir: sequencia invertida apagaria o
# historico justamente na noite em que o dump novo falhou.
APAGADOS=$(find "$DESTINO" -maxdepth 1 -name 'sherlock-*.dump' -type f -mtime "+$DIAS_RETENCAO" -print -delete | wc -l)
[[ "$APAGADOS" -gt 0 ]] && log "expurgados $APAGADOS dump(s) com mais de $DIAS_RETENCAO dias"

log "backups mantidos: $(find "$DESTINO" -maxdepth 1 -name 'sherlock-*.dump' -type f | wc -l) ($(du -sh "$DESTINO" | cut -f1))"
