![](https://github.com/TraceabilityInternet/TraceabilityDriver/raw/main/Images/Traceability%20Internet%20Organization-A2-cropped.png)

# Traceability Driver

The Traceability Driver is a service that can be installed into an existing Traceability Solution or any software that stores or needs to receive traceability data.

## Cross-Platform
The Traceability Driver is written in .NET 5 and will be upgraded to .NET 6 upon it's release in November 2021. The driver is meant to be able to be installed on either a Windows, Linux, or Mac machine.

## Registering your Traceability Driver
Registration is 100% free. In order to participate in the Traceability Internet with your Traceability Driver, you need to register your Driver by emailing registration@traceabilityinternet.org. 

Your email needs to contain the following information:

- Company Name
- Company Website
- Role (Solution Provider / Government Agency / etc.)
- Point of Contact

## Installation
In order to install the Traceability Driver you need to download the ZIP file for the latest version. A link to this is here below:

Once you have unzipped the contents, you need to run the **TraceabilityDriver.exe** file inside. If the Traceability Driver has not been configuyred, it will launch and take you to the configuration page to configure the Traceability Driver.

### Building it yourself...
You are more than welcome to build the source-code yourself.

## Configuration
In order to configure your Traceability Driver you will need to provide the following information:

- Service Provider DID
  - You will receive this as a file when you register your Traceability Driver.
- URL
  - This is the URL you wish to host your Traceability Driver under.
- SSL Certification (optional)
- Mapper DLL File

## Mapper
At the core of the Traceability Driver is your mapper. Your mapper is what takes your local data format and converts it into the common C# models provided. Your mapper should be written in C# and implements the `ITETraceabilityMapper` interface provided in the `TEInterfaces.dll` provided in installation package. 

### Help Writing the Mapper
If you would like to hire the Traceability Internet to write and manage your Mapper for you. Please submit an email to `mappers@traceabilityinternet.org` and we will assist you in the matter.
