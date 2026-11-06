// ViewModels/LabViewModelBase.cs
using StructLab.ViewModels;

public abstract class LabViewModelBase : ViewModelBase
{
	public abstract string LabNumber { get; }
	public abstract string LabTitle { get; }
	public abstract string LabDescription { get; }

	public virtual void Reset() { }
}
