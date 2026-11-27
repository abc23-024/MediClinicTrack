namespace MediClinicTrack.Tests;

// Proves the app starts and serves its pages.
public class SmokeTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public SmokeTests(TestAppFactory factory) => _factory = factory;

    [Fact]
    public async Task HomePage_Loads()
    {
        var response = await _factory.CreateHttpsClient().GetAsync("/");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task LoginPage_Loads()
    {
        var response = await _factory.CreateHttpsClient().GetAsync("/Identity/Account/Login");

        response.EnsureSuccessStatusCode();
    }
}
