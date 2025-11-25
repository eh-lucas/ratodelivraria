-- =============================================================================
-- Script para migrar banco de dados para snake_case
-- Execute ANTES de rodar: dotnet ef database update
-- =============================================================================

-- 1. Renomear colunas da tabela de histórico de migrações do EF Core
ALTER TABLE "__EFMigrationsHistory" RENAME COLUMN "MigrationId" TO "migration_id";
ALTER TABLE "__EFMigrationsHistory" RENAME COLUMN "ProductVersion" TO "product_version";

-- Pronto! Agora execute: dotnet ef database update
-- O EF Core vai aplicar a migração UseSnakeCaseNaming que renomeia todas as outras tabelas e colunas
