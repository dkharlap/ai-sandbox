namespace SimpleAccount.Preferences.Data;

public static class CatalogSeed
{
    public static readonly CatalogModel[] Models =
    [
        new() { Id = "claude-opus-5",   DisplayName = "Claude Opus 5",   Vendor = "Anthropic" },
        new() { Id = "claude-sonnet-5", DisplayName = "Claude Sonnet 5", Vendor = "Anthropic" },
        new() { Id = "claude-haiku-4-5",DisplayName = "Claude Haiku 4.5",Vendor = "Anthropic" },
        new() { Id = "gpt-5-codex",     DisplayName = "GPT-5 Codex",     Vendor = "OpenAI" },
        new() { Id = "gemini-3-pro",    DisplayName = "Gemini 3 Pro",    Vendor = "Google" },
    ];
}
