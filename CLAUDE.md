# CLAUDE.md — Multi-Cloud DotNet Container

## Recent changes (attended edits to this file — read, then carry on)

- **2026-11-10 (attended session with Matt): this file was written and the repository set up.** Nothing here has
  run yet. The solution does not exist until the first session creates it from `docs/SDD.md`; the layout section
  below says where.
- **2026-11-10 (attended session with Matt): the design's open questions were decided** (`docs/SDD.md` section 15).
  Five projects, and the heartbeat Orchestrator references no other project. The solution architecture paragraph
  below now says so.
- **2026-11-10 (attended session with Matt): the first build created the solution**, the `Dockerfile` and
  `docs/INSTALL-local.md`. The layout section below is now what exists, not a plan. `docs/SDD.md` section 14 says
  which unverified items the build confirmed and which still wait on the container.
- **2026-11-10 (attended session with Matt): no views; the application is a JSON API** (`docs/SDD.md`, decision
  D7). `GET /` answers JSON, and the Stack section below says controllers without views.
- **2026-11-11 (attended session with Matt): GitHub Actions workflows, started only by hand** (`docs/SDD.md`
  section 16, decision D9). The working agreement and the layout below now say so.

Orientation for Claude Code sessions in this repository. It is **public** and exists for one purpose: one small
ASP.NET Core MVC application, built into one container image, deployed unchanged to several clouds, with a
step-by-step install file for each. It is a placeholder application for checking that a platform is wired correctly:
health probes, log delivery, error handling, and every HTTP verb. Build it so that a stranger can read it: small,
conventional, and explained by its own code, comments and documents.

**`docs/SDD.md` is the design.** Read it in full before writing code. Where this file and the design disagree about
what to build, the design wins and you say so in your final message. Where the design is wrong or silent, stop and
ask; do not fill the gap with a guess. Section 14 of the design lists what was not verified when it was written:
confirm each item as you reach it, and correct the design in the same change.

## Working agreement — Matt commits; you never do

All work happens on `main`, with no feature branches and no pull requests; the workflows in `.github/workflows/`
run only when Matt starts them (`docs/SDD.md` section 16), and you never run them. You change files in the
working tree, build and test, and stop. **Matt reviews the tree and makes every commit himself, under his own
identity.** A hook (`.claude/hooks/git-guardrail`, `handback` profile) denies `git commit`, `git push`, `git merge`,
`git rebase`, `git reset`, forced or destructive forms of the other git commands, and anything that rewrites history.
If it denies a command you were about to run, that is the agreement working, not a defect: do not look for another
way to make the same change.

- **End every session with a drafted commit message** in the form the `devops-git-conventions` skill gives
  (`.claude/skills/devops-git-conventions/references/commit-messages.md`): a Conventional Commits subject of at most
  72 characters, a body that says why, and a last line that names the verification you ran. Put it in your final
  message so Matt can paste it. A drafted message with no verification line means the work was not verified, and
  say so plainly if that is the case.
- **Leave the tree in a state that builds.** If you must stop midway, say exactly which files are half done.
- **Never touch files you did not change.** No reformatting passes, no `dotnet format` across the solution unless the
  session asks for it.
- **Read-only git is always fine:** `status`, `diff`, `log`, `show`, `branch`.

**This repository is public: nothing private goes into it.** No keys, no credentials, no connection strings, no
cloud account identifiers, no project or account numbers, no personal data, no reference to any other person's or
company's internal systems, and nothing copied from a private repository. Sample data is invented and obviously so.
An install file shows a placeholder such as `<your-project-id>` wherever a real value would go. If a request would
put anything of that kind into the tree, decline it and say why.

**You never create, change or delete a cloud resource.** The install files are steps Matt runs himself in his own
accounts. You write and correct the steps from what he reports. Running the application and the container on this
machine is yours to do.

## The one rule of the design

**The code and the image are identical in every environment.** Everything that differs between a developer machine,
a local container and each cloud is an environment variable or a platform setting. A change to `Program.cs`, or a
branch in code on which cloud it is running in, to make one platform work is a defect in the design: raise it before
making it. The platform Adapter reports where the application is running; nothing else in the code decides anything
from it.

## Stack

- .NET 10 SDK (10.0.x is installed on this machine) and C# 14, the versions the house C# skill is written for.
- ASP.NET Core MVC: controllers without views, a JSON API that returns no HTML page. Errors are Problem Details
  (RFC 9457). No views, no Razor Pages, no Blazor, no minimal-API endpoints.
- Logging through `ILogger<T>`, with Serilog as the provider writing one JSON object per line to the console.
- Entity Framework Core over an in-memory SQLite database, created and seeded when the application starts.
- Tests: MSTest, written as the `code-csharp-mstest` skill describes.
- Docker for the image. One `Dockerfile` at the repository root.
- Every package and base image is pinned to one exact version, recorded in `README.md`.

## Layout

```
Dockerfile                                 the one image definition
.dockerignore                              keeps bin, obj, .git, .vs and .claude out of the build context
.github/workflows/
  publish-image.yml                        builds the image once, publishes it to ghcr.io; started by hand
  deploy-<cloud>.yml                       one per cloud: copies the image by digest, deploys it; started by hand
docs/
  SDD.md                                   the solution design
  INSTALL-local.md                         run it with dotnet, run it with Docker
  INSTALL-<cloud>.md                       one per cloud, added by later phases
src/
  Multi-Cloud Container Solution.slnx      the one solution file
  Directory.Build.props                    house build settings for every project (code-csharp-standards, section 1)
  .editorconfig                            house copy, see below
  MultiCloud.Application.Web/              the MVC host
  MultiCloud.Adapter.Platform/             detects and describes the hosting platform
  MultiCloud.Adapter.Notes/                the in-memory Notes database
  MultiCloud.Orchestrator.Heartbeat/       the background heartbeat
  MultiCloud.Test.Unit/                    the unit tests
```

Inside each class library, only `Configuration/`, `Contracts/` and `ServiceCollectionExtensions` are public; data
types that stay inside are under `DTOs/` or `Entities/`, behavior under `Services/` or `Data/`. The web application
keeps its public data types (the JSON resources and bodies, the session) under `Models/`, the MVC convention, and
groups its behavior by concern: `Controllers/`, `Hosting/`, `Errors/`, `Logging/`, `Json/`, `Mapping/`.
`MultiCloud.Application.Web.http` holds one request for every route.

## Skills: owned by Matt

`.claude/skills/` holds 5 Windows junctions, never copies, each gitignored. They point into the public
[Signalbox](https://github.com/mattonweb/Signalbox) repository, cloned at `C:\Source\Signalbox`; its `REPO-SETUP.md`
explains the mechanism. This repository has no skill of its own.

| Skill | What it governs |
|---|---|
| `code-software-standards` | The language-neutral half of the house code standards, read before the language skill |
| `code-csharp-standards` | House C# rules: file layout, type taxonomy, method shape, the 500-line ceiling, allowed language features. Its enforceable half is `src/.editorconfig` plus the analyzers |
| `code-csharp-mstest` | How tests are written: naming, assertion patterns, structure |
| `dotnet-solution-architecture` | Which projects exist and what each may reference: `{{solution}}.{{kind}}.{{name}}` naming, the four project kinds, the dependency rules, and the public surface of a class library. Read it before creating a project or adding a project reference |
| `devops-git-conventions` | The commit-message form you draft for Matt. Its branch, pull-request and tag sections do not apply here; this repository has no branches and no pipeline |

⛔ **Matt owns every skill. Never edit a file inside one of the five junctions**, however wrong it looks; report the
defect in your final message instead. An edit through a junction lands in Signalbox's working tree.

**Commit scopes (`devops-git-conventions` section 3): one scope per drafted commit, from this list, or none when the
change spans the repository.** `web` (the MVC application), `platform` (platform detection), `heartbeat` (the
background heartbeat), `data` (the Notes database), `tests` (the unit tests), `container` (the `Dockerfile` and
`.dockerignore`), `deps` (package and base-image updates), `docs` (`README.md`, the design and the install files),
`skills` (the junctions and their gitignore lines), `charter` (this file). A scope that is not here is added to this
list in its own commit before the commit that uses it; never invented in the message. A change to the workflows is
type `ci` with no scope.

**The 500-line ceiling (`code-csharp-standards` section 8): the named-exception list for this repository is
EMPTY**, and the repository starts with no files over it. A file that needs to exceed the ceiling gets its entry
here, with its line count and a reason, in the same session that creates it.

**Solution architecture (`dotnet-solution-architecture` section 7): adoption state `conforming`.** The planned
solution holds one Application (`MultiCloud.Application.Web`), two Adapters (`MultiCloud.Adapter.Platform` for the
hosting platform, `MultiCloud.Adapter.Notes` for the one data store), one Orchestrator
(`MultiCloud.Orchestrator.Heartbeat`, which references no other project in the solution) and one test project
(`MultiCloud.Test.Unit`). **One Adapter per data store it owns:** a new entity type joins
`MultiCloud.Adapter.Notes`, never a new project. Each class library exposes only `Configuration/`, `Contracts/` and
`ServiceCollectionExtensions`; controllers stay presentation. A change that would break a dependency rule or make an
internal type public is declined with the reason. No architecture test exists yet. The five-project shape is Matt's
decision D1 (`docs/SDD.md` section 15), provisional until the cloud phases have deployed it.

The house `.editorconfig` (`src/.editorconfig`) and `.gitattributes` are verbatim copies of Signalbox's root files,
applied 2026-11-10. A copy that differs from Signalbox is drift: it is fixed in Signalbox first, never here.

## Session start

1. `git status` and `git log -1` in this repository, so you know what Matt last committed and whether the tree
   already holds uncommitted work from an earlier session. **Uncommitted work is Matt's to commit, not yours to
   discard.** Build on it or ask; never revert it.
2. Read this file and `docs/SDD.md` in full. If a tool's behavior disagrees with either, stop that thread and report
   the surprise in your final message rather than working around it.
3. If the junctioned skills under `.claude/skills/` look stale or missing, say so; Matt pulls Signalbox. Do not pull
   it yourself.
