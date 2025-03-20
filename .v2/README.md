![](https://github.com/TraceabilityInternet/TraceabilityDriver/raw/main/Images/Traceability%20Internet%20Organization-B1-thin.jpg)

# Need Help?
The Traceability Internet Organization will try it's best to assist everyone the best they can. Below we have documentation that we are constantly updating to try and assist the implementation of the Traceability Driver. If the documentation is not enough, please feel free to email us at philip@pandoscape.com and we can help you further.

## Submitting Issues
If you find any issues in the code, or have ideas on how the software can be improved, please submit issues through our GitHub. We are dedicated to ensuring the quality of the software and making sure it provides as much benefit as possible to those implementing it. Thank you!

# Documentation
Below is documentation about the Traceability Driver.

## What is this thing?
The Traceability Driver is a free open-source software tool that can be used to help reduce the costs of making a traceability solution interoperable. It is a standalone module that can be installed into an existing software system to expose traceability data using the GDST module.

![](https://github.com/TraceabilityInternet/TraceabilityDriver/raw/main/Images/diagram01.png)

## Build on GDST and GS1 Standards
The Traceability Driver uses data models and communication protocols from GS1 to exchange traceability data.

# How does it work?
The Traceability Driver works by mapping data in an existing database into GDST events and master data, then it stores that data in a MongoDB and allows that data to be queried using the GDST Communication Protocol.

## Configuring the Mapping
The Traceability Driver targets individual events from the database and maps them into the Common Event Model which is a normalized into a common data model.

### Event Type
The `EventType` of a mapping tells the Traceability Driver what type of event is being built.

### Selectors
The first part is the selector which is the core select statement for selecting the data from the database.

```sql
SELECT * FROM dbo.FishingLogs WHERE updated >= @LastSynchronized
```

> By default, the Traceability Driver will detect the selector has the parameter `@LastSynchronized` and provide the highest timestamp from the last record that was synchronized.

### Mappings
Mappings tell the Traceability Driver where each field in the select results go into which field in the common JSON mapping model.

- **Field** - This is the field from the selector.
- **Target** - This is the JSON path to put the value from the field.
- **Dictionary** - Optionally, a dictionary can be provided that will map the field.
- **Transformer** - Transformers allow for transforming values into other values, such as one date time stamp format into another or even transforming a local date time into a UTC date time. You can find more about which transformers are supported below.

```
{

    "Mappings": [
        {
            "Field": "x",
            "Target": "Product/LotNumber",
        },
        {
            "Field" : "y",
            "Target": "Location/Name"
        }
    ]
}
```

### Common JSON Mapping Data Model
```
{
    "EventType": "Fishing",
    "Identifiers": [ "" ]
    "Product": {
        "LotNumber": "",
        "SerialNumber": "",
        "Species" : "",
        "ProductForm": "",
        "Identifiers": [ "", "", ""]
    },
    "Location" :{
        "Identifiers": [ "" ],
        "Name": "",
    },
    "Source": {
        "Location": {
            ""
        }
    }
}
```

> This simpler JSON model for events includes master data objects inside of it for a simpler mapping.

### Identifiers
Because database may not store individual fields that map into things like `Event ID`, `GTIN`, `GLN`, or `PGLN`, the model supports an array of values in a field called `Identifier`. The values populated there are combined and hashed together to create a deterministic unique identifier in the proper GS1 identifier format.

## Database Connectors
Currently the database supports mapping from the following database technologies:
- SQL Server

