﻿using System;

using umbraco.cms.businesslogic.datatype;
using umbraco.interfaces;

namespace uComponents.DataTypes.UserPicker
{
	/// <summary>
	/// Data Editor for the User Picker data type.
	/// </summary>
	public class UserPickerDataType : AbstractDataEditor
	{
		/// <summary>
		/// The control for the User Picker.
		/// </summary>
		private UserPickerControl m_Control = new UserPickerControl();

		/// <summary>
		/// The PreValue Editor for the data-type.
		/// </summary>
		private UserPickerPrevalueEditor m_PreValueEditor;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserPickerDataType"/> class.
		/// </summary>
		public UserPickerDataType()
		{
			// set the render control as the placeholder
			this.RenderControl = this.m_Control;

			// assign the initialise event for the control
			this.m_Control.Init += new EventHandler(this.m_Control_Init);

			// assign the save event for the data-type/editor
			this.DataEditorControl.OnSave += new AbstractDataEditorControl.SaveEventHandler(this.DataEditorControl_OnSave);
		}

		/// <summary>
		/// Gets the id of the data-type.
		/// </summary>
		/// <value>The id of the data-type.</value>
		public override Guid Id
		{
			get
			{
				return new Guid(DataTypeConstants.UserPickerId);
			}
		}

		/// <summary>
		/// Gets the name of the data type.
		/// </summary>
		/// <value>The name of the data type.</value>
		public override string DataTypeName
		{
			get
			{
				return "uComponents: User Picker";
			}
		}

		/// <summary>
		/// Gets the prevalue editor.
		/// </summary>
		/// <value>The prevalue editor.</value>
		public override IDataPrevalue PrevalueEditor
		{
			get
			{
				if (this.m_PreValueEditor == null)
				{
					this.m_PreValueEditor = new UserPickerPrevalueEditor(this);
				}

				return this.m_PreValueEditor;
			}
		}

		/// <summary>
		/// Handles the Init event of the control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void m_Control_Init(object sender, EventArgs e)
		{
			// get the prevalue options
			this.m_Control.Options = ((UserPickerPrevalueEditor)this.PrevalueEditor).GetPreValueOptions<UserPickerOptions>();

			// set the value of the control
			if (this.Data.Value != null)
			{
				this.m_Control.SelectedValue = this.Data.Value.ToString();
			}
			else
			{
				this.m_Control.SelectedValue = string.Empty;
			}
		}

		/// <summary>
		/// Datas the editor control_ on save.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void DataEditorControl_OnSave(EventArgs e)
		{
			this.Data.Value = this.m_Control.SelectedValue;
		}
	}
}
