namespace Content.Shared.Speech;

[Obsolete("Use ListenLanguageEvent instead.", true)] // DEN: Languages
public sealed class ListenEvent : EntityEventArgs
{
    public readonly string Message;
    public readonly EntityUid Source;

    public ListenEvent(string message, EntityUid source)
    {
        Message = message;
        Source = source;
    }
}

[Obsolete("Use ListenLanguageAttemptEvent instead.", true)] // DEN: Languages
public sealed class ListenAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;

    public ListenAttemptEvent(EntityUid source)
    {
        Source = source;
    }
}
