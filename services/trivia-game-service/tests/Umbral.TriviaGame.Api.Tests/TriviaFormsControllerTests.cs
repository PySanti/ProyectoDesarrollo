using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Umbral.TriviaGame.Api.Tests.Testing;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Api.Tests;

public sealed class TriviaFormsControllerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public TriviaFormsControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ValidForm_Returns201WithFormDetail()
    {
        var client = _factory.CreateClient();

        var command = new
        {
            title = "Test Form",
            questions = new[]
            {
                new
                {
                    text = "Q1?",
                    assignedScore = 100,
                    timeLimitSeconds = 30,
                    displayOrder = 1,
                    options = new[]
                    {
                        new { text = "A", isCorrect = true },
                        new { text = "B", isCorrect = false },
                        new { text = "C", isCorrect = false },
                        new { text = "D", isCorrect = false },
                    },
                },
            },
        };

        var response = await client.PostAsJsonAsync("/api/trivia-forms", command);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGet_ReturnsSameForm()
    {
        var client = _factory.CreateClient();

        var command = new
        {
            title = "Form A",
            questions = new[]
            {
                new
                {
                    text = "Q1?",
                    assignedScore = 100,
                    timeLimitSeconds = 30,
                    displayOrder = 1,
                    options = new[]
                    {
                        new { text = "A", isCorrect = true },
                        new { text = "B", isCorrect = false },
                        new { text = "C", isCorrect = false },
                        new { text = "D", isCorrect = false },
                    },
                },
            },
        };

        var createResponse = await client.PostAsJsonAsync("/api/trivia-forms", command);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var getResponse = await client.GetAsync(createResponse.Headers.Location!);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGetAll_ReturnsCompleteFormWithQuestionCount()
    {
        var client = _factory.CreateClient();

        var command = new
        {
            title = $"Form List {Guid.NewGuid():N}",
            questions = new[]
            {
                new
                {
                    text = "Q1?",
                    assignedScore = 100,
                    timeLimitSeconds = 30,
                    displayOrder = 1,
                    options = new[]
                    {
                        new { text = "A", isCorrect = true },
                        new { text = "B", isCorrect = false },
                        new { text = "C", isCorrect = false },
                        new { text = "D", isCorrect = false },
                    },
                },
            },
        };

        var createResponse = await client.PostAsJsonAsync("/api/trivia-forms", command);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<TriviaFormDetailDto>();
        Assert.NotNull(created);

        var listResponse = await client.GetAsync("/api/trivia-forms");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var forms = await listResponse.Content.ReadFromJsonAsync<IReadOnlyList<TriviaFormListItemDto>>();

        var listed = Assert.Single(forms!.Where(form => form.Id == created.Id));
        Assert.True(listed.IsComplete);
        Assert.Equal(1, listed.QuestionsCount);
    }

    [Fact]
    public async Task CreateThenUpdateThenGet_ReturnsUpdatedForm()
    {
        var client = _factory.CreateClient();

        var createCmd = new
        {
            title = "Original",
            questions = new[]
            {
                new
                {
                    text = "Q1?",
                    assignedScore = 100,
                    timeLimitSeconds = 30,
                    displayOrder = 1,
                    options = new[]
                    {
                        new { text = "A", isCorrect = true },
                        new { text = "B", isCorrect = false },
                        new { text = "C", isCorrect = false },
                        new { text = "D", isCorrect = false },
                    },
                },
            },
        };

        var createResponse = await client.PostAsJsonAsync("/api/trivia-forms", createCmd);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<TriviaFormDetailDto>();
        Assert.NotNull(created);

        var updateCmd = new
        {
            formId = created.Id,
            title = "Updated Title",
            questions = new[]
            {
                new
                {
                    text = "Q2?",
                    assignedScore = 200,
                    timeLimitSeconds = 60,
                    displayOrder = 1,
                    options = new[]
                    {
                        new { text = "X", isCorrect = true },
                        new { text = "Y", isCorrect = false },
                        new { text = "Z", isCorrect = false },
                        new { text = "W", isCorrect = false },
                    },
                },
            },
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/trivia-forms/{created.Id}", updateCmd);
        var updateBody = await updateResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TriviaFormDetailDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Single(updated.Questions);
        Assert.Equal("Q2?", updated.Questions[0].Text);
        Assert.Equal(200, updated.Questions[0].AssignedScore);

        var getResponse = await client.GetAsync($"/api/trivia-forms/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<TriviaFormDetailDto>();
        Assert.NotNull(fetched);
        Assert.Equal(updated.Title, fetched.Title);
    }

    [Fact]
    public async Task Get_NonExistentForm_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/trivia-forms/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidForm_MissingQuestions_Returns400()
    {
        var client = _factory.CreateClient();
        var command = new { title = "Empty", questions = Array.Empty<object>() };
        var response = await client.PostAsJsonAsync("/api/trivia-forms", command);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithoutOperadorRole_Returns403()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TestClaimsProvider));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddScoped(_ => TestClaimsProvider.WithoutOperadorRole());
            });
        }).CreateClient();

        var command = new
        {
            title = "Unauthorized",
            questions = new[]
            {
                new
                {
                    text = "Q?",
                    assignedScore = 100,
                    timeLimitSeconds = 30,
                    displayOrder = 1,
                    options = new[]
                    {
                        new { text = "A", isCorrect = true },
                        new { text = "B", isCorrect = false },
                        new { text = "C", isCorrect = false },
                        new { text = "D", isCorrect = false },
                    },
                },
            },
        };

        var response = await client.PostAsJsonAsync("/api/trivia-forms", command);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
