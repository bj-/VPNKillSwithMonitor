# Testing plan

Run only in disposable Windows VMs with hypervisor console access.

- Cold boot with no VPN: no ordinary traffic before service readiness.
- VPN connect/disconnect while generating TCP, UDP, ICMP and DNS traffic.
- IPv4 and IPv6 leak tests against a controlled external endpoint.
- Ethernet/Wi-Fi switch, DHCP renew, sleep/resume and route churn.
- Multiple VPNs, split tunnel, endpoint rotation and DNS changes.
- Start forbidden process during unlock and at high frequency around policy transition.
- Same executable name from a different path; renamed executable; multiple instances.
- Process owned by another user and elevated process.
- Service crash, forced termination, update and uninstall.
- Confirm WFP filters remain in fail-closed state after service failure.
