using Penguin.Configuration.Abstractions;
using Penguin.Debugging;
using Penguin.DependencyInjection.Abstractions;
using Penguin.DependencyInjection.Extensions;
using Penguin.DependencyInjection.ServiceProviders;
using Penguin.Persistence.Abstractions;
using System;
using System.Reflection;
using DependencyEngine = Penguin.DependencyInjection.Engine;

namespace Penguin.Persistence.DependencyInjection
{
    /// <summary>
    /// Contains the interface that automatically registers PersistenceConnectionInfo to resolve to DefaultConnectionString from the default IProvideConfiguration source
    /// </summary>
    public class DependencyRegistrations : IRegisterDependencies
    {
        #region Methods

        private const string CONNECTION_STRING_NAME = "DefaultConnectionString";

        /// <summary>
        /// Registers the dependencies
        /// </summary>
        public void RegisterDependencies()
        {
            StaticLogger.Log($"Penguin.Persistence.DependencyInjection: {Assembly.GetExecutingAssembly().GetName().Version}", StaticLogger.LoggingLevel.Call);

            if (!DependencyEngine.IsRegistered<PersistenceConnectionInfo>())
            {
                //Wonky ass logic to support old EF connection strings from web.config.
                //Most of this can be removed when CE isn't needed.
                DependencyEngine.Register<PersistenceConnectionInfo>((IServiceProvider ServiceProvider) =>
                {
                    IProvideConfigurations Configuration = ServiceProvider.GetService<IProvideConfigurations>();

                    if (Configuration is null)
                    {
                        throw new NullReferenceException("IProvideConfigurations must be registered before building database connection");
                    }

                    string ConnectionString = Configuration.GetConnectionString("DefaultConnectionString");

                    if (ConnectionString is null)
                    {
                        throw new NullReferenceException($"Can not find connection string {CONNECTION_STRING_NAME} in registered configuration provider");
                    }

                    string Provider = "System.Data.SqlClient";

                    if (ConnectionString.StartsWith("name="))
                    {
                        ConnectionString = ConnectionString.Replace("name=", "");
                        ConnectionString = Configuration.GetConnectionString(ConnectionString);
                    }

                    PersistenceConnectionInfo connectionInfo = new PersistenceConnectionInfo(ConnectionString, Provider);

                    if (connectionInfo.ProviderType == ProviderType.SQLCE)
                    {
                        connectionInfo.ProviderName = "System.Data.SqlServerCe.4.0";
                    }

                    return connectionInfo;
                }, typeof(SingletonServiceProvider));
            }
        }

        #endregion Methods
    }
}