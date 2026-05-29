namespace Scrapile.Desktop.Services;

using System;

public sealed class UpdateSettings
{
    public bool Enabled { get; init; } = true;
    public string GithubRepositoryUrl { get; init; } = "https://github.com/rezun/scrapile";
    public TimeSpan CheckInterval { get; init; } = TimeSpan.FromHours(4);
}
