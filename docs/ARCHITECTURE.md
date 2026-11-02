# Architecture

The Windows Service is the sole policy authority. Tray is an untrusted client.
All state transitions are serialized in PolicyEngine. WFP is the enforcement
boundary and must be verified independently of desired state.

## Security invariant
Network access may be enabled only when:
- VPN health and protected route are positively verified;
- process inventory is complete and contains no forbidden process;
- WFP enforcement state is verified;
- the manual-lock latch is clear.

Unknown, stale, or failed observations are fail-closed.

## Current scaffold limitations
- VpnMonitor always returns Unknown.
- WfpController is a no-op lock stub and refuses unlock.
- ProcessMonitor uses periodic process enumeration only; it is not race-free.
- No Named Pipes, tray icon, installer, ACL hardening, boot-time filters, or native driver.
