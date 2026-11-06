#include "BookList.h"
#include <sstream>
#include <cmath>

namespace Lab01 {
    DoublyBookList::DoublyBookList() : head(nullptr), tail(nullptr), count(0) {}
    DoublyBookList::~DoublyBookList() { Clear(); }

    void DoublyBookList::Clear() {
        DNode* curr = head;
        while (curr != nullptr) {
            DNode* next = curr->next;
            delete curr;
            curr = next;
        }
        head = tail = nullptr; count = 0;
    }

    void DoublyBookList::AddLast(const Book& b) {
        DNode* newNode = new DNode(b);
        if (!head) { head = tail = newNode; }
        else { newNode->prev = tail; tail->next = newNode; tail = newNode; }
        count++;
    }

    void DoublyBookList::AddFirst(const Book& b) {
        DNode* newNode = new DNode(b);
        if (!head) { head = tail = newNode; }
        else { newNode->next = head; head->prev = newNode; head = newNode; }
        count++;
    }

    void DoublyBookList::AddSorted(const Book& b) {
        DNode* newNode = new DNode(b);
        if (!head) { head = tail = newNode; }
        else if (b.title < head->data.title) {
            newNode->next = head; head->prev = newNode; head = newNode;
        }
        else if (b.title >= tail->data.title) {
            newNode->prev = tail; tail->next = newNode; tail = newNode;
        }
        else {
            DNode* curr = head;
            while (curr && curr->data.title <= b.title) curr = curr->next;
            newNode->prev = curr->prev;
            newNode->next = curr;
            curr->prev->next = newNode;
            curr->prev = newNode;
        }
        count++;
    }

    bool DoublyBookList::AddBefore(const std::string& targetTitle, const Book& b) {
        DNode* curr = head;
        while (curr && curr->data.title != targetTitle) curr = curr->next;
        if (!curr) return false;
        DNode* newNode = new DNode(b);
        newNode->next = curr; newNode->prev = curr->prev;
        if (curr->prev) curr->prev->next = newNode; else head = newNode;
        curr->prev = newNode;
        count++; return true;
    }

    bool DoublyBookList::AddAfter(const std::string& targetTitle, const Book& b) {
        DNode* curr = head;
        while (curr && curr->data.title != targetTitle) curr = curr->next;
        if (!curr) return false;
        DNode* newNode = new DNode(b);
        newNode->prev = curr; newNode->next = curr->next;
        if (curr->next) curr->next->prev = newNode; else tail = newNode;
        curr->next = newNode;
        count++; return true;
    }

    bool DoublyBookList::DeleteByTitle(const std::string& title) {
        DNode* curr = head;
        while (curr && curr->data.title != title) curr = curr->next;
        if (!curr) return false;
        if (curr->prev) curr->prev->next = curr->next; else head = curr->next;
        if (curr->next) curr->next->prev = curr->prev; else tail = curr->prev;
        delete curr; count--; return true;
    }

    void DoublyBookList::Sort() {
        // Пузырьковая сортировка перестановкой данных (простая и надежная для двусвязного)
        if (count < 2) return;
        bool swapped = true;
        while (swapped) {
            swapped = false;
            DNode* curr = head;
            while (curr->next) {
                if (curr->data.title > curr->next->data.title) {
                    Book temp = curr->data;
                    curr->data = curr->next->data;
                    curr->next->data = temp;
                    swapped = true;
                }
                curr = curr->next;
            }
        }
    }

    int DoublyBookList::GetCount() const { return count; }

    Book DoublyBookList::GetAt(int index) const {
        DNode* curr = head; int i = 0;
        while (curr) {
            if (i == index) return curr->data;
            curr = curr->next; i++;
        }
        return Book();
    }

    int DoublyBookList::FindForRequest(const std::string& genre, int targetPages) const {
        if (targetPages <= 0) return -1;
        DNode* curr = head; int i = 0;
        while (curr) {
            if (curr->data.genre == genre) {
                double diff = std::abs(curr->data.pages - targetPages);
                if ((diff / (double)targetPages) <= 0.10) return i;
            }
            curr = curr->next; i++;
        }
        return -1;
    }

    bool DoublyBookList::SaveToFile(const std::string& filename) const {
        std::ofstream out(filename); if (!out.is_open()) return false;
        DNode* curr = head;
        while (curr) {
            out << curr->data.id << "\t" << curr->data.author << "\t" << curr->data.title << "\t"
                << curr->data.genre << "\t" << curr->data.year << "\t" << curr->data.pages << "\t" << curr->data.copies << "\n";
            curr = curr->next;
        }
        out.close(); return true;
    }

    bool DoublyBookList::LoadFromFile(const std::string& filename) {
        std::ifstream in(filename); if (!in.is_open()) return false;
        Clear();
        std::string line;
        while (std::getline(in, line)) {
            if (line.empty()) continue;
            std::stringstream ss(line);
            std::string idStr, yearStr, pagesStr, copiesStr; Book b;
            std::getline(ss, idStr, '\t'); std::getline(ss, b.author, '\t'); std::getline(ss, b.title, '\t');
            std::getline(ss, b.genre, '\t'); std::getline(ss, yearStr, '\t'); std::getline(ss, pagesStr, '\t');
            std::getline(ss, copiesStr, '\t');
            try { b.id = std::stoi(idStr); b.year = std::stoi(yearStr); b.pages = std::stoi(pagesStr); b.copies = std::stoi(copiesStr); }
            catch (...) { continue; }
            AddLast(b);
        }
        in.close(); return true;
    }
}