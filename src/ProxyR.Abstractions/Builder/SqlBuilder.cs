using ProxyR.Abstractions.Commands;
using ProxyR.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace ProxyR.Abstractions.Builder
{
    public class SqlBuilder
    {
        private readonly StringBuilder _builder = new StringBuilder();

        public int IndentLevel { get; private set; }

        public IDisposable UseIndent(int levels = 1)
        {
            IndentLevel += levels;
            return new DisposableAction(() => IndentLevel -= levels);
        }

        public IDisposable UseBlock()
        {
            Line("BEGIN");
            var indent = UseIndent();
            return new DisposableAction(() =>
            {
                indent.Dispose();
                Line("END");
            });
        }

        public SqlBuilder Print(string value)
        {
            Line($"PRINT {Sql.Quote(value)};");
            return this;
        }

        public SqlBuilder Segment(Action<SqlBuilder> segment)
        {
            segment(this);
            return this;
        }

        public SqlBuilder TryBlock(Action<SqlBuilder> tryBlock)
        {
            if (tryBlock == null)
            {
                throw new ArgumentNullException(nameof(tryBlock));
            }

            Line("BEGIN TRY");
            Indent(tryBlock);
            Line("END TRY");

            return this;
        }

        public SqlBuilder CatchBlock(Action<SqlBuilder> catchBlock)
        {
            if (catchBlock == null)
            {
                throw new ArgumentNullException(nameof(catchBlock));
            }

            Line("BEGIN CATCH");
            Indent(catchBlock);
            Line("END CATCH");

            return this;
        }

        public SqlBuilder InsertInto(string tableName, DataTable sourceTable, int batchSize = 100)
        {
            if (!sourceTable.Rows.Cast<DataRow>().Any())
            {
                return this;
            }

            var columnNames = sourceTable.Columns
                .Cast<DataColumn>()
                .Select(column => column.ColumnName)
                .ToArray();

            var columns = Sql.ColumnLines(columnNames);

            var rows = sourceTable
                .Rows
                .Cast<DataRow>()
                .Select(r => Sql.Values(r.ItemArray));

            var chunks = rows.Split(batchSize);

            foreach (var chunk in chunks)
            {
                var rowLines = Sql.ParenthesisLines(chunk.ToArray());
                Line($"INSERT INTO {tableName} (");
                Indent(b => b.Line($"{columns})"));
                Line("VALUES");
                Indent(b => b.Line($"{rowLines};"));
            }

            return this;
        }

        public SqlBuilder InsertInto<TSource, TResult>(
            string tableName,
            IEnumerable<TSource> source,
            Func<TSource, TResult> selector)
        {
            if (!source.Any())
            {
                return this;
            }

            var properties = typeof(TResult)
                .GetProperties();

            var columnNames = properties
                .Select(p => p.Name)
                .ToArray();

            var columns = Sql.ColumnLines(columnNames);
            var items = source.Select(r => Sql.Values(Sql.GetPropertyValues(selector(r), properties)));
            var itemLines = Sql.ParenthesisLines(items.ToArray());

            Line($"INSERT INTO {tableName} (\n\t{columns})\nVALUES\n\t{itemLines};");

            return this;
        }

        public void UpsertMerge(
            string sourceTable,
            string targetTable,
            string[] keyFields,
            string[] columnNames,
            string[] updateColumnNames = null,
            bool updateWhenMatched = true,
            bool insertWhenNotMatched = true)
        {
            if (updateColumnNames == null)
                updateColumnNames = columnNames
                    .Except(keyFields)
                    .ToArray();

            StartNewLine("MERGE");
            Indent(targetClause =>
            {
                targetClause.StartNewLine($"{targetTable} AS TARGET");
            });

            StartNewLine("USING");
            Indent(sourceClause => sourceClause.StartNewLine($"{sourceTable} AS SOURCE"));

            StartNewLine("ON");
            Indent(onClause =>
            {
                var prefix = String.Empty;
                foreach (var keyField in keyFields)
                {
                    onClause.StartNewLine($"{prefix}TARGET.[{keyField}] = SOURCE.[{keyField}]");
                    prefix = "AND ";
                }
            });

            if (updateWhenMatched)
            {
                StartNewLine("WHEN MATCHED THEN");
                Indent(matchedClause =>
                {
                    matchedClause.StartNewLine("UPDATE SET");
                    matchedClause.Indent(updateFields => updateFields.StartNewLine(Sql.CommaLines(updateColumnNames.Select(columnName => $"TARGET.[{columnName}] = SOURCE.[{columnName}]").ToArray())));
                });
            }

            if (insertWhenNotMatched)
            {
                StartNewLine("WHEN NOT MATCHED THEN");
                Indent(notMatchedClause =>
                {
                    notMatchedClause.StartNewLine("INSERT (");
                    notMatchedClause.Indent(subClauseFields => subClauseFields.StartNewLine(Sql.CommaLines(columnNames)));
                    notMatchedClause.Literal(")");
                    notMatchedClause.StartNewLine("VALUES (");
                    notMatchedClause.Indent(subClauseFields => subClauseFields.StartNewLine(Sql.CommaLines(columnNames.Select(columnName => $"SOURCE.[{columnName}]").ToArray())));
                    notMatchedClause.Literal(")");
                });
            }

            Literal(";");
            Line();
        }

        public SqlBuilder Transaction(Action<SqlBuilder> transactionBlock)
        {
            if (transactionBlock == null)
                throw new ArgumentNullException(nameof(transactionBlock));

            Line("BEGIN TRANSACTION");

            TryBlock(tryBlock =>
            {
                transactionBlock(tryBlock);
                tryBlock.Line();
                tryBlock.Line("COMMIT TRANSACTION");
            })
            .CatchBlock(catchBlock =>
            {
                // TODO: Do we need to report error?
                catchBlock.Line("ROLLBACK TRANSACTION");
            });

            return this;
        }

        public SqlBuilder TableExists(string tableName, Action<SqlBuilder> ifBlock, Action<SqlBuilder> elseBlock = null, bool not = false)
        {
            var identifier = Sql.GetSchemaAndObjectName(tableName);
            var notPart = not ? "NOT " : String.Empty;

            IfElse(conditionExpression: condition => condition
                    .Literal(notPart)
                    .Literal($"EXISTS (SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = {Sql.Quote(identifier.Schema)} AND TABLE_NAME = {Sql.Quote(identifier.Object)})"),
                ifSegment: ifBlock,
                elseSegment: elseBlock);
            return this;
        }

        public SqlBuilder TableNotExists(string tableName, Action<SqlBuilder> ifSegment, Action<SqlBuilder> elseSegment = null)
        {
            TableExists(tableName, ifSegment, elseSegment, not: true);
            return this;
        }

        public SqlBuilder ColumnExists(string columnName, string tableName, Action<SqlBuilder> ifSegment, Action<SqlBuilder> elseSegment = null, bool not = false)
        {
            var identifier = Sql.GetSchemaAndObjectName(tableName);
            var notPart = not ? "NOT " : String.Empty;

            IfElse(conditionExpression: condition => condition
                    .Literal(notPart)
                    .Literal($"EXISTS (SELECT TOP 1 1 FROM sys.columns WHERE Name = N{Sql.Quote(columnName)} AND Object_ID = Object_ID(N{Sql.Quote(tableName)}))"),
                ifSegment,
                elseSegment);
            return this;
        }

        public SqlBuilder ColumnNotExists(string columnName, string tableName, Action<SqlBuilder> ifSegment, Action<SqlBuilder> elseSegment = null)
        {
            ColumnExists(columnName, tableName, ifSegment, elseSegment, not: true);
            return this;
        }

        public SqlBuilder If(Action<SqlBuilder> conditionExpression, Action<SqlBuilder> ifSegment)
        {
            IfElse(conditionExpression, ifSegment);
            return this;
        }

        public SqlBuilder IfElse(Action<SqlBuilder> conditionExpression, Action<SqlBuilder> ifSegment, Action<SqlBuilder> elseSegment = null, bool putIfInBlock = true, bool putElseInBlock = true)
        {
            StartOfLine("IF ");
            Segment(conditionExpression);
            Line();

            if (ifSegment != null)
            {
                if (putIfInBlock)
                {
                    Block(b => b.Segment(ifSegment));
                }
                else
                {
                    Indent(b => b.Segment(ifSegment));
                }
            }

            if (elseSegment != null)
            {
                Line("ELSE");
                if (putElseInBlock)
                {
                    Block(b => b.Segment(elseSegment));
                }
                else
                {
                    Indent(b => b.Segment(elseSegment));
                }
            }

            return this;
        }

        public SqlBuilder AddColumn(string tableName, string columnName, string type, bool isNullable = false)
        {
            Line($"ALTER TABLE {tableName} ADD {Sql.ColumnDefinition(columnName: columnName, type: type, isNullable: isNullable)};");
            return this;
        }

        public SqlBuilder AddMissingColumns(string tableName, DataTable table)
        {
            foreach (var column in table.Columns.Cast<DataColumn>())
            {
                ColumnNotExists(
                    column.ColumnName,
                    tableName,
                    ifSegment => ifSegment.AddColumn(tableName, column.ColumnName, DbTypes.GetDbTypeSyntax(column.DataType), column.AllowDBNull));
            }

            return this;
        }

        public SqlBuilder CreateTable(string tableName, params string[] columnDefinitions)
        {
            Line($"CREATE TABLE {tableName} (");
            Indent(columnBlock => columnBlock.Line(Sql.CommaLines(columnDefinitions)));
            Line(");");
            return this;
        }

        public SqlBuilder CreateTable(string tableName, DataTable table)
        {
            CreateTable(tableName, table.Columns.Cast<DataColumn>().ToArray());
            return this;
        }

        public SqlBuilder CreateTable(string tableName, params DataColumn[] columns)
        {
            var maxColumnNameLength = columns.Max(c => c.ColumnName.Length);
            var columnDefintions = columns
                .Select(c => Sql.ColumnDefinition(
                    columnName: c.ColumnName,
                    type: DbTypes.GetDbTypeSyntax(c.DataType, c.MaxLength),
                    isNullable: c.AllowDBNull,
                    columnNamePadding: maxColumnNameLength))
                .ToArray();
            CreateTable(tableName, columnDefintions);
            return this;
        }

        public SqlBuilder DropConstraint(string constraintName, string tableName)
        {
            Line($"ALTER TABLE {tableName}");
            Line($"DROP CONSTRAINT [{constraintName}];");
            return this;
        }

        public SqlBuilder AddPrimaryKeyConstraint(string constraintName, string tableName, params string[] columnNames)
        {
            Line($"ALTER TABLE {tableName}");
            Line($"ADD CONSTRAINT [{constraintName}] PRIMARY KEY (");
            Indent(block => block.Line(Sql.ColumnLines(columnNames)));
            Line(");");
            return this;
        }

        public SqlBuilder ConstraintExists(string constraintName, Action<SqlBuilder> ifSegment, Action<SqlBuilder> elseSegment = null, bool not = false)
        {
            var notPart = not ? "NOT " : String.Empty;
            IfElse(conditionExpression: condition => condition.Literal(notPart).Literal($"EXISTS (SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = {Sql.Quote(constraintName)})"),
                ifSegment,
                elseSegment);
            return this;
        }

        public SqlBuilder ConstraintNotExists(string constraintName, Action<SqlBuilder> ifSegment, Action<SqlBuilder> elseSegment = null)
        {
            return ConstraintExists(constraintName, ifSegment, elseSegment, not: true);
        }

        public SqlBuilder DropTableIfExists(string tableName)
        {
            Line($"DROP TABLE IF EXISTS {tableName};");
            return this;
        }

        public SqlBuilder Block(Action<SqlBuilder> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            using (UseBlock())
            {
                action(this);
            }
            return this;
        }

        public SqlBuilder Indent(Action<SqlBuilder> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            using (UseIndent())
            {
                action(this);
            }
            return this;
        }

        public SqlBuilder Literal(string content)
        {
            _builder.Append(content);
            return this;
        }

        public SqlBuilder Line()
        {
            Line(String.Empty);
            return this;
        }

        public SqlBuilder StartNewLine(string line = null)
        {
            _builder.AppendLine();
            StartOfLine(line);
            return this;
        }

        public SqlBuilder StartOfLine(string line = null)
        {
            line = line ?? String.Empty;
            _builder.Append(line.Indent(tabCount: IndentLevel));
            return this;
        }

        public SqlBuilder Line(string line)
        {
            _builder.AppendLine(line.Indent(tabCount: IndentLevel));
            return this;
        }

        public SqlBuilder Comment(params string[] lines)
        {
            foreach (var line in lines)
            {
                Line($"-- {line}");
            }
            return this;
        }

        public override string ToString() => _builder.ToString();
    }

}
