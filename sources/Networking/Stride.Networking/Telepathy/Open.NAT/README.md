![Logo](https://github.com/lontivero/Open.Nat/raw/gh-pages/images/logos/128.jpg)

Open.NAT
======

Open.NAT is a lightweight and easy-to-use class library to allow port forwarding in NAT devices that support UPNP (Universal Plug & Play) and/or PMP (Port Mapping Protocol). 


Goals
-----
NATed computers cannot be reached from outside and this is particularly painful for peer-to-peer or friend-to-friend software.
The main goal is to simplify communication amoung computers behind NAT devices that support UPNP and/or PMP providing a clean 
and easy interface to get the external IP address and map ports and helping you to achieve peer-to-peer communication. 

+ Tested with .NET  _YES_
+ Tested with Mono  _YES_

How to use?
-----------
With nuget :
> **Install-Package Open.NAT** 

Go on the [nuget website](https://www.nuget.org/packages/Open.Nat/) for more information.

Example
--------

The simplest scenario:

```c#
var discoverer = new NatDiscoverer();
var device = await discoverer.DiscoverDeviceAsync();
var ip = await device.GetExternalIPAsync();
Console.WriteLine("The external IP Address is: {0} ", ip);
```

The following piece of code shows a common scenario: It starts the discovery process for a NAT-UPNP device and onces discovered it creates a port mapping. If no device is found before ten seconds, it fails with NatDeviceNotFoundException.


```c#
var discoverer = new NatDiscoverer();
var cts = new CancellationTokenSource(10000);
var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);

await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, 1600, 1700, "The mapping name"));
```

For more info please check the [Wiki](https://github.com/lontivero/Open.Nat/wiki)

Awesome software using Open.NAT
-------------
+ [War For The Overworld](https://wftogame.com/)  
+ [OpenRA](http://www.openra.net/)
+ [Interstellar RIFT](http://www.interstellarrift.com/)
+ [The Kindred](http://thekindred.net/)

Documentation
-------------
+ Why Open.NAT? Here you have [ten reasons](https://github.com/lontivero/Open.NAT/wiki/Why-Open.NAT) that make Open.NAT a good candidate for you projects
+ [Visit the Wiki page](https://github.com/lontivero/Open.Nat/wiki)

Development
-----------
Open.NAT is been developed by [Lucas Ontivero](http://geeks.ms/blogs/lontivero) ([@lontivero](http://twitter.com/lontivero)). 
You are welcome to contribute code. You can send code both as a patch or a GitHub pull request. 

Here you can see what are the next features to implement. [Take it a look!](https://trello.com/b/rkHdEm5H/open-nat)
Build Status
------------

[![Build status](https://ci.appveyor.com/api/projects/status/dadcbt26mrlri8cg)](https://ci.appveyor.com/project/lontivero/open-nat)

[![NuGet version](https://badge.fury.io/nu/open.nat.png)](http://badge.fury.io/nu/open.nat)

##Help me to maintain Open.NAT

![Bitcoin address](https://github.com/lontivero/Open.Nat/raw/gh-pages/images/bitcoinQR.png)

15fdF4xeZBZMqj8ybrrW7L392gZbx4sCXH
