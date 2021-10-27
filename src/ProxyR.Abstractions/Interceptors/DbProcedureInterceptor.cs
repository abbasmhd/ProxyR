using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using Castle.DynamicProxy;
using ProxyR.Abstractions.Execution;

namespace ProxyR.Abstractions.Interceptors
{
    /// <summary>
    ///     Intercepts calls to the methods of an interface using a generated proxy.
    ///     Once called, it will convert the method signature into a DbCommand,
    ///     which will execute a stored procedure with the same name as the method,
    ///     with the parameters of the method passed as parameters to the stored procedure.
    /// </summary>
    public class DbProcedureInterceptor : IInterceptor
    {
        private static readonly ProxyGenerator _generator = new ProxyGenerator();

        private readonly string _connectionString;
        private readonly DbConnection _connection;
        private readonly Func<DbConnection> _connectionGetter;

        public DbProcedureInterceptor(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbProcedureInterceptor(DbConnection connection)
        {
            _connection = connection;
        }

        public DbProcedureInterceptor(Func<DbConnection> connectionGetter)
        {
            _connectionGetter = connectionGetter;
        }

        /// <summary>
        ///     Fired whenever one of the interfaces methods have been called.
        /// </summary>
        public void Intercept(IInvocation invocation)
        {
            // Resolve the procedure name from the attribute, failing that the method name.
            var procedureAttribute = invocation.Method.GetCustomAttribute<ProcedureAttribute>();
            var procedureName = procedureAttribute?.Name
                                ?? invocation.Method.Name;

            // Convert the parameters over to a dictionary.
            var methodParameters = invocation.Method.GetParameters();
            var dbParameters = new Dictionary<string, object>();
            for (var parameterIndex = 0; parameterIndex < methodParameters.Length; parameterIndex++)
            {
                var name = methodParameters[parameterIndex].Name;
                dbParameters[name] = invocation.Arguments[parameterIndex];
            }

            // Create the result, based upon the connection we have.
            DbResult result;
            if (_connectionString != null)
            {
                result = Db.Query(_connectionString, procedureName, dbParameters);
            }
            else if (_connection != null)
            {
                result = Db.Query(_connection, procedureName, dbParameters);
            }
            else if (_connectionGetter != null)
            {
                result = Db.Query(_connectionGetter(), procedureName, dbParameters);
            }
            else
            {
                throw new InvalidOperationException("No connection, connection-string, or context has been provided, command cannot be creaed.");
            }

            // Set the return value to our result.
            // This result permits the caller to call the procedure in a manner that suits it.
            invocation.ReturnValue = result;
        }

        /// <summary>
        ///     Creates a proxy for an interface that will create connections on-demand using a connection-string or configured
        ///     connection-string name.
        /// </summary>
        /// <typeparam name="TInterface">The interface which contains methods representing stored procedures.</typeparam>
        /// <param name="connectionStringOrName">The connection-string or name of the configured connection-string to use.</param>
        public static TInterface Create<TInterface>(string connectionStringOrName) where TInterface : class
        {
            var interceptor = new DbProcedureInterceptor(connectionStringOrName);
            var proxy = _generator.CreateInterfaceProxyWithoutTarget<TInterface>(interceptor);
            return proxy;
        }

        /// <summary>
        ///     Creates a proxy for an interface that will use an existing connection.
        /// </summary>
        /// <typeparam name="TInterface">The interface which contains methods representing stored procedures.</typeparam>
        /// <param name="connection">The already created connection to use.</param>
        public static TInterface Create<TInterface>(DbConnection connection) where TInterface : class
        {
            var interceptor = new DbProcedureInterceptor(connection);
            var proxy = _generator.CreateInterfaceProxyWithoutTarget<TInterface>(interceptor);
            return proxy;
        }

        /// <summary>
        ///     Creates a proxy for an interface that will ask for a connection on-demand.
        /// </summary>
        /// <typeparam name="TInterface">The interface which contains methods representing stored procedures.</typeparam>
        public static TInterface Create<TInterface>(Func<DbConnection> connectionGetter) where TInterface : class
        {
            var interceptor = new DbProcedureInterceptor(connectionGetter);
            var proxy = _generator.CreateInterfaceProxyWithoutTarget<TInterface>(interceptor);
            return proxy;
        }
    }
}
