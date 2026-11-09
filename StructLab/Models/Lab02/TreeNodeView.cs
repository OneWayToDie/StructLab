using System.Windows.Media;

namespace StructLab.Models.Lab02
{
	public class TreeNodeView
	{
		public int Index { get; set; }
		public int Key { get; set; }
		public int Priority { get; set; }
		public int ParentIndex { get; set; }
		public int IsLeft { get; set; }
		public double X { get; set; }
		public double Y { get; set; }

		public string PriorityText => Priority.ToString();

		public string IdText => "#" + Key;
		//Сдвиг фазы «дыхания» узла — чтобы не моргали синхронно.
		public int PulseDelayMs => (Index * 137) % 1500;
		public Brush NodeColor
		{
			get
			{
				if (Priority >= 9) return B("#EF4444"); // красный — критично
				if (Priority >= 7) return B("#F59E0B"); // оранжевый — высокий
				if (Priority >= 4) return B("#4F46E5"); // индиго — средний
				return B("#94A3B8");                     // серый — низкий
			}
		}

		private static Brush B(string hex)
		{
			var brush = new SolidColorBrush(
				(Color)ColorConverter.ConvertFromString(hex));
			brush.Freeze();
			return brush;
		}
	}
}