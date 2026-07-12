# GitHub Security Hardening — Sm_API

This maps the four hardening requirements to what's implemented here, what's
automated in the pipeline, and what still requires a human with the right
GitHub role to click a button.

## 1. Harden GitHub organisation/repo settings

| Control | Status | Where |
|---|---|---|
| Branch protection on `main` | Automated | `scripts/setup-github-security.ps1` (run once after the repo exists on GitHub) |
| Required PR reviews (≥ 1 reviewer) | Automated | Same script, `required_pull_request_reviews.required_approving_review_count` |
| Signed commits required | Automated | Same script, `PUT /repos/:owner/:repo/branches/main/protection/required_signatures` |
| SSO enforcement | **Manual, org-owner action** | GitHub Enterprise only. Org Settings → Authentication security → "Require SAML SSO". Cannot be set via repo-scoped API/token. |
| Audit log retention | **Manual, enterprise-owner action** | GitHub Enterprise Cloud only. Enterprise Settings → Audit log → retention/streaming. Not exposed to any repo-level API. |

Verified continuously by `.github/workflows/security-compliance-audit.yml`, which
runs weekly (and on demand via `workflow_dispatch`) and reports pass/fail for
everything checkable from a repo token, and explicitly flags SSO/audit-log
retention as unverifiable from CI — see the job summary, not just logs.

## 2. Enable secret scanning

Chosen approach: **gitleaks** in GitHub Actions (works on any plan/visibility,
unlike native GitHub Advanced Security secret scanning which needs an
Enterprise license or a public repo).

- `.github/workflows/gitleaks.yml` — `diff-scan` job runs on every push/PR,
  scanning only the changed commits.
- Same workflow's `full-history-scan` job (weekly + manual) runs
  `gitleaks detect --log-opts="--all"` over the entire git history — this is
  also the mechanism for requirement 3 below.
- `.gitleaks.toml` — baseline config extending gitleaks' default ruleset, with
  an allowlist for known-safe local dev fixtures (e.g. SQLite connection
  strings, which are not secrets).
- `scripts/setup-github-security.ps1` also attempts to turn on native GitHub
  secret scanning + push protection via the repo `security_and_analysis` API
  as defense-in-depth; it silently no-ops if the plan doesn't support it.

## 3. Scan git history for committed credentials

Same `full-history-scan` job as above satisfies this: it walks **all** commits
(`--log-opts="--all"`), not just the current diff, so it catches secrets that
were committed and later removed in a subsequent commit (which a diff-only
scan would miss).

If it finds something, the workflow fails loudly with this remediation order
(also printed in the job log):

1. **Rotate/revoke the credential at the issuing provider first.** A leaked
   key is compromised the moment it's pushed, regardless of whether it's later
   removed from history.
2. Purge it from git history (`git filter-repo` or BFG Repo-Cleaner).
3. Force-push the rewritten history; have all collaborators re-clone rather
   than pull/rebase.

For a one-off local check before ever pushing (e.g. to also cross-check with
a second tool), run:

```bash
docker run --rm -v "$PWD:/repo" zricethezav/gitleaks:latest detect --source=/repo --log-opts="--all"
docker run --rm -v "$PWD:/repo" trufflesecurity/trufflehog:latest git file:///repo
```

## 4. Dependency vulnerability scanning

Chosen approach: **Dependabot** (native to GitHub, no external account/token).

- `.github/dependabot.yml` — weekly scans for both the `nuget` ecosystem
  (this solution's packages) and `github-actions` (the workflow files
  themselves), opening PRs automatically.
- `scripts/setup-github-security.ps1` enables Dependabot vulnerability alerts
  and automated security fixes at the repo level (both are off by default on
  some account types).
- `.github/workflows/ci.yml` adds a build-time gate:
  `dotnet list package --vulnerable --include-transitive` fails the build if
  any referenced package (direct or transitive) has a known advisory. This is
  defense-in-depth alongside Dependabot — it catches a vulnerable package the
  moment it's introduced in a PR, rather than waiting for the next scheduled
  Dependabot run.

**Remediation SLA policy (recommended, enforce via team process, not code):**
Critical/High severity advisories should be patched or have a tracked
mitigation plan within **7 days**; Medium within **30 days**; Low at the next
regular dependency-update cycle.

## Verifying this end-to-end

1. Push this repo to GitHub, then run `./scripts/setup-github-security.ps1 -Repo "owner/Sm_API"`.
2. Open a PR touching any file → confirm `CI` and `Secret Scanning (gitleaks)` checks appear and must pass before merge (branch protection).
3. Manually run the `Security Compliance Audit` workflow (`gh workflow run security-compliance-audit.yml`) → check the job summary.
4. On a throwaway branch, commit a dummy secret pattern (e.g. `AKIAABCDEFGHIJKLMNOP` — a fake AWS-shaped key), push, confirm the PR check fails, then remove it before merging. Do not leave real or fake secrets in `main`.
