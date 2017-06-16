using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XHTMLr
{
	public class Form
	{
		#region Fields

		private XElement _form;

		#endregion

		#region Constructors

		public Form() : this(new XElement("form"))
		{
		}

		public Form(XElement form)
		{
			_form = form;
		}

		#endregion

		#region Properties

		public string Action
		{
			get
			{
				return (string)_form.Attribute("action") ?? string.Empty;
			}
			set
			{
				_form.SetAttributeValue("action", value);
			}
		}

		public XElement Element { get { return _form; } }

		public string EncType
		{
			get
			{
				return ((string)_form.Attribute("enctype")).NotEmpty("application/x-www-form-urlencoded");
			}
			set
			{
				_form.SetAttributeValue("enctype", value);
			}
		}

		public IEnumerable<string> Keys
		{
			get
			{
				return _form.Descendants()
					.Select(x => x.Attribute("name"))
					.Where(x => x != null)
					.Select(x => x.Value)
					.Where(x => !string.IsNullOrEmpty(x))
					.Distinct();
			}
		}

		public string Method
		{
			get
			{
				return ((string)_form.Attribute("method")).NotEmpty("get");
			}
			set
			{
				_form.SetAttributeValue("method", value);
			}
		}

		#endregion

		#region Indexers

		public string this[string name]
		{
			get
			{
				return _form.Field(name);
			}
			set
			{
				_form.Field(name, value);
			}
		}

		#endregion

		#region Methods

		public static explicit operator XElement(Form form)
		{
			return form._form;
		}

		public static Form[] GetForms(string html)
		{
			return XDocument.Parse(XHTML.ToXml(html)).GetForms();
		}

		public static Form[] GetForms(XDocument xdoc)
		{
			return xdoc.GetForms();
		}

		public void Remove(string name)
		{
			_form.GetElementsByName(name).Remove();
		}

		public FormData Serialize()
		{
			return _form.Serialize();
		}

		public Tuple<string, byte[]> SerializeData(System.Text.Encoding encoding = null)
		{
			return _form.SerializeData(encoding);
		}

		public override string ToString()
		{
			return ToString(null);
		}

		public string ToString(System.Text.Encoding encoding)
		{
			encoding = encoding ?? System.Text.Encoding.Default;
			return encoding.GetString(_form.SerializeData(encoding).Item2);
		}

		#endregion
	}
}