![](https://github.com/TraceabilityInternet/TraceabilityDriver/raw/main/Images/Traceability%20Internet%20Organization-B1-thin.jpg)

# Need Help?
The Traceability Internet Organization will try it's best to assist everyone the best they can. Below we have documentation that we are constantly updating to try and assist the implementation of the Traceability Driver. If the documentation is not enough, please feel free to email us at help@traceabilityinternet.org and we can help you further.

## Submitting Issues
If you find any issues in the code, or have ideas on how the software can be improved, please submit issues through our GitHub. We are dedicated to ensuring the quality of the software and making sure it provides as much benefit as possible to those implementing it. Thank you!

# Documentation
Below is documentation about the Traceability Driver.

## What is this thing?
The Traceability Driver is a free open-source software tool that can be used to help reduce the costs of making a traceability solution interoperable. 

### White Paper
For starters, you can review the [Traceability Driver White Paper](https://github.com/TraceabilityInternet/TraceabilityDriver/blob/main/Traceability%20Driver%20White%20Paper.docx?raw=true) and the [Traceability Driver Security White Paper](https://github.com/TraceabilityInternet/TraceabilityDriver/blob/main/Traceability%20Driver%20Security%20White%20Paper.docx?raw=true). These were the original documents written around the Traceability Driver.

## Cross-Platform
The Traceability Driver is written in .NET 5 and will be upgraded to .NET 6 upon it's release in November 2021. The driver is meant to be able to be installed on either a Windows, Linux, or Mac machine.

## Registering your Traceability Driver (optional)
In order to use the Directory Service, you will need to register your traceability solution with the Traceability Internet Organization by contacting help@traceabilityinternet.org . This is not required to utilize the Traceability Driver.

## Installation
The `TraceabilityDriverService` is what needs to be installed. You will need to build the project yourself and publish the `TraceabilityDriverService` project. Once published, it will produce a package that can either be hosted in IIS, self-hosted on any machine, or hosted on Azure. You can even publish it directly to Azure using Visual Studio. 

### Building it yourself...
Building the source-code yourself is easy and can be done for free using Visual Studio Community 2019 which is available for both Windows and Mac. If you are using a Linux based system like Ubuntu, you can use Visual Code which is available for Linux to build the source-code.

### What if I can't build the code?
We recommned you review and build the code yourself so that you can feel condifent about using it. If this is not an option, please feel free to use the pre-built package in the `Published` folder in the root directory.

## Configuration
There are several important fields that need to be configured in the `appsettings.json` file before you can use the Traceability Driver.

* `MapperDLLPath` - This is the physical filepath to the compiled DLL that contains the mapper for your Traceability Driver. 
* `MapperClassName` - This is the full namespace to the class the mapper DLL that will be used for mapping to/from your local data format into the common data model.
* `APIKey` - This is the configured API Key that will be required to talk to your Traceability Driver through the Internal API. More about the Internal API is documented below.
* `RequiresTradingPartnerAuthorization` - This is a flag that can be used to require Trading Parties to be authorized before they can access traceability data from your accounts. By default, this flag is set to `TRUE`, but can be set to `FALSE` to allow traceability data to be accessed publicly by anyone.
* `ConnectionString` - This is a connection string to a MongoDB. MongoDB is free to use, can be installed locally, or a free database can be setup in the cloud at https://www.mongodb.com/cloud
* `DirectoryURL` (Optional) - If you want to utilize the Directory Service, then you need to configure this to `https://directory.traceabilityinternet.org`. Before you can use the Directory Service you must contact help@traceabilityinternet.org and register your traceability solution. They will provide you with a registered Service Provider DID and PGLN that needs to be configured on your end.
* `ServiceProviderDID` (Optional) - If you are using the Directory Service, you will set this configuration value to the provided DID when you registered your traceability solution.
* `ServiceProviderPGLN` (Optional) - If you are using the Directory Service, you will set this configuration value to the provided PGLN when you registered your traceability solution.
* `DatabaseName` (Optional) - By default, the service will use the database name "TraceabilityDriver" when connecting to your MongoDB. However, you can use this configuration value to override that and use a different name.
* `EventURLTemplate` - This is used to tell the Traceability Driver where to forward requests for events to. This should be a URL to a local API for your traceability solution. There are three potential arguments that can be used in the URL Template. Those are `{account_id}`, `{tradingpartner_id}`, and `{epc}`. The `{account_id}` will be replaced with the Account ID that the data is be requested from. The `{tradingpartner_id}` will be replaced with the ID of the Trading Partner that is making the request. The `{epc}` will be replaced with the EPC that they are requesting event data for. Example URL templates are provided below.
* `TradeItemURLTemplate` - This is used to tell the Traceability Driver where to forward requests for trade items / product definitions to. This should be a URL to a local API for your traceability solution. There are three potential arguments that can be used in the URL Template. Those are `{account_id}`, `{tradingpartner_id}`, and `{gtin}`. The `{account_id}` will be replaced with the Account ID that the data is be requested from. The `{tradingpartner_id}` will be replaced with the ID of the Trading Partner that is making the request. The `{gtin}` will be replaced with the GTIN that they are requesting trade item / production definition data for. Example URL templates are provided below.
* `LocationURLTemplate` - This is used to tell the Traceability Driver where to forward requests for locations to. This should be a URL to a local API for your traceability solution. There are three potential arguments that can be used in the URL Template. Those are `{account_id}`, `{tradingpartner_id}`, and `{gln}`. The `{account_id}` will be replaced with the Account ID that the data is be requested from. The `{tradingpartner_id}` will be replaced with the ID of the Trading Partner that is making the request. The `{gln}` will be replaced with the GLN that they are requesting location data for. Example URL templates are provided below.
* `TradingPartnerURLTemplate` - This is used to tell the Traceability Driver where to forward requests for trading parties to. This should be a URL to a local API for your traceability solution. There are three potential arguments that can be used in the URL Template. Those are `{account_id}`, `{tradingpartner_id}`, and `{pgln}`. The `{account_id}` will be replaced with the Account ID that the data is be requested from. The `{tradingpartner_id}` will be replaced with the ID of the Trading Partner that is making the request. The `{pgln}` will be replaced with the PGLN that they are requesting trading party data for. Example URL templates are provided below.

*More about Accounts, Trading Partners, and Trading Parties can be found below. There is also information about Trading Partner ID and Account ID below.*

### Example Configuration
`appsettings.json`
```
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning",
            "Microsoft.Hosting.Lifetime": "Information"
        }
    },
    "AllowedHosts": "*",
    "MapperDLLPath": "c:\\webservices\\traceability_driver\\mapper\\MyMapper.dll",
    "MapperClassName": "MyMapper.MyMapperClass",
    "ConnectionString": "mongodb://localhost",
    "DirectoryURL": "https://directory.traceabilityinternet.org",
    "ServiceProviderDID": "did:traceabilityengine_v1:972e6c79-b169-4ce2-b815-47a0db2c785e.ew0KICAiUHVibGljS2V5IjogIlBGSlRRVXRsZVZaaGJIVmxQanhOYjJSMWJIVnpQakIyVkhBM1FYRlNhVGw1YzFsamEwaDNaMUUwVDI1SFVHc3lTVEkwVVRVNU1XSlFhM0pWTVhkVWEyaENTREJHYkZGamR6UnVkVFpHYm14TFFVbERkM0paVUVKdlNHMUVNMXBvU1RWU1drbERPRkV5TWpCV2RWbEhZbmh1WjNwSVJFNUtkakI2Y2pkWlEyODRabHBNTVRCUlEwdENkbWM0Yld0c1dEUm9RVzFOUVVOaVNYbGFNa2xtTjFCTlZ6WlhRMEpWY0RkQ1ZtTnFlU3RJZDIxdVJscFpiMmcwTXpGUFpVZE5WVDA4TDAxdlpIVnNkWE0rUEVWNGNHOXVaVzUwUGtGUlFVSThMMFY0Y0c5dVpXNTBQand2VWxOQlMyVjVWbUZzZFdVKyIsDQogICJQcml2YXRlS2V5IjogIlBGSlRRVXRsZVZaaGJIVmxQanhOYjJSMWJIVnpQakIyVkhBM1FYRlNhVGw1YzFsamEwaDNaMUUwVDI1SFVHc3lTVEkwVVRVNU1XSlFhM0pWTVhkVWEyaENTREJHYkZGamR6UnVkVFpHYm14TFFVbERkM0paVUVKdlNHMUVNMXBvU1RWU1drbERPRkV5TWpCV2RWbEhZbmh1WjNwSVJFNUtkakI2Y2pkWlEyODRabHBNTVRCUlEwdENkbWM0Yld0c1dEUm9RVzFOUVVOaVNYbGFNa2xtTjFCTlZ6WlhRMEpWY0RkQ1ZtTnFlU3RJZDIxdVJscFpiMmcwTXpGUFpVZE5WVDA4TDAxdlpIVnNkWE0rUEVWNGNHOXVaVzUwUGtGUlFVSThMMFY0Y0c5dVpXNTBQanhRUGpOcE0yWldURGxPVUV0Rk1HRmhWVGh3WWt0cGIweGtkVnBwYm14Q1pGVTNOVXhOY2tGM1dWTlNkM2g1VFhVd1VsRnFZM1JxTjNnNVdXcHFhM1kwVVRCUU1VSjJVbEp1Tm5sV1YwVkpZakowTlZjdlF6WjNQVDA4TDFBK1BGRStPSGhITVRZelFXODNMMVEyUWk5M1prTTBORkpJWkdReWMyWnhhSFpZYUdaRk5EUlNZMFpOUlVaMk5UaGxVQ3M1VDNOdVkyZDNNblkxWkVoMk9HNVJaelZCT0Znd1pHTkJhMmx2TTBwQk1VSm1NbVpJUkhjOVBUd3ZVVDQ4UkZBK2IxTlhTV0pKUm5OV1pVNWxhbkJ0YjJsVk5IUnZiMEZ2T1V4VVExSm9OMHBIUkVoVWNYQm1SM1ptTWtzdmRVUjJkV0ZWYm5sTE9HZERhRkJXTldkeVVHdHVVMWR1TDI5a05tUlhObmh1V1RkSlRWTldSRkU5UFR3dlJGQStQRVJSUGxoMGNsQmxiVkpZY0ZkVFpGZG9XbUZoZVVSNWNuZHRVbE5XTURoV2RuTjBXbmt3ZFhkMk1ubG1LMHR3Tkc1SmVXWXdiV1JKZDNSd1ZrTTRPRU4zY1d0a2VuTnZiVmgyYTNacFZqVlNWR3hxUlZCM1NHOTNQVDA4TDBSUlBqeEpiblpsY25ObFVUNU1jVTFsZW1admVISkRTblI1UzBGM1ZFaDVMMlZXVFRJNFpEQlJVbEJFVlVsSVQyVlJZVFo0ZEZWV2NrMXhTbWRwTTFSUllWaFJTelJGV25aTVYzbG1MMEpCWlU5cEwzRmhja2hpYVRRNE1WaE1XVEpzZHowOVBDOUpiblpsY25ObFVUNDhSRDVZYkVKSk1VNU1WWGd5WW1SWGMzQTFkMWhCU21jelpVbzNVMkZZZVc0MmEwYzBjbFo2WmxVeFNrbHhRazExZDFaNlVtb3djRE50VlVjemFHVXJRVTVJVlZsMWNIZFJjM2hGUmpGT1dtRkZiREUwWkN0R09FOVBWalF3UW5OcVEwTk9TMDUyY3pWNE56aE5Ra2RIWjNSaFdGWkpTRU5VYTFCU2VIRlBiRWxXT0hGelpqSXlXWFIwYUVaV04yMURZMHBTY0hsdmMxRlplakZXTWpoamRUbGtORGN5V0Rkb1JXVnFTRVU5UEM5RVBqd3ZVbE5CUzJWNVZtRnNkV1UrIg0KfQ==",
    "ServiceProviderPGLN": "urn:traceabilityinternet:party:ServiceProvider.7a8aa860-279a-4c49-9b21-32bbbad57f63",
    "DatabaseName": "TraceabilityDriver",
    "EventURLTemplate": "http://localhost:1360/xml/{account_id}/{tradingpartner_id}/events/{epc}",
    "TradeItemURLTemplate": "http://localhost:1360/xml/{account_id}/{tradingpartner_id}/tradeitem/{gtin}",
    "LocationURLTemplate": "http://localhost:1360/xml/{account_id}/{tradingpartner_id}/location/{gln}",
    "TradingPartyURLTemplate": "http://localhost:1360/xml/{account_id}/{tradingpartner_id}/tradingpartner/{pgln}"
}
```

## Mapper
At the core of the Traceability Driver is your mapper. Your mapper is what takes your local data format and converts it into the common C# models provided. Your mapper should be written in C# and implements the `ITETraceabilityMapper` interface provided in the `TEInterfaces.dll` provided in installation package. The mapper provides 4 pairs of methods, for mapping **Events**, **Trade Items / Product Definitions**, **Locations**, and **Trading Parties**.

## Accounts, Trading Parties, Trading Partners, what's all this about?
There are three terms that are thrown around throughout this documentation, and other places in the technical realm of traceability that merit a bit of an explination because they can be confusing.
* **Trading Party** is any company/business that can take ownership of a product, product definition, and/or location. They are the most generic term.
* **Account** is a Trading Party that is hosted by a solution provider. This would be a way for a solution provider to refer to a Trading Party that they are hosting on their traceabililty solution
* **Trading Partner** is a Trading Party that an Account sells and/or buys products to/from. This would generally be a Trading Party that an Account exchanges traceability directly with. 

All Accounts and Trading Partners are Trading Parties. A Trading Partner, is simple another Account on the same solution provider or another solution provider. If anyone thinks this is still confusing or has a better way to explain this, please post an issue and we will promptly review it.

## Registering an Account
Once you have installed the Traceability Driver, you will need to register your each account with the Traceability Driver.

## Adding a Trading Partner
