using OpenTraceability.Utility;
using System.Collections;
using System.Data;
using System.Reflection;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Services.Mapping;

namespace TraceabilityDriver.Services;

/// <summary>
/// A service that can map events from a data table to the common event model
/// using the event mapping object.
/// </summary>
public class EventsTableMappingService : IEventsTableMappingService
{
    private readonly ILogger<EventsTableMappingService> _logger;
    private readonly IMappingFunctionFactory _mappingFunctionFactory;

    public EventsTableMappingService(ILogger<EventsTableMappingService> logger, IMappingFunctionFactory mappingFunctionFactory)
    {
        _logger = logger;
        _mappingFunctionFactory = mappingFunctionFactory;
    }

    /// <summary>
    /// Map a list of events from a data table to the common event model.
    /// </summary>
    /// <param name="eventMapping">The event mapping object.</param>
    /// <param name="dataTable">The data table to map from.</param>
    /// <returns>A list of mapped events.</returns>
    public List<CommonEvent> MapEvents(TDEventMapping eventMapping, DataTable dataTable, CancellationToken cancellationToken)
    {
        var events = new List<CommonEvent>();

        foreach (DataRow row in dataTable.Rows)
        {
            // Check for cancellation.
            if (cancellationToken.IsCancellationRequested) return events;

            var commonEvent = MapDataRowToCommonEvent(eventMapping, row);
            events.Add(commonEvent);
        }

        return events;
    }

    /// <summary>
    /// Map an event from a data row to the common event model.
    /// </summary>
    /// <param name="eventMapping">The event mapping object.</param>
    /// <param name="row">The data row to map.</param>
    /// <returns>The mapped event.</returns>
    public CommonEvent MapDataRowToCommonEvent(TDEventMapping eventMapping, DataRow row)
    {
        var commonEvent = new CommonEvent();

        foreach (var field in eventMapping.Fields)
        {
            MapEventFieldToCommonEvent(field, row, commonEvent);
        }

        return commonEvent;
    }

    /// <summary>
    /// Iterates over the fields of the TDEventMapping and then maps those into fields of the CommonEvent.
    /// </summary>
    /// <param name="eventMapping">The event mapping object.</param>
    /// <param name="row">The data row to map.</param>
    /// <param name="commonEvent">The common event model to map to.</param>
    /// <param name="path">The path to the field in the data row.</param>
    public void MapEventFieldToCommonEvent(TDEventMappingField eventMapping, DataRow row, CommonEvent commonEvent)
    {
        try
        {
            var targetObject = GetTargetObject(commonEvent, eventMapping.Path);

            var mappingValue = eventMapping.GetMappingValue(row);

            switch (mappingValue.Type)
            {
                case TDEventMappingValueType.Static:
                case TDEventMappingValueType.Variable:

                    // Check that there is a value to set.
                    if (mappingValue.Values.Count == 0)
                    {
                        throw new Exception($"The mapping value for {eventMapping.Path} is a variable but no values were found.");
                    }

                    // Try and convert the value.
                    object? value = TryToConvertValue(mappingValue.Values[0], eventMapping.PropertyInfo.PropertyType);

                    // Try and set the value.
                    TryToSetValue(targetObject, eventMapping.PropertyInfo, value);

                    break;
                case TDEventMappingValueType.Function:

                    // Get the return value of the function.
                    var functionReturnValue = GetFunctionReturnValue(mappingValue.FunctionName, mappingValue.Values);

                    // Try and set the value.
                    TryToSetValue(targetObject, eventMapping.PropertyInfo, functionReturnValue);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping field {Path} on {CommonEvent}", eventMapping.Path, commonEvent.GetType().Name);
        }
    }

    /// <summary>
    /// Gets the target object from the common event.
    /// </summary>
    /// <param name="commonEvent">The common event object.</param>
    /// <param name="path">The path to the target object.</param>
    /// <returns>The target object.</returns>
    public object GetTargetObject(CommonEvent commonEvent, string path)
    {
        object targetObject = commonEvent;

        if (!string.IsNullOrEmpty(path))
        {
            var pathParts = path.Split('.');
            pathParts = pathParts.Take(pathParts.Length - 1).ToArray();

            foreach (var pathPart in pathParts)
            {
                // Check if the path part contains an array indexer
                if (pathPart.Contains('[') && pathPart.Contains(']'))
                {
                    var propertyName = pathPart.Substring(0, pathPart.IndexOf('['));
                    var indexStr = pathPart.Substring(pathPart.IndexOf('[') + 1, pathPart.IndexOf(']') - pathPart.IndexOf('[') - 1);

                    if (!int.TryParse(indexStr, out int index))
                    {
                        throw new Exception($"Invalid array index in path: {pathPart}");
                    }

                    // Get the collection property
                    var collectionProperty = targetObject.GetType().GetProperty(propertyName);
                    if (collectionProperty == null)
                    {
                        throw new Exception($"Property not found: {propertyName} on {targetObject.GetType().Name}");
                    }

                    // Get the collection instance
                    var collection = collectionProperty.GetValue(targetObject);

                    // If the collection is null, create a new instance
                    if (collection == null)
                    {
                        var collectionType = collectionProperty.PropertyType;
                        collection = Activator.CreateInstance(collectionType);
                        collectionProperty.SetValue(targetObject, collection);
                    }

                    // Handle different collection types
                    if (collection is IList list)
                    {
                        // Ensure the list has enough items
                        var elementType = collectionProperty.PropertyType.GetGenericArguments()[0];

                        while (list.Count <= index)
                        {
                            var newItem = Activator.CreateInstance(elementType);
                            list.Add(newItem);
                        }

                        targetObject = list[index]!;
                    }
                    else
                    {
                        throw new Exception($"Unsupported collection type: {collection?.GetType().Name}");
                    }
                }
                else
                {
                    // Handle regular property access
                    var property = targetObject.GetType().GetProperty(pathPart);
                    if (property == null)
                    {
                        throw new Exception($"Property not found: {pathPart} on {targetObject.GetType().Name}");
                    }

                    var value = property.GetValue(targetObject);
                    if (value == null)
                    {
                        // Create a new instance of the property type
                        value = Activator.CreateInstance(property.PropertyType);
                        property.SetValue(targetObject, value);
                    }

                    targetObject = value!;
                }
            }
        }

        return targetObject;
    }

    /// <summary>
    /// Tries to convert a value to a target type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The target type to convert to.</param>
    /// <returns>The converted value.</returns>
    public object? TryToConvertValue(string? value, Type targetType)
    {
        try
        {
            if (value == null)
            {
                return null;
            }

            // unwrap target type if it is nullable
            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (targetType == typeof(string))
            {
                return value;
            }

            if (targetType == typeof(int))
            {
                return int.Parse(value);
            }

            if (targetType == typeof(double))
            {
                return double.Parse(value);
            }

            if (targetType == typeof(DateTimeOffset))
            {
                return DateTimeOffset.Parse(value);
            }

            if (targetType == typeof(bool))
            {
                return bool.Parse(value);
            }

            if (targetType == typeof(Country))
            {
                return Countries.Parse(value);
            }

            if(targetType.IsEnum)
            {
                if(Enum.TryParse(targetType, value, true, out var result))
                {
                    return result;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting value {Value} to type {TargetType}", value, targetType.Name);
            return null;
        }
    }

    /// <summary>
    /// Tries to set a value to a property.
    /// </summary>
    /// <param name="targetObject">The target object.</param>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="value">The value to set.</param>
    public void TryToSetValue(object targetObject, PropertyInfo propertyInfo, object? value)
    {
        try
        {
            propertyInfo.SetValue(targetObject, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value {Value} to {TargetObject}", value, targetObject.GetType().Name);
        }
    }

    /// <summary>
    /// Gets the return value of a function.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="parameters">The parameters of the function.</param>
    /// <returns>The return value of the function.</returns>
    public string? GetFunctionReturnValue(string functionName, List<string?> parameters)
    {
        var mappingFunction = _mappingFunctionFactory.Create(functionName);
        return mappingFunction.Execute(parameters);
    }
}