using System.Windows;
using System.Windows.Controls;

namespace StructLab.Views.Lab01
{
	public partial class Lab01View : UserControl
	{
		public Lab01View()
		{
			InitializeComponent();

			Loaded += OnViewLoaded;
			Unloaded += OnViewUnloaded;
			DataContextChanged += OnViewDataContextChanged;
		}
	}
}
