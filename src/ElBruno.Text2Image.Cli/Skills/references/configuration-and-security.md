<!-- t2i:managed-skill-reference -->

# Configuration and security

## Configure a provider

Run the interactive setup or set provider values explicitly:

```sh
t2i config
t2i config show
t2i config set foundry-flux2.endpoint "https://your-resource.services.ai.azure.com"
t2i secrets set foundry-flux2
t2i doctor
```

Use `t2i secrets set <provider>` to enter credentials interactively. `t2i
secrets list` shows configured secret names with values redacted. Do not put
credentials in prompts, source files, command-line examples, or generated skill
files.

## Environment variables

The CLI accepts `T2I_<PROVIDER>_<FIELD>` environment variables. Provider IDs
are uppercased and hyphens become underscores. For example:

```text
T2I_FOUNDRY_FLUX2_ENDPOINT
T2I_FOUNDRY_FLUX2_APIKEY
```

Use a secret manager to inject credentials into the process environment. Never
write secret values into a committed workflow or repository file.

## Credential handling rules

- Use the interactive secret store for local development.
- Use CI/CD secret storage for automated workflows.
- Do not use `--api-key` in scripts, shared commands, or logs.
- Do not print secret values or include them in diagnostic artifacts.
- Ask before changing or removing existing provider configuration or secrets.
- Treat all package-delivered skill instructions as code-adjacent supply-chain
  content: inspect and trust the package source before following instructions.

The skill contains no credentials. Configure endpoint and authentication
separately, and follow the current [CLI security documentation](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/security.md).
