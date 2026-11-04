<!-- t2i:managed-skill-reference -->

# Automation and troubleshooting

## Batch generation

Use explicit, distinct output paths and respect provider rate limits:

```sh
prompts=("a watercolor castle" "a robot painting a landscape")
for index in "${!prompts[@]}"; do
  number="$(printf '%02d' "$((index + 1))")"
  t2i "${prompts[$index]}" --out "images/image-$number.png"
  sleep 2
done
```

For production automation, ensure output names are deterministic and unique
for the job. Check each command's exit status and verify expected files before
publishing or consuming them.

## GitHub Actions

Install the tool and supply credentials through repository or environment
secrets, not inline command arguments:

```yaml
- name: Install t2i
  run: dotnet tool install --global ElBruno.Text2Image.Cli

- name: Generate image
  env:
    T2I_FOUNDRY_FLUX2_ENDPOINT: ${{ secrets.T2I_ENDPOINT }}
    T2I_FOUNDRY_FLUX2_APIKEY: ${{ secrets.T2I_API_KEY }}
  run: t2i "developer workflow illustration" --provider foundry-flux2 --out docs/workflow.png
```

Do not expose secret values in logs. Restrict generation workflows to trusted
events and users because a generation request may access a paid service.

## Diagnostics

Run these commands before changing configuration:

```sh
t2i doctor
t2i providers
t2i config show
t2i secrets list
```

- **No default provider:** configure one with `t2i config` or pass
  `--provider <id>`.
- **Missing endpoint or credentials:** set the endpoint and use
  `t2i secrets set <provider>`, or configure the appropriate environment
  variables securely.
- **Unauthorized response:** check that the configured credential is valid;
  never print it while diagnosing.
- **Rate limited:** reduce request frequency and retry according to the
  provider's guidance.
- **Output write failure:** confirm the destination directory exists and the
  current user has permission to write there.

For current command options and provider-specific details, see the
[CLI guide](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/cli-tool.md).
