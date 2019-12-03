using Microsoft.Web.Http;
using Namotion.Reflection;
using NSwag.Generation.Processors.Contexts;
using System.Collections;
using System.Linq;
using System.Reflection;
using NSwagProcessors = NSwag.Generation.Processors;

namespace Ibistic.Public.NSwag.Extensions
{
    /// <summary>
    /// Add the api version value specified in the ApiVersion attribute to the query string path of an action controller (controller/action -> controller/action?api-version=X.X)
    /// </summary>
    /// <remarks>
    /// In case of multiple versions in a controller or action, this processor requires them ordered from the newest to the oldest.
    /// </remarks>
    public class ApiVersionQueryStringProcessor : NSwagProcessors.IOperationProcessor
    {
        /// <summary>Processes an action controller trying to add its api version to the action query string path.</summary>
        /// <param name="context">The processor context.</param>
        /// <returns>true if the action should be added to the OpenApi specification.</returns>
        public bool Process(OperationProcessorContext context)
        {
            var nswagApiVersionProcessor = context.Settings.OperationProcessors.TryGet<NSwagProcessors.ApiVersionProcessor>();
            string[] filteredVersions = nswagApiVersionProcessor.IncludedVersions;

            string[] actionApiVersions = GetActionApiVersions(context, typeof(ApiVersionAttribute).Name);

            // If the action/controller has no the ApiVersion attribute
            if (actionApiVersions.Length == 0)
            {
                // Include action in the OpenApi specification if the document is not being created with filtered versions.
                return filteredVersions == null || filteredVersions.Length == 0;
            }

            // If no filter by versions, get the first (higher) one
            if (filteredVersions == null || filteredVersions.Length == 0)
            {
                IncludeApiVersionInQueryString(context, actionApiVersions[0]);
                return true;
            }

            foreach (string actionApiVersion in actionApiVersions)
            {
                // The first matching is considered the preferred api version usage
                if (nswagApiVersionProcessor.IncludedVersions.Contains(actionApiVersion))
                {
                    IncludeApiVersionInQueryString(context, actionApiVersion);
                    return true;
                }
            }

            return false;
        }

        private string[] GetActionApiVersions(OperationProcessorContext context, string attributeType)
        {
            var versionAttributes = context.MethodInfo.GetCustomAttributes()
                .Concat(context.MethodInfo.DeclaringType.GetTypeInfo().GetCustomAttributes())
                .Concat(context.ControllerType.GetTypeInfo().GetCustomAttributes(true))
                .GetAssignableToTypeName(attributeType, TypeNameStyle.Name)
                .Where(a => a.HasProperty("Versions"))
                .SelectMany((dynamic a) => ((IEnumerable)a.Versions).OfType<object>().Select(v => v.ToString()))
                .ToArray();

            return versionAttributes;
        }

        private void IncludeApiVersionInQueryString(OperationProcessorContext context, string version)
        {
            var operationDescription = context.OperationDescription;
            operationDescription.Path = $"{operationDescription.Path}?api-version={version}";
        }
    }
}
