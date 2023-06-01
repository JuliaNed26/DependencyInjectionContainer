namespace DIFixture.Test_classes.DisposableClasses
{
    internal sealed class DisposableAsyncClass : IAsyncDisposable
    {
        private readonly DisposableSequence disposableSequence;

        public DisposableAsyncClass(DisposableSequence sequence)
        {
            disposableSequence = sequence;
        }

        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            if (!IsDisposed)
            {
                disposableSequence.SaveDisposedClassType(GetType());
            }
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
