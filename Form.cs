using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Xml.Linq;

namespace XHTMLr {

    public class Form : NameValueCollection {
        public string Method { get; set; }
        public string Name { get; set; }
        public string Action { get; set; }

        private static Form Parse(XElement node) {
            Form form = new Form();
            form.Method = ((string)node.Attribute("method")).NotEmpty("GET");
            form.Action = (string)node.Attribute("action");
            form.Name = (string)node.Attribute("name");

            var nodes = node.Descendants("input");
            if (!nodes.IsNullOrEmpty())
                foreach (var input in nodes) {
                    string name = (string)input.Attribute("name");
                    if (!name.IsNullOrEmpty()) {
                        form[name] = (string)input.Attribute("value");
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
                            form[name] = ((string)option.Attribute("value")).NotEmpty(option.Value.NotNull().Trim());
                        }
                    }
                }

            return form;
        }

        public static Form[] GetForms(string html) {
            var doc = XDocument.Parse(XHTML.ToXml(html));
            return GetForms(doc);
        }

        public static Form[] GetForms(XDocument doc) {
            var forms = new List<Form>();
            if (doc != null)
                foreach (var frm in doc.Descendants("form")) {
                    forms.Add(Parse(frm));
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
