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
		public Form(XElement form) {
			_form = form;
		}

		public string Action {
			get {
				return (string)_form.Attribute("action") ?? string.Empty;
			}
		}

		public string Method {
			get {
				return ((string)_form.Attribute("method")).NotEmpty("get");
			}
		}

		public IEnumerable<string> Keys {
			get {
				return _form.Descendants()
					.OfType<XAttribute>()
					.Where(x => x.Name.LocalName == "name")
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

		public static explicit operator XElement(Form form) {
			return form._form;
		}
	}
}
