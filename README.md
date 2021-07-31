![](https://github.com/TraceabilityInternet/TraceabilityDriver/raw/main/Images/Traceability%20Internet%20Organization-B1-thin.jpg)

# Need Help?
The Traceability Internet Organization will try it's best to assist everyone the best they can. Below we have documentation that we are constantly updating to try and assist the implementation of the Traceability Driver. If the documentation is not enough, please feel free to email us at help@traceabilityinternet.org and we can help you further.

## Submitting Issues
If you find any issues in the code, or have ideas on how the software can be improved, please submit issues through our GitHub. We are dedicated to ensuring the quality of the software and making sure it provides as much benefit as possible to those implementing it. Thank you!

# Documentation
Below is documentation about the Traceability Driver.

## What is this thing?
The Traceability Driver is a free open-source software tool that can be used to help reduce the costs of making a traceability solution interoperable. It is a cross-platform webservice that can be installed into any existing traceability solution, who can then use the traceability driver to exchange traceability data with anyone else who implements the Traceability Driver.

![](https://github.com/TraceabilityInternet/TraceabilityDriver/raw/main/Images/diagram01.png)

## Build on GS1 Standards
The Traceability Driver uses data models and communication protocols from GS1 to exchange traceability data.

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

## Hosting
The software can be hosted either via IIS, Self-Hosting, or Azure Web Services. 

*Coming soon will be detailed step-by-step guides on each hosting method.*

## Configuration
There are several important fields that need to be configured in the `appsettings.json` file before you can use the Traceability Driver.

* `URL` - This is the URL you want to host the Traceability Driver under.
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
    "URL" : "https://localhost:8001/",
    "APIKey": "<insert_api_key_here>",
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

## Adding an Account
Once you have installed the Traceability Driver, you will need to add each account to the Traceability Driver. This can be done by using the Internal API for the Traceability Driver. Only the host of the Traceability Driver can access the Internal API, and authorization is required using the configured API Key in the `appsettings.json`. More about this is discussed above.

The easiest way to add an account is using the C# Traceability Driver Client provided in the `TEClients` project. 

```
using (ITEInternalClient client = TEClientFactory.InternalClient(url, configuration.APIKey))
{
     // create the account
     ITEDriverAccount account = new TEDriverAccount();
     account.ID = 1;
     account.Name = "Test Account #1";

     // add the account
     account = await client.SaveAccountAsync(account);
}
```
*We are planning to provide clients for other programming languages in the future.*


Otherwise you can make an HTTP Request to your Traceability Driver like so:
```
POST 

URL: https://localhost:8001/api/account

HEADERS
Authorization: Basic <api_key>

BODY
{
  "ID": 1,
  "Name": "Test Account #1"
}
```

After the account is created, the HTTP Request will return an account object that looks like:
```
{
     "DigitalLinkURL" : "https://localhost:8001/1/digital_link",
     "PublicDID":"did:traceabilityengine_v1:c63481c6-8a42-4723-af6b-b2cd30f0d4e1.ew0KICAiUHVibGljS2V5IjogIlBGSlRRVXRsZVZaaGJIVmxQanhOYjJSMWJIVnpQblpyUjA4dk1VeHFZV3hGTlhsQ1NESmFSM0J5UVhnck5FaEZUbEUyYm5RemNWUmhibXh3WTBSUVRGTXpjMDlYVUhVdllrOTVhekZJTmpOMFVrZE9iM1pMVldoSFRqRmtVakJIVFhreGRITlBjMVZSWnpST1VVbzBNbXROYWxkVFZFaHdTMVprUTA5SFpVdFFOMHRXTjJGWWJWSmxhWFowT0hVMWFsYzRhMm9yUlVoTU1uZHdkbWR4ZDFscE9WaGpZbUowV2toUlNrTTVlR2hGYlZoRmVEaEdaalZpWkVwa1JrZGtNRDA4TDAxdlpIVnNkWE0rUEVWNGNHOXVaVzUwUGtGUlFVSThMMFY0Y0c5dVpXNTBQand2VWxOQlMyVjVWbUZzZFdVKyIsDQogICJQcml2YXRlS2V5IjogIiINCn0=",
     "DID":"did:traceabilityengine_v1:6c6f252a-a574-47dd-a809-72f4aa5d722f.ew0KICAiUHVibGljS2V5IjogIlBGSlRRVXRsZVZaaGJIVmxQanhOYjJSMWJIVnpQbTR5YkZnMlkyTk9WVnB0Ymt4TFJEUTVPVTVrUlVoblJrbHhWMjVuZGs1cU5uUTBZbGcwYW5aTmJWVlJVa1kyYVdSVE1XRlhaVUl5Ym5wNU1WVmhZVnBRYlZBd1JrOVVWVEF2TlhCU1lYTm1kalF6YmpCSlYzWndNR05LWlhwVk5tRnBRWGRDWmpOS2REZGtZbUpRY1VSbFRYcEpjWFJzZUZwRWR6RkZUa1ZsWkdsdVdITlBXSHBWVldZMGJqTTBlVWROV0dwSFNsaFBURFpXYld4WlFrMDJhVWhvVFdKSmFWUXhhejA4TDAxdlpIVnNkWE0rUEVWNGNHOXVaVzUwUGtGUlFVSThMMFY0Y0c5dVpXNTBQand2VWxOQlMyVjVWbUZzZFdVKyIsDQogICJQcml2YXRlS2V5IjogIlBGSlRRVXRsZVZaaGJIVmxQanhOYjJSMWJIVnpQbTR5YkZnMlkyTk9WVnB0Ymt4TFJEUTVPVTVrUlVoblJrbHhWMjVuZGs1cU5uUTBZbGcwYW5aTmJWVlJVa1kyYVdSVE1XRlhaVUl5Ym5wNU1WVmhZVnBRYlZBd1JrOVVWVEF2TlhCU1lYTm1kalF6YmpCSlYzWndNR05LWlhwVk5tRnBRWGRDWmpOS2REZGtZbUpRY1VSbFRYcEpjWFJzZUZwRWR6RkZUa1ZsWkdsdVdITlBXSHBWVldZMGJqTTBlVWROV0dwSFNsaFBURFpXYld4WlFrMDJhVWhvVFdKSmFWUXhhejA4TDAxdlpIVnNkWE0rUEVWNGNHOXVaVzUwUGtGUlFVSThMMFY0Y0c5dVpXNTBQanhRUGpBNFNta3lVV3BhVm5ONldVNUVOMkV5TDJsMGIwVkxOblEwYWs1dmFrVkdlazFwZG5KR1drcEdUVmgwVFRoM0wzQlZNRWhIVWtWSk9FZDJibEFyZG01cGJIWlpMMkZpZERoaWQwdFhVVUpKTmpCNGQxZDNQVDA4TDFBK1BGRStkMHhqTTFwUmIxWmxlRWhzVVRGVGNTOUVMM2hRYmxsbFpteHdNV3RJYmpVM1VrcGpVRlJPVDFkcmIyeFBNalprUlRSVlZtdFFjR2hNUVdSSGFsQlpVVEJWTDJ0YVRXRjZORUZWUVdwUlZGRTJTRVpPVjNjOVBUd3ZVVDQ4UkZBK2RFcFZRVlp5VlVaSmRFSjBWRFpEUzFsNWQyVmFTbmxFVUdsRFluVTRVM2xtV1VKdGVqQkRSamhuUlZneGVWSkhORzFDWkhaVVMxcDJUekZJZERKemJHVlJaalpqT1ZReU9WUTJNbFpGS3pVek1qZEJTVkU5UFR3dlJGQStQRVJSUG5OdFdIQkNZMDgzT1hReWRVZG9TVFkxY1VKaGRISlBjSEowV1Zkc2RGRlVjRWcyYlc1S1JIVkxNamhRTkVkRFdsVkJVMWN6YTNGd2QxcFNTMjU0Y2padWIydFRjVXRsTkhjNWVHUnJhVEoyU21vMFkyMVJQVDA4TDBSUlBqeEpiblpsY25ObFVUNXJhVnBCSzNWSVUwWTFORzgyWnpBemN6RlpTVXhsZWxSVFUyMXZRbE5CVUhvdlJtczJRWFpLUjI1T1VVdHpVVm80WkhRNGQyVmxSMWxMYTFwTWVHWktTWFJ5Tkc5SFozWkRRV0p3YUU5NFluQjVjVVIwVVQwOVBDOUpiblpsY25ObFVUNDhSRDVHWjBsa1QxSkdZVXhYU0doVGNrRlBjakpvYlN0T1kxcEliV1IzWTFGMldsSTFkbE5sTmxsVlNVRnJSRGRsZVVNd1ZuRndiSEUzSzNOWk1WbEhlVmM1Ulc1blMxRnlaekZEWlN0a05sSm9ka1ZoYzNSNlkzaFFaMmN3ZVhaTU9HRTRVVTR5VmpseVdEVlhOVVJST1U1d2NYWjZWVU00VVZsd1lXdEZUVFJZZFVweFlXczRSVmx4ZURORWJWSjJRblozTVVWT1luSXlXRGc1TlRkTlIyeDNkR2xyTVdkaVdXRm1SMFU5UEM5RVBqd3ZVbE5CUzJWNVZtRnNkV1UrIg0KfQ==",
     "Name" : "Test Account #1",
     "ID" : 1, 
     "PGLN" :"urn:gdst:foodontology.com:party:1.0"
}
```

### Account ID
The Account ID is an internal identifier for accounts and is optional when creating an account. This is useful for linking accounts on the Traceability Driver to the account in the traceability solution. If an ID is not provided when you create an account, then one will be generated for you.

### Account PGLN
The Account PGLN is a globally unique identifier for identifying the the Account. This would typically be provided when generating the account, however, if it is not provided, then it will be generated for you.

### Account Digital Link URL
The Account Digital Link URL is the URL to the Digital Link Resolver for the account. When creating an account, **you should rarely set this yourself**. The Traceability Driver will generate this itself.

### Account DID
The Account DID is a Decentralized Identifier (DID) that also contains a public / private key for the Account. **You should never share this with anyone**. Inside of it is a baked a Public / Private key. The Traceability Driver knows how to share this without including the Private Key, which is critical in ensuring the security of the system.

### Account Public DID
The `PublicDID` property contains the DID without the private key. This can be shared with others who want to add the account as a trading partner.

## Adding a Trading Partner
Once you have added all the accounts to the Traceability Driver, you will want to add Trading Partners to each account. Adding a Trading Party as a Trading Partner to an account indicates, that this Account is allowed to exchange traceability data with this Trading Party.

This can be done in two ways:
1. You can add a Trading Partner using the Directory Service by providing the PGLN.
2. You can manually add a Trading Partner by providing the PGLN, PublicDID, and Digital Link Resolver URL.

### Adding a Trading Partner with the Directory Service
*Coming soon..*

### Adding a Trading Partner Manually
*Coming soon..*
