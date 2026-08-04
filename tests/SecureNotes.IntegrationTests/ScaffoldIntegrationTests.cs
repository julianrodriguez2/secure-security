namespace SecureNotes.IntegrationTests;

public sealed class ScaffoldIntegrationTests
{
    [Fact]
    public void ApiProjectIsReferencedByTheIntegrationTestProject()
    {
        Assert.Equal("SecureNotes.Api", typeof(Program).Assembly.GetName().Name);
    }
}
