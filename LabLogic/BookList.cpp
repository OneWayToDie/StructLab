#include "BookList.h"
#include <sstream>
#include <cmath>

namespace Lab01 {
    BookList::BookList() : head(nullptr), count(0) {}
    BookList::~BookList() { Clear(); }

    void BookList::Clear() {
        Node* current = head;
        while (current != nullptr) {
            Node* next = current->next;
            delete current;
            current = next;
        }
        head = nullptr; count = 0;
    }

    void BookList::AddLast(const Book& b) {
        Node* newNode = new Node(b);
        if (head == nullptr) { head = newNode; }
        else {
            Node* curr = head;
            while (curr->next != nullptr) curr = curr->next;
            curr->next = newNode;
        }
        count++;
    }

    void BookList::AddFirst(const Book& b) {
        Node* newNode = new Node(b);
        newNode->next = head;
        head = newNode;
        count++;
    }

    void BookList::AddSorted(const Book& b) {
        Node* newNode = new Node(b);
        if (head == nullptr || head->data.title > b.title) {
            newNode->next = head; head = newNode;
        }
        else {
            Node* curr = head;
            while (curr->next != nullptr && curr->next->data.title <= b.title) curr = curr->next;
            newNode->next = curr->next; curr->next = newNode;
        }
        count++;
    }

    bool BookList::AddBefore(const std::string& targetTitle, const Book& b) {
        if (head == nullptr) return false;
        if (head->data.title == targetTitle) { AddFirst(b); return true; }
        Node* curr = head;
        while (curr->next != nullptr && curr->next->data.title != targetTitle) curr = curr->next;
        if (curr->next == nullptr) return false;
        Node* newNode = new Node(b);
        newNode->next = curr->next; curr->next = newNode;
        count++; return true;
    }

    bool BookList::AddAfter(const std::string& targetTitle, const Book& b) {
        Node* curr = head;
        while (curr != nullptr && curr->data.title != targetTitle) curr = curr->next;
        if (curr == nullptr) return false;
        Node* newNode = new Node(b);
        newNode->next = curr->next; curr->next = newNode;
        count++; return true;
    }

    bool BookList::DeleteByTitle(const std::string& title) {
        if (head == nullptr) return false;
        if (head->data.title == title) {
            Node* temp = head; head = head->next; delete temp; count--; return true;
        }
        Node* curr = head;
        while (curr->next != nullptr && curr->next->data.title != title) curr = curr->next;
        if (curr->next == nullptr) return false;
        Node* temp = curr->next; curr->next = temp->next; delete temp; count--; return true;
    }

    void BookList::Sort() {
        if (head == nullptr || head->next == nullptr) return;
        Node* sorted = nullptr; Node* current = head;
        while (current != nullptr) {
            Node* next = current->next; current->next = nullptr;
            if (sorted == nullptr || sorted->data.title > current->data.title) {
                current->next = sorted; sorted = current;
            }
            else {
                Node* temp = sorted;
                while (temp->next != nullptr && temp->next->data.title <= current->data.title) temp = temp->next;
                current->next = temp->next; temp->next = current;
            }
            current = next;
        }
        head = sorted;
    }

    int BookList::GetCount() const { return count; }

    Book BookList::GetAt(int index) const {
        Node* curr = head; int i = 0;
        while (curr != nullptr) {
            if (i == index) return curr->data;
            curr = curr->next; i++;
        }
        return Book();
    }

    int BookList::FindForRequest(const std::string& genre, int targetPages) const {
        if (targetPages <= 0) return -1;
        Node* curr = head; int i = 0;
        while (curr != nullptr) {
            if (curr->data.genre == genre) {
                double diff = std::abs(curr->data.pages - targetPages);
                if ((diff / (double)targetPages) <= 0.10) return i;
            }
            curr = curr->next; i++;
        }
        return -1;
    }

    bool BookList::SaveToFile(const std::string& filename) const {
        std::ofstream out(filename); if (!out.is_open()) return false;
        Node* curr = head;
        while (curr != nullptr) {
            out << curr->data.id << "\t" << curr->data.author << "\t" << curr->data.title << "\t"
                << curr->data.genre << "\t" << curr->data.year << "\t" << curr->data.pages << "\t" << curr->data.copies << "\n";
            curr = curr->next;
        }
        out.close(); return true;
    }

    bool BookList::LoadFromFile(const std::string& filename) {
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

    bool BookList::AdjustCopies(int bookId, int delta) {
        Node* curr = head;
        while (curr) {
            if (curr->data.id == bookId) {
                int newCopies = curr->data.copies + delta;
                if (newCopies < 0) return false;
                curr->data.copies = newCopies;
                return true;
            }
            curr = curr->next;
        }
        return false;
    }
}