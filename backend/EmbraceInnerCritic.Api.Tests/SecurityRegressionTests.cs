using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using EmbraceInnerCritic.Api.Contracts;
using EmbraceInnerCritic.Api.Controllers;
using EmbraceInnerCritic.Api.Data;
using EmbraceInnerCritic.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace EmbraceInnerCritic.Api.Tests;

public sealed class SecurityRegressionTests
{
    private const string UserId = "security-test-user";

    [Fact]
    public async Task Create_RejectsAnswersLargerThan32Kb()
    {
        await using var testDatabase = await CreateDatabaseAsync();
        var database = testDatabase.Context;
        var controller = CreateController(database);
        var request = new UpsertDiaryRequest(
            "山姆",
            ParseAnswers(new string('a', 33 * 1024)),
            false);

        var result = await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(database.DiaryEntries);
    }

    [Fact]
    public async Task Create_RejectsThe501stDiaryEntry()
    {
        await using var testDatabase = await CreateDatabaseAsync();
        var database = testDatabase.Context;
        database.Users.Add(new ApplicationUser
        {
            Id = UserId,
            UserName = "test@example.com",
            DiaryEntryCount = 500
        });
        database.DiaryEntries.AddRange(Enumerable.Range(0, 500).Select(index => NewEntry(index)));
        await database.SaveChangesAsync();
        var controller = CreateController(database);
        var request = new UpsertDiaryRequest("山姆", ParseAnswers("可以慢慢來"), false);

        var result = await controller.Create(request);

        var response = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status429TooManyRequests, response.StatusCode);
        Assert.Equal(500, await database.DiaryEntries.CountAsync());
    }

    [Fact]
    public async Task Create_AtomicallyAllowsOnlyOneFinalQuotaReservation()
    {
        await using var testDatabase = await CreateDatabaseAsync();
        var database = testDatabase.Context;
        database.Users.Add(new ApplicationUser
        {
            Id = UserId,
            UserName = "test@example.com",
            DiaryEntryCount = 499
        });
        database.DiaryEntries.AddRange(Enumerable.Range(0, 499).Select(index => NewEntry(index)));
        await database.SaveChangesAsync();
        await using var firstDatabase = testDatabase.CreateAdditionalContext();
        await using var secondDatabase = testDatabase.CreateAdditionalContext();
        var request = new UpsertDiaryRequest("山姆", ParseAnswers("可以慢慢來"), false);

        var results = await Task.WhenAll(
            CreateController(firstDatabase).Create(request),
            CreateController(secondDatabase).Create(request));

        await using var verificationDatabase = testDatabase.CreateAdditionalContext();
        Assert.Equal(500, await verificationDatabase.DiaryEntries.CountAsync());
        Assert.Single(results, result => result.Result is CreatedAtActionResult);
        Assert.Single(results, result => result.Result is ObjectResult { StatusCode: StatusCodes.Status429TooManyRequests });
    }

    [Fact]
    public async Task List_ReturnsOnlyTheRequestedBoundedPage()
    {
        await using var database = CreateInMemoryDatabase();
        database.Users.Add(new ApplicationUser
        {
            Id = UserId,
            UserName = "test@example.com",
            DiaryEntryCount = 60
        });
        database.DiaryEntries.AddRange(Enumerable.Range(0, 60).Select(index => NewEntry(index)));
        await database.SaveChangesAsync();
        var controller = CreateController(database);

        var result = await controller.List(page: 2, pageSize: 20);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var entries = Assert.IsAssignableFrom<IEnumerable<DiaryResponse>>(response.Value).ToList();
        Assert.Equal(20, entries.Count);
        Assert.Equal(39, entries[0].UpdatedAt.Minute);
        Assert.Equal(20, entries[^1].UpdatedAt.Minute);
    }

    [Fact]
    public async Task Logout_RejectsMissingAntiforgeryToken_AndAcceptsValidToken()
    {
        await using var factory = new SecurityWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });

        using var rejected = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        var tokenResponse = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        Assert.NotNull(tokenResponse?.RequestToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Add("X-CSRF-TOKEN", tokenResponse.RequestToken);

        using var accepted = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);
    }

    [Fact]
    public void QuotaMigration_IsDiscoverableAndBackfillsExistingCounts()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options;
        using var database = new ApplicationDbContext(options);

        var migrations = database.Database.GetMigrations();
        var script = database.GetService<IMigrator>().GenerateScript(
            "20260916084528_InitialIdentity",
            "20260916120000_AddDiaryEntryQuota");

        Assert.Contains("20260916120000_AddDiaryEntryQuota", migrations);
        Assert.Contains("DiaryEntryCount", script);
        Assert.Contains("COUNT(*)::integer", script);
    }

    [Fact]
    public void DataProtectionKeyMigration_IsDiscoverableAndCreatesKeyTable()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options;
        using var database = new ApplicationDbContext(options);

        var migrations = database.Database.GetMigrations();
        var script = database.GetService<IMigrator>().GenerateScript(
            "20260916120000_AddDiaryEntryQuota",
            "20260921095133_PersistDataProtectionKeys");

        Assert.Contains("20260921095133_PersistDataProtectionKeys", migrations);
        Assert.Contains("DataProtectionKeys", script);
        Assert.DoesNotContain("CREATE TABLE \"AspNetUsers\"", script);
        Assert.DoesNotContain("CREATE TABLE \"DiaryEntries\"", script);
    }

    private static async Task<TestDatabase> CreateDatabaseAsync()
    {
        var connectionString = $"Data Source=security-{Guid.NewGuid()};Mode=Memory;Cache=Shared;Default Timeout=10";
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new TestDatabase(connection, context, connectionString);
    }

    private static ApplicationDbContext CreateInMemoryDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static DiaryEntriesController CreateController(ApplicationDbContext database)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, UserId)],
            TestAuthHandler.SchemeName);
        return new DiaryEntriesController(database)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private static JsonElement ParseAnswers(string reply)
    {
        var json = JsonSerializer.Serialize(new
        {
            trigger = "",
            critic = "",
            emotions = Array.Empty<string>(),
            behaviors = Array.Empty<string>(),
            origin = "",
            reply
        });
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private static DiaryEntry NewEntry(int index) => new()
    {
        Id = Guid.NewGuid(),
        UserId = UserId,
        CriticName = "山姆",
        AnswersJson = ParseAnswers("可以慢慢來").GetRawText(),
        IsComplete = false,
        CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(index),
        UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(index)
    };

    private sealed record CsrfResponse(string RequestToken);

    private sealed class TestDatabase(
        SqliteConnection connection,
        ApplicationDbContext context,
        string connectionString) : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; } = context;

        public ApplicationDbContext CreateAdditionalContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString)
                .Options;
            return new ApplicationDbContext(options);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}

internal sealed class SecurityWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("security-integration-tests"));
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "SecurityTest";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "security-test-user")],
            SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
