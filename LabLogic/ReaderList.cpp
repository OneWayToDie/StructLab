#include "ReaderList.h"
#include <sstream>

namespace Lab01 {
    ReaderList::ReaderList() : head(nullptr), count(0) {}
    ReaderList::~ReaderList() { Clear(); }

    void ReaderList::Clear() {
        RNode* curr = head;
        while (curr) { RNode* next = curr->next; delete curr; curr = next; }
        head = nullptr; count = 0;
    }

    void ReaderList::AddLast(const Reader& r) {
        RNode* newNode = new RNode(r);
        if (!head) { head = newNode; }
        else {
            RNode* curr = head;
            while (curr->next) curr = curr->next;
            curr->next = newNode;
        }
        count++;
    }

    bool ReaderList::DeleteById(int id) {
        if (!head) return false;
        if (head->data.id == id) {
            RNode* temp = head; head = head->next; delete temp; count--; return true;
        }
        RNode* curr = head;
        while (curr->next && curr->next->data.id != id) curr = curr->next;
        if (!curr->next) return false;
        RNode* temp = curr->next; curr->next = temp->next; delete temp; count--; return true;
    }

    int ReaderList::GetCount() const { return count; }

    Reader ReaderList::GetAt(int index) const {
        RNode* curr = head; int i = 0;
        while (curr) {
            if (i == index) return curr->data;
            curr = curr->next; i++;
        }
        return Reader();
    }

    bool ReaderList::SaveToFile(const std::string& filename) const {
        std::ofstream out(filename); if (!out.is_open()) return false;
        RNode* curr = head;
        while (curr) {
            out << curr->data.id << "\t" << curr->data.fullName << "\t"
                << curr->data.phone << "\t" << curr->data.ticketNumber << "\t" << curr->data.address << "\n";
            curr = curr->next;
        }
        return true;
    }

    bool ReaderList::LoadFromFile(const std::string& filename) {
        std::ifstream in(filename); if (!in.is_open()) return false;
        Clear();
        std::string line;
        while (std::getline(in, line)) {
            if (line.empty()) continue;
            std::stringstream ss(line);
            std::string idStr; Reader r;
            std::getline(ss, idStr, '\t'); std::getline(ss, r.fullName, '\t');
            std::getline(ss, r.phone, '\t'); std::getline(ss, r.ticketNumber, '\t'); std::getline(ss, r.address, '\t');
            try { r.id = std::stoi(idStr); }
            catch (...) { continue; }
            AddLast(r);
        }
        return true;
    }
}