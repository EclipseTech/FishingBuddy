namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    using Blish_HUD;
    using System;
    using System.Threading.Tasks;

    public static class RetryHelper
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(RetryHelper));

        public static async Task RetryOnExceptionAsync(Func<Task> operation, int maxAttempts = 3) => await RetryOnExceptionAsync<Exception>(operation, maxAttempts);

        public static async Task RetryOnExceptionAsync<TException>(Func<Task> operation, int maxAttempts = 3) where TException : Exception
        {
            if (maxAttempts <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts));

            int attempt = 1;
            do
            {
                try
                {
                    await operation();
                    break;
                }
                catch (TException ex)
                {
                    Logger.Debug($"Exception on attempt {attempt} of {maxAttempts}.", ex);
                    if (attempt == maxAttempts)
                        throw;

                    await CreateDelayForException(attempt, maxAttempts, ex);
                }
                attempt++;
            } while (true);
        }

        private static Task CreateDelayForException(int attemptNumber, int maxAttempts, Exception ex)
        {
            int delaytime = IncreasingDelayInSeconds(attemptNumber);
            return Task.Delay(delaytime);
        }

        //
        // Summary:
        //     Increased delay timer between checks.
        //
        // Parameters:
        //   failedAttempt:
        //     Failed attempt number.
        //
        // Returns:
        //     Delay TimeSpan.
        static int IncreasingDelayInSeconds(int failedAttempt)
        {
            int delaySeconds = 10;
            if (failedAttempt <= 0) throw new ArgumentOutOfRangeException();

            return failedAttempt * delaySeconds;
        }
    }
}
