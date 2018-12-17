﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.UI.WebControls;

namespace JocysCom.ClassLibrary.Web
{
	public partial class WebControlsHelper
	{

		public static System.Web.UI.Control FindControlRecursive(System.Web.UI.Control root, string id)
		{
			if (root.ID == id)
				return root;
			foreach (System.Web.UI.Control control in root.Controls)
			{
				var t = FindControlRecursive(control, id);
				if (t != null)
					return t;
			}
			return null;
		}

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static IEnumerable<System.Web.UI.Control> GetAll(System.Web.UI.Control control, Type type = null, bool includeTop = false)
		{
			// Get all child controls.
			var controls = control.Controls.Cast<System.Web.UI.Control>();
			return controls
				// Get children controls and flatten resulting sequences into one sequence.
				.SelectMany(x => GetAll(x))
				// Merge controls with their children.
				.Concat(controls)
				// Include top control if required.
				.Concat(includeTop ? new[] { control } : new System.Web.UI.Control[0])
				// Filter controls by type.
				.Where(x => type == null || (type.IsInterface ? x.GetType().GetInterfaces().Contains(type) : type.IsAssignableFrom(x.GetType())));
		}

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static T[] GetAll<T>(System.Web.UI.Control control, bool includeTop = false)
		{
			if (control == null) return new T[0];
			var type = typeof(T);
			// Get all child controls.
			var controls = control.Controls.Cast<System.Web.UI.Control>();
			// Get children of controls and flatten resulting sequences into one sequence.
			var result = controls.SelectMany(x => GetAll(x)).ToArray();
			// Merge controls with their children.
			result = result.Concat(controls).ToArray();
			// Include top control if required.
			if (includeTop) result = result.Concat(new[] { control }).ToArray();
			// Filter controls by type.
			result = type.IsInterface
				? result.Where(x => x.GetType().GetInterfaces().Contains(type)).ToArray()
				: result.Where(x => type.IsAssignableFrom(x.GetType())).ToArray();
			// Cast to required type.
			var result2 = result.Select(x => (T)(object)x).ToArray();
			return result2;
		}

		#region Apply Date Suffix

		/// <summary>
		/// All scripts, managed by ScriptManager and server side StyleSheets will be suffixed with v=LastWriteTime.
		/// How to use:
		/// protected void Page_PreRender(object sender, System.EventArgs e)
		/// {
		/// 	JocysCom.ClassLibrary.Web.WebControlsHelper.ApplyDateSuffix(Page);
		/// }
		/// </summary>
		/// <param name="page"></param>
		public static void ApplyDateSuffix(System.Web.UI.Page page)
		{
			foreach (var control in page.Header.Controls)
			{
				var link = control as System.Web.UI.HtmlControls.HtmlLink;
				if (link == null)
					continue;
				var isCss =
					"text/css".Equals(link.Attributes["type"], StringComparison.CurrentCultureIgnoreCase) ||
					"stylesheet".Equals(link.Attributes["rel"], StringComparison.CurrentCultureIgnoreCase) ||
					"icon".Equals(link.Attributes["rel"], StringComparison.CurrentCultureIgnoreCase);
				if (!isCss)
					continue;
				link.Href = GetFileWithSuffix(link.Href, page);
			}
			// ScriptManager requires System.Web.Extensions.dll
			var sm = System.Web.UI.ScriptManager.GetCurrent(page);
			if (sm == null)
				return;
			sm.ResolveScriptReference += ScriptManager_ResolveScriptReference;
		}


		/// <summary>
		/// ScriptReferenceEventArgs requires System.Web.Extensions.dll
		/// </summary>
		private static void ScriptManager_ResolveScriptReference(object sender, System.Web.UI.ScriptReferenceEventArgs e)
		{
			// ScriptManager requires System.Web.Extensions.dll
			var sm = (System.Web.UI.ScriptManager)sender;
			e.Script.Path = GetFileWithSuffix(e.Script.Path, sm.Page);
		}

		static string GetFileWithSuffix(string path, System.Web.UI.Page page)
		{
			if (string.IsNullOrEmpty(path))
				return path;
			// If path is absolute then return.
			if (path.Contains(":"))
				return path;
			var resolvedPath = page.ResolveUrl(path);
			// Check if path contains query.
			var index = resolvedPath.IndexOf('?');
			if (index > -1)
				resolvedPath = resolvedPath.Substring(0, index);
			var localPath = page.MapPath(resolvedPath);
			var fi = new System.IO.FileInfo(localPath);
			if (fi.Exists)
			{
				var v = string.Format("v={0:yyyyMMddHHmmss}", fi.LastWriteTime);
				if (path.Contains(v))
					return path;
				path += (index > -1 ? "&" : "?") + v;
			}
			return path;
		}

		#endregion

		#region Bind Lists

		/// <summary>
		/// Bind enumeration to ComboBox.
		/// </summary>
		/// <typeparam name="T">Enumeration type</typeparam>
		/// <param name="control">List control</param>
		/// <param name="format">{0} - string value, {1} - number value, {2} - description attribute.</param>
		/// <param name="addEmpty"></param>
		public static void BindEnum<T>(DropDownList control, T selected = default(T), bool addEmpty = false, bool sort = false, T[] exclude = null, string format = null)
		// Declare T as same as Enumeration.
		// where T :  struct, IComparable, IFormattable, IConvertible
		{
			var t = typeof(T);
			if (Runtime.RuntimeHelper.IsNullable(t))
				t = Nullable.GetUnderlyingType(t) ?? t;
			var list = new List<ListItem>();
			var values = Enum.GetValues(t).Cast<T>().ToArray();
			foreach (var value in values)
			{
				if (exclude != null && exclude.Contains(value))
					continue;
				var description = Runtime.Attributes.GetDescription(value);
				var stringValue = string.Format("{0}", value);
				var numberValue = System.Convert.ToInt64(value);
				var text = string.IsNullOrEmpty(format)
					? description
					: string.Format(format, stringValue, numberValue, description);
				var item = new ListItem(text, stringValue);
				list.Add(item);
			}
			if (sort)
				list = list.OrderBy(x => x.Text).ToList();
			if (addEmpty)
			{
				var defaultValue = string.Format("{0}", default(T));
				if (!list.Any(x => x.Text == defaultValue))
					list.Insert(0, new ListItem("", defaultValue));
			}
			// Make sure sorted is disabled, because it is not allowed when using DataSource.
			control.DataSource = list;
			control.DataTextField = "Text";
			control.DataValueField = "Value";
			control.DataBind();
			if (list.Count > 0)
			{
				var selectedValue = string.Format("{0}", selected);
				if (selectedValue != list[0].Value)
					SelectEnumValue(control, selected);
			}
		}

		public static void SelectEnumValue<T>(DropDownList control, T value)
		// Declare T as same as Enum.
		// where T : struct, IComparable, IFormattable, IConvertible
		{
			var stringValue = string.Format("{0}", value);
			for (var i = 0; i < control.Items.Count; i++)
			{
				var v = control.Items[i].Value;
				if (Equals(v, stringValue))
				{
					control.SelectedValue = stringValue;
					return;
				}
			}
		}

		#endregion
	}
}
