using System;
using System.Collections;
using System.Linq;
using System.Xml.Linq;

namespace XHTMLr {
	public static class Extensions {
		static internal bool Is(this string a, string b = null) {
			return string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);
		}

		static internal void WriteLine(this System.IO.Stream stream, string line = null, System.Text.Encoding encoding = null) {
			stream.Write(line + Environment.NewLine, encoding);
		}
		static internal void Write(this System.IO.Stream stream, byte[] data) {
			stream.Write(data, 0, data.Length);
		}
		static internal void Write(this System.IO.Stream stream, string line = null, System.Text.Encoding encoding = null) {
			var data = (encoding ?? System.Text.Encoding.Default).GetBytes(line ?? string.Empty);
			stream.Write(data, 0, data.Length);
		}

		public static Tuple<string, byte[]> SerializeData(this XElement form, System.Text.Encoding encoding = null) {
			var values = form.Serialize();
			var action = (string)form.Attribute("action");
			var multipart = "multipart/form-data".Is((string)form.Attribute("enctype"));
			var boundary = multipart ? ("---xhtmlr-" + Guid.NewGuid().ToString()) : null;
			var ct = multipart
				? ("multipart/form-data, boundary=" + boundary)
				: "application/x-www-form-urlencoded";
			using (var mem = new System.IO.MemoryStream()) {
				if (multipart) {
					foreach (var key in values) {
						mem.WriteLine("--" + boundary, encoding);
						mem.WriteLine("Content-Disposition: form-data; name=\"" + key.Name + "\"", encoding);
						mem.WriteLine();
						var value = key.Value;
						if (value is FilePointer) {
							var ptr = (FilePointer)value;
							if (!ptr.File.IsNullOrEmpty()) {
								value = System.IO.File.ReadAllBytes(ptr.File);
							}
						} else if (value is byte[]) {
							mem.Write((byte[])value);
						} else {
							mem.WriteLine(Convert.ToString(value), encoding);
						}
					}
					mem.WriteLine("--" + boundary + "--", encoding);
				} else {
					var sep = string.Empty;
					foreach (var key in values) {
						mem.Write(sep + Uri.EscapeDataString(key.Name) + "=" + Uri.EscapeDataString(Convert.ToString(key.Value)), encoding);
						sep = "&";
					}
				}

				return Tuple.Create(ct, mem.ToArray());
			}
		}

		internal static Uri ToUri(this string url, Uri baseUri = null) {
			Uri uri;
			if (baseUri != null) {
				if (Uri.TryCreate(baseUri, url, out uri))
					return uri;
			} else if (Uri.TryCreate(url, UriKind.Absolute, out uri))
				return uri;
			return null;
		}

		public static string Post(this System.Net.WebClient web, XElement form, Uri baseUrl = null, System.Text.Encoding encoding = null) {
			var values = form.SerializeData();
			var action = (string)form.Attribute("action");
			web.Headers[System.Net.HttpRequestHeader.ContentType] = values.Item1;

			var url = action.ToUri(baseUrl ?? web.ResponseHeaders[System.Net.HttpResponseHeader.Location].ToUri());

			var data = web
				.UploadData(url, ((string)form.Attribute("method")).NotEmpty("post").ToUpper(), values.Item2);

			return (encoding ?? System.Text.Encoding.Default).GetString(data);
		}

		public static void Field(this XElement form, string name, object value) {
			var inputs = form.Descendants().Where(x => name.Is((string)x.Attribute("name"))).ToArray();
			if (inputs.Any(x => x.Name.LocalName.Is("select"))) {
				inputs.Remove();
				inputs = new XElement[0];
			}
			if (inputs.Length == 0)
				form.Add(inputs = new[] { new XElement("input", new XAttribute("name", name)) });
			var strValue = Convert.ToString(value);
			foreach (var input in inputs) {
				SetField(input, strValue);
			}
		}

		public static Form[] GetForms(this XDocument doc) {
			if (doc == null) return new Form[0];
			return doc.Descendants("form").Select(x => new Form(x)).ToArray();
		}

		private static void SetField(XElement input, string value) {
			switch (input.Name.LocalName) {
				case "button":
				case "input":
					var type = ((string)input.Attribute("type") ?? "text").ToLower();
					if ((type == "radio" || type == "checkbox")) {
						if ((string)input.Attribute("value") != value)
							return;
						else input.SetAttributeValue("checked", "checked");
					} else input.SetAttributeValue("value", value);
					break;
				case "select":
					var options = input.Descendants("option");
					options.Descendants().OfType<XAttribute>().Where(x => x.Name.LocalName == "selected").Remove();
					var option = options.FirstOrDefault(x => (string)x.Attribute("value") == value)
						?? options.FirstOrDefault(x => x.Value == value);
					if (option != null)
						option.SetAttributeValue("selected", "selected");
					break;
				case "textarea":
					input.SetValue(value);
					break;
			}
		}

		public static FormData Serialize(this XElement form) {
			var values = new FormData();
			var inputTypes = new System.Collections.Generic.HashSet<string> { "input", "select", "textarea" };
			var inputs = form.Descendants().Where(x => inputTypes.Contains(x.Name.LocalName));
			foreach (var input in inputs) {
				var name = (string)input.Attribute("name");
				if (string.IsNullOrEmpty(name)) continue;
				var value = Field(input);
				var type = (string)input.Attribute("type");
				if (value == null) continue;
				if (type.Is("file")) values.Add(name, new FilePointer { File = value });
				else values.Add(name, value);
			}

			return values;
		}

		public static XElement[] GetElementsByName(this XElement form, string name) {
			return form.Descendants().Where(x => (string)x.Attribute("name") == name).ToArray();
		}

		public static string Field(this XElement form, string name) {
			var input = form.GetElementsByName(name).FirstOrDefault();
			if (input == null) return null;
			return Field(input);
		}

		private static string Field(XElement input) {
			switch (input.Name.LocalName) {
				case "input":
					var type = ((string)input.Attribute("type") ?? "text").ToLower();
					if (type == "submit" || type == "button") return null;
					if ((type == "radio" || type == "checkbox")) {
						if (input.Attribute("checked") == null)
							return null;
					}
					return (string)input.Attribute("value") ?? string.Empty;

				case "select":
					var options = input.Descendants("option");
					var option = options.FirstOrDefault(x => x.Attribute("selected") != null) ?? options.FirstOrDefault();
					if (option == null) return null;
					return (string)option.Attribute("value") ?? option.Value;

				case "textarea":
					return input.Value;
			}
			return null;
		}

		internal static string NotEmpty(this string input, string @default) {
			return input.IsNullOrEmpty() ? @default : input;
		}
		internal static string NotNull(this string input) {
			return input ?? string.Empty;
		}

		internal static bool IsNullOrEmpty(this IEnumerable items) {
			if (items == null) return true;

			if (items is Array) return ((Array)items).Length == 0;
			if (items is ICollection) return ((ICollection)items).Count == 0;

			return !items.GetEnumerator().MoveNext();
		}

		internal static int ToInt(this string input, int DefaultValue = 0) {
			int value;
			if (int.TryParse(input, out  value)) {
				return value;
			} else {
				return DefaultValue;
			}
		}

		internal static bool Between<T>(this T a, T b, T c) where T : IComparable<T> {
			int ab = a.CompareTo(b);
			if (ab == 0) return true;
			int ac = a.CompareTo(c);
			if (ac == 0) return true;
			if (ab + ac == 0) return true;
			return false;
		}

		internal static T AtMost<T>(this T input, T max) where T : struct,IComparable<T> {
			if (input.CompareTo(max) == 1) return max;
			return input;
		}

		internal static int HexToInt(this string input, int defaultValue = 0) {
			int ret;
			if (int.TryParse(input, System.Globalization.NumberStyles.HexNumber, null, out ret))
				return ret;
			return defaultValue;
		}

		internal static string Left(this string input, int length) {
			input = input ?? string.Empty;
			return input.Substring(0, length.AtMost(input.Length));
		}

		internal static bool Contains<T>(this T bitmask, T flag) where T : struct, IConvertible {
			int v1 = Convert.ToInt32(bitmask);
			int v2 = Convert.ToInt32(flag);
			return (v1 & v2) == v2;
		}
	}
}
