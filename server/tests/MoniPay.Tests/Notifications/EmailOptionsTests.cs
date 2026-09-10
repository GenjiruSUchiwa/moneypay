using MoniPay.Notifications;
using Xunit;

namespace MoniPay.Tests.Notifications;

public sealed class EmailOptionsTests
{
    [Theory]
    [InlineData("https://email.tests")]
    [InlineData("https://email.tests/")]
    [InlineData("https://email.tests:8443")]
    public void Valid_base_urls_are_accepted(string baseUrl) =>
        Assert.True(Options(baseUrl: baseUrl).HasValidBaseUrl());

    [Theory]
    [InlineData("http://email.tests")]
    [InlineData("https://email.tests?q=1")]
    [InlineData("https://email.tests#fragment")]
    [InlineData("https://key@email.tests")]
    [InlineData("https://email.tests/v1")]
    [InlineData("https://email.tests/v1/")]
    [InlineData("not-a-url")]
    [InlineData("")]
    public void Invalid_base_urls_are_rejected(string baseUrl) =>
        Assert.False(Options(baseUrl: baseUrl).HasValidBaseUrl());

    [Theory]
    [InlineData("test-email-api-key")]
    [InlineData("bk_eu1_abc123")]
    public void Safe_api_keys_are_accepted(string apiKey) =>
        Assert.True(Options(apiKey: apiKey).HasValidApiKey());

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("key-with\nnewline")]
    [InlineData("key-with\ttab")]
    public void Unsafe_api_keys_are_rejected(string apiKey) =>
        Assert.False(Options(apiKey: apiKey).HasValidApiKey());

    [Theory]
    [InlineData("no-reply@tests.monipay.example")]
    public void Valid_senders_are_accepted(string sender) =>
        Assert.True(Options(sender: sender).HasValidFromAddress());

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    public void Invalid_senders_are_rejected(string sender) =>
        Assert.False(Options(sender: sender).HasValidFromAddress());

    [Fact]
    public void Workable_email_requires_all_three_settings() =>
        Assert.True(Options().HasWorkableEmail());

    private static EmailOptions Options(
        string baseUrl = "https://email.tests",
        string apiKey = "test-email-api-key",
        string sender = "no-reply@tests.monipay.example") =>
        new() { BaseUrl = baseUrl, ApiKey = apiKey, FromAddress = sender };
}
