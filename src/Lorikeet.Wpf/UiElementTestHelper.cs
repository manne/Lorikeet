using System;
using System.Threading.Tasks;
using System.Windows;
using FluentAssertions.Execution;
using Polly;
using Polly.Retry;
using Xunit.Abstractions;

namespace Lorikeet.Wpf
{
    public class UiElementTestHelper<TControlUnderTest> where TControlUnderTest : UIElement
    {
        private readonly IWindowHost _windowHost;
        private readonly TaskCompletionSource<bool> _taskCompletionSource;
        private readonly RetryPolicy _waitAndRetry;
        private TControlUnderTest _controlUnderTest;

        public UiElementTestHelper(ITestOutputHelper output, IWindowHost windowHost)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            _windowHost = windowHost ?? throw new ArgumentNullException(nameof(windowHost));
            _waitAndRetry = Policy
                .Handle<AssertionFailedException>()
                .WaitAndRetryAsync(new[]
                    {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(2)
                    },
                    (exception, _) => output.WriteLine($"Intermediate Evaluation: {exception.Message}"));

            _taskCompletionSource = new TaskCompletionSource<bool>();
        }

        public void Initialize(UIElement content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            _windowHost.SetContentAndShowWindow(content);
        }

        public void Find(Func<TControlUnderTest> search)
        {
            _controlUnderTest = search() ?? throw new ArgumentNullException(nameof(search));
        }

        public async Task EvaluateAsync(Action<TControlUnderTest> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var policyResult = await _waitAndRetry.ExecuteAndCaptureAsync(() =>
            {
                action(_controlUnderTest);
                _taskCompletionSource.SetResult(true);
                return Task.CompletedTask;
            });

            if (policyResult.Outcome == OutcomeType.Failure)
            {
                var policyException = policyResult.FinalException;
                _windowHost.CloseWindow();
                _taskCompletionSource.SetException(policyException);
            }
        }

        public Task RunAsync() => _taskCompletionSource.Task;
    }
}
