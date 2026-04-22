# Architecture

This document captures the non-obvious design decisions behind the current console app structure. It intentionally avoids restating details that are already easy to see in individual source files.

## System Shape

The application is split into three layers with deliberately small responsibilities:

- `Program.cs` is only the composition root. It bootstraps configuration, turns bootstrap failures into process exit code `1`, and delegates everything else.
- `AppBootstrapper` owns startup validation. This keeps configuration lookup order, required runtime files, and user-facing startup errors in one place instead of scattering them across the CLI and runner code.
- `AppCli` is the only command-line front door. It decides which execution mode to run and wires mode-specific objects together.

The design intentionally stops short of introducing a DI container or a deeper service graph. The codebase is still small enough that explicit object construction is easier to follow than service registration indirection.

## CLI Model

The CLI uses `System.CommandLine` for two reasons that are not obvious from the code alone:

- The interactive REPL remains the default action of the root command to preserve the original low-friction user experience. Starting the app without arguments should still enter chat mode immediately.
- The one-shot path is modeled as an explicit `run <prompt>` command rather than custom `args` parsing or a root option. This follows the .NET command-line guidance that actions should be represented as commands and parameters as arguments or options.

This split is important for future extensions. Additional automation-oriented actions can become sibling commands without changing REPL behavior or overloading the root invocation rules.

## Execution Modes

The two execution modes are intentionally separate runners rather than flags inside one loop:

- `ReplRunner` owns the interactive loop, shell-like commands, conversation-state growth, and conversational streaming output.
- `OneShotRunner` owns exactly one validated request and exits immediately after writing the answer.

This prevents the one-shot mode from accidentally inheriting REPL-specific behavior such as banners, prompts, slash-command handling, or persistent conversation state.

The different output contracts are deliberate:

- REPL writes `Assistant:` prefixes and input prompts because it is optimized for humans.
- One-shot writes only the model output so that it stays script-friendly.

## Conversation State

`ConversationState` stores only completed chat turns, not arbitrary input history. That rule exists to keep model input semantically clean.

The state model deliberately stays independent from request construction details. It keeps completed turns in a small internal role-tagged form and materializes OpenAI `ResponseItem` message objects only when a runner asks for input items.

The following inputs are intentionally excluded from conversation state:

- Slash-commands such as `/exit`
- Invalid user input that fails validation
- Failed requests
- One-shot invocations

The REPL computes the next request input by appending the current user turn transiently to the completed turns, and only persists the new user/assistant pair after the streamed response produced actual assistant text. This avoids polluting future model input with failed or partial exchanges.

## Slash-Command Boundary

Slash-commands are treated as a separate input class, not as normal user prompts with ad-hoc `if` checks. The dispatcher is intentionally lightweight, but the boundary matters:

- REPL control commands should not become model input.
- New slash-commands can be added without touching the conversation model.
- Unknown slash-commands can be handled explicitly instead of silently reaching the model.

The project does not use a general command framework for in-REPL commands because the current scope does not justify a second abstraction stack on top of `System.CommandLine`.

## Configuration and Runtime Invariants

Several runtime assumptions are centralized because they are easy to break during refactors:

- Configuration precedence is `User Secrets` first, then environment variables. This ordering is preserved intentionally and should not be changed casually.
- `system-prompt.md` is treated as a runtime asset, not just a source file. The application resolves it from `AppContext.BaseDirectory`, so the project file must continue copying it to the output directory.
- Startup configuration errors are separated from request failures. Missing API keys or prompt files fail before CLI execution begins; request errors happen inside the runners and return mode-appropriate exit codes.

`AppRuntime` is intentionally thin and only holds the shared runtime dependencies that both execution modes genuinely need: the OpenAI client and the loaded system prompt.

## Request Construction

The OpenAI request setup remains inside the runners instead of being hidden behind a shared chat service. This is deliberate.

The request creation logic is small, but the surrounding behavior differs by mode:

- REPL needs typed conversation-item handling and human-oriented prefixes.
- One-shot needs direct output and no state persistence.
- Error handling and exit-code decisions differ between the modes.

Keeping request construction close to those behaviors makes it easier to reason about mode-specific changes and avoids inventing a premature abstraction that would mostly forward parameters.

## Validation Rules

Input validation is shared through `PromptValidator` so that REPL and one-shot mode enforce the same prompt contract. The important architectural point is not the specific rules, but that validation happens before slash-command dispatch reaches the model and before either runner starts a request.

If validation changes in the future, both execution paths should continue to consume the same validator unless there is an explicit product reason for mode-specific limits.