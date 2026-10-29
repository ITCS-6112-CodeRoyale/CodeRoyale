using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CodeRoyale.Data;

/// <summary>
/// This class is called at design time by the <c>dotnet ef migrations</c> tool.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public const string ConnectionStringName = "Database";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();

        string? connectionString = config.GetConnectionString(ConnectionStringName);

        if (connectionString == null)
        {
            throw new FormatException($"Connection string not found. Add to user secrets at \"ConnectionStrings:{ConnectionStringName}\".");
        }

        DbContextOptionsBuilder<ApplicationDbContext> builder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(connectionString, ServerVersion.Create(8, 0, 0, ServerType.MySql));

        DbContextOptions<ApplicationDbContext> options = builder.Options;

        return new ApplicationDbContext(options);
    }
}
