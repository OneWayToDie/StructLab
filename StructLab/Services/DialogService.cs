using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using StructLab.Models.Dialogs;

namespace StructLab.Services
{
	/// <summary>
	/// Единый API для показа диалогов в стиле Material Design.
	/// Все диалоги открываются внутри DialogHost с Identifier="Lab02Dialogs".
	/// </summary>
	public static class DialogService
	{
		private const string DialogHostIdentifier = "Lab02Dialogs";

		public static async Task ShowMessageAsync(
			string title, string text, MessageKind kind = MessageKind.Info)
		{
			var model = new MessageDialogModel
			{
				Title = title,
				Text = text,
				Kind = kind
			};

			await DialogHost.Show(model, DialogHostIdentifier);
		}

		public static async Task<bool> ShowConfirmAsync(
			string title, string text,
			string okText = "Да", string cancelText = "Отмена")
		{
			var model = new ConfirmDialogModel
			{
				Title = title,
				Text = text,
				OkText = okText,
				CancelText = cancelText
			};

			object result = await DialogHost.Show(model, DialogHostIdentifier);
			return result?.ToString() == "True";
		}
	}
}