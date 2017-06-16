using System.Collections.Generic;

namespace XHTMLr
{
	public class FormData : List<FormData.Pair>
	{
		#region Methods

		public void Add(string name, object value)
		{
			Add(new Pair { Name = name, Value = value });
		}

		#endregion

		#region Classes

		public class Pair
		{
			#region Properties

			public string Name { get; set; }

			public object Value { get; set; }

			#endregion
		}

		#endregion
	}
}