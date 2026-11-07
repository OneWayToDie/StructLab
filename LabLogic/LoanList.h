#pragma once
#include <string>
#include <fstream>

namespace Lab01 {

    struct Loan {
        int id;
        int bookId;
        int readerId;
        std::string issueDate;    // "yyyy-MM-dd"
        std::string dueDate;      // "yyyy-MM-dd"
        std::string returnDate;   // пусто = не возвращено
    };

    struct LNode {
        Loan data;
        LNode* next;
        LNode(const Loan& l) : data(l), next(nullptr) {}
    };

    class LoanList {
    private:
        LNode* head;
        int count;
    public:
        LoanList();
        ~LoanList();
        void AddLast(const Loan& l);
        bool MarkReturned(int loanId, const std::string& returnDate);
        bool DeleteById(int id);
        void Clear();
        int GetCount() const;
        Loan GetAt(int index) const;
        bool SaveToFile(const std::string& filename) const;
        bool LoadFromFile(const std::string& filename);
    };
}
