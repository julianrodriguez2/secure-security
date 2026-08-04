namespace SecureNotes.UnitTests;

public sealed class ScaffoldTests
{
    [Fact]
    public void UnitTestProjectIsDiscoverable()
    {
        Assert.Equal("SecureNotes.UnitTests", typeof(ScaffoldTests).Assembly.GetName().Name);
    }
}
