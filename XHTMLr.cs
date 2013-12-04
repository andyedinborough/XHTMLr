using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace XHTMLr {
	public sealed class XHTML {

		private static readonly char[] _Whitespace = new[] { ' ', '\t', '\r', '\n' };
		private static readonly char[] _ValidAttrName = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:-".ToCharArray();
		private static readonly char[] _ValidTagName = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:-?!".ToCharArray();
		private static string[] _CommonTags = new[] { "a", "abbr", "address", "area", "article", "aside", "audio", "b", "base", "bb", "bdo", "blockquote", "body", "br", "button",
						"canvas", "caption", "cite", "code", "col", "colgroup", "command", "datagrid", "datalist", "dd", "del", "details", "dialog", "dfn", "div", "dl", "dt", "em", "embed", "eventsource", 
						"fieldset", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6", "head", "header", "hr", "html", "i", "iframe", "img", "input", "ins", "kbd", "label", "legend", "li", 
						"link", "mark", "map", "menu", "meta", "meter", "nav", "noscript", "object", "ol", "optgroup", "option", "output", "p", "param", "pre", "progress", "q", "ruby", "rp", "rt", "samp", 
						"script", "section", "select", "small", "source", "span", "strong", "style", "sub", "sup", "table", "tbody", "td", "textarea", "tfoot", "th", "thead", "time", "title", "tr", "ul",
						"var", "video" };

		private static string[][][] _NestingRules = new[]{
						new[]{
								new[]{ "font" },
								new[]{ "font" },
								new[]{ "font" },
						},
						new[]{
								new[]{ "li" },
								new[]{ "ul", "ol"},
								new[]{ "li" },
						},
						new[]{
								new[]{ "tr" },
								new[]{ "table", "thead", "tbody"},
								new[]{ "td", "th", "tr"  },
						},
						new[]{
								new[]{ "td", "th" },
								new[]{ "table", "thead", "tbody"},
								new[]{ "td", "th" },
						},
						new[]{
								new[]{ "p", "blockquote", "ul", "ol" },
								new[]{ "div", "table", "body" },
								new[]{ "p", "blockquote", "ul", "ol" },
						},
						new[]{
								new[]{ "frame" },
								new[]{ "framset" },
								new[]{ "frame" },
						},
						new[]{
								new[]{ "frameset" },
								new[]{ "frame" },
								new[]{ "frame" },
						},
				};

		private static string[] _SelfClosingTags = new[] { "area", "base", "basefront", "input", "meta", "img", "link", "br", "hr", "wbr" };

		private enum Modes {
			text, entity, attrValue, attrValueTick, attrValueQuote
		}

		[Flags()]
		public enum Options {
			None = 0, EntitiesOnly = 1,
			RemoveExtraWhitespace = 4, RemoveXMLNS = 8, RemoveComments = 16, Pretty = 32, CleanUpWordHTML = 64,
			KeepTagCase = 128,
			Default = RemoveXMLNS
		}

		public static string ToXml(string html, Options options = Options.Default) {
			var parser = new XHTML();
			parser.Process(html, options);
			html = parser.Document.ToString().Trim();

			if (options.Contains(Options.Pretty)) {
				var xml = System.Xml.Linq.XDocument.Parse(html);
				using (var str = new System.IO.StringWriter())
				using (var xmlw = new System.Xml.XmlTextWriter(str)) {
					xmlw.Formatting = System.Xml.Formatting.Indented;
					xml.WriteTo(xmlw);
					html = str.ToString();
				}

				html = html.Substring(html.IndexOf("?>") + 2).Trim();
			}

			return html;
		}

		public static XDocument Parse(string html, Options options = Options.Default) {
			var parser = new XHTML();
			parser.Process(html, options);
			return parser.Document;
		}

		private void Process(string html, Options options = Options.Default) {
			if (options.Contains(Options.CleanUpWordHTML)) {
				html = WordCleanup.CleanWordHtml(html);
			}

			using (Input = new StringReader(html)) {
				RemoveExtraWhitespace = options.Contains(Options.RemoveExtraWhitespace);
				RemoveComments = options.Contains(Options.RemoveComments);
				RemoveXmlns = options.Contains(Options.RemoveXMLNS);
				KeepTagCase = options.Contains(Options.KeepTagCase);

				while (Next != null) {
					var next = Next;
					Next = null;
					next();
				}
			}
		}

		#region Instance Members
		public StringReader Input { get; set; }
		public XDocument Document { get; set; }

		private XElement _Element;

		public XElement Element {
			get { return _Element; }
			set {
				_Element = value;
			}
		}

		public List<string> OpenTags { get; private set; }
		public bool EnforceHtmlElement { get; set; }
		public bool RemoveExtraWhitespace { get; set; }
		public bool RemoveComments { get; set; }
		public bool RemoveXmlns { get; set; }
		public bool KeepTagCase { get; set; }
		public long NumTagsWritten { get; set; }
		public Action Next { get; set; }

		private XHTML() {
			OpenTags = new List<string>();
			Next = ReadText;
		}

		private void ReadText() {
			var block = Read(Modes.text);

			if (block.Text.Length > 0) {
				if (RemoveExtraWhitespace) {
					if (_Whitespace.Contains(block.Text[0])) {
						block.Text = ' ' + block.Text.TrimStart(_Whitespace);
					}
					if (_Whitespace.Contains(block.Text[block.Text.Length - 1])) {
						block.Text = block.Text.TrimEnd(_Whitespace) + ' ';
					}
				}

				AppendText(block.Text);
			}

			if (block.Last == '&') {
				Next = ReadEntity;

			} else if (block.Last == '<') {
				Next = ReadTag;
			}
		}

		private void ReadEntity() {
			var block = Read(Modes.entity);

			if (block.Last == ';' || _Whitespace.Contains(block.Last)) {
				AppendText(Entities.DecodeEntity(block.Text));
				if (block.Last != ';') AppendText(block.Last.ToString());
				Next = ReadText;

			} else {
				AppendText("&" + block.Text);

				if (block.Last == '<') {
					Next = ReadTag;
				} else {
					if (block.Last == '<') {
						AppendText("<" + block.Text);

					} else if (block.Last == '>') {
						AppendText(">" + block.Text);

					} else if (block.Last == '&') {
						Next = ReadEntity;
						return;
					} else AppendText(block.Last.ToString());
					Next = ReadText;
				}
			}
		}

		/// <summary>
		/// TODO: Enable tracking current prefixes and only strip those that aren't declared
		/// </summary>
		private static string stripPrefix(string tagName) {
			int i = tagName.IndexOf(':');
			if (i == -1) return tagName;
			if (i == tagName.Length - 1) return tagName.Substring(0, i);
			return tagName.Substring(i + 1);
		}

		private void AutoClose(string[] parentTags, string[] tags) {
			int iparent = parentTags.Max(x => OpenTags.LastIndexOf(x));
			if (iparent == -1) return;
			var itags = tags.Select(x => OpenTags.IndexOf(x, iparent)).Where(x => x > -1);
			if (itags.IsNullOrEmpty()) return;
			var itag = itags.Min();
			Close(itag);
		}

		private void Close(int openerIndex) {
			if (openerIndex > -1) {
				for (int i = OpenTags.Count - 1; i >= openerIndex; i--) {
					Element = Element.Parent ?? Document.Root;
					OpenTags.RemoveAt(i);
				}
			}
		}

		private static bool IsLetter(int i) {
			return i.Between(65, 90) || i.Between(97, 122);
		}
		private static bool IsDigit(int i) {
			return i.Between(48, 57);
		}

		private void ReadTag() {
			var block = ReadTagName();
			bool closer = false;
			if (block.Text.Length == 0 && block.Last == '/') {
				closer = true;
				block = ReadTagName();
			}

			var tagName = stripPrefix(KeepTagCase ? block.Text : block.Text.ToLower());

			if (block.Text.Length > 0 && block.Text[0] == '!') {
				if (block.Text.Left(3) == "!--") {
					if (block.Last == '>' && block.Text.EndsWith("--")) {
						if (!RemoveComments) {
							block.Text = block.Text.Substring(3);
							if (block.Text.EndsWith("--")) block.Text = block.Text.Substring(0, block.Text.Length - 2);
							Element.Add(new XComment(block.Text.TrimEnd('-').Replace("--", "  ")));
						}

					} else {
						var comment = ReadUntil("-->");
						if (!RemoveComments && comment.Length >= 3) {
							Element.Add(new XComment((block.Text.Substring(3) + block.Last + comment.Substring(0, comment.EndsWith("-->") ? (comment.Length - 3) : 0)).TrimEnd('-').Replace("--", "  ")));
						}
					}
				} else {
					//if (next.text.StartsWith("!DOCTYPE html", StringComparison.OrdinalIgnoreCase)) enforceHtmlElement = true;
					if (block.Last != '>') block = ReadUntil('>'); // ignore this, doctypes and such
				}

				Next = ReadText;

			} else if (tagName.Length == 0 && block.Last == '?') {
				string text = ReadUntil("?>");
				Next = ReadText;
				return;

			} else if (tagName.Length == 0 || block.Last == '<' || !IsLetter(tagName[0]) || tagName.Contains(':')) {
				if (OpenTags.Count > 0) {
					AppendText("<" + block.Text);

					if (block.Last == '<') {
						Next = ReadTag;
					} else if (block.Last == '&') {
						Next = ReadEntity;
					} else {
						if (block.Last == '>') AppendText(">");
						else AppendText(block.Last.ToString());
						Next = ReadText;
					}
				} else Next = ReadText;

			} else {
				if (closer) {
					if (tagName != "body" && tagName != "html") { //we'll close these manually
						var openerIndex = OpenTags.LastIndexOf(tagName);

						Close(openerIndex);
					}

					Next = ReadText;

				} else {
					bool enabled = true;
					bool selfClosing = _SelfClosingTags.Contains(tagName); // These are handled manually 
					var attrs = new Dictionary<string, string>();


					foreach (var rule in _NestingRules) {
						if (rule[0].Contains(tagName)) {
							AutoClose(rule[1], rule[2]);
							break;
						}
					}

					if (EnforceHtmlElement) {
						if (tagName != "html" && OpenTags.Count == 0) {
							AppendElement("html");

						} else if (tagName == "html" && OpenTags.Count > 0) {
							enabled = false;
						} else if (tagName == "body" && OpenTags.Contains("body")) {
							enabled = false;
						}

						if (OpenTags.Count > 0 && tagName != "body" && tagName != "head" && !OpenTags.Contains("head") && !OpenTags.Contains("body")) {
							AppendElement("body");
						}
					}

					if (enabled) {
						AppendElement(tagName);
					}

					while (block.Last != '>') {
						ReadWhileWhitespace();
						block = ReadAttrName();
						if (block.Text.Length == 0 && block.Last == 0) break;
						var attrName = KeepTagCase ? block.Text : block.Text.ToLower();

						attrName = stripPrefix(attrName);
						if (attrName.Length == 0
										|| !IsLetter(attrName[0])
										|| attrName.Contains(':')) continue;

						ReadWhileWhitespace();

						char? c = Peek();
						if (c == null) break;

						var attrValue = string.Empty;

						if (block.Last == '=' || c == '=') {
							if (c == '=') Input.Read();
							ReadWhileWhitespace();
							c = Peek();
							if (c == null) break;

							if (c == '\'') {
								Input.Read();
								block = Read(Modes.attrValueTick);
								attrValue = block.Text.Trim();

							} else if (c == '"') {
								Input.Read();
								block = Read(Modes.attrValueQuote);
								attrValue = block.Text.Trim();

							} else {
								block = Read(Modes.attrValue);
								attrValue = block.Text;
							}
						} else {
							attrValue = attrName;
						}

						if (enabled && !(RemoveXmlns && attrName == "xmlns")) {
							attrValue = Entities.Decode(attrValue);

							if (attrs.ContainsKey(attrName))
								attrs[attrName] = attrValue;
							else attrs.Add(attrName, attrValue);
						}
					}

					foreach (var attr in attrs)
						Element.Add(new XAttribute(attr.Key, attr.Value));

					if (_SelfClosingTags.Contains(tagName)) {
						OpenTags.Remove(tagName);
						Element = Element.Parent ?? Document.Root;
					}

					if (tagName == "script" || tagName == "style") {
						ReadWhileWhitespace();
						var text = ReadUntil("</" + tagName, StringComparison.OrdinalIgnoreCase);
						if (text.Length >= tagName.Length + 2) {
							text = text.Substring(0, text.Length - tagName.Length - 2).TrimEnd();
							ReadUntil('>');
						}

						if (text.Length > 0) {
							var i = text.IndexOf("<![CDATA[");
							if (i == -1) {
								text = "/*<![CDATA[*/" + text;
							} else {
								var part = text.Substring(0, i);
								text = Entities.Decode(part) + text.Substring(i);
							}

							i = text.IndexOf("]]>");
							if (i == -1) {
								text = text + "/*]]>*/";
							} else {
								var part = text.Substring(i + 3);
								text = text.Substring(0, i + 3) + Entities.Decode(part);
							}
						}

						if (enabled) {
							if (!text.IsNullOrEmpty())
								AppendText(text);
							Element = Element.Parent ?? Document.Root;
							OpenTags.Remove(tagName);
						}
					}

					Next = ReadText;
				}
			}
		}

		private static HashSet<string> _specialElements = new HashSet<string> { "html", "body", "head" };

		private void AppendElement(string name, bool changeContext = true) {
			var elm = new XElement(name);

			NumTagsWritten++;
			OpenTags.Add(name);
			if (Element == null) {
				if (Document == null) Document = new XDocument(elm);
				else Document.Root.Add(elm);
			} else {
				if (_specialElements.Contains(name)) {
					elm = Document.Root.Descendants(name).FirstOrDefault() ?? elm;
					if (elm.Parent == null) {
						Element.Add(elm);
					}
				} else {
					Element.Add(elm);
				}
			}
			if (changeContext) {
				Element = elm;
			}
		}

		private void AppendText(string value) {
			if (value.IsNullOrEmpty()) return;

			if (Element == null) {
				if (string.IsNullOrWhiteSpace(value)) {
					return;
				}
				AppendElement("html");
				AppendElement("body");
			}
			value = Entities.Decode(value);
			XText txt = Element.LastNode as XText;
			if (txt != null) txt.Value += value;
			else Element.Add(new XText(value));
		}

		/*
		private void Out(string value) {
				if (OpenTags.Count == 0 && !EntitiesOnly) value = value.Trim();
				if (value.Length == 0) return;

				if (!EntitiesOnly && OpenTags.Count == 0 && EnforceHtmlElement) {
						OpenTags.Add("html");
						OpenTags.Add("body");
						Out("<html><body>");
				}

				foreach (char c in value)
						Out(c);
		}
		*/

		//http://seattlesoftware.wordpress.com/2008/09/11/hexadecimal-value-0-is-an-invalid-character/
		private static bool IsLegalXmlChar(int c) {
			return c == 0x9 || c == 0xA || c == 0xD
							|| (c >= 0x20 && c <= 0xD7FF)
							|| (c >= 0xE000 && c <= 0xFFFD)
							|| (c >= 0x10000 && c <= 0x10FFFF);
		}

		private char Read() {
			return Convert.ToChar(Input.Read());
		}

		private char? Peek() {
			int i = Input.Peek();
			if (i == -1) return null;
			return Convert.ToChar(i);
		}

		private void ReadWhileWhitespace() {
			while (true) {
				var c = Peek();
				if (c == null || !_Whitespace.Contains(c.Value)) return;
				Input.Read();
			}
		}

		private string ReadUntil(string marker, StringComparison comparison = StringComparison.Ordinal) {
			string last = string.Empty;
			var c = new char[1];
			using (var output = new StringWriter()) {
				while (true) {
					if (Input.Read(c, 0, 1) == 0) break;
					if (last.Length == marker.Length) last = last.Substring(1);
					last += c[0];
					output.Write(c[0]);
					if (string.Equals(marker, last, comparison)) break;
				}

				return output.ToString();
			}
		}

		private Block ReadTagName() {
			return Read((c, n) => {
				if (IsLetter(c)) return true;

				if (n == 0) {
					return c == '!';
				} else {
					return IsDigit(c) || c == '-' || c == ':';
				}
			});
		}

		private Block ReadAttrName() {
			return Read((c, n) => {
				if (IsLetter(c)) return true;

				if (n == 0) {
					return false;
				} else {
					return IsDigit(c) || c == '-' || c == ':';
				}
			});
		}

		private Block ReadWhile(params char[] chars) {
			return Read((c, n) => chars.Contains(c));
		}

		private Block ReadUntil(params char[] chars) {
			return Read((c, n) => !chars.Contains(c));
		}

		private Block Read(Func<char, int, bool> func) {
			int n = 0;
			using (var output = new StringWriter()) {
				var c = new char[1];
				while (true) {
					if (Input.Read(c, 0, 1) == 0) break;
					if (!func(c[0], n)) break;
					output.Write(c[0]);
					n++;
				}

				return new Block { Text = output.ToString(), Last = c[0] };
			}
		}

		private Block Read(Modes mode) {
			switch (mode) {
				case Modes.text: return ReadUntil('<', '&');
				case Modes.entity: return ReadUntil(';', ' ', '\r', '\n', '\t', '<', '>', '&', '\'', '"');
				//case Modes.tagName: return ReadWhile(_ValidTagName);
				//case Modes.attrName: return ReadWhile(_ValidAttrName);
				case Modes.attrValue: return ReadUntil('>', ' ', '\r', '\n', '\t', '\'', '"');
				case Modes.attrValueTick: return ReadUntil('\'');
				case Modes.attrValueQuote: return ReadUntil('"');
				default:
					throw new NotImplementedException();
			}
		}

		#endregion

		private class Block {
			public string Text { get; set; }
			public char Last { get; set; }
		}
	}
}
