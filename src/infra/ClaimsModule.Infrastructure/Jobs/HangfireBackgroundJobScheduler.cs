using System.Linq.Expressions;
using ClaimsModule.Application.Common.Interfaces;
using Hangfire;

namespace ClaimsModule.Infrastructure.Jobs;

/// <summary>Hangfire-backed implementation of <see cref="IBackgroundJobScheduler"/>.</summary>
public sealed class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
{
    public string Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall) =>
        BackgroundJob.Enqueue(methodCall);

    public void AddOrUpdateRecurring<TJob>(string recurringJobId, Expression<Func<TJob, Task>> methodCall, string cronExpression) =>
        RecurringJob.AddOrUpdate(recurringJobId, methodCall, cronExpression);
}
