using System;
using System.Data.Common;

namespace ProxyR.Abstractions.Execution
{
    public class DbCommandFactoryResult : IDisposable
    {
        public DbCommand Command { get; set; }
        public DbConnection Connection { get; set; }
        public string ConnectionString { get; set; }
        public bool OwnsConnection { get; set; }

        /// <summary>
        /// Disposes of the <see cref='System.ComponentModel.Component'/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes all the resources associated with this component.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            lock (this)
            {
                if (OwnsConnection)
                {
                    Connection?.Dispose();
                    Connection = null;
                }

                Command?.Dispose();
                Command = null;
            }
        }
    }
}
