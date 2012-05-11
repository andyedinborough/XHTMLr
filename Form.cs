using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Xml.Linq;

namespace XHTMLr {

  public class Form : NameValueCollection {
    public Form() {
      Labels = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
    }

    public string Method { get; set; }
    public string Name { get; set; }
    public string Action { get; set; }
    public NameValueCollection Labels { get; private set; }

    public void Load(XElement node) {
      Method = ((string)node.Attribute("method")).NotEmpty("GET");
      Action = (string)node.Attribute("action");
      Name = (string)node.Attribute("name");

      var nodes = node.Descendants("input");
      if (!nodes.IsNullOrEmpty())
        foreach (var input in nodes) {
          string name = (string)input.Attribute("name");
          if (!name.IsNullOrEmpty()) {
            this[name] = (string)input.Attribute("value");
          }
        }

      nodes = node.Descendants("select");
      if (!nodes.IsNullOrEmpty())
        foreach (var select in nodes) {
          string name = (string)select.Attribute("name");
          if (!name.IsNullOrEmpty()) {
            var option = select.Descendants("option").FirstOrDefault(x => ((string)x.Attribute("selected")).IsNullOrEmpty())
                ?? select.Descendants("option").FirstOrDefault();

            if (option != null) {
              this[name] = ((string)option.Attribute("value")).NotEmpty(option.Value.NotNull().Trim());
            }
          }
        }

      nodes = node.Descendants("textarea");
      if (!nodes.IsNullOrEmpty())
        foreach (var input in nodes) {
          string name = (string)input.Attribute("name");
          if (!name.IsNullOrEmpty()) {
            this[name] = (string)input.Value;
          }
        }

      nodes = node.Descendants("label");
      if (!nodes.IsNullOrEmpty())
        foreach (var label in nodes) {
          var @for = (string)label.Attribute("for");
          if (@for.IsNullOrEmpty()) continue;
          Labels[@for] = label.Value;
        }
    }

    public static Form[] GetForms(string html) {
      var doc = XDocument.Parse(XHTML.ToXml(html));
      return GetForms(doc);
    }

    public static Form[] GetForms(XDocument doc) {
      var forms = new List<Form>();
      if (doc != null)
        foreach (var frm in doc.Descendants("form")) {
          var form = new Form();
          form.Load(frm);
          forms.Add(form);
        }
      return forms.ToArray();
    }

    public string PostString() {
      return System.Text.Encoding.Default.GetString(Post());
    }

    public byte[] Post() {
      using (var web = new System.Net.WebClient())
        return web.UploadValues(Action, Method ?? "POST", this);
    }
  }
}
