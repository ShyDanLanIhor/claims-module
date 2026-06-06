using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClaimsModule.Application.Reference;
using ClaimsModule.Domain.Documents;
using Xunit;

namespace ClaimsModule.Api.IntegrationTests;

/// <summary>
/// End-to-end smoke test through the real HTTP pipeline (controller → MediatR → handler) for a
/// database-independent endpoint, validating composition, routing and JSON enum serialisation.
/// </summary>
public sealed class ReferenceEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task GetClaimStatuses_Returns_All_Lifecycle_States()
    {
        var response = await _client.GetAsync("/api/reference/claim-statuses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var statuses = await response.Content.ReadFromJsonAsync<List<ClaimStatusInfoDto>>(JsonOptions);
        Assert.NotNull(statuses);
        Assert.Equal(7, statuses!.Count); // Draft, Open, UnderInvestigation, PendingPayment, Closed, Reopened, Withdrawn

        var open = statuses.Single(s => s.Status == Domain.Enums.ClaimStatus.Open);
        Assert.Contains(Domain.Enums.ClaimStatus.PendingPayment, open.AllowedNextStatuses);
    }

    [Fact]
    public async Task GetDocumentTypes_Returns_The_Single_Source_Allowlist()
    {
        var response = await _client.GetAsync("/api/reference/document-types");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var types = await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions);
        Assert.NotNull(types);
        // The endpoint must expose exactly the backend single source of truth (same values + order),
        // so the Angular picker never drifts from what the upload validator accepts.
        Assert.Equal(DocumentTypes.All, types!);
        Assert.Contains(DocumentTypes.Default, types!);
    }
}
