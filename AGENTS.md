## Documentation

This .NET Application uses the following libraries:

* OpenAI .NET SDK - Context7 libraryId `/openai/openai-dotnet`
* xUnit - Context7 libraryId `/xunit/xunit.net`

Use the `find-docs` skill to learn about how to research in Context7. Use these library IDs and skip the "resolve library" step in this case.

In case of questions regarding classes or concepts from .NET, always prefer the Microsoft Learn CLI (`microsoft-code-reference` and `microsoft-docs` skills) over Context7.

## Quality Assurance

For .NET projects, run `dotnet format style --no-restore --verify-no-changes --severity info` to catch IDE-level diagnostics (e.g. collection initializer suggestions, namespace mismatches) that `dotnet build` alone does not report. Fix all findings.

## Documentation

Consult the existing documentation in `docs` before planning changes. Use it to understand the current architecture, design constraints, and non-obvious decisions before you propose or implement code changes.

Treat the documentation as part of the codebase, not as a separate afterthought. If your changes alter architecture, responsibilities, invariants, workflows, or other behavior described in `docs`, update the relevant documentation in the same task so it stays aligned with the code.
