
using Nito.Disposables.Internals;

namespace TraceabilityDriver.Services.Mapping.Functions
{
    /// <summary>
    /// JoinFunction implements IMappingFunction. It has methods to execute a join operation on parameters and to
    /// initialize the function.
    /// </summary>
    public class JoinFunction : IMappingFunction
    {
        public string? Execute(List<string?> parameters)
        {
            if (parameters.Count > 1)
            {
                return string.Join(parameters[0], parameters.Skip(1).WhereNotNull());
            }

            return null;
        }

        public void Initialize()
        {
            // do nothing
        }
    }
}
