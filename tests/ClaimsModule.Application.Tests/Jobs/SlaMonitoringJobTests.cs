using ClaimsModule.Application.Tests.TestSupport;
using ClaimsModule.Domain.Auditing;
using ClaimsModule.Infrastructure.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClaimsModule.Application.Tests.Jobs;

public class SlaMonitoringJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 7, 12, 0, 0, TimeSpan.Zero);

    private static SlaMonitoringJob Build(FakeBackgroundJobData data, FakeAuditLog audit) =>
        new(data, audit, new FakeClock(Now), NullLogger<SlaMonitoringJob>.Instance);

    [Fact]
    public async Task Flags_A_Stale_Claim_With_No_Prior_Breach()
    {
        var data = new FakeBackgroundJobData { StaleClaims = [TestData.NewClaim()], LastBreachAt = null };
        var audit = new FakeAuditLog();

        await Build(data, audit).ScanAsync();

        Assert.Equal(1, audit.Count(AuditEventTypes.SlaBreachDetected));
    }

    [Fact]
    public async Task Does_Not_Reflag_Within_24_Hours()
    {
        var data = new FakeBackgroundJobData { StaleClaims = [TestData.NewClaim()], LastBreachAt = Now.AddHours(-1) };
        var audit = new FakeAuditLog();

        await Build(data, audit).ScanAsync();

        Assert.Equal(0, audit.Count(AuditEventTypes.SlaBreachDetected)); // de-duped within 24h
    }

    [Fact]
    public async Task Reflags_After_24_Hours()
    {
        var data = new FakeBackgroundJobData { StaleClaims = [TestData.NewClaim()], LastBreachAt = Now.AddHours(-25) };
        var audit = new FakeAuditLog();

        await Build(data, audit).ScanAsync();

        Assert.Equal(1, audit.Count(AuditEventTypes.SlaBreachDetected));
    }

    [Fact]
    public async Task No_Stale_Claims_Writes_Nothing()
    {
        var data = new FakeBackgroundJobData { StaleClaims = [] };
        var audit = new FakeAuditLog();

        await Build(data, audit).ScanAsync();

        Assert.Equal(0, audit.Count(AuditEventTypes.SlaBreachDetected));
    }
}
