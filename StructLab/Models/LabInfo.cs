using System;

namespace StructLab.Models
{
	public class LabInfo
	{
		public string Number { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }
		public Type ViewType { get; set; }  // ссылка на View для навигации

		public override string ToString() => $"ЛР {Number}. {Title}";
	}
}
