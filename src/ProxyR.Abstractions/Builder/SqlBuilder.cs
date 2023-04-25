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

        /// <summary>
        /// Creates an indentation level for the output.
        /// </summary>
        /// <param name="levels">The number of levels to indent.</param>
        /// <returns>An IDisposable object that will reduce the indentation level when disposed.</returns>
        public IDisposable UseIndent(int levels = 1)
        {
            IndentLevel += levels;
            return new DisposableAction(() => IndentLevel -= levels);
        }

        /// <summary>
        /// Creates a new disposable action that will execute a given action when disposed.
        /// </summary>
        /// <returns>A disposable action that will execute a given action when disposed.</returns>
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

        /// <summary>
        /// Prints the given value to the output.
        /// </summary>
        /// <param name="value">The value to print.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder Print(string value)
        {
            Line($"PRINT {Sql.Quote(value)};");
            return this;
        }

        /// <summary>
        /// Executes the given action on the SqlBuilder instance.
        /// </summary>
        /// <param name="segment">The action to execute.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder Segment(Action<SqlBuilder> segment)
        {
            segment(this);
            return this;
        }

        /// <summary>
        /// Executes a try block with the given action.
        /// </summary>
        /// <param name="tryBlock">The action to execute in the try block.</param>
        /// <returns>The SqlBuilder instance.</returns>
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

        /// <summary>
        /// Executes a catch block with the given action.
        /// </summary>
        /// <param name="catchBlock">The action to execute in the catch block.</param>
        /// <returns>The SqlBuilder instance.</returns>
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

        /// <summary>
        /// Inserts a DataTable into a table in the database.
        /// </summary>
        /// <param name="tableName">The name of the table to insert into.</param>
        /// <param name="sourceTable">The DataTable to insert.</param>
        /// <param name="batchSize">The size of the batches to insert.</param>
        /// <returns>The SqlBuilder instance.</returns>
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
        /// <summary>
        /// Inserts a set of objects into a table.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="source">The source.</param>
        /// <param name="selector">The selector.</param>
        /// <returns>The SqlBuilder instance.</returns>

        public SqlBuilder InsertInto<TSource, TResult>(string tableName, IEnumerable<TSource> source, Func<TSource, TResult> selector)
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

        /// <summary>
        /// Generates a MERGE statement to upsert data from one table to another.
        /// </summary>
        /// <param name="sourceTable">The source table.</param>
        /// <param name="targetTable">The target table.</param>
        /// <param name="keyFields">The key fields.</param>
        /// <param name="columnNames">The column names.</param>
        /// <param name="updateColumnNames">The update column names.</param>
        /// <param name="updateWhenMatched">if set to <c>true</c> [update when matched].</param>
        /// <param name="insertWhenNotMatched">if set to <c>true</c> [insert when not matched].</param>
        public void UpsertMerge(string sourceTable, string targetTable, string[] keyFields, string[] columnNames, string[] updateColumnNames = null, bool updateWhenMatched = true, bool insertWhenNotMatched = true)
        {
            if (updateColumnNames == null)
            {
                updateColumnNames = columnNames.Except(keyFields).ToArray();
            }

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

        /// <summary>
        /// Executes a transaction block on the SqlBuilder.
        /// </summary>
        /// <param name="transactionBlock">The action to execute within the transaction.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder Transaction(Action<SqlBuilder> transactionBlock)
        {
            if (transactionBlock == null)
            {
                throw new ArgumentNullException(nameof(transactionBlock));
            }

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

        /// <summary>
        /// Checks if a table exists in the database and executes the given ifBlock or elseBlock accordingly.
        /// </summary>
        /// <param name="tableName">The name of the table to check.</param>
        /// <param name="ifBlock">The action to execute if the table exists.</param>
        /// <param name="elseBlock">The action to execute if the table does not exist.</param>
        /// <param name="not">Whether to check if the table does not exist.</param>
        /// <returns>The SqlBuilder instance.</returns>
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

        /// <summary>
        /// Checks if the specified table does not exist and executes the corresponding action.
        /// </summary>
        /// <param name="tableName">The name of the table to check.</param>
        /// <param name="ifSegment">The action to execute if the table does not exist.</param>
        /// <param name="elseSegment">The action to execute if the table does exist.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder TableNotExists(string tableName, Action<SqlBuilder> ifSegment, Action<SqlBuilder> elseSegment = null)
        {
            TableExists(tableName, ifSegment, elseSegment, not: true);
            return this;
        }

        /// <summary>
        /// Checks if a column exists in a table and executes the corresponding action.
        /// </summary>
        /// <param name="columnName">Name of the column to check.</param>
        /// <param name="tableName">Name of the table to check.</param>
        /// <param name="ifSegment">Action to execute if the column exists.</param>
        /// <param name="elseSegment">Action to execute if the column does not exist.</param>
        /// <param name="not">Flag to indicate if the condition should be negated.</param>
        /// <returns>The SqlBuilder instance.</returns>
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

        /// <summary>
        /// Checks if the specified column exists in the specified table and executes the corresponding action.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="ifSegment">Action to execute if the column exists.</param>
        /// <param name="elseSegment">Action to execute if the column does not exist.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder ColumnNotExists(string columnName, string tableName, Action<SqlBuilder> ifSegment, Action<SqlBuilder> elseSegment = null)
        {
            ColumnExists(columnName, tableName, ifSegment, elseSegment, not: true);
            return this;
        }

        /// <summary>
        /// Executes an if statement with the given condition and if segment.
        /// </summary>
        /// <param name="conditionExpression">The condition expression.</param>
        /// <param name="ifSegment">The if segment.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder If(Action<SqlBuilder> conditionExpression, Action<SqlBuilder> ifSegment)
        {
            IfElse(conditionExpression, ifSegment);
            return this;
        }

        /// <summary>
        /// Creates an IF-ELSE statement in the SQL query.
        /// </summary>
        /// <param name="conditionExpression">The condition expression to evaluate.</param>
        /// <param name="ifSegment">The segment to execute if the condition is true.</param>
        /// <param name="elseSegment">The segment to execute if the condition is false.</param>
        /// <param name="putIfInBlock">Whether to put the if segment in a block.</param>
        /// <param name="putElseInBlock">Whether to put the else segment in a block.</param>
        /// <returns>The SqlBuilder instance.</returns>
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

        /// <summary>
        /// Adds a column to the specified table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="type">The type of the column.</param>
        /// <param name="isNullable">Whether the column is nullable.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder AddColumn(string tableName, string columnName, string type, bool isNullable = false)
        {
            Line($"ALTER TABLE {tableName} ADD {Sql.ColumnDefinition(columnName: columnName, type: type, isNullable: isNullable)};");
            return this;
        }

        /// <summary>
        /// Adds missing columns to the specified table.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="table">The table.</param>
        /// <returns>The SqlBuilder instance.</returns>
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

        /// <summary>
        /// Creates a table with the given name and column definitions.
        /// </summary>
        /// <param name="tableName">The name of the table to create.</param>
        /// <param name="columnDefinitions">The definitions of the columns to create.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder CreateTable(string tableName, params string[] columnDefinitions)
        {
            Line($"CREATE TABLE {tableName} (");
            Indent(columnBlock => columnBlock.Line(Sql.CommaLines(columnDefinitions)));
            Line(");");
            return this;
        }

        /// <summary>
        /// Creates a table with the given name and columns from the given DataTable.
        /// </summary>
        /// <param name="tableName">The name of the table to create.</param>
        /// <param name="table">The DataTable containing the columns for the table.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder CreateTable(string tableName, DataTable table)
        {
            CreateTable(tableName, table.Columns.Cast<DataColumn>().ToArray());
            return this;
        }

        /// <summary>
        /// Creates a table with the given name and columns.
        /// </summary>
        /// <param name="tableName">The name of the table to create.</param>
        /// <param name="columns">The columns to create in the table.</param>
        /// <returns>The SqlBuilder instance.</returns>
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

        /// <summary>
        /// Drops the specified constraint from the specified table.
        /// </summary>
        /// <param name="constraintName">The name of the constraint to drop.</param>
        /// <param name="tableName">The name of the table from which to drop the constraint.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder DropConstraint(string constraintName, string tableName)
        {
            Line($"ALTER TABLE {tableName}");
            Line($"DROP CONSTRAINT [{constraintName}];");
            return this;
        }

        /// <summary>
        /// Adds a primary key constraint to the table.
        /// </summary>
        /// <param name="constraintName">The name of the constraint.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="columnNames">The names of the columns.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder AddPrimaryKeyConstraint(string constraintName, string tableName, params string[] columnNames)
        {
            Line($"ALTER TABLE {tableName}");
            Line($"ADD CONSTRAINT [{constraintName}] PRIMARY KEY (");
            Indent(block => block.Line(Sql.ColumnLines(columnNames)));
            Line(");");
            return this;
        }

        /// <summary>
        /// Checks if a constraint exists and executes the given segments accordingly.
        /// </summary>
        /// <param name="constraintName">The name of the constraint to check.</param>
        /// <param name="ifSegment">The segment to execute if the constraint exists.</param>
        /// <param name="elseSegment">The segment to execute if the constraint does not exist.</param>
        /// <param name="not">Whether to check if the constraint does not exist.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder ConstraintExists(string constraintName, Action<SqlBuilder> ifSegment, Action<SqlBuilder> elseSegment = null, bool not = false)
        {
            var notPart = not ? "NOT " : String.Empty;
            IfElse(conditionExpression: condition => condition.Literal(notPart).Literal($"EXISTS (SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = {Sql.Quote(constraintName)})"),
                ifSegment,
                elseSegment);
            return this;
        }

        /// <summary>
        /// Creates a constraint that checks if the specified constraint does not exist.
        /// </summary>
        /// <param name="constraintName">The name of the constraint to check.</param>
        /// <param name="ifSegment">The segment to execute if the constraint does not exist.</param>
        /// <param name="elseSegment">The segment to execute if the constraint does exist.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder ConstraintNotExists(string constraintName, Action<SqlBuilder> ifSegment, Action<SqlBuilder> elseSegment = null)
        {
            return ConstraintExists(constraintName, ifSegment, elseSegment, not: true);
        }

        /// <summary>
        /// Drops the specified table if it exists.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder DropTableIfExists(string tableName)
        {
            Line($"DROP TABLE IF EXISTS {tableName};");
            return this;
        }

        /// <summary>
        /// Executes an action within a block.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>The SqlBuilder instance.</returns>
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

        /// <summary>
        /// Executes an action with an indented SqlBuilder.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>The SqlBuilder instance.</returns>
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

        /// <summary>
        /// Appends a literal string to the SqlBuilder.
        /// </summary>
        /// <param name="content">The literal string to append.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder Literal(string content)
        {
            _builder.Append(content);
            return this;
        }

        /// <summary>
        /// Adds a line to the SqlBuilder.
        /// </summary>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder Line()
        {
            Line(String.Empty);
            return this;
        }

        /// <summary>
        /// Starts a new line in the SqlBuilder and optionally adds a line of text.
        /// </summary>
        /// <param name="line">Optional line of text to add.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder StartNewLine(string line = null)
        {
            _builder.AppendLine();
            StartOfLine(line);
            return this;
        }

        /// <summary>
        /// Appends the given line to the SqlBuilder with the current indent level.
        /// </summary>
        /// <param name="line">The line to append. If null, an empty string is used.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder StartOfLine(string line = null)
        {
            line = line ?? String.Empty;
            _builder.Append(line.Indent(tabCount: IndentLevel));
            return this;
        }

        /// <summary>
        /// Appends a line to the SqlBuilder with the specified indent level.
        /// </summary>
        /// <param name="line">The line to append.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder Line(string line)
        {
            _builder.AppendLine(line.Indent(tabCount: IndentLevel));
            return this;
        }

        /// <summary>
        /// Adds a comment to the SQL query.
        /// </summary>
        /// <param name="lines">The lines of the comment.</param>
        /// <returns>The SqlBuilder instance.</returns>
        public SqlBuilder Comment(params string[] lines)
        {
            foreach (var line in lines)
            {
                Line($"-- {line}");
            }
            return this;
        }

        /// <summary>
        /// Returns a string representation of the StringBuilder object.
        /// </summary>
        public override string ToString() => _builder.ToString();
    }

}
