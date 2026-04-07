# Workflow: Security Audit

## Autonomy Level: FULL

## Process
1. **Audit**: Scan for vulnerabilities in current codebase
2. **Classify**: Rate each finding by severity (Critical, High, Medium, Low)
3. **Fix**: Address findings from highest to lowest severity
4. **Verify**: Confirm fixes don't introduce regressions
5. **Document**: Record findings and fixes in project-memory.md

## Common VR Security Concerns
- Input validation for multiplayer data (if/when added)
- Save file integrity (prevent tampered save data)
- No sensitive data in PlayerPrefs (use encrypted storage)
- Asset bundle validation (prevent modified bundles)
- Network traffic encryption for any online features
