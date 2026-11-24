# DocFX Migration

- **Status:** Completed; local verification and hosted build/deploy are recorded
  separately below. This is documentation delivery, not a published mod release.
- **Recorded:** November 23, 2026.
- **Implementation / evidence source:** [527257e](https://github.com/mrdav30/Rimgate/commit/527257ed7a2469dfb83eaabb213afd6948f7cc2d);
  the original local verification text is preserved below as an as-at snapshot.
- **Scope:** Wiki removal, repository-owned Guides, compiled API documentation,
  pinned portable build references, Pages workflow, README branding and the
  in-game Docs link. API prose enrichment and RimWorld 1.7 migration were excluded.
- **Current instructions:** [Contributor guide](../../guides/developer-and-contributor-notes.md#build-documentation-and-api-reference).

## Local migration verification

On November 23, 2026, all 23 wiki Markdown articles were migrated into the
repository's Guides section, with normalized filenames and explicit `.md` links.
Three stale Wraith biomass links were corrected and a source-backed settings
reference added for the previously missing target. The wiki worktree and
`.gitmodules` entry were removed after checking every original file against a
local backup; existing module history remains recoverable in Git's object store.

The documentation build compiled against public RimWorld 1.6.4871 references and
pinned mod binaries, then generated 332 public type pages across three namespaces
and 24 guides. The ordinary installed-game reference build also passed. Compiler
and DocFX checks completed with zero warnings and errors. The full script passed
under Windows PowerShell 5.1 when invoked from an unrelated working directory,
and in an independent checkout fixture with an empty NuGet package cache and no
game files or pre-restored mod references. Generated-site checks rejected both
a missing-link control and a wrong-case-link control; dependency binaries were
absent from the rendered site.

The local browser preview covered the home page, API navigation, the global
glyph-parchment type, topic search and the gate guide at a 390-pixel viewport
without horizontal overflow. The search index contains both guides and API
pages. The in-game documentation URL and English button label now target the
site. The Git index, HEAD and all four baseline bundle hashes were preserved;
no content migration to RimWorld 1.7 was made. A hosted Actions/deployment run
still requires these source changes to land on `main`; local build success is
not recorded as a hosted CI run.

GitHub settings were verified with Pages enabled in workflow mode, Wiki disabled
and the repository website set to the documentation URL. No commit or push was
made and no hosted deployment was triggered during this migration.

## Hosted publication closeout

After the implementation was committed and pushed,
[Documentation run 45277849409](https://github.com/mrdav30/Rimgate/actions/runs/45277849409)
completed successfully for `527257ed7a2469dfb83eaabb213afd6948f7cc2d`. The run was created at
`2026-11-23T22:15:04Z`; both the `build` and `deploy` jobs reported success.
The build job compiled the library, guides and API reference and uploaded the
Pages artifact; the deploy job configured Pages and deployed documentation.
These statuses were read back when this record was archived.

The original local snapshot's pending-publication statements describe its time
of writing. This later closeout establishes hosted workflow success; it does not
turn those local checks into a hosted run or establish new gameplay/performance
validation. The user also reported the site deployed successfully.
