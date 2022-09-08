using ProxyR.Abstractions.Builder;
using ProxyR.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using ProxyR.Core.Extensions;
using System.Text.RegularExpressions;
using ProxyR.Abstractions.Parameters;

namespace ProxyR.Middleware.Helpers
{
  internal static class StatementBuilder
  {
    /// <summary>
    /// Format the Function or View name
    /// </summary>
    /// <param name="segments"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static (string schema, string name) FormatProcsName(this IReadOnlyList<string> segments, ProxyROptions options)
    {
      // Get safe versions of the segments used 
      // for the schema and function name.
      var defaultSchema   = options.DefaultSchema ?? "dbo";
      var delimiterChar   = options.Seperator;
      var delimiterString = delimiterChar?.ToString() ?? String.Empty;
      var schema          = options.IncludeSchemaInPath ? Sql.Sanitize(segments[0]) : defaultSchema;
      var segment         = Sql.Sanitize(String.Join(delimiterString, segments.Skip(options.IncludeSchemaInPath ? 1 : 0))).Trim('_');
      var prefix          = Sql.Sanitize(options.Prefix ?? "Query_");
      var suffix          = Sql.Sanitize(options.Suffix ?? "_GRID");
      var name            = $"{prefix}{segment}{suffix}";

      return (schema, name);
    }

    public static void BuildSqlUnit(
      SqlBuilder statement,
      ParameterBuilder paramBuilder,
      ProxyRQueryParameters requestParams,
      string schema,
      string name,
      string[] arguments,
      bool isView = false)
    {
      statement.Comment("Queries and outputs the results.", "Optionally including, paging, sorting, filtering and grouping.");
      BuildSelectStatement(statement, paramBuilder, requestParams, schema, name, arguments, isView: isView);
      if (requestParams.ShowTotal)
      {
        statement.Comment("Calculates the total row count.", "Optionally including filtering, but no paging or sorting.");
        BuildSelectStatement(statement, paramBuilder, requestParams, schema, name, arguments, includeCount: true, isView);
      }
    }

    private static void BuildSelectStatement(
      SqlBuilder statement,
      ParameterBuilder paramBuilder,
      ProxyRQueryParameters requestParams,
      string schema,
      string name,
      string[] arguments,
      bool includeCount = false,
      bool isView = false)
    {
      // Write the SELECT clause, with the output columns.
      BuildSelectClause(statement, requestParams, includeCount);

      // Write the FROM clause.
      statement.StartNewLine("FROM");

      if (isView)
      {
        statement.Indent(fn => fn.StartNewLine($"[{schema}].[{name}] RESULTS"));
      }
      else
      {
        statement.Indent(fn =>
        {
          fn.StartNewLine($"[{schema}].[{name}](");

          if (arguments.Any())
          {
            fn.Indent(p => p.StartNewLine(Sql.CommaLines(arguments)));
          }

          fn.Literal(")");

          if (!includeCount)
          {
            fn.Literal(" RESULTS");
          }
        });
      }

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
      if (!includeCount && requestParams.Grouping.Count > 0)
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
      var shouldSort = requestParams.Sort.Count > 0 || requestParams.Skip.HasValue || requestParams.Take.HasValue;

      if (shouldSort && !includeCount)
      {
        // Do we not have something to sort?
        if (requestParams.Sort.Count == 0)
        {
          // Get us to sort the value [1] instead of a column.
          requestParams.Sort = new List<ProxyRSortParameters> {
            new ProxyRSortParameters {
              Literal = "1",
              IsDescending = false
            }
          };
        }

        // Create an array of column sort expressions.
        var orderParts = requestParams.Sort.Select(x => x.ToString()).ToArray();

        // Write the ORDER BY clause.
        statement.StartNewLine("ORDER BY");
        statement.Indent(cols => cols.StartNewLine(Sql.CommaLines(orderParts)));

        // Use OFFSET clause for Paging.
        if (!requestParams.LoadAll && (requestParams.Skip != null || requestParams.Take != null))
        {
          statement.Indent(offset =>
          {
            offset.StartNewLine($"OFFSET {requestParams.Skip ?? 0} ROWS");
            offset.StartNewLine($"FETCH NEXT {requestParams.Take ?? 200} ROWS ONLY");
          });
        }
      }

      // End the statement with a semi-colon.
      statement.Line(";");
      statement.Line();
    }

    private static void BuildSelectClause(SqlBuilder statement, ProxyRQueryParameters parameters, bool includeCount = false)
    {
      // Write the SELECT clause and columns.
      if (includeCount)
      {
        statement.StartOfLine("SELECT");
        statement.Indent(cols => cols.StartNewLine(Sql.CommaLines("[$Type] = '$Root'", "[TotalRecords] = COUNT(*)")));
        return;
      }

      // Start the list of select-expressions.
      var selectExpressions = new List<string> { "[$Type] = 'Result'" };

      if (parameters.Fields.Count > 0)
      {
        var dataFields = parameters.Fields.Select(selectField => $"RESULTS.[{Sql.Sanitize(selectField)}]");
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

    private static void BuildWhereExpression(
      SqlBuilder targetExpression,
      JArray sourceExpression,
      ParameterBuilder paramBuilder,
      bool includeBrackets = true)
    {
      if (sourceExpression.Count < 2)
      {
        return; // Bail out.
      }

      var left = sourceExpression[0];
      var firstOperation = sourceExpression[1]?.ToString().ToLower();

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
        var operation = sourceExpression[sourcePartIndex]?.ToString().ToLower();

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

    public static void GetODataQueryStringParameters(IQueryCollection queryString, ProxyRQueryParameters queryParams)
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

      var queryStringTop = queryString["$take"].FirstOrDefault()?.Trim();
      if (int.TryParse(queryStringTop, out var takeValue))
      {
        queryParams.Take = takeValue;
      }

      var queryStringSkip = queryString["$skip"].FirstOrDefault()?.Trim();
      if (int.TryParse(queryStringSkip, out var skipValue))
      {
        queryParams.Skip = skipValue;
      }

      var queryStringFilter = queryString["$filter"].FirstOrDefault()?.Trim();
      if (queryStringFilter.HasContent())
      {
        queryParams.Filter = GetODataFilterExpression(queryStringFilter);
      }

      var queryStringOrderBy = queryString["$orderby"].FirstOrDefault()?.Trim();
      if (queryStringOrderBy.HasContent())
      {
        var orderByColumns = queryStringOrderBy.Split(',').Select(x => x.Trim());
        foreach (var orderByColumn in orderByColumns)
        {
          queryParams.Sort.Add(GetODataSortExpression(orderByColumn));
        }
      }

      var queryStringSelect = queryString["$select"].FirstOrDefault()?.Trim();
      if (queryStringSelect.HasContent())
      {
        queryParams.Fields = queryStringSelect == "*"
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
          queryParams.ShowTotal= true;
          break;
        case "none":
          queryParams.ShowTotal= false;
          break;
        default:
          throw new NotSupportedException($"Value for $inlinecount={queryStringInlineCount} is not supported");
      }
    }

    private static ProxyRSortParameters GetODataSortExpression(string orderByColumn)
    {
      var parts = orderByColumn.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(x => x.Trim())
                               .ToArray();

      var isDescending = parts.Length > 1 && parts[1].Equals("desc", StringComparison.InvariantCultureIgnoreCase);

      return new ProxyRSortParameters
      {
        ColumnName = parts[0],
        IsDescending = isDescending
      };
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
  }
}
