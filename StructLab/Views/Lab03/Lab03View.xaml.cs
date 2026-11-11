using System.Windows.Controls;

namespace StructLab.Views.Lab03
{
	public partial class Lab03View : UserControl
	{
		public Lab03View()
		{
			InitializeComponent();
		}

		private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (DataContext is ViewModels.Lab03.Lab03ViewModel vm)
			{
				vm.OpenDetailsCommand.Execute(null);
			}
		}


	}
}
