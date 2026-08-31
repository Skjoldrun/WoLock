<#
.SYNOPSIS
    Sends Wake-on-LAN (WOL) magic packets to one or more target MAC addresses.

.DESCRIPTION
    Builds the standard WOL "magic packet" (6 x 0xFF followed by the target MAC
    repeated 16 times) and sends it as a UDP datagram to the given target IPs
    on ports 9 and 7 (with broadcast enabled).

.PARAMETER MacAddress
    The target MAC address (e.g. 84:47:09:88:78:56). Defaults to the MUNIN
    server's Ethernet NIC. Override by passing a different value.

.PARAMETER TargetIps
    The destination IP(s) to send the packet to. Defaults to the network
    broadcast and the DHCP-assigned addresses for the MUNIN server.

.EXAMPLE
    # Wake the MUNIN server (default MAC)
    .\Send-WakeOnLan.ps1

.EXAMPLE
    # Wake a different machine, overriding the default MAC
    .\Send-WakeOnLan.ps1 -MacAddress "AA:BB:CC:DD:EE:FF"

.NOTES
    WOL generally only works over a wired (Ethernet) connection, not WLAN.
    The target machine must have WOL enabled in BIOS/UEFI and in the NIC driver.
#>

[CmdletBinding()]
# Default target MAC address (MUNIN server, Ethernet NIC)
param(
    [string]$MacAddress = '84:47:09:88:78:56',

    [string[]]$TargetIps = @(
        '255.255.255.255',
        '172.19.1.255',
        '172.19.1.87',
        '172.19.1.86'
    )
)

# Parse the MAC address string into byte values (e.g. "AA:BB:CC:DD:EE:FF")
$macBytes = $MacAddress -split '[:\\-]' | ForEach-Object { [Convert]::ToByte($_, 16) }

if ($macBytes.Count -ne 6) {
    throw "Invalid MAC address format: $MacAddress"
}

# Build the magic packet: 6 bytes of 0xFF followed by the MAC repeated 16 times
$packet = [byte[]]::new(102)
for ($i = 0; $i -lt 6; $i++) { $packet[$i] = 0xFF }
for ($i = 1; $i -le 16; $i++) { [Array]::Copy($macBytes, 0, $packet, $i * 6, 6) }

foreach ($ip in $TargetIps) {
    foreach ($port in 9, 7) {
        $udp = [System.Net.Sockets.UdpClient]::new()
        $udp.EnableBroadcast = $true
        [void]$udp.Send($packet, $packet.Length, $ip, $port)
        $udp.Close()
        Write-Output "Magic packet sent to $MacAddress -> $ip :$port"
    }
}

Write-Output "Done."