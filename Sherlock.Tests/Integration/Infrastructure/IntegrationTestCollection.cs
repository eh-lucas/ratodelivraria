namespace Sherlock.Tests.Integration.Infrastructure;

// Compartilha um unico SherlockApiFactory entre todas as classes de teste de integracao.
// Garante que so sobe um container Postgres por execucao (rapido) e roda sequencial.
[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<SherlockApiFactory>;
