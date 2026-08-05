# Administrative foundation performance baseline

Measured on 2026-08-05 on the current Windows development workstation, using a Release build and no production service.

| Metric | Result | Method |
| --- | --- | --- |
| `STARTUP_TIME` | `NOT_MEASURED` | Requires controlled WPF process instrumentation and an interactive login. |
| `LOGIN_TO_SHELL_TIME` | `NOT_MEASURED` | Requires test credentials and an isolated running API. |
| `SHELL_NAVIGATION_TIME` | `NOT_MEASURED` | Requires UI automation with a rendered dispatcher. |
| `CAPABILITIES_LOAD_TIME` | `NOT_MEASURED` | Requires a controlled authenticated API endpoint. |
| `TEST_EXECUTION_TIME` | `9.6 s` | Clean-clone `dotnet test` Release, `--no-build`, 22 tests. |

The clean-clone Release solution build completed in 22.28 seconds with 0 errors and 43 warnings: 33 from the client and 10 from Core. This is supporting context, not one of the required runtime metrics.

The unmeasured values must remain unclaimed until a repeatable isolated environment, non-production credentials, and a UI timing harness exist. The next baseline should record machine profile, API latency, warm/cold process state, sample count, median, and p95.
