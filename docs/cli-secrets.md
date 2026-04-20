# 🔐 t2i — Where Do My Secrets Live?

The `t2i` CLI stores credentials across multiple secure backends. This doc covers the full resolution chain (highest priority first) plus guidance on which to use.

## 1. Local Development — OS-Native Encrypted Storage (DPAPI/File)

### Windows: DPAPI Encryption ✅ Recommended

Secrets stored at `%LOCALAPPDATA%\t2i\secrets.dpapi` are encrypted using Windows Data Protection API and keyed to your Windows login. Only your user can decrypt them — no shell history leakage, no accidental dotfile commits.

```bash
t2i config    # Interactive setup — credentials stored encrypted
t2i "your prompt"
```

### macOS/Linux: File with Restricted Permissions ✅ Recommended

Secrets live in `~/.config/t2i/secrets.json` with Unix `0600` permissions (readable only by you). No encryption, but OS-level access control prevents shell history and sibling process leakage.

```bash
t2i config    # Interactive setup — credentials stored with 0600 permissions
t2i "your prompt"
```

## 2. CI/CD Pipelines — Environment Variables (With Caution)

Environment variables are appropriate **only** for ephemeral, isolated environments:

```bash
export T2I_FOUNDRY_FLUX2_APIKEY="your-key-here"
t2i "your prompt"
```

⚠️ **Environment Variable Security Risks (Local Development):**
- Process visibility: Shell history captures env var assignments (`~/.bash_history`, PowerShell logs)
- Process tree leakage: Child processes inherit all env vars; developers inherit vars from build tools, CI runners, etc.
- Accidental exposure: Debug output (`env`, `printenv`, `set`) reveals all environment variables
- Dotfile commits: Developers often add env vars to `.bashrc`, `.zshrc`, `.profile` — then accidentally commit to personal dotfiles repos
- **DO NOT use env vars for local development.** Use the OS-native storage above instead.

✅ **When env vars ARE appropriate:**
- GitHub Actions / Azure Pipelines: Secrets injected from vault, process lifetime is ephemeral (minutes), environment is isolated (container, fresh VM)
- Docker: Use orchestrator secret injection (Docker secrets, Kubernetes), NOT `ENV` directives in Dockerfile
- Server/container deployments: 12-factor app pattern, dedicated service account, no interactive shell

## 3. CLI Override — For One-Off Tests Only

```bash
t2i "your prompt" --api-key "key-here"
```

⚠️ Secrets passed via CLI flags appear in your shell history. Only use for quick testing; prefer stored config for regular use.

## 4. How Resolution Works

The CLI checks in this order:

1. CLI flag (`--api-key`) — highest priority, ephemeral
2. Environment variable (`T2I_FOUNDRY_FLUX2_APIKEY`)
3. DPAPI store (Windows: `%LOCALAPPDATA%\t2i\secrets.dpapi`)
4. Plaintext file (Unix: `~/.config/t2i/secrets.json`)
5. Not found

## 5. Security Best Practices

| Do ✅ | Don't ❌ |
|------|---------|
| Use `t2i config` for local setup (stores encrypted/restricted) | Add `export T2I_*=...` to `.bashrc` or `.zshrc` — commits to dotfiles repo |
| Use env vars in CI/CD from a vault (GitHub Secrets, Azure Key Vault) | Use `ENV` directives in Dockerfiles — secrets baked into image layers |
| Use `--api-key` for one-time testing | Leave API keys in shell history or error logs |
| Rotate API keys regularly | Share credentials via chat, email, or code comments |

## Related

- [t2i CLI documentation](./cli-tool.md)
- [Security considerations (library)](./security.md)
