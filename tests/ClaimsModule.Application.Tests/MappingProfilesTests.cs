using AutoMapper;
using ClaimsModule.Application.Claims;
using Xunit;

namespace ClaimsModule.Application.Tests;

public class MappingProfilesTests
{
    [Fact]
    public void AutoMapper_Configuration_Is_Valid() // CQRS-03: fail fast on any unmapped destination member
    {
        var config = new MapperConfiguration(cfg => cfg.AddMaps(typeof(ClaimsMappingProfile).Assembly));

        config.AssertConfigurationIsValid();
    }
}
