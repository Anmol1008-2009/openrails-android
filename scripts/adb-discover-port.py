#!/usr/bin/env python3
"""Discover the current Android wireless-debugging TCP port via mDNS.

`adb mdns services` is unavailable in the adb build used by this project, so
query the `_adb-tls-connect._tcp.local` service directly and print `host:port`.
"""

import socket
import struct
import time

SERVICE = ["_adb-tls-connect", "_tcp", "local"]
MDNS = ("224.0.0.251", 5353)


def main() -> int:
    question = b"".join(bytes([len(label)]) + label.encode() for label in SERVICE) + b"\x00"
    packet = struct.pack("!HHHHHH", 0, 0, 1, 0, 0, 0) + question + struct.pack("!HH", 12, 1)

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM, socket.IPPROTO_UDP)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.settimeout(2)
    sock.sendto(packet, MDNS)

    found: set[tuple[str, int]] = set()
    deadline = time.time() + 5
    while time.time() < deadline:
        try:
            data, addr = sock.recvfrom(9000)
        except socket.timeout:
            break
        if b"_adb-tls-connect" not in data:
            continue

        pos = 0
        while True:
            srv = data.find(b"\x00\x21\x00\x01", pos)
            if srv < 0:
                break
            # SRV RDATA: priority(2) weight(2) port(2), starting at offset +10.
            port = struct.unpack("!H", data[srv + 14 : srv + 16])[0]
            if port:
                found.add((addr[0], port))
            pos = srv + 1

    for host, port in sorted(found):
        print(f"{host}:{port}")

    return 0 if found else 1


if __name__ == "__main__":
    raise SystemExit(main())
