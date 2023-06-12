using Xunit.Sdk;

namespace KafkaFlow.ProcessManagers.IntegrationTests;

public static class TestUtils
{
    public static void RetryFor(TimeSpan maxWait, TimeSpan period, Action action) =>
        RetryFor(maxWait, period, () =>
        {
            action();
            return Task.CompletedTask;
        }).GetAwaiter().GetResult();

    public static async Task RetryFor(TimeSpan maxWait, TimeSpan period, Func<Task> action)
    {
        var deadline = DateTime.UtcNow.Add(maxWait);
        while (true)
        {
            try
            {
                await action();
                return;
            }
            catch (XunitException)
            {
                if (DateTime.UtcNow > deadline) throw;
                await Task.Delay(period);
            }
        }
    }
}
