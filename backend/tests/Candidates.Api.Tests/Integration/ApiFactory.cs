using Candidates.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Candidates.Api.Tests.Integration;

// Sobe a API de verdade (em memória) contra um SQL Server real, num container. Um container para todos os testes.
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // A mesma imagem do docker-compose.yml.
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public Task InitializeAsync() => _sqlServer.StartAsync();

    Task IAsyncLifetime.DisposeAsync() => _sqlServer.DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" não lê o user-secrets nem o appsettings.Development.json: nada aponta para o banco de desenvolvimento.
        builder.UseEnvironment("Testing");

        var connection = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "CandidatesTests",
        };
        builder.UseSetting("ConnectionStrings:Default", connection.ConnectionString);

        // Cada execução cria o banco do zero pelas migrations, como no README.
        builder.UseSetting("Database:MigrateOnStartup", "true");
    }

    // Os testes compartilham o banco: quem precisa de um estado conhecido começa com a tabela vazia.
    public async Task ResetAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Candidates.ExecuteDeleteAsync();
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
