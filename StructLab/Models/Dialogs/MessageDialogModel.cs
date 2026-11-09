namespace StructLab.Models.Dialogs
{
	public enum MessageKind
	{
		Info,
		Warning,
		Error
	}

	public class MessageDialogModel
	{
		public string Title { get; set; } = "Сообщение";
		public string Text { get; set; } = "";
		public MessageKind Kind { get; set; } = MessageKind.Info;
	}
}