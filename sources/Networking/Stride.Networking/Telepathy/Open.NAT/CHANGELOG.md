### Version 2.1.0
* Fixes [defect #43](https://github.com/lontivero/Open.NAT/issues/43).
UpnpNatDevice.CreatePortMapAsync does not await retry
* Fixes [defect #45](https://github.com/lontivero/Open.NAT/issues/45).
UpnpNatDevice.GetSpecificMappingAsync returns wrong protocol

### Version 2.0.16
* Supports .NET 3.5
* Fixes [defect #28](https://github.com/lontivero/Open.NAT/issues/28).
GetSpecificMappingAsync under LINKSYS WRT1900AC AC1900 fails with 501

### Version 2.0.11
* Allows the creation of mappings with arbitrary Private IP address.
* Fixes [defect #22](https://github.com/lontivero/Open.NAT/issues/22). 
Routers failed with 404 when service control url had a question mark (?) - DD-WRT Linux base router (and others probably) fails with
402-InvalidArgument when index is out of range. - Some routers retuns invalid mapping entries with empty internal client.

* Fixes [defect #24](https://github.com/lontivero/Open.NAT/issues/24).
GetSpecificMappingEntry fails with
402-InvalidArgument in DD-WRT Linux base router when mapping is not found.

### Version 2.0.10
Fixes [defect #20](https://github.com/lontivero/Open.NAT/issues/20). Absolute service control URL path and query miscalculated.   

### Version 2.0.9
* Fixes [defect #16](https://github.com/lontivero/Open.NAT/issues/16)

### Version 2.0.8
* Fixes several defects. [#10](https://github.com/lontivero/Open.NAT/issues/10), 
[#11](https://github.com/lontivero/Open.NAT/issues/11), [#12](https://github.com/lontivero/Open.NAT/issues/12),
[#13](https://github.com/lontivero/Open.NAT/issues/13) and [#14](https://github.com/lontivero/Open.NAT/issues/12)

### Version 2.0.0
* Thus version breaks backward compatibility with v1.
* Changes the event-based nature of discovery to an asynchronous one.
* Managed port mapping timelife.

### Version 1.1.0
* Fix for SSDP Location header.
* After this version Open.NAT breaks backward compatibility.

### Version 1.0.19
* Minor changes previous to v2.

### Version 1.0.18
* Discovery timeout raises an event.
* Permanent mappings are created when NAT only supports that kind of mappings.
* Protocol to use in discovery process can be specified.
* Automatic renew port mappings before expiration.
* Add operations timeout after 4 seconds.
* Add parameters validation in Mapping class.
* Fix UnhandledException event was never raised.

### Version 1.0.17
*  Discovery timeout added.
*  Auto release ports opened in the session.
*  Fix NextSearch to use UtcNow (also performance)
*  Fix LocalIP property after add a port.
*  Tracing improvements

### Version 1.0.16
*  Discovery performance and bandwidth improved
*  Tracing improved
*  Cleaner code
