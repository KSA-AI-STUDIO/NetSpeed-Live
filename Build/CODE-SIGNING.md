# Code Signing — Readiness Analysis

Status of code signing for the NetSpeed Live release pipeline.

## Current status

At the time of writing, the release installer is **unsigned**.

Verified with the OS tooling:

```
Get-AuthenticodeSignature .\Release\NetSpeedLive-Setup-1.1.0.exe
Status: NotSigned
Signer: (none)
Timestamp: none
```

The Signal Radar icon, version metadata and branding are all present, but the
executable carries **no digital certificate**.

## Why it matters

An unsigned Windows executable has no verified publisher identity. Windows 10/11
uses SmartScreen reputation (based on file reputation and the publisher's
history), so first-time downloaders will typically see a prompt such as
*"Windows protected your PC"* before they can run the installer.

- Code signing establishes **who published the software** and that it has not
  been tampered with since signing.
- A publisher identity plus a gradually building download reputation **can**
  reduce these prompts over time.
- Signing does **not guarantee** that SmartScreen warnings disappear
  immediately; reputation builds with real usage.

## Recommended future options

These are high-level guidance only — no vendor is endorsed and no prices are
quoted here.

| Option | Typical use |
|---|---|
| Standard (OV) code-signing certificate | Common choice for independent ISVs; signs with an organization identity. |
| EV code-signing certificate | Stronger identity vetting; can help establish reputation faster (SmartScreen still uses reputation, not the cert alone). |
| Cloud-based signing services | Sign in CI without holding the private key on a build machine; several reputable providers offer this. |

The certificate should be a **code signing** certificate (not SSL). When
signing on Windows you must cross-sign with the Microsoft
cross-certificate and **timestamp the signature** so it remains valid after the
certificate expires.

## Future signing pipeline (concept)

```
Build (dotnet build -c Release)
   │
   ▼
Publish (Build\Publish.ps1, self-contained win-x64)
   │
   ▼
Compile installer (ISCC Installer\NetSpeedLive.iss)
   │
   ▼
Code-sign installer (signtool.exe sign)
   │
   ▼
Timestamp signature (signtool.exe timestamp)
   │
   ▼
Verify signature (Get-AuthenticodeSignature / signtool verify)
   │
   ▼
Generate checksum (SHA-256)
   │
   ▼
Release
```

No signing is performed in this repository today because no certificate is
available. When one is obtained, the `signtool` steps above can be added to the
pipeline without changing the application.