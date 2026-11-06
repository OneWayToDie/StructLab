#pragma once
#include <string>
#include <fstream>

namespace Lab01 {

    struct Book {
        int id;
        std::string author;
        std::string title;
        std::string genre;
        int year;
        int pages;
        int copies;
    };

    // Îáùèé èíòåðôåéñ äëÿ îáîèõ ñïèñêîâ
    class IBookList {
    public:
        virtual ~IBookList() = default;
        virtual void AddLast(const Book& b) = 0;
        virtual void AddFirst(const Book& b) = 0;
        virtual void AddSorted(const Book& b) = 0;
        virtual bool AddBefore(const std::string& targetTitle, const Book& b) = 0;
        virtual bool AddAfter(const std::string& targetTitle, const Book& b) = 0;
        virtual bool DeleteByTitle(const std::string& title) = 0;
        virtual void Sort() = 0;
        virtual void Clear() = 0;
        virtual int GetCount() const = 0;
        virtual Book GetAt(int index) const = 0;
        virtual int FindForRequest(const std::string& genre, int targetPages) const = 0;
        virtual bool SaveToFile(const std::string& filename) const = 0;
        virtual bool LoadFromFile(const std::string& filename) = 0;
    };

    // --- ÎÄÍÎÑÂßÇÍÛÉ ÑÏÈÑÎÊ ---
    struct Node {
        Book data;
        Node* next;
        Node(const Book& b) : data(b), next(nullptr) {}
    };

    class BookList : public IBookList {
    private:
        Node* head;
        int count;
    public:
        BookList();
        ~BookList() override;
        void AddLast(const Book& b) override;
        void AddFirst(const Book& b) override;
        void AddSorted(const Book& b) override;
        bool AddBefore(const std::string& targetTitle, const Book& b) override;
        bool AddAfter(const std::string& targetTitle, const Book& b) override;
        bool DeleteByTitle(const std::string& title) override;
        void Sort() override;
        void Clear() override;
        int GetCount() const override;
        Book GetAt(int index) const override;
        int FindForRequest(const std::string& genre, int targetPages) const override;
        bool SaveToFile(const std::string& filename) const override;
        bool LoadFromFile(const std::string& filename) override;
    };

    // --- ÄÂÓÑÂßÇÍÛÉ ÑÏÈÑÎÊ ---
    struct DNode {
        Book data;
        DNode* prev;
        DNode* next;
        DNode(const Book& b) : data(b), prev(nullptr), next(nullptr) {}
    };

    class DoublyBookList : public IBookList {
    private:
        DNode* head;
        DNode* tail;
        int count;
    public:
        DoublyBookList();
        ~DoublyBookList() override;
        void AddLast(const Book& b) override;
        void AddFirst(const Book& b) override;
        void AddSorted(const Book& b) override;
        bool AddBefore(const std::string& targetTitle, const Book& b) override;
        bool AddAfter(const std::string& targetTitle, const Book& b) override;
        bool DeleteByTitle(const std::string& title) override;
        void Sort() override;
        void Clear() override;
        int GetCount() const override;
        Book GetAt(int index) const override;
        int FindForRequest(const std::string& genre, int targetPages) const override;
        bool SaveToFile(const std::string& filename) const override;
        bool LoadFromFile(const std::string& filename) override;
    };
}