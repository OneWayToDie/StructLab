namespace StructLab.Models.Lab01
{
	public class Reader : ObservableModel
	{
		private int _id;
		private string _fullName;
		private string _phone;
		private string _ticketNumber;
		private string _address;

		public int Id { get => _id; set => SetProperty(ref _id, value); }
		public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
		public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
		public string TicketNumber { get => _ticketNumber; set => SetProperty(ref _ticketNumber, value); }
		public string Address { get => _address; set => SetProperty(ref _address, value); }

		public Reader Clone() => new Reader
		{
			Id = Id,
			FullName = FullName,
			Phone = Phone,
			TicketNumber = TicketNumber,
			Address = Address
		};

		public void CopyFrom(Reader other)
		{
			Id = other.Id;
			FullName = other.FullName;
			Phone = other.Phone;
			TicketNumber = other.TicketNumber;
			Address = other.Address;
		}
	}
}
