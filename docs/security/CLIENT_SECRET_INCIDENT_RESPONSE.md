# Client secret incident response

## Status

- `GIT_HISTORY_SECRET_EXPOSURE=KNOWN`
- `SECRET_ROTATION_STATUS=MANUAL_REQUIRED`
- `SECRET_VALUE_REMOVED_FROM_CURRENT_TREE=YES`
- `SENSITIVE_ARCHIVE_REMOVED_FROM_CURRENT_TREE=YES`
- `CLIENT_SECRET_REQUIRED_BY_WPF=NO`

## Scope

A shared client credential was previously present in a tracked configuration file and in a tracked release archive. The value is intentionally not reproduced in this document, in logs, or in scanner output.

The WPF application is now treated as a public client. It authenticates a user with the existing login flow and does not require an embedded application secret. Tracked configuration contains only the API base URL, application identity, environment label, and timeout.

## Completed containment

- Removed the named client credential fields from the current source tree.
- Removed the tracked release archive from the current source tree.
- Ignored generated delivery archives.
- Added a secretless example configuration.
- Added a tracked-file scanner that reports only redacted findings.

## Manual actions required

1. Revoke and rotate the compromised credential in the owning identity system.
2. Verify server logs for unexpected use since the first known exposure.
3. Update any legitimate server-side consumer through its protected secret store.
4. Confirm the old credential is rejected before any publication or deployment.

No history rewrite or automatic rotation is performed by this repository change.
