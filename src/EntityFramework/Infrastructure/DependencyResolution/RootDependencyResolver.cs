// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.DependencyResolution
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity.Infrastructure.Pluralization;
    using System.Data.Entity.Internal;
    using System.Data.Entity.Migrations.History;
    using System.Data.Entity.ModelConfiguration.Utilities;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    ///     This resolver is always the last resolver in the internal resolver chain and is
    ///     responsible for providing the default service for each dependency or throwing an
    ///     exception if there is no reasonable default service.
    /// </summary>
    internal class RootDependencyResolver : IDbDependencyResolver
    {
        private readonly ResolverChain _secondaryResolvers = new ResolverChain();
        private readonly ResolverChain _resolvers = new ResolverChain();
        private readonly DatabaseInitializerResolver _databaseInitializerResolver;

        public RootDependencyResolver()
            : this(new DefaultProviderServicesResolver(), new DatabaseInitializerResolver())
        {
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        [SuppressMessage("Microsoft.Reliability", "CA2000: Dispose objects before losing scope")]
        public RootDependencyResolver(
            DefaultProviderServicesResolver defaultProviderServicesResolver,
            DatabaseInitializerResolver databaseInitializerResolver)
        {
            DebugCheck.NotNull(defaultProviderServicesResolver);
            DebugCheck.NotNull(databaseInitializerResolver);

            _databaseInitializerResolver = databaseInitializerResolver;

            _resolvers.Add(_databaseInitializerResolver);
            _resolvers.Add(new DefaultExecutionStrategyResolver());
            _resolvers.Add(new CachingDependencyResolver(defaultProviderServicesResolver));
            _resolvers.Add(new CachingDependencyResolver(new DefaultProviderFactoryResolver()));
            _resolvers.Add(new CachingDependencyResolver(new DefaultInvariantNameResolver()));
            _resolvers.Add(new SingletonDependencyResolver<IDbConnectionFactory>(new SqlConnectionFactory()));
            _resolvers.Add(new SingletonDependencyResolver<Func<DbContext, IDbModelCacheKey>>(new DefaultModelCacheKeyFactory().Create));
            _resolvers.Add(new SingletonDependencyResolver<IManifestTokenResolver>(new DefaultManifestTokenResolver()));
            _resolvers.Add(new SingletonDependencyResolver<Func<DbConnection, string, HistoryContext>>(HistoryContext.DefaultFactory));
            _resolvers.Add(new SingletonDependencyResolver<IPluralizationService>(new EnglishPluralizationService()));
            _resolvers.Add(new SingletonDependencyResolver<AttributeProvider>(new AttributeProvider()));
            _resolvers.Add(new SingletonDependencyResolver<Func<DbContext, Action<string>, DbCommandLogger>>((c, w) => new DbCommandLogger(c, w)));

#if NET40
            _resolvers.Add(new SingletonDependencyResolver<IDbProviderFactoryResolver>(new Net40DefaultDbProviderFactoryResolver()));
#else
            _resolvers.Add(new SingletonDependencyResolver<IDbProviderFactoryResolver>(new DefaultDbProviderFactoryResolver()));
#endif
        }

        public DatabaseInitializerResolver DatabaseInitializerResolver
        {
            get { return _databaseInitializerResolver; }
        }

        /// <inheritdoc />
        public virtual object GetService(Type type, object key)
        {
            return _secondaryResolvers.GetService(type, key) ?? _resolvers.GetService(type, key);
        }

        public virtual void AddSecondaryResolver(IDbDependencyResolver resolver)
        {
            DebugCheck.NotNull(resolver);

            _secondaryResolvers.Add(resolver);
        }

        public IEnumerable<object> GetServices(Type type, object key)
        {
            return _secondaryResolvers.GetServices(type, key).Concat(_resolvers.GetServices(type, key));
        }
    }
}
