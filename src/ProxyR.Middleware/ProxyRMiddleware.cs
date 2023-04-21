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
  public class ProxyRMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly ILogger<ProxyRMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private readonly IOptions<ProxyROptions> _options;
    private readonly IOptions<ProxyRRuntimeOptions> _runtimeOptions;

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
      _logger.LogInformation($"Query-Params: {JsonConvert.SerializeObject(queryParams)}");

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

        context.Response.StatusCode = 404;
        await _next(context).ConfigureAwait(false);

        return;
      }

      // Generate the SELECT statements from the parameters given.
      var sqlBuilder = new SqlBuilder();
      StatementBuilder.BuildSqlUnit(sqlBuilder, paramBuilder, queryParams, functionSchema, functionName, functionArguments.ToArray(), isView);

      // Get the SQL generated.
      var sql = sqlBuilder.ToString();
      _logger.LogInformation($"SQL Parameters:\n{JsonConvert.SerializeObject(paramBuilder.Parameters.Values.Select(x => new { Type = x.GetType().Name, Value = x }).ToArray())}");
      _logger.LogInformation($"SQL Generated:\n{sql}");

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
