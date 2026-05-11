using Microsoft.Extensions.Options;

namespace KucukMericHukuk.Tests.Infrastructure;

/// <summary>
/// Test stub for IOptionsSnapshot&lt;T&gt; — Options.Create() returns IOptions&lt;T&gt;,
/// not IOptionsSnapshot&lt;T&gt;. Use this when service ctor accepts IOptionsSnapshot.
/// </summary>
public class OptionsSnapshotStub<T> : IOptionsSnapshot<T> where T : class
{
    public OptionsSnapshotStub(T value) { Value = value; }
    public T Value { get; }
    public T Get(string? name) => Value;
}
