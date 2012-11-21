using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XHTMLr {
	public class FilePointer {
		public string File { get; set; }
	}

	public class FormData : List<FormData.Pair> {
		public class Pair {
			public string Name { get; set; }
			public object Value { get; set; }
		}
		public void Add(string name, object value) {
			Add(new Pair { Name = name, Value = value });
		}
	}

	public class Form {
		XElement _form;
		public Form() : this(new XElement("form")) { }
		public Form(XElement form) {
			_form = form;
		}

		public XElement Element { get { return _form; } }

		public static Form[] GetForms(string html) {
			return XDocument.Parse(XHTML.ToXml(html)).GetForms();
		}

		public static Form[] GetForms(XDocument xdoc) {
			return xdoc.GetForms();
		}

		public void Remove(string name) {
			_form.GetElementsByName(name).Remove();
		}

		public string Action {
			get {
				return (string)_form.Attribute("action") ?? string.Empty;
			}
			set {
				_form.SetAttributeValue("action", value);
			}
		}

		public string Method {
			get {
				return ((string)_form.Attribute("method")).NotEmpty("get");
			}
			set {
				_form.SetAttributeValue("method", value);
			}
		}

		public string EncType {
			get {
				return ((string)_form.Attribute("enctype")).NotEmpty("application/x-www-form-urlencoded");
			}
			set {
				_form.SetAttributeValue("enctype", value);
			}
		}

		public IEnumerable<string> Keys {
			get {
				return _form.Descendants()
					.SelectMany(x => x.Attributes("name"))
					.Select(x => x.Value).Distinct();
			}
		}

		public string this[string name] {
			get {
				return _form.Field(name);
			}
			set {
				_form.Field(name, value);
			}
		}

		public FormData Serialize() {
			return _form.Serialize();
		}

		public Tuple<string, byte[]> SerializeData(System.Text.Encoding encoding = null) {
			return _form.SerializeData(encoding);
		}

		public static explicit operator XElement(Form form) {
			return form._form;
		}

		public override string ToString() {
			return ToString(null);
		}

		public string ToString(System.Text.Encoding encoding) {
			encoding = encoding ?? System.Text.Encoding.Default;
			return encoding.GetString(_form.SerializeData(encoding).Item2);
		}
	}
}
