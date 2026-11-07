#include "BookList.h"
#include "ReaderList.h"
#include <cstring>
#include "LoanList.h" 

// ==================================================
// Ñòðóêòóðû äëÿ ïåðåäà÷è äàííûõ ìåæäó C# è C++
// ==================================================
#pragma pack(push, 1)
struct CLoan {
    int id;
    int bookId;
    int readerId;
    char issueDate[20];
    char dueDate[20];
    char returnDate[20];
};

struct CBook {
    int id;
    char author[100];
    char title[100];
    char genre[50];
    int year;
    int pages;
    int copies;
};

struct CReader {
    int id;
    char fullName[100];
    char phone[20];
    char ticketNumber[20];
    char address[200];
};

#pragma pack(pop)

// ==================================================
// Êîíâåðòåðû: CBook <-> Lab01::Book
// ==================================================
Lab01::Book ConvertToNative(const CBook& cb) {
    Lab01::Book b;
    b.id = cb.id;
    b.author = cb.author;
    b.title = cb.title;
    b.genre = cb.genre;
    b.year = cb.year;
    b.pages = cb.pages;
    b.copies = cb.copies;
    return b;
}

void ConvertToC(const Lab01::Book& b, CBook& cb) {
    cb.id = b.id;
    strncpy_s(cb.author, b.author.c_str(), sizeof(cb.author) - 1);
    strncpy_s(cb.title, b.title.c_str(), sizeof(cb.title) - 1);
    strncpy_s(cb.genre, b.genre.c_str(), sizeof(cb.genre) - 1);
    cb.year = b.year;
    cb.pages = b.pages;
    cb.copies = b.copies;
}

// ==================================================
// Êîíâåðòåðû: CReader <-> Lab01::Reader
// ==================================================
Lab01::Reader ConvertToNativeReader(const CReader& cr) {
    Lab01::Reader r;
    r.id = cr.id;
    r.fullName = cr.fullName;
    r.phone = cr.phone;
    r.ticketNumber = cr.ticketNumber;
    r.address = cr.address;
    return r;
}

void ConvertToCReader(const Lab01::Reader& r, CReader& cr) {
    cr.id = r.id;
    strncpy_s(cr.fullName, r.fullName.c_str(), sizeof(cr.fullName) - 1);
    strncpy_s(cr.phone, r.phone.c_str(), sizeof(cr.phone) - 1);
    strncpy_s(cr.ticketNumber, r.ticketNumber.c_str(), sizeof(cr.ticketNumber) - 1);
    strncpy_s(cr.address, r.address.c_str(), sizeof(cr.address) - 1);
}

Lab01::Loan ConvertToNativeLoan(const CLoan& cl) {
    Lab01::Loan l;
    l.id = cl.id;
    l.bookId = cl.bookId;
    l.readerId = cl.readerId;
    l.issueDate = cl.issueDate;
    l.dueDate = cl.dueDate;
    l.returnDate = cl.returnDate;
    return l;
}

void ConvertToCLoan(const Lab01::Loan& l, CLoan& cl) {
    cl.id = l.id;
    cl.bookId = l.bookId;
    cl.readerId = l.readerId;
    strncpy_s(cl.issueDate, l.issueDate.c_str(), sizeof(cl.issueDate) - 1);
    strncpy_s(cl.dueDate, l.dueDate.c_str(), sizeof(cl.dueDate) - 1);
    strncpy_s(cl.returnDate, l.returnDate.c_str(), sizeof(cl.returnDate) - 1);
}

// ==================================================
// Ýêñïîðòû (ìîñò äëÿ C#)
// ==================================================
extern "C" {

    // ============================================
    // ---------- ÊÍÈÃÈ ----------
    // ============================================
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
        if (listPtr) {
            return static_cast<Lab01::IBookList*>(listPtr)->AddBefore(std::string(targetTitle), ConvertToNative(book));
        }
        return false;
    }

    __declspec(dllexport) bool Lab01_AddAfter(void* listPtr, const char* targetTitle, CBook book) {
        if (listPtr) {
            return static_cast<Lab01::IBookList*>(listPtr)->AddAfter(std::string(targetTitle), ConvertToNative(book));
        }
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
                Lab01::Book b = list->GetAt(index);
                ConvertToC(b, *outBook);
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
        if (listPtr) {
            return static_cast<Lab01::IBookList*>(listPtr)->FindForRequest(std::string(genre), targetPages);
        }
        return -1;
    }

    // ============================================
    // ---------- ×ÈÒÀÒÅËÈ ----------
    // ============================================
    __declspec(dllexport) void* Lab01_Reader_CreateList() {
        return new Lab01::ReaderList();
    }

    __declspec(dllexport) void Lab01_Reader_DestroyList(void* ptr) {
        if (ptr) delete static_cast<Lab01::ReaderList*>(ptr);
    }

    __declspec(dllexport) void Lab01_Reader_Clear(void* ptr) {
        if (ptr) static_cast<Lab01::ReaderList*>(ptr)->Clear();
    }

    __declspec(dllexport) void Lab01_Reader_Add(void* ptr, CReader reader) {
        if (ptr) static_cast<Lab01::ReaderList*>(ptr)->AddLast(ConvertToNativeReader(reader));
    }

    __declspec(dllexport) bool Lab01_Reader_DeleteById(void* ptr, int id) {
        if (ptr) return static_cast<Lab01::ReaderList*>(ptr)->DeleteById(id);
        return false;
    }

    __declspec(dllexport) int Lab01_Reader_GetCount(void* ptr) {
        return ptr ? static_cast<Lab01::ReaderList*>(ptr)->GetCount() : 0;
    }

    __declspec(dllexport) bool Lab01_Reader_GetAt(void* ptr, int index, CReader* outReader) {
        if (ptr) {
            Lab01::ReaderList* list = static_cast<Lab01::ReaderList*>(ptr);
            if (index >= 0 && index < list->GetCount()) {
                Lab01::Reader r = list->GetAt(index);
                ConvertToCReader(r, *outReader);
                return true;
            }
        }
        return false;
    }

    __declspec(dllexport) bool Lab01_Reader_SaveToFile(void* ptr, const char* filename) {
        if (ptr) return static_cast<Lab01::ReaderList*>(ptr)->SaveToFile(std::string(filename));
        return false;
    }

    __declspec(dllexport) bool Lab01_Reader_LoadFromFile(void* ptr, const char* filename) {
        if (ptr) return static_cast<Lab01::ReaderList*>(ptr)->LoadFromFile(std::string(filename));
        return false;
    }

    // ============================================
// ---------- ÓÏÐÀÂËÅÍÈÅ ÝÊÇÅÌÏËßÐÀÌÈ ----------
// ============================================
    __declspec(dllexport) bool Lab01_AdjustCopies(void* listPtr, int bookId, int delta) {
        if (listPtr) return static_cast<Lab01::IBookList*>(listPtr)->AdjustCopies(bookId, delta);
        return false;
    }

    // ============================================
    // ---------- ÂÛÄÀ×È ----------
    // ============================================
    __declspec(dllexport) void* Lab01_Loan_CreateList() {
        return new Lab01::LoanList();
    }

    __declspec(dllexport) void Lab01_Loan_DestroyList(void* ptr) {
        if (ptr) delete static_cast<Lab01::LoanList*>(ptr);
    }

    __declspec(dllexport) void Lab01_Loan_Clear(void* ptr) {
        if (ptr) static_cast<Lab01::LoanList*>(ptr)->Clear();
    }

    __declspec(dllexport) void Lab01_Loan_Add(void* ptr, CLoan loan) {
        if (ptr) static_cast<Lab01::LoanList*>(ptr)->AddLast(ConvertToNativeLoan(loan));
    }

    __declspec(dllexport) bool Lab01_Loan_MarkReturned(void* ptr, int loanId, const char* returnDate) {
        if (ptr) return static_cast<Lab01::LoanList*>(ptr)->MarkReturned(loanId, std::string(returnDate));
        return false;
    }

    __declspec(dllexport) bool Lab01_Loan_DeleteById(void* ptr, int id) {
        if (ptr) return static_cast<Lab01::LoanList*>(ptr)->DeleteById(id);
        return false;
    }

    __declspec(dllexport) int Lab01_Loan_GetCount(void* ptr) {
        return ptr ? static_cast<Lab01::LoanList*>(ptr)->GetCount() : 0;
    }

    __declspec(dllexport) bool Lab01_Loan_GetAt(void* ptr, int index, CLoan* outLoan) {
        if (ptr) {
            Lab01::LoanList* list = static_cast<Lab01::LoanList*>(ptr);
            if (index >= 0 && index < list->GetCount()) {
                ConvertToCLoan(list->GetAt(index), *outLoan);
                return true;
            }
        }
        return false;
    }

    __declspec(dllexport) bool Lab01_Loan_SaveToFile(void* ptr, const char* filename) {
        if (ptr) return static_cast<Lab01::LoanList*>(ptr)->SaveToFile(std::string(filename));
        return false;
    }

    __declspec(dllexport) bool Lab01_Loan_LoadFromFile(void* ptr, const char* filename) {
        if (ptr) return static_cast<Lab01::LoanList*>(ptr)->LoadFromFile(std::string(filename));
        return false;
    }

} // extern "C"