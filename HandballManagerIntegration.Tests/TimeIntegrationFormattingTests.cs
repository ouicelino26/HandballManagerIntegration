using HandballIntegration.ViewModels;

namespace HandballManagerIntegration.Tests;

public sealed class TimeIntegrationFormattingTests
{
    [Fact]
    public void LimitForApi_TrimsAndTruncatesValues()
    {
        var result = TimeIntegrationViewModel.LimitForApi("  ABCDE  ", 4, "fallback");

        Assert.Equal("ABCD", result);
    }

    [Fact]
    public void LimitForApi_UsesFallbackForBlankValues()
    {
        var result = TimeIntegrationViewModel.LimitForApi("  ", 32, "Equipe");

        Assert.Equal("Equipe", result);
    }
}
