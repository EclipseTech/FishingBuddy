using Blish_HUD;
using System;
using System.Threading.Tasks;

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    public static class RetryHelper
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(RetryHelper));

        //
        // Summary:
        //     Retry task until result expression true or max attempts reached.
        //     Uses longer delay between each attempt.
        //
        // Parameters:
        //   maxAttempts:
        //     Max number of attempts before failing.
        //   operation:
        //     Await result of this Task function.
        //   resultExpression:
        //     Boolean expression function to check, retry on false
        //
        // Returns:
        //     Delay TimeSpan.
        public static async Task RetryAsync<TType>(int maxAttempts, Task<TType> task, Func<Task> operation, Func<bool> resultExpression)
        {
            if (maxAttempts <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts));

            int attempt = 1;
            do
            {
                Logger.Debug("Attempt #" + attempt);
                try
                {
                    await operation();
                    if (resultExpression()) break;
                }
                catch (Exception)
                {
                    if (attempt == maxAttempts)
                        throw;
                }
                // TODO This doesn't seem to be delaying or waiting the # of seconds... is it even retrying the operation?
                await CreateDelay(attempt, maxAttempts);
                attempt++;
            } while (attempt != maxAttempts);
        }

        private static Task CreateDelay(int attempt, int maxAttempts)
        {
            int delaytime = IncreasingDelayInSeconds(attempt);
            Logger.Debug($"Failed result expression on attempt {attempt} of {maxAttempts}. Will retry after sleeping for {delaytime}.");
            return Task.Delay(delaytime);
        }

        public static async Task RetryOnExceptionAsync(int times, Func<Task> operation)
        {
            await RetryOnExceptionAsync<Exception>(times, operation);
        }

        public static async Task RetryOnExceptionAsync<TException>(int maxAttempts, Func<Task> operation) where TException : Exception
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
            Logger.Debug($"Exception on attempt {attemptNumber} of {maxAttempts}. Will retry after sleeping for {delaytime}.", ex);
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
