namespace StructLab.Models.Dialogs
{
	public class ConfirmDialogModel
	{
		public string Title { get; set; } = "Подтверждение";
		public string Text { get; set; } = "";
		public string OkText { get; set; } = "Да";
		public string CancelText { get; set; } = "Отмена";
	}
}