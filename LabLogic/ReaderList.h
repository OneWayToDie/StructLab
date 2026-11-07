#pragma once
#include <string>
#include <fstream>

namespace Lab01 {
    struct Reader {
        int id;
        std::string fullName;
        std::string phone;
        std::string ticketNumber;
        std::string address;
    };

    struct RNode {
        Reader data;
        RNode* next;
        RNode(const Reader& r) : data(r), next(nullptr) {}
    };

    class ReaderList {
    private:
        RNode* head;
        int count;
    public:
        ReaderList();
        ~ReaderList();
        void AddLast(const Reader& r);
        bool DeleteById(int id);
        void Clear();
        int GetCount() const;
        Reader GetAt(int index) const;
        bool SaveToFile(const std::string& filename) const;
        bool LoadFromFile(const std::string& filename);
    };
}
