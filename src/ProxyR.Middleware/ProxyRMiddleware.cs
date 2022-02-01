using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
using ProxyR.Abstractions.Utilities;
using ProxyR.Core.Extensions;

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

        private class ParameterBuilder
        {
            /// <summary>
            ///     The collection of parameters being built.
            /// </summary>
            public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            /// <summary>
            ///     Counter that increments with each unnamed parameter.
            /// </summary>
            public int ParameterCounter { get; private set; }

            /// <summary>
            ///     Adds a parameter with a value, and returns the generated parameter name.
            /// </summary>
            public string Add(object value)
            {
                var name = $"@{ParameterCounter}";
                Parameters.Add(name, value);
                ParameterCounter++;

                return name;
            }

        }

        public async Task Invoke(HttpContext context)
        {
            // Get the request body, 
            // if one has been passed.
            string requestBody = null;

            if (context.Request.Body != null)
            {
                var (stream, text) = await StreamUtility.ReadAsStringAsync(context.Request.Body);
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
                await _next(context);

                return;
            }

            // Resolve the function name.
            var (functionSchema, functionName) = FormatFunctionName(segments, _options.Value?.Prefix, _options.Value?.Suffix);

            // Does the function exist?
            var functionExists = await DbCommands
                                  .ObjectExists(connectionString, functionName, functionSchema, DbObjectType.TableValuedFunction, DbObjectType.InlineTableValuedFunction)
                                  .ToScalarAsync<bool?>();

            if (!(functionExists.GetValueOrDefault() && functionExists.HasValue))
            {
                await _next(context);

                return;
            }
            // Get it into an interrogatable JSON object (JObject).
            var queryParams = requestBody != null
                ? JsonConvert.DeserializeObject<ProxyRQueryParameters>(requestBody)
                : new ProxyRQueryParameters();

            // Override query-parameters from the query-String.
            GetODataQueryStringParameters(context.Request.Query, queryParams);
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

            // Get all the parameter names currently on the function.
            var functionParamNames = await DbCommands
                                          .GetParameterNames(connectionString, functionName, functionSchema)
                                          .ToScalarArrayAsync<string>();

            // var matchedParams = requestParams
            var functionArguments = from functionParamName in functionParamNames
                                    let paramName = functionParamName.TrimStart('@')
                                    let paramExists = paramValues.ContainsKey(paramName)
                                    let paramArgument = paramExists
                                        ? paramBuilder.Add(paramValues[paramName])
                                        : "DEFAULT"
                                    select paramArgument;

            // Check for required parameters.
            if (_options.Value?.RequiredParameterNames != null && _options.Value.RequiredParameterNames.Any())
            {
                foreach (var requiredParameterName in _options.Value.RequiredParameterNames)
                {
                    if (functionParamNames.Contains(requiredParameterName, StringComparer.InvariantCultureIgnoreCase) && paramValues.ContainsKey(requiredParameterName))
                    {
                        continue;
                    }

                    _logger.LogWarning($"DbFunction [{functionSchema}].[{functionName}] did not have required parameter {requiredParameterName} provided.");

                    context.Response.StatusCode = 404;
                    await _next(context);

                    return;
                }
            }

            // Generate the SELECT statements from the parameters given.
            var sqlBuilder = new SqlBuilder();
            BuildSqlUnit(sqlBuilder, paramBuilder, queryParams, functionSchema, functionName, functionArguments.ToArray());

            // Get the SQL generated.
            var sql = sqlBuilder.ToString();
            _logger.LogInformation($"SQL Parameters:\n{JsonConvert.SerializeObject(paramBuilder.Parameters.Values.Select(x => new { Type = x.GetType().Name, Value = x }).ToArray())}");
            _logger.LogInformation($"SQL Generated:\n{sql}");

            // Run the SQL.
            var results = await Db.Query(connectionString: connectionString, sql: sql, parameters: paramBuilder.Parameters.Values.ToArray()).ToJDataSetAsync();

            if (results.Property("results") == null)
            {
                results.Add("results", new JArray());
            }

            var json = results.ToString(Formatting.None);

            // Output the SQL to the response.
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }

        private static void GetODataQueryStringParameters(IQueryCollection queryString, ProxyRQueryParameters queryParams)
        {
            var queryStringExpand = queryString["$expand"].FirstOrDefault();

            if (queryStringExpand.HasContent())
            {
                throw new NotSupportedException("Parameter $expand is not supported");
            }

            var queryStringFormat = queryString["$format"].FirstOrDefault();

            if (queryStringFormat.HasContent())
            {
                throw new NotSupportedException("Parameter $format is not supported");
            }

            var queryStringTop = queryString["$top"].FirstOrDefault()?.Trim();

            if (queryStringTop.HasContent())
            {
                queryParams.Take = int.Parse(queryStringTop);
            }

            var queryStringSkip = queryString["$skip"].FirstOrDefault()?.Trim();

            if (queryStringSkip.HasContent())
            {
                queryParams.Skip = int.Parse(queryStringSkip);
            }

            var queryStringFilter = queryString["$filter"].FirstOrDefault()?.Trim();

            if (queryStringFilter.HasContent())
            {
                var expression = GetODataFilterExpression(queryStringFilter);
                queryParams.Filter = expression;
            }

            var queryStringOrderBy = queryString["$orderby"].FirstOrDefault()?.Trim();

            if (queryStringOrderBy.HasContent())
            {
                var orderByColumns = queryStringOrderBy.Split(',').Select(x => x.Trim());

                foreach (var orderByColumn in orderByColumns)
                {
                    var parts = orderByColumn.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(x => x.Trim())
                                             .ToArray();

                    var isDescending = false;

                    if (parts.Length > 1)
                    {
                        isDescending = parts[1].Equals("desc", StringComparison.InvariantCultureIgnoreCase);
                    }

                    queryParams.Sort.Add(new ProxyRSortParameters { ColumnName = parts[0], IsDescending = isDescending });
                }
            }

            var queryStringSelect = queryString["$select"].FirstOrDefault()?.Trim();

            if (queryStringSelect.HasContent())
            {
                queryParams.SelectFields = queryStringSelect == "*"
                    ? null
                    : queryStringSelect.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(x => x.Trim())
                                       .ToArray();
            }

            var queryStringInlineCount = queryString["$inlinecount"].FirstOrDefault()?.Trim()?.ToLowerInvariant();

            if (queryStringInlineCount.IsNullOrWhiteSpace())
            {
                return;
            }

            switch (queryStringInlineCount)
            {
                case "allpages":
                    queryParams.RequireTotalCount = true;
                    break;
                case "none":
                    queryParams.RequireTotalCount = false;
                    break;
                default:
                    throw new NotSupportedException($"Value for $inlinecount={queryStringInlineCount} is not supported");
            }
        }

        private static JArray GetODataFilterExpression(string filterText)
        {
            var rootExpression = new JArray();
            var expressionStack = new Stack<JArray>();
            expressionStack.Push(rootExpression);

            while (filterText.HasContent())
            {
                var expression = expressionStack.Peek();
                filterText = filterText.TrimStart();

                if (filterText.StartsWith("("))
                {
                    var subExpression = new JArray();
                    expression.Add(subExpression);
                    expressionStack.Push(subExpression);
                    filterText = filterText.Remove(0, 1);
                    continue;
                }

                if (filterText.StartsWith(")"))
                {
                    filterText = filterText.Remove(0, 1);
                    expressionStack.Pop();
                    continue;
                }

                var identifierRegexValue = Regex.Match(filterText, "^([a-z0-9_]+)", RegexOptions.IgnoreCase);

                if (identifierRegexValue.Success && expression.Count == 0)
                {
                    filterText = filterText.Remove(0, identifierRegexValue.Groups[1].Length);
                    expression.Add(identifierRegexValue.Groups[1].Value);

                    continue;
                }

                var conditionalOperatorRegexValue = Regex.Match(filterText, "^(eq|ne|gt|ge|lt|le|contains|notcontains|startswith|endswith)[^a-z0-9]", RegexOptions.IgnoreCase);

                if (conditionalOperatorRegexValue.Success && expression.Count == 1)
                {
                    filterText = filterText.Remove(0, conditionalOperatorRegexValue.Groups[1].Length);

                    var expressionOperator = "=";

                    switch (conditionalOperatorRegexValue.Groups[1].Value.ToLowerInvariant())
                    {
                        case "eq":
                            expressionOperator = "=";
                            break;
                        case "ne":
                            expressionOperator = "<>";
                            break;
                        case "gt":
                            expressionOperator = ">";
                            break;
                        case "ge":
                            expressionOperator = ">=";
                            break;
                        case "lt":
                            expressionOperator = "<";
                            break;
                        case "le":
                            expressionOperator = "<=";
                            break;
                        case "contains":
                            expressionOperator = "contains";
                            break;
                        case "notcontains":
                            expressionOperator = "notcontains";
                            break;
                        case "startswith":
                            expressionOperator = "startswith";
                            break;
                        case "endswith":
                            expressionOperator = "endswith";
                            break;
                    }

                    expression.Add(expressionOperator);
                    continue;
                }

                var logicalOperatorRegexValue = Regex.Match(filterText, "^(and|or)[^a-z0-9]", RegexOptions.IgnoreCase);

                if (logicalOperatorRegexValue.Success && expression.Count % 2 != 0 && expression.Count >= 1)
                {
                    filterText = filterText.Remove(0, logicalOperatorRegexValue.Groups[1].Length);
                    expression.Add(logicalOperatorRegexValue.Groups[1].Value.ToLowerInvariant());
                    continue;
                }

                var stringLiteralRegexValue = Regex.Match(filterText, "^'((\\'|.)*?)'");

                if (stringLiteralRegexValue.Success && expression.Count == 2)
                {
                    filterText = filterText.Remove(0, stringLiteralRegexValue.Groups[1].Length + 2);
                    expression.Add(stringLiteralRegexValue.Groups[1].Value);
                    continue;
                }

                var decimalLiteralRegexValue = Regex.Match(filterText, "^(\\d*\\.\\d+|\\d+)");

                if (decimalLiteralRegexValue.Success && expression.Count == 2)
                {
                    filterText = filterText.Remove(0, decimalLiteralRegexValue.Groups[1].Length);
                    expression.Add(decimalLiteralRegexValue.Groups[1].Value);
                    continue;
                }

                var unknownText = filterText.Substring(0, Math.Min(10, filterText.Length));
                throw new InvalidOperationException($"Filter expression contained \"{unknownText}\" which was not expected.");
            }

            if (expressionStack.Count > 1)
            {
                throw new InvalidOperationException("Filter expression was not terminated by a ')' bracket.");
            }

            return rootExpression;
        }

        private void BuildSqlUnit(
            SqlBuilder statement,
            ParameterBuilder paramBuilder,
            ProxyRQueryParameters requestParams,
            string functionSchema,
            string functionName,
            string[] functionArguments)
        {
            statement.Comment("Queries and outputs the results.", "Optionally including, paging, sorting, filtering and grouping.");
            BuildSelectStatement(statement, paramBuilder, requestParams, functionSchema, functionName, functionArguments);
            if (requestParams.RequireTotalCount)
            {
                statement.Comment("Calculates the total row count.", "Optionally including filtering, but no paging or sorting.");
                BuildSelectStatement(statement, paramBuilder, requestParams, functionSchema, functionName, functionArguments, forCount: true);
            }
        }

        private (string schema, string name) FormatFunctionName(IReadOnlyList<string> segments, string prefix = null, string suffix = null)
        {
            // Get safe versions of the segments used 
            // for the schema and function name.
            var defaultSchema = _options.Value.DefaultSchema ?? "dbo";
            var delimiterChar = _options.Value.Seperator;
            var delimiterString = delimiterChar?.ToString() ?? String.Empty;
            var functionSchema = _options.Value.IncludeSchemaInPath ? Sql.Sanitize(segments[0]) : defaultSchema;
            var functionSegment = Sql.Sanitize(String.Join(delimiterString, segments.Skip(_options.Value.IncludeSchemaInPath ? 1 : 0))).Trim('_');
            var functionPrefix = Sql.Sanitize(prefix ?? "Query_");
            var functionSuffix = Sql.Sanitize(suffix ?? "_GRID");
            var formattedFunctionName = $"{functionPrefix}{functionSegment}{functionSuffix}";

            return (functionSchema, formattedFunctionName);
        }

        private void BuildSelectStatement(
            SqlBuilder statement,
            ParameterBuilder paramBuilder,
            ProxyRQueryParameters requestParams,
            string functionSchema,
            string functionName,
            string[] functionArguments,
            bool forCount = false)
        {
            // Write the SELECT clause, with the output columns.
            BuildSelectClause(statement, requestParams, forCount);

            // Write the FROM clause.
            statement.StartNewLine("FROM");

            statement.Indent(fn =>
            {
                fn.StartNewLine($"[{functionSchema}].[{functionName}](");

                if (functionArguments.Any())
                {
                    fn.Indent(p => p.StartNewLine(Sql.CommaLines(functionArguments)));
                }

                fn.Literal(")");

                if (!forCount)
                {
                    fn.Literal(" RESULTS");
                }
            });

            // Should we write a WHERE clause?
            if (requestParams.Filter != null && requestParams.Filter.Any())
            {
                // Do we need to use WHERE or WHERE NOT?
                if (requestParams.Filter[0].ToString() == "!")
                {
                    statement.StartNewLine("WHERE NOT");

                    statement.Indent(clause =>
                    {
                        clause.StartNewLine();
                        BuildWhereExpression(clause, (JArray)requestParams.Filter[1], paramBuilder, includeBrackets: false);
                    });

                }
                else
                {
                    statement.StartNewLine("WHERE");
                    statement.Indent(clause =>
                    {
                        clause.StartNewLine();
                        BuildWhereExpression(clause, requestParams.Filter, paramBuilder, includeBrackets: false);
                    });
                }
            }

            // Should we do a GROUP BY clause?
            if (requestParams.Grouping != null && requestParams.Grouping.Any())
            {
                var groupColumns = requestParams.Grouping
                                                .Select(x => Sql.Sanitize(x.ColumnName))
                                                .ToArray();

                statement.StartNewLine("GROUP BY");

                statement.Indent(clause => clause.StartNewLine(Sql.ColumnLines(groupColumns)));
            }

            // Should we write an ORDER BY clause? 
            // Has a sort been given? or has paging been requested?
            // Also not done for counts.
            var shouldSort = requestParams.Sort != null && requestParams.Sort.Any() || requestParams.Skip != null || requestParams.Take != null;

            if (shouldSort && !forCount)
            {
                // Do we not have something to sort?
                if (requestParams.Sort == null || !requestParams.Sort.Any())
                {
                    // Get us to sort the value [1] instead of a column.
                    requestParams.Sort = new List<ProxyRSortParameters> { new ProxyRSortParameters { Literal = "1", IsDescending = false } };
                }

                // Create an array of column sort expressions.
                var orderParts = requestParams.Sort.Select(x => x.ToString()).ToArray();

                // Write the whole ORDER BY clause.
                statement.StartNewLine("ORDER BY");
                statement.Indent(cols => cols.StartNewLine(Sql.CommaLines(orderParts)));

                // Should we do some paging?
                // Using the OFFSET clause.
                if (!requestParams.IsLoadingAll && (requestParams.Skip != null || requestParams.Take != null))
                {
                    statement.Indent(offset =>
                    {
                        offset.StartNewLine($"OFFSET {requestParams.Skip ?? 0} ROWS");
                        offset.StartNewLine($"FETCH NEXT {requestParams.Take ?? 10000000} ROWS ONLY");
                    });
                }
            }

            // End the whole statement with a semi-colon.
            statement.Line(";");
            statement.Line();
        }

        private static void BuildSelectClause(SqlBuilder statement, ProxyRQueryParameters parameters, bool forCount)
        {
            // Write the SELECT clause and columns.
            if (forCount)
            {
                statement.StartOfLine("SELECT");
                statement.Indent(cols => cols.StartNewLine(Sql.CommaLines("[$Type] = '$Root'", "[Count] = COUNT(*)")));
                return;
            }

            // Start the list of select-expressions.
            var selectExpressions = new List<string> { "[$Type] = 'Result'" };

            // Should we get all fields, or a certain one?
            if (parameters.DataField != null)
            {
                // Just one field has been requested?
                var dataField = $"RESULTS.[{Sql.Sanitize(parameters.DataField)}]";
                selectExpressions.Add(dataField);
            }
            else if (parameters.SelectFields != null)
            {
                var dataFields = parameters.SelectFields.Select(selectField => $"RESULTS.[{Sql.Sanitize(selectField)}]");
                selectExpressions.AddRange(dataFields);
            }
            else
            {
                // No grouping... output everything?
                selectExpressions.Add("RESULTS.*");
            }

            // Write the select clause with all its expressions.
            statement.StartOfLine("SELECT");
            statement.Indent(cols => cols.StartNewLine(Sql.CommaLines(selectExpressions.ToArray())));
        }

        private void BuildWhereExpression(SqlBuilder targetExpression, JArray sourceExpression, ParameterBuilder paramBuilder, bool includeBrackets = true)
        {
            if(sourceExpression.Count < 2)
            {
                return; // Bail out.
            }

            var left = sourceExpression[0];
            var firstOperation = sourceExpression[1]?.ToString()?.ToLower();

            // Work out if we need to surround this expression in brackets.
            var needsBrackets = includeBrackets && (firstOperation == "and" || firstOperation == "or");

            // Do we need to a starting bracket?
            if (needsBrackets)
            {
                targetExpression.Literal("(");
            }

            // Have we got another expression?
            // Or is this the left column-name of the expression?
            if (left is JArray leftSourceExpression)
            {
                targetExpression.Indent(leftTargetExpression => BuildWhereExpression(leftTargetExpression, leftSourceExpression, paramBuilder));
            }
            else if (left is JValue leftValue)
            {
                var leftIdentifier = Sql.Sanitize(leftValue.ToString(CultureInfo.InvariantCulture));
                targetExpression.Literal($"[{leftIdentifier}]");
            }

            // Add a space after the left part.
            targetExpression.Literal(" ");

            // Loop through each operation and it's right-side in the expression.
            for (var sourcePartIndex = 1; sourcePartIndex < sourceExpression.Count; sourcePartIndex += 2)
            {
                // Get the operation for this segment of the expression.
                var operation = sourceExpression[sourcePartIndex]?.ToString()?.ToLower();

                // Get the right side.
                var right = sourceExpression[sourcePartIndex + 1];
                var rightValue = right as JValue;

                // Do we have a right-value? Get a string-form of it.
                // We do this so the operator has a chance 
                // to change the right-value.
                string rightString = null;

                if (rightValue != null)
                {
                    rightString = rightValue.ToString(CultureInfo.InvariantCulture);
                }

                // What is the operation being applied 
                // between the left and right sides?
                switch (operation)
                {
                    case "and":
                        targetExpression.StartNewLine("AND");
                        break;
                    case "or":
                        targetExpression.StartNewLine("OR");
                        break;
                    case "contains":
                        targetExpression.Literal("LIKE");
                        rightString = $"%{rightString}%";
                        break;
                    case "notcontains":
                        targetExpression.Literal("NOT LIKE");
                        rightString = $"%{rightString}%";
                        break;
                    case "startswith":
                        targetExpression.Literal("LIKE");
                        rightString = $"{rightString}%";
                        break;
                    case "endswith":
                        targetExpression.Literal("LIKE");
                        rightString = $"%{rightString}";
                        break;
                    case "=":
                        if (rightValue != null && rightValue.Type == JTokenType.Null)
                        {
                            targetExpression.Literal("IS");
                        }
                        else
                        {
                            targetExpression.Literal("=");
                        }
                        break;
                    case "<>":
                        targetExpression.Literal("<>");
                        break;
                    case "<=":
                        targetExpression.Literal("<=");
                        break;
                    case ">=":
                        targetExpression.Literal(">=");
                        break;
                    case "<":
                        targetExpression.Literal("<");
                        break;
                    case ">":
                        targetExpression.Literal(">");
                        break;
                    default:
                        throw new NotSupportedException($"Query filter operator [{operation}] is not supported.");
                }

                // Add a space after the operator.
                targetExpression.Literal(" ");

                // Have we got another expression?
                // Or is this the right value of the expression?
                if (right is JArray rightSourceExpression)
                {
                    targetExpression.Indent(rightTargetExpression =>
                    {
                        BuildWhereExpression(rightTargetExpression, rightSourceExpression, paramBuilder);
                    });
                }
                else
                {
                    object rightCastValue;

                    switch (rightValue.Type)
                    {
                        case JTokenType.Null:
                            rightCastValue = null;
                            break;
                        case JTokenType.Date:
                            rightCastValue = (DateTime)rightValue;
                            break;
                        case JTokenType.Float:
                            rightCastValue = (double)rightValue;
                            break;
                        case JTokenType.Integer:
                            rightCastValue = (long)rightValue;
                            break;
                        case JTokenType.TimeSpan:
                            rightCastValue = (TimeSpan)rightValue;
                            break;
                        default:
                            rightCastValue = rightString;
                            break;
                    }

                    // Quote and append.
                    if (rightCastValue == null)
                    {
                        targetExpression.Literal("NULL");
                    }
                    else
                    {
                        var paramName = paramBuilder.Add(rightCastValue);
                        targetExpression.Literal($"{paramName}");
                    }
                }
            }

            // Do we need finish surrounding it in brackets?
            if (needsBrackets)
            {
                targetExpression.Literal(")");
            }
        }

    }
}
