// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity
{
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Infrastructure.DependencyResolution;
    using System.Data.Entity.Infrastructure.Pluralization;
    using System.Data.Entity.Migrations;
    using System.Data.Entity.Migrations.History;
    using System.Data.Entity.Migrations.Sql;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Spatial;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reflection;

    /// <summary>
    ///     A class derived from this class can be placed in the same assembly as a class derived from
    ///     <see cref="DbContext" /> to define Entity Framework configuration for an application.
    ///     Configuration is set by calling protected methods and setting protected properties of this
    ///     class in the constructor of your derived type.
    ///     The type to use can also be registered in the config file of the application.
    ///     See http://go.microsoft.com/fwlink/?LinkId=260883 for more information about Entity Framework configuration.
    /// </summary>
    public class DbConfiguration
    {
        private readonly InternalConfiguration _internalConfiguration;

        /// <summary>
        ///     Any class derived from <see cref="DbConfiguration" /> must have a public parameterless constructor
        ///     and that constructor should call this constructor.
        /// </summary>
        protected internal DbConfiguration()
            : this(new InternalConfiguration())
        {
            _internalConfiguration.Owner = this;
        }

        internal DbConfiguration(InternalConfiguration internalConfiguration)
        {
            DebugCheck.NotNull(internalConfiguration);

            _internalConfiguration = internalConfiguration;
            _internalConfiguration.Owner = this;
        }

        /// <summary>
        ///     The Singleton instance of <see cref="DbConfiguration" /> for this app domain. This can be
        ///     set at application start before any Entity Framework features have been used and afterwards
        ///     should be treated as read-only.
        /// </summary>
        public static void SetConfiguration(DbConfiguration configuration)
        {
            Check.NotNull(configuration, "configuration");

            InternalConfiguration.Instance = configuration.InternalConfiguration;
        }

        /// <summary>
        /// Attempts to discover and load the <see cref="DbConfiguration"/> associated with the given
        /// <see cref="DbContext"/> type. This method is intended to be used by tooling to ensure that
        /// the correct configuration is loaded into the app domain. Tooling should use this method
        /// before accessing the <see cref="DependencyResolver"/> property.
        /// </summary>
        /// <param name="contextType">A <see cref="DbContext"/> type to use for configuration discovery.</param>
        public static void LoadConfiguration(Type contextType)
        {
            Check.NotNull(contextType, "contextType");

            if (!typeof(DbContext).IsAssignableFrom(contextType))
            {
                throw new ArgumentException(Strings.BadContextTypeForDiscovery(contextType.Name));
            }

            DbConfigurationManager.Instance.EnsureLoadedForContext(contextType);
        }

        /// <summary>
        /// Attempts to discover and load the <see cref="DbConfiguration"/> from the given assembly.
        /// This method is intended to be used by tooling to ensure that the correct configuration is loaded into
        /// the app domain. Tooling should use this method before accessing the <see cref="DependencyResolver"/>
        /// property. If the tooling knows the <see cref="DbContext"/> type being used, then the
        /// <see cref="LoadConfiguration(Type)"/> method should be used since it gives a greater chance that
        /// the correct configuration will be found.
        /// </summary>
        /// <param name="contextType">A <see cref="DbContext"/> type to use for configuration discovery.</param>
        public static void LoadConfiguration(Assembly assemblyHint)
        {
            Check.NotNull(assemblyHint, "assemblyHint");

            DbConfigurationManager.Instance.EnsureLoadedForAssembly(assemblyHint, null);
        }

        /// <summary>
        ///     Occurs during EF initialization after the DbConfiguration has been constructed but just before
        ///     it is locked ready for use. Use this event to inspect and/or override services that have been
        ///     registered before the configuration is locked. Note that this event should be used carefully
        ///     since it may prevent tooling from discovering the same configuration that is used at runtime.
        /// </summary>
        /// <remarks>
        ///     Handlers can only be added before EF starts to use the configuration and so handlers should
        ///     generally be added as part of application initialization. Do not access the DbConfiguration
        ///     static methods inside the handler; instead use the the members of <see cref="DbConfigurationLoadedEventArgs" />
        ///     to get current services and/or add overrides.
        /// </remarks>
        public static event EventHandler<DbConfigurationLoadedEventArgs> Loaded
        {
            add
            {
                Check.NotNull(value, "value");

                DbConfigurationManager.Instance.AddLoadedHandler(value);
            }
            remove
            {
                Check.NotNull(value, "value");

                DbConfigurationManager.Instance.RemoveLoadedHandler(value);
            }
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to
        ///     add a <see cref="IDbDependencyResolver" /> instance to the Chain of Responsibility of resolvers that
        ///     are used to resolve dependencies needed by the Entity Framework.
        /// </summary>
        /// <remarks>
        ///     Resolvers are asked to resolve dependencies in reverse order from which they are added. This means
        ///     that a resolver can be added to override resolution of a dependency that would already have been
        ///     resolved in a different way.
        ///     The exceptions to this is that any dependency registered in the application's config file
        ///     will always be used in preference to using a dependency resolver added here.
        /// </remarks>
        /// <param name="resolver"> The resolver to add. </param>
        protected internal void AddDependencyResolver(IDbDependencyResolver resolver)
        {
            Check.NotNull(resolver, "resolver");

            _internalConfiguration.CheckNotLocked("AddDependencyResolver");
            _internalConfiguration.AddDependencyResolver(resolver);
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to
        ///     add a <see cref="IDbDependencyResolver" /> instance to the Chain of Responsibility of resolvers that
        ///     are used to resolve dependencies needed by the Entity Framework. Unlike the AddDependencyResolver
        ///     method, this method puts the resolver at the bottom of the Chain of Responsibility such that it will only
        ///     be used to resolve a dependency that could not be resolved by any of the other resolvers.
        /// </summary>
        /// <remarks>
        ///     A <see cref="DbProviderServices" /> implementation is automatically registered as a secondary resolver
        ///     when it is added with a call to <see cref="ProviderServices"/>. This allows EF providers to act as secondary
        ///     resolvers for other services that may need to be overridden by the provider.
        /// </remarks>
        /// <param name="resolver"> The resolver to add. </param>
        protected internal void AddSecondaryResolver(IDbDependencyResolver resolver)
        {
            Check.NotNull(resolver, "resolver");

            _internalConfiguration.CheckNotLocked("AddSecondaryResolver");
            _internalConfiguration.AddSecondaryResolver(resolver);
        }

        /// <summary>
        ///     Gets the <see cref="IDbDependencyResolver" /> that is being used to resolve service
        ///     dependencies in the Entity Framework.
        /// </summary>
        public static IDbDependencyResolver DependencyResolver
        {
            get { return InternalConfiguration.Instance.DependencyResolver; }
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to register
        ///     an Entity Framework provider.
        /// </summary>
        /// <remarks>
        ///     Note that the provider is both registered as a service itself and also registered as a secondary resolver with
        ///     a call to AddSecondaryResolver.  This allows EF providers to act as secondary resolvers for other services that
        ///     may need to be overridden by the provider.
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="DbProviderServices" /> and also using AddSecondaryResolver to add the provider as a secondary
        ///     resolver. This means that, if desired, the same functionality can be achieved using a custom resolver or a
        ///     resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <param name="providerInvariantName"> The ADO.NET provider invariant name indicating the type of ADO.NET connection for which this provider will be used. </param>
        /// <param name="provider"> The provider instance. </param>
        [CLSCompliant(false)]
        protected internal void ProviderServices(string providerInvariantName, DbProviderServices provider)
        {
            Check.NotEmpty(providerInvariantName, "providerInvariantName");
            Check.NotNull(provider, "provider");

            _internalConfiguration.CheckNotLocked("ProviderServices");
            _internalConfiguration.RegisterSingleton(provider, providerInvariantName);

            AddSecondaryResolver(provider);
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to register
        ///     an ADO.NET provider.
        /// </summary>
        /// <remarks>
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolvers for
        ///     <see cref="DbProviderFactory" /> and <see cref="IProviderInvariantName" />. This means that, if desired,
        ///     the same functionality can be achieved using a custom resolver or a resolver backed by an
        ///     Inversion-of-Control container.
        /// </remarks>
        /// <param name="providerInvariantName"> The ADO.NET provider invariant name indicating the type of ADO.NET connection for which this provider will be used. </param>
        /// <param name="providerFactory"> The provider instance. </param>
        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "1#"), CLSCompliant(false)]
        protected internal void ProviderFactory(string providerInvariantName, DbProviderFactory providerFactory)
        {
            Check.NotEmpty(providerInvariantName, "providerInvariantName");
            Check.NotNull(providerFactory, "providerFactory");

            _internalConfiguration.CheckNotLocked("ProviderFactory");
            _internalConfiguration.RegisterSingleton(providerFactory, providerInvariantName);
            _internalConfiguration.AddDependencyResolver(new InvariantNameResolver(providerFactory, providerInvariantName));
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to add an
        ///     <see cref="IDbExecutionStrategy" /> for use with the provider represented by the given invariant name.
        /// </summary>
        /// <remarks>
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="IDbExecutionStrategy" />. This means that, if desired, the same functionality can be achieved using
        ///     a custom resolver or a resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <param name="providerInvariantName"> The ADO.NET provider invariant name indicating the type of ADO.NET connection for which this execution strategy will be used. </param>
        /// <param name="getExecutionStrategy"> A function that returns a new instance of an execution strategy. </param>
        protected internal void ExecutionStrategy(string providerInvariantName, Func<IDbExecutionStrategy> getExecutionStrategy)
        {
            Check.NotEmpty(providerInvariantName, "providerInvariantName");
            Check.NotNull(getExecutionStrategy, "getExecutionStrategy");

            _internalConfiguration.CheckNotLocked("ExecutionStrategy");
            _internalConfiguration.AddDependencyResolver(
                new ExecutionStrategyResolver<IDbExecutionStrategy>(providerInvariantName, /*serverName:*/ null, getExecutionStrategy));
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to add an
        ///     <see cref="IDbExecutionStrategy" /> for use with the provider represented by the given invariant name and for a given server name.
        /// </summary>
        /// <remarks>
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="IDbExecutionStrategy" />. This means that, if desired, the same functionality can be achieved using
        ///     a custom resolver or a resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <param name="providerInvariantName"> The ADO.NET provider invariant name indicating the type of ADO.NET connection for which this execution strategy will be used. </param>
        /// <param name="getExecutionStrategy"> A function that returns a new instance of an execution strategy. </param>
        /// <param name="serverName"> A string that will be matched against the server name in the connection string. </param>
        protected internal void ExecutionStrategy(
            string providerInvariantName, Func<IDbExecutionStrategy> getExecutionStrategy, string serverName)
        {
            Check.NotEmpty(providerInvariantName, "providerInvariantName");
            Check.NotEmpty(serverName, "serverName");
            Check.NotNull(getExecutionStrategy, "getExecutionStrategy");

            _internalConfiguration.CheckNotLocked("ExecutionStrategy");
            _internalConfiguration.AddDependencyResolver(
                new ExecutionStrategyResolver<IDbExecutionStrategy>(providerInvariantName, serverName, getExecutionStrategy));
        }

        /// <summary>
        ///     Sets the <see cref="IDbConnectionFactory" /> that is used to create connections by convention if no other
        ///     connection string or connection is given to or can be discovered by <see cref="DbContext" />.
        ///     Note that a default connection factory is set in the app.config or web.config file whenever the
        ///     EntityFramework NuGet package is installed. As for all config file settings, the default connection factory
        ///     set in the config file will take precedence over any setting made with this method. Therefore the setting
        ///     must be removed from the config file before calling this method will have any effect.
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to change
        ///     the default connection factory being used.
        /// </summary>
        /// <remarks>
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="IDbConnectionFactory" />. This means that, if desired, the same functionality can be achieved using
        ///     a custom resolver or a resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <param name="connectionFactory"> The connection factory. </param>
        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "0#")]
        protected internal void ConnectionFactory(IDbConnectionFactory connectionFactory)
        {
            Check.NotNull(connectionFactory, "connectionFactory");

            _internalConfiguration.CheckNotLocked("ConnectionFactory");
            _internalConfiguration.RegisterSingleton(connectionFactory);
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to
        ///     set the pluralization service.
        /// </summary>
        /// <param name="pluralizationService"> The pluralization service to use. </param>
        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "0#")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pluralization")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "pluralization")]
        protected internal void PluralizationService(IPluralizationService pluralizationService)
        {
            Check.NotNull(pluralizationService, "pluralizationService");

            _internalConfiguration.CheckNotLocked("PluralizationService");
            _internalConfiguration.RegisterSingleton(pluralizationService);
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to
        ///     set the database initializer to use for the given context type.  The database initializer is called when a
        ///     the given <see cref="DbContext" /> type is used to access a database for the first time.
        ///     The default strategy for Code First contexts is an instance of <see cref="CreateDatabaseIfNotExists{TContext}" />.
        /// </summary>
        /// <remarks>
        ///     Calling this method is equivalent to calling <see cref="Database.SetInitializer{TContext}" />.
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="IDatabaseInitializer{TContext}" />. This means that, if desired, the same functionality can be achieved using
        ///     a custom resolver or a resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <typeparam name="TContext"> The type of the context. </typeparam>
        /// <param name="initializer"> The initializer to use, or null to disable initialization for the given context type. </param>
        protected internal void DatabaseInitializer<TContext>(IDatabaseInitializer<TContext> initializer) where TContext : DbContext
        {
            _internalConfiguration.CheckNotLocked("DatabaseInitializer");
            _internalConfiguration.RegisterSingleton(initializer ?? new NullDatabaseInitializer<TContext>());
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to add a
        ///     <see cref="Migrations.Sql.MigrationSqlGenerator" /> for use with the provider represented by the given invariant name.
        /// </summary>
        /// <remarks>
        ///     This method is typically used by providers to register an associated SQL generator for Code First Migrations.
        ///     It is different from setting the generator in the <see cref="DbMigrationsConfiguration" /> because it allows
        ///     EF to use the Migrations pipeline to create a database even when there is no Migrations configuration in the project
        ///     and/or Migrations are not being explicitly used.
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="Migrations.Sql.MigrationSqlGenerator" />. This means that, if desired, the same functionality can be achieved using
        ///     a custom resolver or a resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <param name="providerInvariantName"> The invariant name of the ADO.NET provider for which this generator should be used. </param>
        /// <param name="sqlGenerator"> A delegate that returns a new instance of the SQL generator each time it is called. </param>
        protected internal void MigrationSqlGenerator(string providerInvariantName, Func<MigrationSqlGenerator> sqlGenerator)
        {
            Check.NotEmpty(providerInvariantName, "providerInvariantName");
            Check.NotNull(sqlGenerator, "sqlGenerator");

            _internalConfiguration.CheckNotLocked("MigrationSqlGenerator");
            _internalConfiguration.RegisterSingleton(sqlGenerator, providerInvariantName);
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to set
        ///     an implementation of <see cref="IManifestTokenResolver" /> which allows provider manifest tokens to
        ///     be obtained from connections without necessarily opening the connection.
        /// </summary>
        /// <remarks>
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="IManifestTokenResolver" />. This means that, if desired, the same functionality can be achieved using
        ///     a custom resolver or a resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <param name="resolver"> The manifest token resolver. </param>
        protected internal void ManifestTokenResolver(IManifestTokenResolver resolver)
        {
            Check.NotNull(resolver, "resolver");

            _internalConfiguration.CheckNotLocked("ManifestTokenResolver");
            _internalConfiguration.RegisterSingleton(resolver);
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to set
        ///     an implementation of <see cref="IDbProviderFactoryResolver" /> which allows a <see cref="DbProviderFactory" />
        ///     to be obtained from a <see cref="DbConnection" /> in cases where the default implementation is not
        ///     sufficient.
        /// </summary>
        /// <remarks>
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="IDbProviderFactoryResolver" />. This means that, if desired, the same functionality can be achieved using
        ///     a custom resolver or a resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <param name="providerFactoryResolver"> The provider factory service. </param>
        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "0#")]
        protected internal void ProviderFactoryResolver(IDbProviderFactoryResolver providerFactoryResolver)
        {
            Check.NotNull(providerFactoryResolver, "providerFactoryResolver");

            _internalConfiguration.CheckNotLocked("ProviderFactoryResolver");
            _internalConfiguration.RegisterSingleton(providerFactoryResolver);
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to set
        ///     a <see cref="Func{DbContext, IDbModelCacheKey}" /> as the model cache key factory which allows the key
        ///     used to cache the model behind a <see cref="DbContext" /> to be changed.
        /// </summary>
        /// <remarks>
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="Func<DbContext, IDbModelCacheKey>" />. This means that, if desired, the same functionality can
        ///     be achieved using a custom resolver or a resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <param name="keyFactory"> The key factory. </param>
        protected internal void ModelCacheKey(Func<DbContext, IDbModelCacheKey> keyFactory)
        {
            Check.NotNull(keyFactory, "keyFactory");

            _internalConfiguration.CheckNotLocked("ModelCacheKey");
            _internalConfiguration.RegisterSingleton(keyFactory);
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to set
        ///     a <see cref="Func{DbConnection, String, HistoryContext}" /> delegate which allows for creation of a customized
        ///     <see cref="Migrations.History.HistoryContext" /> for a given <see cref="DbMigrationsConfiguration" />.
        /// </summary>
        /// <remarks>
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="Func{DbConnection, String, HistoryContext}" />. This means that, if desired, the same functionality
        ///     can be achieved using a custom resolver or a resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <param name="providerInvariantName"> The invariant name of the ADO.NET provider for which this generator should be used. </param>
        /// <param name="factory"> 
        /// A factory for creating <see cref="Migrations.History.HistoryContext"/> instances for a given <see cref="DbConnection"/> and
        /// <see cref="String"/> representing the default schema.
        /// </param>
        protected internal void HistoryContext(string providerInvariantName, Func<DbConnection, string, HistoryContext> factory)
        {
            Check.NotEmpty(providerInvariantName, "providerInvariantName");
            Check.NotNull(factory, "factory");

            _internalConfiguration.CheckNotLocked("HistoryContext");
            _internalConfiguration.RegisterSingleton(factory, providerInvariantName);
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to set
        ///     the global instance of <see cref="DbSpatialServices" /> which will be used whenever a spatial provider is
        ///     required and a provider-specific spatial provider cannot be found. Normally, a provider-specific spatial provider
        ///     is obtained from the a <see cref="DbProviderServices" /> implementation which is in turn returned by resolving
        ///     a service for <see cref="DbSpatialServices" /> passing the provider invariant name as a key. However, this
        ///     cannot work for stand-alone instances of <see cref="DbGeometry" /> and <see cref="DbGeography" /> since
        ///     it is impossible to know the spatial provider to use. Therefore, when creating stand-alone instances
        ///     of <see cref="DbGeometry" /> and <see cref="DbGeography" /> the global spatial provider is always used.
        /// </summary>
        /// <remarks>
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="DbSpatialServices" />. This means that, if desired, the same functionality can be achieved using
        ///     a custom resolver or a resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <param name="spatialProvider"> The spatial provider. </param>
        protected internal void SpatialServices(DbSpatialServices spatialProvider)
        {
            Check.NotNull(spatialProvider, "spatialProvider");

            _internalConfiguration.CheckNotLocked("SpatialServices");
            _internalConfiguration.RegisterSingleton(spatialProvider);
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to add
        ///     an implementation of <see cref="DbSpatialServices" /> to use for a specific provider and provider
        ///     manifest token.
        /// </summary>
        /// <remarks>
        ///     Use <see cref="SpatialServices(DbProviderInfo, DbSpatialServices)" />
        ///     to register spatial services for use only when a specific manifest token is returned by the provider.
        ///     Use <see cref="SpatialServices(System.Data.Entity.Spatial.DbSpatialServices)" /> to register global
        ///     spatial services to be used when provider information is not available or no provider-specific
        ///     spatial services are found.
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="DbSpatialServices" />. This means that, if desired, the same functionality can be achieved using
        ///     a custom resolver or a resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <param name="key">
        ///     The <see cref="DbProviderInfo" /> indicating the type of ADO.NET connection for which this spatial provider will be used.
        /// </param>
        /// <param name="spatialProvider"> The spatial provider. </param>
        protected internal void SpatialServices(DbProviderInfo key, DbSpatialServices spatialProvider)
        {
            Check.NotNull(key, "key");
            Check.NotNull(spatialProvider, "spatialProvider");

            _internalConfiguration.CheckNotLocked("SpatialServices");
            _internalConfiguration.RegisterSingleton(spatialProvider, key);
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to add
        ///     an implementation of <see cref="DbSpatialServices" /> to use for a specific provider with any
        ///     manifest token.
        /// </summary>
        /// <remarks>
        ///     Use <see cref="SpatialServices(String, DbSpatialServices)"/> 
        ///     to register spatial services for use when any manifest token is returned by the provider.
        ///     Use <see cref="SpatialServices(System.Data.Entity.Spatial.DbSpatialServices)"/> to register global
        ///     spatial services to be used when provider information is not available or no provider-specific
        ///     spatial services are found.
        /// 
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="DbSpatialServices" />. This means that, if desired, the same functionality can be achieved using
        ///     a custom resolver or a resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <param name="providerInvariantName"> The ADO.NET provider invariant name indicating the type of ADO.NET connection for which this spatial provider will be used. </param>
        /// <param name="spatialProvider"> The spatial provider. </param>
        protected internal void SpatialServices(string providerInvariantName, DbSpatialServices spatialProvider)
        {
            Check.NotEmpty(providerInvariantName, "providerInvariantName");
            Check.NotNull(spatialProvider, "spatialProvider");

            _internalConfiguration.CheckNotLocked("SpatialServices");
            RegisterSpatialServices(providerInvariantName, spatialProvider);
        }

        private void RegisterSpatialServices(string providerInvariantName, DbSpatialServices spatialProvider)
        {
            DebugCheck.NotEmpty(providerInvariantName);
            DebugCheck.NotNull(spatialProvider);

            _internalConfiguration.RegisterSingleton(
                spatialProvider,
                k =>
                    {
                        var asSpatialKey = k as DbProviderInfo;
                        return asSpatialKey != null && asSpatialKey.ProviderInvariantName == providerInvariantName;
                    });
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to set
        ///     a factory for the type of <see cref="DbCommandLogger" /> to use with <see cref="Database.Log" />.
        /// </summary>
        /// <remarks>
        ///     Note that setting the type of logger to use with this method does change the way command are
        ///     logged when <see cref="Database.Log" />is used. It is still necessary to set a <see cref="TextWriter" />
        ///     instance onto <see cref="Database.Log" /> before any commands will be logged.
        ///     For more low-level control over logging/interception see <see cref="IDbCommandInterceptor" /> and
        ///     <see cref="Interception" />.
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="Func{DbCommandLogger}" />. This means that, if desired, the same functionality can be achieved using
        ///     a custom resolver or a resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <param name="commandLoggerFactory">A delegate that will create logger instances.</param>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        protected internal void CommandLogger(Func<DbContext, Action<string>, DbCommandLogger> commandLoggerFactory)
        {
            Check.NotNull(commandLoggerFactory, "commandLoggerFactory");

            _internalConfiguration.CheckNotLocked("CommandLogger");
            _internalConfiguration.RegisterSingleton(commandLoggerFactory);
        }

        /// <summary>
        ///     Call this method from the constructor of a class derived from <see cref="DbConfiguration" /> to
        ///     register an <see cref="IDbInterceptor" /> at application startup. Note that interceptors can also
        ///     be added and removed at any time using <see cref="Interception" />.
        /// </summary>
        /// <remarks>
        ///     This method is provided as a convenient and discoverable way to add configuration to the Entity Framework.
        ///     Internally it works in the same way as using AddDependencyResolver to add an appropriate resolver for
        ///     <see cref="IDbInterceptor" />. This means that, if desired, the same functionality can be achieved using
        ///     a custom resolver or a resolver backed by an Inversion-of-Control container.
        /// </remarks>
        /// <param name="interceptor">The interceptor to register.</param>
        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "0#")]
        protected internal void Interceptor(IDbInterceptor interceptor)
        {
            Check.NotNull(interceptor, "interceptor");

            _internalConfiguration.CheckNotLocked("Interceptor");
            _internalConfiguration.RegisterSingleton(interceptor);
        }

        internal virtual InternalConfiguration InternalConfiguration
        {
            get { return _internalConfiguration; }
        }
    }
}
