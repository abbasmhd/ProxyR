using System;

namespace ProxyR.Abstractions.Interceptors
{
    /// <summary>
    ///     Attach to a method to describe the database stored procedure it will execute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ProcedureAttribute : Attribute
    {
        /// <summary>
        ///     Attach to a method to describe the database stored procedure it will execute.
        /// </summary>
        /// <param name="name">The name of the stored procedure that will be executed by this method.</param>
        public ProcedureAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     Name of the procedure the method represents.
        /// </summary>
        public string Name { get; }
    }
}
