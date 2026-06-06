using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ClaimsModule.Api.IntegrationTests;

/// <summary>
/// Verifies the consistent error envelope (FRS §10.4) end-to-end through the real HTTP pipeline for
/// every failure class: model-binding/shape errors, FluentValidation failures, and not-found. In
/// particular it pins the contract that a malformed enum returns 422 ValidationError (via
/// <c>InvalidModelStateResponseFactory</c>) rather than the framework's default 400 ProblemDetails.
/// </summary>
public sealed class ErrorContractTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Malformed_Enum_In_Body_Returns_422_ValidationError_Not_400() // F2 / FRS §10.4
    {
        var response = await _client.PutAsync(
            $"/api/claims/{Guid.NewGuid()}/status", Json("""{"targetStatus":"NotARealStatus"}"""));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ValidationError", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal(422, doc.RootElement.GetProperty("status").GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Integer_Enum_Value_Is_Rejected_With_422() // hardening: JsonStringEnumConverter(allowIntegerValues:false)
    {
        var response = await _client.PutAsync(
            $"/api/claims/{Guid.NewGuid()}/status", Json("""{"targetStatus":99}"""));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ValidationError", doc.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Invalid_Create_Body_Returns_422_ValidationError() // FRS §10.4 (FluentValidation path)
    {
        var response = await _client.PostAsync("/api/claims", Json("{}"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ValidationError", doc.RootElement.GetProperty("type").GetString());
        Assert.True(doc.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.EnumerateObject().Any()); // at least one field-keyed message
    }

    [Fact]
    public async Task Unknown_Claim_Returns_404_NotFound()
    {
        var response = await _client.GetAsync($"/api/claims/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("NotFound", doc.RootElement.GetProperty("type").GetString());
    }
}
