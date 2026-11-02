# WFP implementation plan

Native layer should own engine/session/provider/sublayer/filter lifecycle.
Use stable GUIDs for provider and sublayer. Use explicit transactions and
idempotent reconciliation: inspect installed filters, compare with desired
policy, then apply changes atomically.

Required layers include ALE connect/receive-accept and IP packet layers for
both IPv4 and IPv6. ALE-only rules are not sufficient for all traffic classes.
Use boot-time filters where the threat model requires protection before the
service starts. Keep a persistent lock policy as the recovery baseline.

VPN transport exceptions must be narrowly scoped to physical interface,
remote endpoint, protocol and port. Do not allow all traffic from the VPN
client executable. The unlock policy must enforce traffic only through the
verified tunnel and block physical-interface bypass.

This document is a design checklist, not a claim that these filters exist in
the current code.
