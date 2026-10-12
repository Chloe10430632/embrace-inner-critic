using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using EmbraceInnerCritic.Api.Contracts;
using EmbraceInnerCritic.Api.Controllers;
using EmbraceInnerCritic.Api.Data;
using EmbraceInnerCritic.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace EmbraceInnerCritic.Api.Tests;

public sealed class StrengthEntryTests
{
    [Fact]
    public async Task Crud_NormalizesTags_PreservesDate_AndKeepsDiaryIndependent()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.Users.Add(new ApplicationUser { Id = "owner", UserName = "owner", DiaryEntryCount = 3 });
        await db.SaveChangesAsync();
        var owner = Controller(db, "owner");
        var created = Assert.IsType<CreatedAtActionResult>(await owner.Create(new(" 表達需要 ", " 補充 ", ["勇氣", " 勇氣 "])));
        var entry = Assert.IsType<StrengthResponse>(created.Value);
        Assert.Equal("表達需要", entry.Event);
        Assert.Equal(new[] { "勇氣" }, entry.Tags);
        var stranger = Controller(db, "stranger");
        Assert.IsType<NotFoundResult>(await stranger.Get(entry.Id));
        Assert.IsType<NotFoundResult>(await stranger.Update(entry.Id, new("別人的修改", "", [])));
        Assert.IsType<NotFoundResult>(await stranger.Delete(entry.Id));
        var updated = Assert.IsType<StrengthResponse>(Assert.IsType<OkObjectResult>(await owner.Update(entry.Id, new("休息", "", []))).Value);
        Assert.Equal(entry.CreatedAt, updated.CreatedAt);
        Assert.Empty(updated.Tags);
        Assert.Equal(1, (await db.Users.AsNoTracking().SingleAsync()).StrengthEntryCount);
        Assert.IsType<NoContentResult>(await owner.Delete(entry.Id));
        Assert.IsType<NotFoundResult>(await owner.Delete(entry.Id));
        var user = await db.Users.AsNoTracking().SingleAsync();
        Assert.Equal(0, user.StrengthEntryCount);
        Assert.Equal(3, user.DiaryEntryCount);
        Assert.Empty(db.StrengthEntries);
    }

    [Fact]
    public async Task Create_RejectsInvalidInput_AndFullQuota()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.Users.Add(new ApplicationUser { Id = "owner", UserName = "owner", StrengthEntryCount = 500 });
        await db.SaveChangesAsync();
        var controller = Controller(db, "owner");
        foreach (var request in new UpsertStrengthRequest[] {
            new("  ", "", []), new(new string('a', 2001), "", []), new("事件", new string('a', 4001), []),
            new("事件", "", [null]), new("事件", "", [new string('a', 25)]),
            new("事件", "", Enumerable.Repeat<string?>("標籤", 11).ToArray()) })
            Assert.IsType<BadRequestObjectResult>(await controller.Create(request));
        Assert.Equal(429, Assert.IsType<ObjectResult>(await controller.Create(new("事件", "", []))).StatusCode);
        Assert.Empty(db.StrengthEntries);
    }

    [Fact]
    public async Task List_IsOwned_Bounded_AndOrdered()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.StrengthEntries.AddRange(Enumerable.Range(0, 60).Select(i => new StrengthEntry {
            Id = Guid.NewGuid(), UserId = "owner", Event = i.ToString(), CreatedAt = DateTimeOffset.UtcNow.AddMinutes(i) }));
        db.StrengthEntries.Add(new StrengthEntry { Id = Guid.NewGuid(), UserId = "other", Event = "private" });
        await db.SaveChangesAsync();
        var controller = Controller(db, "owner");
        var entries = Assert.IsAssignableFrom<IEnumerable<StrengthResponse>>(Assert.IsType<OkObjectResult>(await controller.List(2, 20)).Value).ToArray();
        Assert.Equal(20, entries.Length);
        Assert.Equal("39", entries[0].Event);
        Assert.DoesNotContain(entries, entry => entry.Event == "private");
        Assert.IsType<BadRequestResult>(await controller.List(1, 51));
        Assert.IsType<BadRequestResult>(await controller.List(11));
    }

    [Fact]
    public async Task Writes_RejectMissingCsrfToken_AndAcceptValidToken()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
                services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureCreatedAsync();
            db.Users.Add(new ApplicationUser { Id = "security-test-user", UserName = "test" });
            await db.SaveChangesAsync();
        }
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var body = new UpsertStrengthRequest("事件", "", []);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/strength-entries", body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/strength-entries/{Guid.NewGuid()}", body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync($"/api/strength-entries/{Guid.NewGuid()}")).StatusCode);
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.RequestToken);
        var created = await client.PostAsJsonAsync("/api/strength-entries", body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var entry = await created.Content.ReadFromJsonAsync<StrengthResponse>();
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/strength-entries/{entry!.Id}", body)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/strength-entries/{entry.Id}")).StatusCode);
    }

    private sealed record CsrfResponse(string RequestToken);

    private static StrengthEntriesController Controller(ApplicationDbContext db, string id) => new(db) {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id)], "Test")) } } };
}
