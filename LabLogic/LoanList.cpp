#include "LoanList.h"
#include <sstream>

namespace Lab01 {
    LoanList::LoanList() : head(nullptr), count(0) {}
    LoanList::~LoanList() { Clear(); }

    void LoanList::Clear() {
        LNode* curr = head;
        while (curr) { LNode* next = curr->next; delete curr; curr = next; }
        head = nullptr; count = 0;
    }

    void LoanList::AddLast(const Loan& l) {
        LNode* newNode = new LNode(l);
        if (!head) { head = newNode; }
        else {
            LNode* curr = head;
            while (curr->next) curr = curr->next;
            curr->next = newNode;
        }
        count++;
    }

    bool LoanList::MarkReturned(int loanId, const std::string& returnDate) {
        LNode* curr = head;
        while (curr) {
            if (curr->data.id == loanId && curr->data.returnDate.empty()) {
                curr->data.returnDate = returnDate;
                return true;
            }
            curr = curr->next;
        }
        return false;
    }

    bool LoanList::DeleteById(int id) {
        if (!head) return false;
        if (head->data.id == id) {
            LNode* temp = head; head = head->next; delete temp; count--; return true;
        }
        LNode* curr = head;
        while (curr->next && curr->next->data.id != id) curr = curr->next;
        if (!curr->next) return false;
        LNode* temp = curr->next; curr->next = temp->next; delete temp; count--; return true;
    }

    int LoanList::GetCount() const { return count; }

    Loan LoanList::GetAt(int index) const {
        LNode* curr = head; int i = 0;
        while (curr) {
            if (i == index) return curr->data;
            curr = curr->next; i++;
        }
        return Loan();
    }

    bool LoanList::SaveToFile(const std::string& filename) const {
        std::ofstream out(filename); if (!out.is_open()) return false;
        LNode* curr = head;
        while (curr) {
            out << curr->data.id << "\t" << curr->data.bookId << "\t" << curr->data.readerId << "\t"
                << curr->data.issueDate << "\t" << curr->data.dueDate << "\t" << curr->data.returnDate << "\n";
            curr = curr->next;
        }
        return true;
    }

    bool LoanList::LoadFromFile(const std::string& filename) {
        std::ifstream in(filename); if (!in.is_open()) return false;
        Clear();
        std::string line;
        while (std::getline(in, line)) {
            if (line.empty()) continue;
            std::stringstream ss(line);
            std::string idStr, bookStr, readerStr; Loan l;
            std::getline(ss, idStr, '\t'); std::getline(ss, bookStr, '\t'); std::getline(ss, readerStr, '\t');
            std::getline(ss, l.issueDate, '\t'); std::getline(ss, l.dueDate, '\t'); std::getline(ss, l.returnDate, '\t');
            try { l.id = std::stoi(idStr); l.bookId = std::stoi(bookStr); l.readerId = std::stoi(readerStr); }
            catch (...) { continue; }
            AddLast(l);
        }
        return true;
    }
}