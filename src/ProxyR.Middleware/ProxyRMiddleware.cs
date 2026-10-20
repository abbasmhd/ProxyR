using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProxyR.Abstractions;
using ProxyR.Abstractions.Builder;
using ProxyR.Abstractions.Commands;
using ProxyR.Abstractions.Execution;
using ProxyR.Abstractions.Extensions;
using ProxyR.Abstractions.Parameters;
using ProxyR.Abstractions.Utilities;
using ProxyR.Core.Extensions;
using ProxyR.Middleware.Helpers;

namespace ProxyR.Middleware
{
    /// <summary>
    /// This class provides a middleware for proxy requests.
    /// </summary>
    public class ProxyRMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ProxyRMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly IOptions<ProxyROptions> _options;
        private readonly IOptions<ProxyRRuntimeOptions> _runtimeOptions;

        /// <summary>
        /// Constructor for ProxyRMiddleware class.
        /// </summary>
        /// <param name="next">The next RequestDelegate in the pipeline.</param>
        /// <param name="logger">The ILogger instance.</param>
        /// <param name="configuration">The IConfiguration instance.</param>
        /// <param name="options">The IOptions instance.</param>
        /// <param name="runtimeOptions">The IOptions instance.</param>
        /// <returns>
        /// A new instance of the ProxyRMiddleware class.
        /// </returns>
        public ProxyRMiddleware(
                   RequestDelegate next,
                   ILogger<ProxyRMiddleware> logger,
                   IConfiguration configuration,
                   IOptions<ProxyROptions> options,
                   IOptions<ProxyRRuntimeOptions> runtimeOptions)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
            _options = options;
            _runtimeOptions = runtimeOptions;
        }

        /// <summary>
        /// Invokes the function specified in the request path and returns the results as a JSON object.
        /// </summary>
        /// <returns>
        /// A JSON object containing the results of the function.
        /// </returns>
        public async Task Invoke(HttpContext context)
        {
            // Get the request body,
            // if one has been passed.
            string requestBody = null;

            if (context.Request.Body != null)
            {
                var (stream, text) = await StreamUtility.ReadAsStringAsync(context.Request.Body).ConfigureAwait(false);
                context.Request.Body = stream;
                requestBody = text ?? "{}";
            }

            // Get the connection-string from the options, or the connection-string
            // from the configuration based on the connection-string name given in the options.
            var connectionString = _options.Value?.ConnectionString;

            if (connectionString.IsNullOrWhiteSpace() && !String.IsNullOrWhiteSpace(_options.Value?.ConnectionStringName))
            {
                if (_configuration == null)
                {
                    throw new ArgumentNullException(nameof(_configuration), $"Service {nameof(IConfiguration)} is required to resolve connection-string names.");
                }

                connectionString = _configuration.GetConnectionString(_options.Value.ConnectionStringName);
            }

            // Decode the path into segments.
            var path = context.Request.Path.ToString();
            var segments = path.TrimStart('/').Split('/');

            // Does this pass the minimum segments?
            if (_options.Value?.IncludeSchemaInPath == true && segments.Length < 2 || _options.Value?.IncludeSchemaInPath == false && segments.Length < 1)
            {
                await _next(context).ConfigureAwait(false);

                return;
            }

            // Resolve the function name.
            var (functionSchema, functionName) = segments.FormatProcsName(_options.Value);

            // Does the object exist?
            var objectType = await DbCommands
                                  .GetObjectType(connectionString, functionName, functionSchema,
                                                 DbObjectType.TableValuedFunction,
                                                 DbObjectType.InlineTableValuedFunction,
                                                 DbObjectType.View)
                                  .ToScalarAsync<string>()
                                  .ConfigureAwait(false);

            if (String.IsNullOrWhiteSpace(objectType))
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            // Get it into an interrogatable JSON object (JObject).
            var queryParams = requestBody != null
              ? JsonConvert.DeserializeObject<ProxyRQueryParameters>(requestBody)
              : new ProxyRQueryParameters();

            // Override query-parameters from the query-String.
            StatementBuilder.GetODataQueryStringParameters(context.Request.Query, queryParams);
            // Log parameter names only (never values): query parameters and
            // overridden parameters can contain sensitive data (#60).
            _logger.LogDebug($"Query-Params: {string.Join(", ", queryParams.UserData.Keys)}; Filter: {queryParams.Filter != null}; Sort: {queryParams.Sort.Count}; Take: {queryParams.Take}; Skip: {queryParams.Skip}");

            // Get the parameters from the User-Data.
            var paramBuilder = new ParameterBuilder();
            var paramValues = new Dictionary<string, object>(queryParams.UserData, StringComparer.InvariantCultureIgnoreCase);

            // Add the parameters from the query-string (that don't start with '$').
            foreach (var queryStringPair in context.Request.Query)
            {
                if (!queryStringPair.Key.StartsWith("$"))
                {
                    paramValues[queryStringPair.Key] = queryStringPair.Value.FirstOrDefault();
                }
            }

            // Add the configured parameter modifiers (Overridden Parameters)
            _logger.LogInformation($"Override-Params: {_runtimeOptions.Value.ParameterModifiers.Count} configured");

            foreach (var parameterModifier in _runtimeOptions.Value.ParameterModifiers)
            {
                parameterModifier(context, paramValues);
            }

            IEnumerable<string> functionParamNames;
            IEnumerable<string> functionArguments;

            var isView = objectType.ToDbObjectType() == DbObjectType.View;
            if (isView)
            {
                functionArguments = new List<string>();
                functionParamNames = new List<string>();
            }
            else
            {
                // Get all the parameter names currently on the function.
                functionParamNames = await DbCommands
                  .GetParameterNames(connectionString, functionName, functionSchema)
                  .ToScalarArrayAsync<string>()
                  .ConfigureAwait(false);

                // var matchedParams = requestParams
                functionArguments = from functionParamName in functionParamNames
                                    let paramName = functionParamName.TrimStart('@')
                                    let paramExists = paramValues.ContainsKey(paramName)
                                    let paramArgument = paramExists
                                        ? paramBuilder.Add(paramValues[paramName])
                                        : "DEFAULT"
                                    select paramArgument;
            }

            // Check for required parameters.
            foreach (var requiredParameterName in _options.Value.RequiredParameterNames)
            {
                if (functionParamNames.Contains(requiredParameterName, StringComparer.InvariantCultureIgnoreCase)
                    && paramValues.ContainsKey(requiredParameterName))
                {
                    continue;
                }

                _logger.LogWarning($"DbFunction [{functionSchema}].[{functionName}] did not have required parameter {requiredParameterName} provided.");

                // ProxyR has decided the response (404): do not continue
                // the pipeline — later middleware could overwrite it (#70).
                context.Response.StatusCode = 404;

                return;
            }

            // Generate the SELECT statements from the parameters given.
            var sqlBuilder = new SqlBuilder();
            StatementBuilder.BuildSqlUnit(sqlBuilder, paramBuilder, queryParams, functionSchema, functionName, functionArguments.ToArray(), isView);

            // Get the SQL generated.
            var sql = sqlBuilder.ToString();
            // Log parameter names and types only (never values): parameters can
            // contain sensitive data, e.g. server-side overridden values (#60).
            _logger.LogDebug($"SQL Parameters: {string.Join(", ", paramBuilder.Parameters.Keys)}");
            _logger.LogDebug($"SQL Generated:\n{sql}");

            // Run the SQL.
            var results = await Db.Query(connectionString: connectionString, sql: sql, parameters: paramBuilder.Parameters.Values.ToArray())
                                  .ToJDataSetAsync()
                                  .ConfigureAwait(false);

            if (results.Property("results") == null)
            {
                results.Add("results", new JArray());
            }

            var json = results.ToString(Formatting.None);

            // Output the SQL to the response.
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json).ConfigureAwait(false);
        }

    }
}
