// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration
{
    using System.ComponentModel;
    using System.Data.Entity.ModelConfiguration.Utilities;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;

    /// <summary>
    ///     Creates a convention that configures stored procedures to be used to update entities in the database.
    /// </summary>
    public class ConventionUpdateModificationFunctionConfiguration : ConventionModificationFunctionConfiguration
    {
        private readonly Type _type;

        internal ConventionUpdateModificationFunctionConfiguration(Type type)
        {
            DebugCheck.NotNull(type);

            _type = type;
        }

        /// <summary> Configures the name of the stored procedure. </summary>
        /// <returns> The same configuration instance so that multiple calls can be chained. </returns>
        /// <param name="procedureName"> The stored procedure name. </param>
        public ConventionUpdateModificationFunctionConfiguration HasName(string procedureName)
        {
            Check.NotEmpty(procedureName, "procedureName");

            Configuration.HasName(procedureName);

            return this;
        }

        /// <summary>Configures the name of the stored procedure.</summary>
        /// <returns> The same configuration instance so that multiple calls can be chained. </returns>
        /// <param name="procedureName">The stored procedure name.</param>
        /// <param name="schemaName">The schema name.</param>
        public ConventionUpdateModificationFunctionConfiguration HasName(string procedureName, string schemaName)
        {
            Check.NotEmpty(procedureName, "procedureName");
            Check.NotEmpty(schemaName, "schemaName");

            Configuration.HasName(procedureName, schemaName);

            return this;
        }

        /// <summary>Configures a parameter for this stored procedure.</summary>
        /// <returns> The same configuration instance so that multiple calls can be chained. </returns>
        /// <param name="propertyName"> The name of the property to configure the parameter for. </param>
        /// <param name="parameterName"> The name of the parameter. </param>
        public ConventionUpdateModificationFunctionConfiguration Parameter(string propertyName, string parameterName)
        {
            Check.NotEmpty(propertyName, "propertyName");
            Check.NotEmpty(parameterName, "parameterName");

            return Parameter(_type.GetProperty(propertyName), parameterName);
        }

        /// <summary>Configures a parameter for this stored procedure.</summary>
        /// <returns> The same configuration instance so that multiple calls can be chained. </returns>
        /// <param name="propertyName"> The property to configure the parameter for. </param>
        /// <param name="parameterName"> The name of the parameter. </param>
        public ConventionUpdateModificationFunctionConfiguration Parameter(
            PropertyInfo propertyInfo, string parameterName)
        {
            Check.NotEmpty(parameterName, "parameterName");

            if (propertyInfo != null)
            {
                Configuration.Parameter(new PropertyPath(propertyInfo), parameterName);
            }

            return this;
        }

        /// <summary>Configures a parameter for this stored procedure.</summary>
        /// <returns> The same configuration instance so that multiple calls can be chained. </returns>
        /// <param name="propertyExpression"> The name of the property to configure the parameter for. </param>
        /// <param name="currentValueParameterName">The current value parameter name.</param>
        /// <param name="originalValueParameterName">The original value parameter name.</param>
        public ConventionUpdateModificationFunctionConfiguration Parameter(
            string propertyName, string currentValueParameterName, string originalValueParameterName)
        {
            Check.NotEmpty(propertyName, "propertyName");
            Check.NotEmpty(currentValueParameterName, "currentValueParameterName");
            Check.NotEmpty(originalValueParameterName, "originalValueParameterName");

            return Parameter(_type.GetProperty(propertyName), currentValueParameterName, originalValueParameterName);
        }

        /// <summary>Configures a parameter for this stored procedure.</summary>
        /// <returns> The same configuration instance so that multiple calls can be chained. </returns>
        /// <param name="propertyExpression"> The property to configure the parameter for. </param>
        /// <param name="currentValueParameterName">The current value parameter name.</param>
        /// <param name="originalValueParameterName">The original value parameter name.</param>
        public ConventionUpdateModificationFunctionConfiguration Parameter(
            PropertyInfo propertyInfo, string currentValueParameterName, string originalValueParameterName)
        {
            Check.NotEmpty(currentValueParameterName, "currentValueParameterName");
            Check.NotEmpty(originalValueParameterName, "originalValueParameterName");

            if (propertyInfo != null)
            {
                Configuration.Parameter(
                    new PropertyPath(propertyInfo),
                    currentValueParameterName,
                    originalValueParameterName);
            }

            return this;
        }

        /// <summary>
        ///     Configures a column of the result for this stored procedure to map to a property.
        ///     This is used for database generated columns.
        /// </summary>
        /// <returns> The same configuration instance so that multiple calls can be chained. </returns>
        /// <param name="propertyName"> The name of the property to configure the result for. </param>
        /// <param name="columnName">The name of the result column.</param>
        public ConventionUpdateModificationFunctionConfiguration Result(string propertyName, string columnName)
        {
            Check.NotEmpty(propertyName, "propertyName");
            Check.NotEmpty(columnName, "columnName");

            Configuration.Result(new PropertyPath(_type.GetProperty(propertyName)), columnName);

            return this;
        }

        /// <summary>
        ///     Configures a column of the result for this stored procedure to map to a property.
        ///     This is used for database generated columns.
        /// </summary>
        /// <returns> The same configuration instance so that multiple calls can be chained. </returns>
        /// <param name="propertyName"> The property to configure the result for. </param>
        /// <param name="columnName">The name of the result column.</param>
        public ConventionUpdateModificationFunctionConfiguration Result(PropertyInfo propertyInfo, string columnName)
        {
            Check.NotNull(propertyInfo, "propertyInfo");
            Check.NotEmpty(columnName, "columnName");

            Configuration.Result(new PropertyPath(propertyInfo), columnName);

            return this;
        }

        /// <summary>Configures the output parameter that returns the rows affected by this stored procedure.</summary>
        /// <returns> The same configuration instance so that multiple calls can be chained. </returns>
        /// <param name="parameterName">The name of the parameter.</param>
        public ConventionUpdateModificationFunctionConfiguration RowsAffectedParameter(string parameterName)
        {
            Check.NotEmpty(parameterName, "parameterName");

            Configuration.RowsAffectedParameter(parameterName);

            return this;
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return base.ToString();
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Type GetType()
        {
            return base.GetType();
        }
    }
}
