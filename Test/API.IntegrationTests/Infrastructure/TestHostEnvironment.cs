using System.Runtime.CompilerServices;

namespace API.IntegrationTests.Infrastructure;

/// <summary>
/// Marks every host created anywhere in this test assembly as a test host, so the
/// production startup safety gate in <c>Program</c> validates the target database
/// even for factories that do not set the "Testing" environment explicitly.
/// </summary>
internal static class TestHostEnvironment
{
    [ModuleInitializer]
    internal static void Initialize() =>
        Environment.SetEnvironmentVariable("DVLD_TEST_HOST", "1");
}
