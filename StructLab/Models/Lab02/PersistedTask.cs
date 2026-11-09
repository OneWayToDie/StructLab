namespace StructLab.Models.Lab02
{
    public class PersistedTask
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Priority { get; set; }
        public string DueDate { get; set; }
        public string DueTime { get; set; }  
        public int Status { get; set; }
        public bool IsManual { get; set; }
    }
}