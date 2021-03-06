﻿// --------------------------------------------------------------------------------------------------------------------
// <summary>
// 11.08.2011 - Created [Ove Andersen]
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml;
using uComponents.Core;
using uComponents.DataTypes.DataTypeGrid.Model;

using umbraco.cms.businesslogic.datatype;
using umbraco.interfaces;

[assembly: WebResource("uComponents.DataTypes.DataTypeGrid.Css.DTG_DataEditor.css", Constants.MediaTypeNames.Text.Css, PerformSubstitution = true)]
[assembly: WebResource("uComponents.DataTypes.DataTypeGrid.Scripts.jquery.dataTables.min.js", Constants.MediaTypeNames.Application.JavaScript)]
[assembly: WebResource("uComponents.DataTypes.DataTypeGrid.Scripts.DTG_DataEditor.js", Constants.MediaTypeNames.Application.JavaScript)]

namespace uComponents.DataTypes.DataTypeGrid
{
	using System.Web;
	using uComponents.DataTypes.DataTypeGrid.Constants;
	using uComponents.DataTypes.DataTypeGrid.Extensions;
	using uComponents.DataTypes.DataTypeGrid.Handlers;
	using uComponents.DataTypes.DataTypeGrid.Interfaces;
	using uComponents.DataTypes.DataTypeGrid.ServiceLocators;
	using uComponents.DataTypes.DataTypeGrid.Validators;
	using umbraco;

	/// <summary>
	/// The DataType Grid Control
	/// </summary>
	[ClientDependency.Core.ClientDependency(ClientDependency.Core.ClientDependencyType.Javascript, "ui/jqueryui.js", "UmbracoClient")]
	[ClientDependency.Core.ClientDependency(ClientDependency.Core.ClientDependencyType.Javascript, "controls/Images/ImageViewer.js", "UmbracoRoot")]
	public class DataEditor : CompositeControl, IDataEditor
	{
		#region Fields

		/// <summary>
		/// Value stored by a datatype instance
		/// </summary>
		private readonly IData data;

		/// <summary>
		/// The datatype definition id
		/// </summary>
		private readonly int dataTypeDefinitionId;

		/// <summary>
		/// The settings.
		/// </summary>
		private readonly PreValueEditorSettings settings;

		/// <summary>
		/// The unique instance id
		/// </summary>
		private readonly string instanceId;

		/// <summary>
		/// Gets the control id.
		/// </summary>
		private string id;
		
		/// <summary>
		/// The prevalue editor settings handler
		/// </summary>
		private readonly IPrevalueEditorSettingsHandler prevalueEditorSettingsHandler;

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="DataEditor"/> class.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="settings">The settings.</param>
		/// <param name="dataTypeDefinitionId">The data type definition id.</param>
		/// <param name="instanceId">The instance id.</param>
		public DataEditor(IData data, PreValueEditorSettings settings, int dataTypeDefinitionId, string instanceId)
		{
			// Set up dependencies
			this.prevalueEditorSettingsHandler = new PrevalueEditorSettingsHandler();
			this.settings = settings;
			this.data = data;

			this.dataTypeDefinitionId = dataTypeDefinitionId;
			this.instanceId = instanceId;
		}

		#region Properties

		/// <summary>
		/// Gets the configuration.
		/// </summary>
        public StoredValueRowCollection Rows { get; private set; }

		/// <summary>
		/// Gets the grid.
		/// </summary>
		public Table Grid { get; private set; }

		/// <summary>
		/// Gets the grid.
		/// </summary>
		public Panel Toolbar { get; private set; }

		/// <summary>
		/// Gets the insert controls.
		/// </summary>
		public Panel InsertControls { get; private set; }

		/// <summary>
		/// Gets the edit controls.
		/// </summary>
		public Panel EditControls { get; private set; }

		/// <summary>
		/// Gets the delete controls.
		/// </summary>
		public Panel DeleteControls { get; private set; }

		/// <summary>
		/// Gets or sets whether to show the grid header.
		/// </summary>
		/// <value>
		/// Whether to show the header.
		/// </value>
		public HiddenField ShowGridHeader { get; set; }

		/// <summary>
		/// Gets or sets whether to show the footer.
		/// </summary>
		/// <value>
		/// Whether to show the footer.
		/// </value>
		public HiddenField ShowGridFooter { get; set; }

        /// <summary>
        /// Gets or sets whether the grid is read only.
        /// </summary>
        /// <value>Whether the grid is read only.</value>
        public HiddenField ReadOnly { get; set; }

		/// <summary>
		/// Gets or sets the number of rows per page.
		/// </summary>
		/// <value>The number of rows per page.</value>
		public HiddenField TableHeight { get; set; }

		/// <summary>
		/// Gets or sets the value control.
		/// </summary>
		/// <value>The value control.</value>
		public HiddenField Value { get; set; }

		/// <summary>
		/// Gets the datatables translation.
		/// </summary>
		/// <value>The datatables translation.</value>
		public LiteralControl DataTablesTranslation { get; private set; }

		/// <summary>
		/// Gets or sets the programmatic identifier assigned to the server control.
		/// </summary>
		/// <value></value>
		/// <returns>The programmatic identifier assigned to the control.</returns>
		public override string ID
		{
			get
			{
				return this.id ?? (this.id = string.Concat("DTG_", this.dataTypeDefinitionId, "_", this.instanceId));
			}
		}
		
		/// <summary>
		/// Gets or sets the column configurations.
		/// </summary>
		/// <value>The column configurations.</value>
		public IEnumerable<PreValueRow> ColumnConfigurations { get; set; }

		/// <summary>
		/// Gets or sets the insert data types
		/// </summary>
		/// <value>The insert data types.</value>
        private IEnumerable<StoredValue> InsertDataTypes { get; set; }

		/// <summary>
		/// Gets or sets the edit data types.
		/// </summary>
		/// <value>The edit data types.</value>
        private IEnumerable<StoredValue> EditDataTypes { get; set; }

		/// <summary>
		/// Gets or sets the current row.
		/// </summary>
		/// <value>The current row.</value>
		private int CurrentRow
		{
			get
			{
				if (this.ViewState["CurrentRow"] != null)
				{
					return (int)this.ViewState["CurrentRow"];
				}

				return 0;
			}

			set
			{
				this.ViewState["CurrentRow"] = value;
			}
		}

		/// <summary>
		/// Gets or sets the data string.
		/// </summary>
		/// <value>The data string.</value>
		private string DataString
		{
			get
			{
				if (this.Value != null && !string.IsNullOrEmpty(this.Value.Value))
				{
					Helper.Log.Debug<DataType>(string.Format("DTG: Returned value from ViewState: {0}", this.Value.Value));

					return this.Value.Value;
				}

				Helper.Log.Warn<DataType>(string.Format("DTG: ViewState did not contain data."));

				return string.Empty;
			}

			set
			{
                // TODO: Parse value and fix sortorder
				Helper.Log.Debug<DataType>(string.Format("DTG: Stored the following data in ViewState: {0}", value));

				this.Value.Value = value;
			}
		}

		#endregion

		#region IDataEditor Members

		/// <summary>
		/// Saves this instance.
		/// </summary>
		public void Save()
		{
			// Get new values
            this.Rows = new StoredValueRowCollection(this.ColumnConfigurations, this.DataString);

            this.data.Value = this.Rows.ToString();

			// Refresh grid
			this.RefreshGrid();

			// Clear input controls
			this.ClearControls();

			Helper.Log.Debug<DataEditor>(string.Format("DTG: Saved the following data to database: {0}", this.data.Value));
		}

		/// <summary>
		/// Stores this instance temporarily.
		/// </summary>
		public void Store()
		{
			// Save values
			DataString = this.Rows.ToString();

			// Refresh grid
			RefreshGrid();

			// Clear input controls
			ClearControls();
		}

		/// <summary>
		/// Gets a value indicating whether [show label].
		/// </summary>
		/// <value>
		///   <c>true</c> if [show label]; otherwise, <c>false</c>.
		/// </value>
		public virtual bool ShowLabel
		{
			get
			{
				return this.settings.ShowLabel;
			}
		}

		/// <summary>
		/// Gets a value indicating whether [treat as rich text editor].
		/// </summary>
		/// <value>
		/// 	<c>true</c> if [treat as rich text editor]; otherwise, <c>false</c>.
		/// </value>
		public virtual bool TreatAsRichTextEditor
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Gets the editor.
		/// </summary>
		public Control Editor
		{
			get
			{
				return this;
			}
		}

		#endregion

		#region Functions

		/// <summary>
		/// Refreshes the grid.
		/// </summary>
		private void RefreshGrid()
		{
			// Remove all rows
			Grid.Rows.Clear();

			// Re-add rows
			GenerateHeaderRow();
			GenerateValueRows();
		}

		/// <summary>
		/// The clear controls.
		/// </summary>
		private void ClearControls()
		{
			InsertDataTypes = GetInsertDataTypes();
			GenerateInsertControls();

			CurrentRow = 0;

			EditDataTypes = GetEditDataTypes();
			GenerateEditControls();
		}

		/// <summary>
		/// Generates the header row.
		/// </summary>
		private void GenerateHeaderRow()
		{
			var tr = new TableRow { TableSection = TableRowSection.TableHeader };

			// Add ID header cell
			tr.Cells.Add(new TableHeaderCell { CssClass = "id", Text = Helper.Dictionary.GetDictionaryItem("ID", "ID") });

			tr.Cells.Add(new TableHeaderCell { CssClass = "actions", Text = Helper.Dictionary.GetDictionaryItem("Actions", "Actions") });

			// Add prevalue cells
			foreach (var s in this.ColumnConfigurations.Where(x => x.Visible))
			{
				var th = new TableHeaderCell { Text = s.Name };

				// If the name starts with a hash, get the dictionary item
				if (s.Name.StartsWith("#"))
				{
					var key = s.Name.Substring(1, s.Name.Length - 1);

					th.Text = uQuery.GetDictionaryItem(key, key);
				}

				tr.Cells.Add(th);
			}

			Grid.Rows.Add(tr);
		}

		/// <summary>
		/// Generates the value rows.
		/// </summary>
		private void GenerateValueRows()
		{
			foreach (var row in this.Rows.OrderBy(x => x.SortOrder))
			{
				var tr = new TableRow();
				tr.Attributes.Add("data-dtg-rowid", row.Id.ToString());

				// Add ID column
				var id = new TableCell { CssClass = "id" };
				id.Controls.Add(new LiteralControl(row.Id.ToString()));

				tr.Cells.Add(id);

				var actions = new TableCell() { CssClass = "actions" };

			    if (!this.settings.ReadOnly)
			    {
                    // Delete button
			        var dInner = new HtmlGenericControl("span");
			        dInner.Attributes["class"] = "ui-button-text";
			        dInner.InnerText = Helper.Dictionary.GetDictionaryItem("Delete", "Delete");

			        var dIcon = new HtmlGenericControl("span");
			        dIcon.Attributes["class"] = "ui-button-icon-primary ui-icon ui-icon-close";

			        var deleteRow = new LinkButton
			                            {
			                                ID = "DeleteButton_" + row.Id,
			                                CssClass =
			                                    "deleteRowDialog ui-button ui-widget ui-state-default ui-corner-all ui-button-icon-only",
			                                CommandArgument = row.Id.ToString(),
			                                OnClientClick = "return confirm('Are you sure you want to delete this?')"
			                            };
			        deleteRow.Click += deleteRow_Click;

			        deleteRow.Controls.Add(dIcon);
			        deleteRow.Controls.Add(dInner);

			        // Edit button
			        var eInner = new HtmlGenericControl("span");
			        eInner.Attributes["class"] = "ui-button-text";
			        eInner.InnerText = Helper.Dictionary.GetDictionaryItem("Edit", "Edit");

			        var eIcon = new HtmlGenericControl("span");
			        eIcon.Attributes["class"] = "ui-button-icon-primary ui-icon ui-icon-pencil";

			        var editRow = new LinkButton
			                          {
			                              ID = "EditButton_" + row.Id,
			                              CssClass =
			                                  "editRowDialog ui-button ui-widget ui-state-default ui-corner-all ui-button-icon-only",
			                              CommandArgument = row.Id.ToString()
			                          };
			        editRow.Click += this.editRow_Click;

			        editRow.Controls.Add(eIcon);
			        editRow.Controls.Add(eInner);

			        actions.Controls.Add(deleteRow);
			        actions.Controls.Add(editRow);
			    }

			    tr.Cells.Add(actions);

				// Print stored values
                foreach (var storedConfig in this.ColumnConfigurations.Where(x => x.Visible))
				{
					var td = new TableCell();

					foreach (var value in row.Cells)
					{
						var text = new Label { Text = DataTypeHandlerServiceLocator.Instance.GetDisplayValue(value.Value) };

						if (value.Name.Equals(storedConfig.Name))
						{
							td.Controls.Add(text);
						}
					}

					tr.Cells.Add(td);
				}

				Grid.Rows.Add(tr);
			}
		}

		/// <summary>
		/// Generates the footer row.
		/// </summary>
		private void GenerateFooterToolbar()
		{
			var inner = new HtmlGenericControl("span") { InnerText = Helper.Dictionary.GetDictionaryItem("Add", "Add") };
			inner.Attributes["class"] = "ui-button-text";

			var icon = new HtmlGenericControl("span");
			icon.Attributes["class"] = "ui-button-icon-primary ui-icon ui-icon-plus";

			var addRowDialog = new LinkButton
			{
				ID = "InsertRowDialog",
				CssClass =
					"insertRowDialog ui-button ui-widget ui-state-default ui-corner-all ui-button-text-icon-primary"
			};
			addRowDialog.Click += this.addRowDialog_Click;

			addRowDialog.Controls.Add(icon);
			addRowDialog.Controls.Add(inner);

			Toolbar.Controls.Add(addRowDialog);
		}

		/// <summary>
		/// Generates the validation controls.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="name">The name.</param>
		/// <param name="config">The config.</param>
		/// <param name="list">The list.</param>
        private void GenerateValidationControls(Control parent, string name, StoredValue config, IEnumerable<StoredValue> list)
		{
			var control = parent.FindControl(config.Value.DataEditor.Editor.ID);

			// If the name starts with a hash, get the dictionary item
			if (config.Name.StartsWith("#"))
			{
				var key = config.Name.Substring(1, config.Name.Length - 1);

				config.Name = uQuery.GetDictionaryItem(key, key);
			}

			// Mandatory
			if (this.ColumnConfigurations.Single(x => x.Alias == config.Alias).Mandatory && control != null)
			{
				try
				{
					var wrapper = new Panel();

					var validator = new ClientSideRequiredFieldValidator(name, config, false);

					wrapper.Controls.Add(validator);
					parent.Controls.Add(wrapper);
				}
				catch (Exception ex)
				{
					HttpContext.Current.Trace.Warn("DataTypeGrid", "EditorControl (" + config.Value.DataTypeName + ") does not support validation", ex);
				}
			}

			// Regex
			if (!string.IsNullOrEmpty(this.ColumnConfigurations.First(x => x.Alias == config.Alias).ValidationExpression) && control != null)
			{
				try
				{
					var wrapper = new Panel();

					var regex = new Regex(this.ColumnConfigurations.First(x => x.Alias == config.Alias).ValidationExpression);
					var validator = new ClientSideRegexValidator(name, config, false)
										{
											ValidationExpression = regex.ToString()
										};

					wrapper.Controls.Add(validator);
					parent.Controls.Add(wrapper);
				}
				catch (ArgumentException ex)
				{
					parent.Controls.Add(
						new HtmlGenericControl("span")
						{
							InnerText =
								string.Concat(
									"Regex validation expression is invalid. Validation will not occur.",
									"<!-- ",
									ex,
									" -->")
						});
				}
			}
		}

		/// <summary>
		/// Generates the insert controls.
		/// </summary>
		private void GenerateInsertControls()
		{
			InsertControls.Controls.Clear();

			InsertControls.Controls.Add(new LiteralControl("<ul class='controls'>"));

			foreach (var config in InsertDataTypes)
			{
				var control = config.Value.DataEditor.Editor;
				control.ID = "insert" + config.Alias;

				// Initialize the datatype so it works with DTG
				DataTypeHandlerServiceLocator.Instance.Initialize(config.Value, new DataTypeLoadEventArgs(this, this.InsertControls));
				config.Value.DataEditor.Editor.Load +=
					(sender, args) =>
					DataTypeHandlerServiceLocator.Instance.Configure(config.Value, new DataTypeLoadEventArgs(this, this.InsertControls));

				InsertControls.Controls.Add(new LiteralControl("<li class='control'>"));

				var title = new Label() { CssClass = "control-label", Text = config.Name };

				// If the name starts with a hash, get the dictionary item
				if (config.Name.StartsWith("#"))
				{
					var key = config.Name.Substring(1, config.Name.Length - 1);

					title.Text = uQuery.GetDictionaryItem(key, key);
				}

				this.InsertControls.Controls.Add(title);

				InsertControls.Controls.Add(control);
				GenerateValidationControls(InsertControls, "insert", config, InsertDataTypes);

				InsertControls.Controls.Add(new LiteralControl("</li>"));
			}

			InsertControls.Controls.Add(new LiteralControl("</ul>"));

			var iInner = new HtmlGenericControl("span") { InnerText = Helper.Dictionary.GetDictionaryItem("Add", "Add") };
			iInner.Attributes["class"] = "ui-button-text";

			var iIcon = new HtmlGenericControl("span");
			iIcon.Attributes["class"] = "ui-button-icon-primary ui-icon ui-icon-plus";

			var addRow = new LinkButton
			{
				ID = "InsertButton",
				CssClass =
					"insertButton ui-button ui-widget ui-state-default ui-corner-all ui-button-text-icon-primary"
			};
			addRow.Click += addRow_Click;

			addRow.Controls.Add(iIcon);
			addRow.Controls.Add(iInner);

			this.InsertControls.Controls.Add(addRow);
		}

		/// <summary>
		/// Handles the Click event of the addRow control.
		/// </summary>
		/// <param name="sender">
		/// The source of the event.
		/// </param>
		/// <param name="e">
		/// The <see cref="System.EventArgs"/> instance containing the event data.
		/// </param>
		protected void addRow_Click(object sender, EventArgs e)
		{
			var row = new StoredValueRow { Id = this.GetAvailableId(), SortOrder = this.Rows.Count() + 1 };

			foreach (var t in this.InsertDataTypes)
			{
				// Save value to datatype
				DataTypeHandlerServiceLocator.Instance.Save(t.Value, new DataTypeSaveEventArgs(this, DataTypeAction.Add));

				// Create new storedvalue object
				var v = new StoredValue { Name = t.Name, Alias = t.Alias, Value = t.Value };

				row.Cells.Add(v);
			}

			this.Rows.Add(row);

			Store();
			Save();
		}

		/// <summary>
		/// Generates the edit controls.
		/// </summary>
		private void GenerateEditControls()
		{
			this.EditControls.Controls.Clear();

			this.EditControls.Controls.Add(new LiteralControl("<ul class='controls'>"));

			foreach (var config in this.EditDataTypes)
			{
				var control = config.Value.DataEditor.Editor;
				control.ID = "Edit" + config.Alias;

				// Initialize the datatype so it works with DTG
				DataTypeHandlerServiceLocator.Instance.Initialize(config.Value, new DataTypeLoadEventArgs(this, this.EditControls));
				config.Value.DataEditor.Editor.Load +=
					(sender, args) =>
					DataTypeHandlerServiceLocator.Instance.Configure(config.Value, new DataTypeLoadEventArgs(this, this.EditControls));

				this.EditControls.Controls.Add(new LiteralControl("<li class='control'>"));

				var title = new Label() { CssClass = "control-label", Text = config.Name };

				// If the name starts with a hash, get the dictionary item
				if (config.Name.StartsWith("#"))
				{
					var key = config.Name.Substring(1, config.Name.Length - 1);

					title.Text = uQuery.GetDictionaryItem(key, key);
				}

				this.EditControls.Controls.Add(title);

				this.EditControls.Controls.Add(control);
				this.GenerateValidationControls(this.EditControls, "edit", config, this.EditDataTypes);

				this.EditControls.Controls.Add(new LiteralControl("</li>"));
			}

			this.EditControls.Controls.Add(new LiteralControl("</ul>"));

			var uInner = new HtmlGenericControl("span") { InnerText = Helper.Dictionary.GetDictionaryItem("Update", "Update") };
			uInner.Attributes["class"] = "ui-button-text";

			var uIcon = new HtmlGenericControl("span");
			uIcon.Attributes["class"] = "ui-button-icon-primary ui-icon ui-icon-pencil";

			var updateRow = new LinkButton
			{
				ID = "UpdateButton",
				CssClass =
					"updateButton ui-button ui-widget ui-state-default ui-corner-all ui-button-text-icon-primary"
			};
			updateRow.Click += this.updateRow_Click;

			updateRow.Controls.Add(uIcon);
			updateRow.Controls.Add(uInner);

			this.EditControls.Controls.Add(updateRow);
		}

		/// <summary>
		/// Handles the Click event of the editRow control.
		/// </summary>
		/// <param name="sender">
		/// The source of the event.
		/// </param>
		/// <param name="e">
		/// The <see cref="System.EventArgs"/> instance containing the event data.
		/// </param>
		protected void editRow_Click(object sender, EventArgs e)
		{
			this.CurrentRow = int.Parse(((LinkButton)sender).CommandArgument);

			this.EditDataTypes = this.GetEditDataTypes();
			this.GenerateEditControls();

			ScriptManager.RegisterClientScriptBlock(
				this,
				this.GetType(),
				"OpenEditDialog_" + this.ID,
				"$(function() {$('#" + this.ClientID + "_ctrlEdit').uComponents().datatypegrid('openDialog'); });",
				true);
		}

		/// <summary>
		/// Handles the Click event of the addRowDialog control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void addRowDialog_Click(object sender, EventArgs e)
		{
			this.ClearControls();

			ScriptManager.RegisterClientScriptBlock(
				this,
				this.GetType(),
				"OpenInsertDialog_" + this.ID,
				"$(function() {$('#" + this.ClientID + "_ctrlInsert').uComponents().datatypegrid('openDialog'); });",
				true);
		}

		/// <summary>
		/// Handles the Click event of the updateRow control.
		/// </summary>
		/// <param name="sender">
		/// The source of the event.
		/// </param>
		/// <param name="e">
		/// The <see cref="System.EventArgs"/> instance containing the event data.
		/// </param>
		protected void updateRow_Click(object sender, EventArgs e)
		{
			foreach (var row in this.Rows.Where(row => row.Id == this.CurrentRow))
			{
				foreach (var cell in row.Cells)
				{
					// Save value to datatype
					DataTypeHandlerServiceLocator.Instance.Save(cell.Value, new DataTypeSaveEventArgs(this, DataTypeAction.Update));
				}
			}

			this.Store();
			this.Save();
		}

		/// <summary>
		/// Generates the delete controls.
		/// </summary>
		/// <param name="rowId">
		/// The row Id.
		/// </param>
		private void GenerateDeleteControls(Guid rowId)
		{
		}

		/// <summary>
		/// Handles the Click event of the deleteRow control.
		/// </summary>
		/// <param name="sender">
		/// The source of the event.
		/// </param>
		/// <param name="e">
		/// The <see cref="System.EventArgs"/> instance containing the event data.
		/// </param>
		protected void deleteRow_Click(object sender, EventArgs e)
		{
			var rowToDelete = new StoredValueRow();
			foreach (
				StoredValueRow row in Rows.Where(row => row.Id.ToString().Equals(((LinkButton)sender).CommandArgument)))
			{
				rowToDelete = row;
			}

			Rows.Remove(rowToDelete);

			Store();
			Save();
		}

		/// <summary>
		/// Gets the insert data types.
		/// </summary>
		/// <returns>
		/// </returns>
        private IEnumerable<StoredValue> GetInsertDataTypes()
		{
			var list = new List<StoredValue>();

			foreach (var config in this.ColumnConfigurations)
			{
				var dtd = DataTypeDefinition.GetDataTypeDefinition(config.DataTypeId);
				var dt = dtd.DataType;

				var s = new StoredValue { Name = config.Name, Alias = config.Alias, Value = dt };

				list.Add(s);
			}

			return list;
		}

		/// <summary>
		/// The get edit data types.
		/// </summary>
		/// <returns>
		/// </returns>
        private IEnumerable<StoredValue> GetEditDataTypes()
		{
			var list = new List<StoredValue>();

			if (this.CurrentRow > 0)
			{
				list = this.GetStoredValueRow(this.CurrentRow).Cells;
			}
			else
			{
				foreach (var config in this.ColumnConfigurations)
				{
					var dtd = DataTypeDefinition.GetDataTypeDefinition(config.DataTypeId);
					var dt = dtd.DataType;

					var s = new StoredValue { Name = config.Name, Alias = config.Alias, Value = dt };

					list.Add(s);
				}
			}

			return list;
		}

		/// <summary>
		/// The get stored value row.
		/// </summary>
		/// <param name="id">
		/// The id.
		/// </param>
		/// <returns>
		/// </returns>
		private StoredValueRow GetStoredValueRow(int id)
		{
			foreach (var row in this.Rows.Where(row => row.Id == id))
			{
				return row;
			}

			return new StoredValueRow();
		}

		/// <summary>
		/// Gets an available id.
		/// </summary>
		/// <returns>
		/// The get available id.
		/// </returns>
		public int GetAvailableId()
		{
			var newId = 1;

			foreach (StoredValueRow row in Rows)
			{
				if (newId <= row.Id)
				{
					newId = row.Id + 1;
				}
			}

			return newId;
		}

		/// <summary>
		/// Gets the datatables translation.
		/// </summary>
		/// <returns>The translation.</returns>
		private string GetDataTablesTranslation()
		{
			var translation =
				string.Format(
					@"<script type=""text/javascript"">$.fn.uComponents().dictionary().dataTablesTranslation = {{""sEmptyTable"":""{0}"",""sInfo"":""{1}"",""sInfoEmpty"":""{2}"",""sInfoFiltered"":""{3}"",""sInfoPostFix"":""{4}"",""sInfoThousands"":""{5}"",""sLengthMenu"":""{6}"",""sLoadingRecords"":""{7}"",""sProcessing"":""{8}"",""sSearch"":""{9}"",""sZeroRecords"":""{10}"",""oPaginate"": {{""sFirst"":""{11}"",""sLast"":""{12}"",""sNext"":""{13}"",""sPrevious"":""{14}""}},""oAria"":{{""sSortAscending"":""{15}"",""sSortDescending"":""{16}""}}}}</script>",
					Helper.Dictionary.GetDictionaryItem("DataTables.sEmptyTable", "No data available in table"),
					Helper.Dictionary.GetDictionaryItem("DataTables.sInfo", "Showing _START_ to _END_ of _TOTAL_ entries"),
					Helper.Dictionary.GetDictionaryItem("DataTables.sInfoEmpty", "Showing 0 to 0 of 0 entries"),
					Helper.Dictionary.GetDictionaryItem("DataTables.sInfoFiltered", "(filtered from _MAX_ total entries"),
					Helper.Dictionary.GetDictionaryItem("DataTables.sInfoPostFix", string.Empty),
					Helper.Dictionary.GetDictionaryItem("DataTables.sInfoThousands", ","),
					Helper.Dictionary.GetDictionaryItem("DataTables.sLengthMenu", "Show _MENU_ entries"),
					Helper.Dictionary.GetDictionaryItem("DataTables.sLoadingRecords", "Loading..."),
					Helper.Dictionary.GetDictionaryItem("DataTables.sProcessing", "Processing..."),
					Helper.Dictionary.GetDictionaryItem("DataTables.sSearch", "Search:"),
					Helper.Dictionary.GetDictionaryItem("DataTables.sZeroRecords", "No matching records found"),
					Helper.Dictionary.GetDictionaryItem("DataTables.sFirst", "First"),
					Helper.Dictionary.GetDictionaryItem("DataTables.sLast", "Last"),
					Helper.Dictionary.GetDictionaryItem("DataTables.sNext", "Next"),
					Helper.Dictionary.GetDictionaryItem("DataTables.sPrevious", "Previous"),
					Helper.Dictionary.GetDictionaryItem("DataTables.sSortAscending", ": activate to sort column ascending"),
					Helper.Dictionary.GetDictionaryItem("DataTables.sSortDescending", ": activate to sort column descending"));

			return translation;
		}

		#endregion

		#region Events

		/// <summary>
		/// Initialize the control, make sure children are created
		/// </summary>
		/// <param name="e">
		/// An <see cref="T:System.EventArgs"/> object that contains the event data.
		/// </param>
		protected override void OnInit(EventArgs e)
		{
			base.OnInit(e);
		}

		/// <summary>
		/// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			// Adds the client dependencies
			this.AddAllDtgClientDependencies();

			this.EnsureChildControls();
		}

		/// <summary>
		/// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
		/// </summary>
		protected override void CreateChildControls()
		{
			base.CreateChildControls();

			// DEBUG: Reset stored values
			// this.Data.Value = "<items><item id='1'><name nodeName='Name' nodeType='-88' >Anna</name><age nodeName='Age' nodeType='-51' >25</age><picture nodeName='Picture' nodeType='1035' ></picture></item><item id='6'><name nodeName='Name' nodeType='-88' >Ove</name><gender nodeName='Gender' nodeType='-88'>Male</gender><age nodeName='Age' nodeType='-51' >23</age><picture nodeName='Picture' nodeType='1035' ></picture></item></items>";

			// Set default value if none exists
			if (this.data.Value == null)
			{
				Helper.Log.Debug<DataEditor>(string.Format("DTG: No values exist in database for this property"));

				this.data.Value = string.Empty;
			}
			else
			{
				Helper.Log.Debug<DataEditor>(
					string.Format("DTG: Retrieved the following data from database: {0}", this.data.Value));
			}

			// Use data from viewstate if present
			if (!string.IsNullOrEmpty(this.DataString))
			{
				this.data.Value = this.DataString;
			}

			this.ShowGridHeader = new HiddenField() { ID = "ShowGridHeader", Value = this.settings.ShowGridHeader.ToString() };
			this.ShowGridFooter = new HiddenField() { ID = "ShowGridFooter", Value = this.settings.ShowGridFooter.ToString() };
            this.ReadOnly = new HiddenField() { ID = "ReadOnly", Value = this.settings.ReadOnly.ToString() };
			this.DataTablesTranslation = new LiteralControl() { ID = "DataTablesTranslation", Text = this.GetDataTablesTranslation() };
			this.TableHeight = new HiddenField() { ID = "TableHeight", Value = this.settings.TableHeight.ToString() };
			this.Value = new HiddenField() { ID = "Value", Value = this.data.Value != null ? this.data.Value.ToString() : string.Empty };
			this.Grid = new Table { ID = "tblGrid", CssClass = "display" };
			this.Toolbar = new Panel { ID = "pnlToolbar", CssClass = "Toolbar" };

            // Get column configurations
            this.ColumnConfigurations = this.prevalueEditorSettingsHandler.GetColumnConfigurations(this.dataTypeDefinitionId);

			// Add value container here, because we need the unique id
			this.Controls.Add(this.Value);

			// Use value from viewstate if present
			if (this.Page != null && !string.IsNullOrEmpty(this.Page.Request.Form[this.Value.UniqueID]))
			{
                // Parse to StoredValueRowCollection to get the values sorted
                var l = new StoredValueRowCollection(this.ColumnConfigurations, this.Page.Request.Form[this.Value.UniqueID]);

                this.DataString = l.ToString();
			}

			// Set up rows
            Rows = new StoredValueRowCollection(this.ColumnConfigurations, this.DataString);
			InsertDataTypes = GetInsertDataTypes();
			EditDataTypes = GetEditDataTypes();

			InsertControls = new Panel { ID = "ctrlInsert", CssClass = "InsertControls" };
			EditControls = new Panel { ID = "ctrlEdit", CssClass = "EditControls" };
			DeleteControls = new Panel { ID = "ctrlDelete", CssClass = "DeleteControls" };

			// Generate header row
			GenerateHeaderRow();

			// Generate rows with edit, delete and row data
			GenerateValueRows();

            // Generate insert and delete controls if grid is not in readonly mode
		    if (!this.settings.ReadOnly)
		    {
		        // Generate header row
		        GenerateFooterToolbar();

		        // Generate insert controls
		        GenerateInsertControls();

		        // Generate edit controls
		        GenerateEditControls();
		    }

		    // Add controls to container
			this.Controls.Add(this.ShowGridHeader);
			this.Controls.Add(this.ShowGridFooter);
			this.Controls.Add(this.TableHeight);
			this.Controls.Add(this.DataTablesTranslation);
			this.Controls.Add(this.Grid);
			this.Controls.Add(this.Toolbar);
			this.Controls.Add(this.InsertControls);
			this.Controls.Add(this.EditControls);
			this.Controls.Add(this.DeleteControls);
		}

		/// <summary>
		/// Sends server control content to a provided <see cref="T:System.Web.UI.HtmlTextWriter"/> object, which writes the content to be rendered on the client.
		/// </summary>
		/// <param name="writer">
		/// The <see cref="T:System.Web.UI.HtmlTextWriter"/> object that receives the server control content.
		/// </param>
		protected override void Render(HtmlTextWriter writer)
		{
			// Prints the grid
			writer.AddAttribute("id", ClientID);
			writer.AddAttribute("class", "dtg");

			writer.RenderBeginTag(HtmlTextWriterTag.Div);
			this.ShowGridHeader.RenderControl(writer);
			this.ShowGridFooter.RenderControl(writer);
			this.TableHeight.RenderControl(writer);
            this.ReadOnly.RenderControl(writer);
			this.DataTablesTranslation.RenderControl(writer);
			this.Value.RenderControl(writer);
			this.Grid.RenderControl(writer);
			this.Toolbar.RenderControl(writer);

			// Prints the insert, edit and delete controls);
			InsertControls.RenderControl(writer);
			EditControls.RenderControl(writer);
			DeleteControls.RenderControl(writer);

			writer.RenderEndTag();
		}

		#endregion
	}
}