﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uComponents.DataTypes.XPathDropDownList
{
	internal class XPathDropDownListOptions
	{
		/// <summary>
        /// XPath string used to get Nodes to be used as CheckBox options in a CheckBoxList
        /// </summary>
        public string XPath { get; set; }

        /// <summary>
        /// Defaults to true, where property value is a csv of NodeIds, else if false, then csv of Node names is stored
        /// </summary>
        public bool UseId { get; set; }

        /// <summary>
        /// Initializes an instance of XPathDropDownListOptions
        /// </summary>
        public XPathDropDownListOptions()
        {
            this.XPath = string.Empty;
            this.UseId = true; // Default to storing Node Id
        }
	}
}
