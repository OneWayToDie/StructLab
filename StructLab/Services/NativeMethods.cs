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
	}
}