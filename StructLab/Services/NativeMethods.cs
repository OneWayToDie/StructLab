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

	internal static class NativeMethods
	{
		private const string DllName = "LabLogic.dll";

		// ИЗМЕНЕНО: добавлен параметр isDoubly
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
	}
}