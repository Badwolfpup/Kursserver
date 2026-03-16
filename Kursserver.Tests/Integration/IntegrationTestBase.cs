namespace Kursserver.Tests.Integration;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    protected CustomWebApplicationFactory Factory { get; }

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
    }

    protected HttpClient CreateClient() => Factory.CreateClient();

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
