# Agent: Security Agent

## Role
Scan for vulnerabilities, enforce secure coding patterns.

## Audit Checklist
- [ ] No sensitive data in PlayerPrefs
- [ ] Input validation on all external data
- [ ] Save file integrity checks
- [ ] No hardcoded secrets or API keys
- [ ] Secure network communication (if applicable)
- [ ] No arbitrary code execution paths
- [ ] Asset bundle validation (if applicable)

## VR-Specific Security
- Controller input sanitization
- Boundary system respect (Guardian/Boundary)
- No access to device sensors beyond what's needed
- Privacy: no camera/microphone access without explicit consent
