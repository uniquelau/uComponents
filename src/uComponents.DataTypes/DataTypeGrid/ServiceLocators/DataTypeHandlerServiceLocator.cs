﻿// --------------------------------------------------------------------------------------------------------------------
// <summary>
// 23.05.2012 - Created [Ove Andersen]
// 09.02.2013 - Rewritten [Ove Andersen]
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace uComponents.DataTypes.DataTypeGrid.ServiceLocators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.UI;

    using uComponents.Core;
    using uComponents.DataTypes.DataTypeGrid.Handlers.DataTypes;
    using uComponents.DataTypes.DataTypeGrid.Interfaces;
    using uComponents.DataTypes.DataTypeGrid.Model;

    using umbraco.interfaces;

    internal class DataTypeHandlerServiceLocator : IDataTypeHandlerServiceLocator
    {
        /// <summary>
        /// The default instance.
        /// </summary>
        private static readonly DataTypeHandlerServiceLocator DefaultInstance = new DataTypeHandlerServiceLocator();

        /// <summary>
        /// All registered datatype factories
        /// </summary>
        private IList<Type> dataTypeFactories;

        /// <summary>
        /// Prevents a default instance of the <see cref="DataTypeHandlerServiceLocator"/> class from being created.
        /// </summary>
        private DataTypeHandlerServiceLocator()
        {
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static DataTypeHandlerServiceLocator Instance
        {
            get
            {
                return DefaultInstance;
            }
        }

        /// <summary>
        /// Method for customizing the way the <see cref="IDataType" /> value is displayed in the grid.
        /// </summary>
        /// <param name="dataType">The <see cref="IDataType" /> instance.</param>
        /// <returns>The display value.</returns>
        /// <remarks>Called when the grid displays the cell value for the specified <see cref="IDataType" />.</remarks>
        public string GetDisplayValue(IDataType dataType)
        {
            var f = this.GetDataTypeFactory(dataType);

            try
            {
                var v = f.GetType().GetMethod("GetDisplayValue").Invoke(f, new object[] { dataType });

                return v.ToString();
            }
            catch (Exception ex)
            {
                Helper.Log.Error<DataType>(
                    string.Format(
                        "An error occured when getting the display value for the DataType {{ Id: {0}, Type: {1}, Name: {2}, Data: {3} }}.",
                        dataType.Id,
                        dataType.DataTypeDefinitionId,
                        dataType.DataTypeName,
                        dataType.Data.Value),
                    ex);

                return dataType.Data.Value != null ? dataType.Data.Value.ToString() : ex.Message;
            }
        }

        /// <summary>
        /// Method for getting the backing object for the specified <see cref="IDataType" />.
        /// </summary>
        /// <typeparam name="TBackingObjectType">The backing object type.</typeparam>
        /// <param name="dataType">The <see cref="IDataType" /> instance.</param>
        /// <returns>The backing object.</returns>
        /// <remarks>Called when the method <see cref="GridCell.GetPropertyValue{TBackingObjectType}()" /> method is called on a <see cref="GridCell" />.</remarks>
        public TBackingObjectType GetPropertyValue<TBackingObjectType>(IDataType dataType)
        {
            var f = this.GetDataTypeFactory(dataType);

            try
            {
                var m = f.GetType().GetMethods().Where(x => x.Name == "GetPropertyValue").First(x => x.GetGenericArguments().Count() == 1);
                var v = m.MakeGenericMethod(typeof(TBackingObjectType)).Invoke(f, new object[] { dataType });

                return v is TBackingObjectType ? (TBackingObjectType)v : default(TBackingObjectType);
            }
            catch (Exception ex)
            {
                var m =
                    string.Format(
                        "An error occured when getting the property value for the DataType {{ Id: {0}, Type: {1}, Name: {2}, Data: {3} }}.",
                        dataType.Id,
                        dataType.DataTypeDefinitionId,
                        dataType.DataTypeName,
                        dataType.Data.Value);

                Helper.Log.Error<DataType>(m, ex);

                throw new Exception(m, ex);
            }
        }

        /// <summary>
        /// Method for getting the backing object for the specified <see cref="IDataType" />.
        /// </summary>
        /// <param name="dataType">The <see cref="IDataType" /> instance.</param>
        /// <returns>The backing object.</returns>
        /// <remarks>Called when the method <see cref="GridCell.GetPropertyValue()" /> method is called on a <see cref="GridCell" />.</remarks>
        public object GetPropertyValue(IDataType dataType)
        {
            var f = this.GetDataTypeFactory(dataType);

            try
            {
                var v = f.GetType().GetMethod("GetPropertyValue").Invoke(f, new object[] { dataType });

                return v;
            }
            catch (Exception ex)
            {
                Helper.Log.Error<DataType>(
                    string.Format(
                        "An error occured when getting the property value for the DataType {{ Id: {0}, Type: {1}, Name: {2}, Data: {3} }}.",
                        dataType.Id,
                        dataType.DataTypeDefinitionId,
                        dataType.DataTypeName,
                        dataType.Data.Value),
                    ex);

                return dataType.Data.Value != null ? dataType.Data.Value.ToString() : ex.Message;
            }
        }

        /// <summary>
        /// Method for getting the control to use when validating the specified <see cref="IDataType" />.
        /// </summary>
        /// <param name="dataType">The <see cref="IDataType" /> instance.</param>
        /// <param name="editorControl">The <see cref="IDataType" /> editor control.</param>
        /// <returns>The control to validate.</returns>
        public Control GetControlToValidate(IDataType dataType, Control editorControl)
        {
            var f = this.GetDataTypeFactory(dataType);

            var v = f.GetType().GetMethod("GetControlToValidate").Invoke(f, new object[] { dataType, editorControl });

            return (Control)v;
        }

        /// <summary>
        /// Method for performing special actions <b>before</b> creating the <see cref="IDataType" /> editor.
        /// </summary>
        /// <param name="dataType">The <see cref="IDataType" /> instance.</param>
        /// <param name="eventArgs">The <see cref="DataTypeLoadEventArgs"/> instance containing the event data.</param>
        /// <remarks>Called <b>before</b> the grid creates the editor controls for the specified <see cref="IDataType" />.</remarks>
        public void Initialize(IDataType dataType, DataTypeLoadEventArgs eventArgs)
        {
            var f = this.GetDataTypeFactory(dataType);

            f.GetType().GetMethod("Initialize").Invoke(f, new object[] { dataType, eventArgs });
        }

        /// <summary>
        /// Method for performing special actions <b>after</b> the <see cref="IDataType" /> <see cref="IDataEditor">editor</see> has been loaded.
        /// </summary>
        /// <param name="dataType">The <see cref="IDataType" /> instance.</param>
        /// <param name="eventArgs">The <see cref="DataTypeLoadEventArgs"/> instance containing the event data.</param>
        /// <remarks>Called <b>after</b> the grid creates the editor controls for the specified <see cref="IDataType" />.</remarks>
        public void Configure(IDataType dataType, DataTypeLoadEventArgs eventArgs)
        {
            var f = this.GetDataTypeFactory(dataType);

            f.GetType().GetMethod("Configure").Invoke(f, new object[] { dataType, eventArgs });
        }

        /// <summary>
        /// Method for executing special actions before saving the editor value to the database.
        /// </summary>
        /// <param name="dataType">The <see cref="IDataType" /> instance.</param>
        /// <param name="eventArgs">The <see cref="DataTypeSaveEventArgs" /> instance containing the event data.</param>
        /// <remarks>Called when the grid is saved for the specified <see cref="IDataType" />.</remarks>
        public void Save(IDataType dataType, DataTypeSaveEventArgs eventArgs)
        {
            var f = this.GetDataTypeFactory(dataType);

            f.GetType().GetMethod("Save").Invoke(f, new object[] { dataType, eventArgs });
        }

        /// <summary>
        /// Gets the factory for the specified <typeparamref name="TDataType">datatype</typeparamref>.
        /// </summary>
        /// <typeparam name="TDataType">The <typeparamref name="TDataType">datatype</typeparamref>.</typeparam>
        /// <param name="dataType">Type of the data.</param>
        /// <returns>A datatype factory.</returns>
        private object GetDataTypeFactory<TDataType>(TDataType dataType) where TDataType : IDataType
        {
            // Get factories implementing IDataTypeHandler and the specified datatype
            var factories =
                this.GetDataTypeFactories()
                    .Where(
                        f =>
                        f.GetInterfaces()
                         .Any(
                             i =>
                             i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDataTypeHandler<>)
                             && i.GetGenericArguments().Any(t => t == dataType.GetType())));

            if (factories.Any())
            {
                // Get the factory with the highest priority
                var f = factories.OrderBy(x => this.GetDataTypeFactoryAttribute(x).Priority).FirstOrDefault();

                if (f != null)
                {
                    return Activator.CreateInstance(f);
                }
            }

            return new DefaultDataTypeHandler();
        }

        /// <summary>
        /// Gets the <see cref="DataTypeHandlerAttribute">datatype factory attribute</see> for the specified type.
        /// </summary>
        /// <param name="factory">The <see cref="IDataTypeHandler{TDataType}">datatype factory type</see>.</param>
        /// <returns>The <see cref="DataTypeHandlerAttribute">datatype factory attribute</see>.</returns>
        private DataTypeHandlerAttribute GetDataTypeFactoryAttribute(Type factory)
        {
            var attribute =
                factory.GetCustomAttributes(typeof(DataTypeHandlerAttribute), false)
                    .FirstOrDefault() as DataTypeHandlerAttribute;
            
            return attribute ?? new DataTypeHandlerAttribute();
        }

        /// <summary>
        /// Gets all datatype factories.
        /// </summary>
        /// <returns>A list of datatype factories.</returns>
        private IEnumerable<Type> GetDataTypeFactories()
        {
            if (this.dataTypeFactories == null || !this.dataTypeFactories.Any())
            {
                var factories = new List<Type>();

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            foreach (
                                var i in
                                    type.GetInterfaces()
                                        .Where(
                                            i =>
                                                i.IsGenericType
                                                && i.GetGenericTypeDefinition() == typeof(IDataTypeHandler<>)))
                            {
                                factories.Add(type);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Helper.Log.Error<DataType>(string.Format("Unable to load types for assembly '{0}'", assembly.FullName), ex);
                    }
                }

                this.dataTypeFactories = factories;
            }

            return this.dataTypeFactories;
        }
    }
}