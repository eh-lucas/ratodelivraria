#!/bin/bash
set -e

# Cria o usuario de aplicacao com privilegios limitados.
# A API conecta como este usuario — nunca como o superusuario (sherlock_admin).
# Roda apenas na primeira inicializacao do volume (banco vazio).
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE USER "${APP_DB_USER}" WITH PASSWORD '${APP_DB_PASSWORD}';

    GRANT CONNECT ON DATABASE "${POSTGRES_DB}" TO "${APP_DB_USER}";

    -- App precisa de DDL porque o EF Core roda migrations no startup;
    -- damos ownership do schema public (suficiente p/ criar tabelas) ao inves de superusuario.
    ALTER SCHEMA public OWNER TO "${APP_DB_USER}";
    GRANT ALL ON SCHEMA public TO "${APP_DB_USER}";
EOSQL
