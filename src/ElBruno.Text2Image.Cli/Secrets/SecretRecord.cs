namespace ElBruno.Text2Image.Cli.Secrets;

/// <summary>
/// Represents a stored secret.
/// </summary>
public sealed record SecretRecord(string Provider, string Field, string Value);
