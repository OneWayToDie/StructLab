using System;
using System.Runtime.InteropServices;

namespace StructLab.Services
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	public struct CBook
	{
		public int Id;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string Author;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string Title;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)] public string Genre;
		public int Year;
		public int Pages;
		public int Copies;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	public struct CReader
	{
		public int Id;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string FullName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)] public string Phone;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)] public string TicketNumber;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)] public string Address;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	public struct CLoan
	{
		public int Id;
		public int BookId;
		public int ReaderId;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)] public string IssueDate;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)] public string DueDate;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)] public string ReturnDate;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	public struct CTask
	{
		public int Id;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)] public string Title;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 500)] public string Description;
		public int Priority;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)] public string DueDate;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)] public string DueTime;   // ← НОВОЕ
		public int Status;
		public int IsManual;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct CSnapshotNode
	{
		public int Key;         // ID задачи
		public int Priority;    // для цвета/подписи
		public int ParentIndex;
		public int IsLeft;
	}
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	public struct CPatient
	{
		public int Id;
		public int Type;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string FullName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)] public string Address;
		public int CardNumber;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)] public string Diagnosis;
		public int Severity;
	}


	internal static class NativeMethods
	{
		private const string DllName = "LabLogic.dll";
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr Lab01_CreateList(int isDoubly);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab01_DestroyList(IntPtr listPtr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab01_Clear(IntPtr listPtr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab01_AddLast(IntPtr listPtr, CBook book);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab01_AddFirst(IntPtr listPtr, CBook book);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab01_AddSorted(IntPtr listPtr, CBook book);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_AddBefore(IntPtr listPtr, string targetTitle, CBook book);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_AddAfter(IntPtr listPtr, string targetTitle, CBook book);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_DeleteByTitle(IntPtr listPtr, string title);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab01_Sort(IntPtr listPtr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab01_GetCount(IntPtr listPtr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_GetAt(IntPtr listPtr, int index, out CBook outBook);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_SaveToFile(IntPtr listPtr, string filename);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_LoadFromFile(IntPtr listPtr, string filename);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab01_FindForRequest(IntPtr listPtr, string genre, int targetPages);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr Lab01_Reader_CreateList();
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab01_Reader_DestroyList(IntPtr ptr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab01_Reader_Clear(IntPtr ptr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab01_Reader_Add(IntPtr ptr, CReader reader);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_Reader_DeleteById(IntPtr ptr, int id);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab01_Reader_GetCount(IntPtr ptr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_Reader_GetAt(IntPtr ptr, int index, out CReader outReader);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_Reader_SaveToFile(IntPtr ptr, string filename);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_Reader_LoadFromFile(IntPtr ptr, string filename);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_AdjustCopies(IntPtr listPtr, int bookId, int delta);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr Lab01_Loan_CreateList();
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab01_Loan_DestroyList(IntPtr ptr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab01_Loan_Clear(IntPtr ptr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab01_Loan_Add(IntPtr ptr, CLoan loan);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_Loan_MarkReturned(IntPtr ptr, int loanId, string returnDate);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_Loan_DeleteById(IntPtr ptr, int id);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab01_Loan_GetCount(IntPtr ptr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_Loan_GetAt(IntPtr ptr, int index, out CLoan outLoan);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_Loan_SaveToFile(IntPtr ptr, string filename);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab01_Loan_LoadFromFile(IntPtr ptr, string filename);
		// ============ Lab02: Деревья ============
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr Lab02_Create();
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab02_Destroy(IntPtr ptr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab02_Clear(IntPtr ptr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab02_AddTask(IntPtr ptr, string title, string description, int priority, string dueDate, string dueTime, int sequential);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab02_DeleteTask(IntPtr ptr, int taskId);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab02_SetStatus(IntPtr ptr, int taskId, int status);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab02_GetTask(IntPtr ptr, int taskId, out CTask outTask);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab02_GetAllTasks(IntPtr ptr, [Out] CTask[] buffer, int maxSize);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab02_GetTaskIdsInRange(IntPtr ptr, int lo, int hi, [Out] int[] buffer, int maxSize);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab02_GetTaskIdsAtLeast(IntPtr ptr, int minPriority, [Out] int[] buffer, int maxSize);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab02_GetTopUrgent(IntPtr ptr, int n, [Out] int[] buffer, int maxSize);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab02_GetSnapshot(IntPtr ptr, [Out] CSnapshotNode[] buffer, int maxSize);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab02_GetTreeHeight(IntPtr ptr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab02_GetIndexNodeCount(IntPtr ptr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab02_IsAvl(IntPtr ptr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab02_SetUseAvl(IntPtr ptr, bool value);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab02_AddTask(IntPtr ptr, string title, string description, int priority, string dueDate, int sequential);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab02_AddTaskWithId(IntPtr ptr, int id, string title, string description, int priority, string dueDate, string dueTime, int status, int isManual);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab02_AddManualTask(IntPtr ptr, string title, string description, int priority, string dueDate, string dueTime);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr Lab03_Create();
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab03_Destroy(IntPtr ptr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern void Lab03_Clear(IntPtr ptr);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab03_AddPatient(IntPtr ptr, int type, string name, string address, int cardNumber, string diagnosis, int severity);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern bool Lab03_RemovePatient(IntPtr ptr, int id);
		[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)] public static extern int Lab03_GetPatients(IntPtr ptr, string filterStr, int minCard, int maxCard, [Out] CPatient[] buffer, int maxSize);

	}
}