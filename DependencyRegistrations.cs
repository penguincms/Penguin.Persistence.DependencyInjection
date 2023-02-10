using Penguin.Configuration.Abstractions.Exceptions;
using Penguin.Configuration.Abstractions.Interfaces;
using Penguin.Debugging;
using Penguin.DependencyInjection.Abstractions.Enums;
using Penguin.DependencyInjection.Abstractions.Interfaces;
using Penguin.Persistence.Abstractions;
using Penguin.Persistence.Abstractions.Constants;
using Penguin.Persistence.Abstractions.Exceptions;
using System;
using System.Reflection;

namespace Penguin.Persistence.DependencyInjection
{
    /// <summary>
    /// Contains the interface that automatically registers PersistenceConnectionInfo to resolve to DefaultConnectionString from the default IProvideConfiguration source
    /// </summary>
    public class DependencyRegistrations : IRegisterDependencies
    {
        /// <summary>
        /// Registers the dependencies
        /// </summary>
        public void RegisterDependencies(IServiceRegister serviceRegister)
        {
            if (serviceRegister is null)
            {
                throw new ArgumentNullException(nameof(serviceRegister));
            }

            StaticLogger.Log($"Penguin.Persistence.DependencyInjection: {Assembly.GetExecutingAssembly().GetName().Version}", StaticLogger.LoggingLevel.Call);

            //if (!DependencyEngine.IsRegistered<PersistenceConnectionInfo>())
            //{
            //Wonky ass logic to support old EF connection strings from web.config.
            //Most of this can be removed when CE isn't needed.
            serviceRegister.Register((IServiceProvider ServiceProvider) =>
                {
                    if (ServiceProvider.GetService(typeof(IProvideConfigurations)) is not IProvideConfigurations Configuration)
                    {
                        throw new NullReferenceException("IProvideConfigurations must be registered before building database connection");
                    }

                    string ConnectionString = Configuration.GetConnectionString("DefaultConnectionString");

                    if (ConnectionString is null)
                    {
                        string error = $"Can not find connection string {Strings.CONNECTION_STRING_NAME} in registered configuration provider";

                        try
                        {
                            throw new DatabaseNotConfiguredException(error,
                                new MissingConfigurationException(Strings.CONNECTION_STRING_NAME, error
                                ));
                        } //Notify dev if possible
                        catch (Exception)
                        {
                            return new PersistenceConnectionInfo("", "");
                        }
                    }

                    string Provider = "System.Data.SqlClient";

                    if (ConnectionString.StartsWith("name="))
                    {
                        ConnectionString = ConnectionString.Replace("name=", "");
                        ConnectionString = Configuration.GetConnectionString(ConnectionString);
                    }

                    PersistenceConnectionInfo connectionInfo = new(ConnectionString, Provider);

                    if (connectionInfo.ProviderType == ProviderType.SQLCE)
                    {
                        connectionInfo.ProviderName = "System.Data.SqlServerCe.4.0";
                    }

                    return connectionInfo;
                }, ServiceLifetime.Singleton);
        }

        //}
    }
}