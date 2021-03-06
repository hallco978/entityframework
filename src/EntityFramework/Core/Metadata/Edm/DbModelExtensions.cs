﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Utilities;

    /// <summary>
    /// Extension methods for <see cref="DbModel"/>.
    /// </summary>
    public static class DbModelExtensions
    {
        /// <summary>
        /// Gets the conceptual model from the specified DbModel.
        /// </summary>
        /// <param name="model">An instance of a class that implements IEdmModelAdapter (ex. DbModel).</param>
        /// <returns>An instance of EdmModel that represents the conceptual model.</returns>
        public static EdmModel GetConceptualModel(this IEdmModelAdapter model)
        {
            Check.NotNull(model, "model");

            return model.ConceptualModel;
        }

        /// Gets the store model from the specified DbModel.
        /// </summary>
        /// <param name="model">An instance of a class that implements IEdmModelAdapter (ex. DbModel).</param>
        /// <returns>An instance of EdmModel that represents the store model.</returns>
        public static EdmModel GetStoreModel(this IEdmModelAdapter model)
        {
            Check.NotNull(model, "model");

            return model.StoreModel;
        }
    }
}
