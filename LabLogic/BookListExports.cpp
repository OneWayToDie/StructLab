#include "BookList.h"
#include <cstring>

#pragma pack(push, 1)
struct CBook {
    int id; char author[100]; char title[100]; char genre[50]; int year; int pages; int copies;
};
#pragma pack(pop)

Lab01::Book ConvertToNative(const CBook& cb) {
    Lab01::Book b;
    b.id = cb.id; b.author = cb.author; b.title = cb.title;
    b.genre = cb.genre; b.year = cb.year; b.pages = cb.pages; b.copies = cb.copies;
    return b;
}

void ConvertToC(const Lab01::Book& b, CBook& cb) {
    cb.id = b.id;
    strncpy_s(cb.author, b.author.c_str(), sizeof(cb.author) - 1);
    strncpy_s(cb.title, b.title.c_str(), sizeof(cb.title) - 1);
    strncpy_s(cb.genre, b.genre.c_str(), sizeof(cb.genre) - 1);
    cb.year = b.year; cb.pages = b.pages; cb.copies = b.copies;
}

extern "C" {
    // —оздание списка. isDoubly = 1 дл€ двусв€зного, 0 дл€ односв€зного
    __declspec(dllexport) void* Lab01_CreateList(int isDoubly) {
        if (isDoubly) return new Lab01::DoublyBookList();
        return new Lab01::BookList();
    }

    __declspec(dllexport) void Lab01_DestroyList(void* listPtr) {
        if (listPtr) delete static_cast<Lab01::IBookList*>(listPtr);
    }

    __declspec(dllexport) void Lab01_Clear(void* listPtr) {
        if (listPtr) static_cast<Lab01::IBookList*>(listPtr)->Clear();
    }

    __declspec(dllexport) void Lab01_AddLast(void* listPtr, CBook book) {
        if (listPtr) static_cast<Lab01::IBookList*>(listPtr)->AddLast(ConvertToNative(book));
    }

    __declspec(dllexport) void Lab01_AddFirst(void* listPtr, CBook book) {
        if (listPtr) static_cast<Lab01::IBookList*>(listPtr)->AddFirst(ConvertToNative(book));
    }

    __declspec(dllexport) void Lab01_AddSorted(void* listPtr, CBook book) {
        if (listPtr) static_cast<Lab01::IBookList*>(listPtr)->AddSorted(ConvertToNative(book));
    }

    __declspec(dllexport) bool Lab01_AddBefore(void* listPtr, const char* targetTitle, CBook book) {
        if (listPtr) return static_cast<Lab01::IBookList*>(listPtr)->AddBefore(std::string(targetTitle), ConvertToNative(book));
        return false;
    }

    __declspec(dllexport) bool Lab01_AddAfter(void* listPtr, const char* targetTitle, CBook book) {
        if (listPtr) return static_cast<Lab01::IBookList*>(listPtr)->AddAfter(std::string(targetTitle), ConvertToNative(book));
        return false;
    }

    __declspec(dllexport) bool Lab01_DeleteByTitle(void* listPtr, const char* title) {
        if (listPtr) return static_cast<Lab01::IBookList*>(listPtr)->DeleteByTitle(std::string(title));
        return false;
    }

    __declspec(dllexport) void Lab01_Sort(void* listPtr) {
        if (listPtr) static_cast<Lab01::IBookList*>(listPtr)->Sort();
    }

    __declspec(dllexport) int Lab01_GetCount(void* listPtr) {
        return listPtr ? static_cast<Lab01::IBookList*>(listPtr)->GetCount() : 0;
    }

    __declspec(dllexport) bool Lab01_GetAt(void* listPtr, int index, CBook* outBook) {
        if (listPtr) {
            Lab01::IBookList* list = static_cast<Lab01::IBookList*>(listPtr);
            if (index >= 0 && index < list->GetCount()) {
                ConvertToC(list->GetAt(index), *outBook);
                return true;
            }
        }
        return false;
    }

    __declspec(dllexport) bool Lab01_SaveToFile(void* listPtr, const char* filename) {
        if (listPtr) return static_cast<Lab01::IBookList*>(listPtr)->SaveToFile(std::string(filename));
        return false;
    }

    __declspec(dllexport) bool Lab01_LoadFromFile(void* listPtr, const char* filename) {
        if (listPtr) return static_cast<Lab01::IBookList*>(listPtr)->LoadFromFile(std::string(filename));
        return false;
    }

    __declspec(dllexport) int Lab01_FindForRequest(void* listPtr, const char* genre, int targetPages) {
        if (listPtr) return static_cast<Lab01::IBookList*>(listPtr)->FindForRequest(std::string(genre), targetPages);
        return -1;
    }
}