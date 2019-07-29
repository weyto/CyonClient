# CyonClient
A DNS client library for https://my.cyon.ch based on .Net Core 2.2.<br /><br />
This library supports the following functions:
* List all domains including all DNS Records
* Create a new DNS record
* Update existing DNS record
* Delete a DNS record

# Usage
The following section describes the usage of the library.

## Initialize connection
To initialize the client you need
* AccountName (see https://my.cyon.ch/domain field Benutzername)
* Username of your account
* Password of your account

```csharp
CyonClient.CyonContext cyonContext = new CyonClient.CyonContext("accountName", "username", "password");
```

## List domains
```csharp
CyonClient.CyonContext cyonContext = new CyonClient.CyonContext("accountName", "username", "password");
List<Domain> domains = cyonContext.GetDomains().Result;
```

## List DNS Records
```csharp
CyonClient.CyonContext cyonContext = new CyonClient.CyonContext("accountName", "username", "password");
List<Domain> domains = cyonContext.GetDomains().Result;
IReadOnlyCollection<DNSRecord> dnsrecords = domains.FirstOrDefault(d => d.Name == "mydomain.ch").DNSRecords;
```

## Create DNS Records
```csharp
CyonClient.CyonContext cyonContext = new CyonClient.CyonContext("accountName", "username", "password");
List<Domain> domains = cyonContext.GetDomains().Result;
IReadOnlyCollection<DNSRecord> mydomain = domains.FirstOrDefault(d => d.Name == "mydomain.ch");
mydomain.CreateARecord("test3.mydomain.ch", "203.0.113.47", DNSTTL.FourHours).Wait()
```

## Update DNS Records
```csharp
CyonClient.CyonContext cyonContext = new CyonClient.CyonContext("accountName", "username", "password");
List<Domain> domains = cyonContext.GetDomains().Result;
Domain mydomain = domains.FirstOrDefault(d => d.Name == "mydomain.ch");
DNSRecord record = mydomain.DNSRecords.FirstOrDefault(r => r.Name.Equals("test.mydomain.ch."));
record.Value = "203.0.113.48";
mydomain.UpdateDNSRecord(record).Wait();
```

## Delete DNS Records
```csharp
CyonClient.CyonContext cyonContext = new CyonClient.CyonContext("accountName", "username", "password");
List<Domain> domains = cyonContext.GetDomains().Result;
Domain mydomain = domains.FirstOrDefault(d => d.Name == "mydomain.ch");
DNSRecord record = mydomain.DNSRecords.FirstOrDefault(r => r.Name.Equals("test.mydomain.ch."));
oninovit.DeleteDNSRecord(record).Wait();
```
