# Security requirements before production

1. Implement persistent and boot-time WFP policy; prove protection before user logon.
2. Keep WFP lock filters alive if the service exits. Never remove filters on shutdown.
3. Do not allow generic outbound traffic as a side effect of VPN transport exceptions.
4. Resolve and pin VPN endpoint/transport; generic virtual adapter detection is insufficient.
5. Block IPv4, IPv6, DNS, UDP, TCP and non-TCP IP traffic outside the protected path.
6. Protect config and binaries with restrictive ACLs and code signing.
7. Authenticate Named Pipe clients and authorize each command by SID.
8. Make process inventory complete; inaccessible process metadata means no unlock.
9. Close process-start/unlock races; periodic polling alone is insufficient.
10. Test boot, sleep/resume, adapter changes, multiple VPNs, split tunnel and IPv6 leaks.
11. Provide a signed, audited recovery/uninstall path that does not silently open the network.
12. Test on disposable VMs with console access; a bad WFP policy can cut off remote access.
